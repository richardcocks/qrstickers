using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using QRStickers.Services;
using System.Text.Json;

namespace QRStickers.Meraki;

/// <summary>
/// Orchestrates syncing Meraki data (organizations, networks, devices) from API to local cache
/// </summary>
public class MerakiSyncOrchestrator
{
    private readonly MerakiServiceFactory _merakiFactory;
    private readonly QRStickersDbContext _db;
    private readonly ILogger<MerakiSyncOrchestrator> _logger;
    private readonly IHubContext<SyncStatusHub> _hubContext;
    private readonly QRCodeGenerationService _qrCodeService;

    public MerakiSyncOrchestrator(
        MerakiServiceFactory merakiFactory,
        QRStickersDbContext db,
        ILogger<MerakiSyncOrchestrator> logger,
        IHubContext<SyncStatusHub> hubContext,
        QRCodeGenerationService qrCodeService)
    {
        _merakiFactory = merakiFactory ?? throw new ArgumentNullException(nameof(merakiFactory));
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _hubContext = hubContext ?? throw new ArgumentNullException(nameof(hubContext));
        _qrCodeService = qrCodeService ?? throw new ArgumentNullException(nameof(qrCodeService));
    }

    /// <summary>
    /// Broadcasts sync status update to connected SignalR clients
    /// </summary>
    private async Task BroadcastStatusUpdateAsync(int connectionId, SyncStatus status)
    {
        var groupName = SyncStatusHub.GetGroupName(connectionId);
        await _hubContext.Clients.Group(groupName).SendAsync("SyncStatusUpdate", new
        {
            status = status.Status.ToString(),
            currentStep = status.CurrentStep,
            currentStepNumber = status.CurrentStepNumber,
            totalSteps = status.TotalSteps,
            errorMessage = status.ErrorMessage
        });
    }

    /// <summary>
    /// Syncs all Meraki data for a connection (organizations, networks, devices)
    /// </summary>
    public async Task SyncConnectionDataAsync(int connectionId)
    {
        _logger.LogInformation("Starting Meraki data sync for connection {ConnectionId}", connectionId);

        try
        {
            // Initialize or update sync status
            var syncStatus = await _db.SyncStatuses.FirstOrDefaultAsync(s => s.ConnectionId == connectionId);
            if (syncStatus == null)
            {
                syncStatus = new SyncStatus { ConnectionId = connectionId };
                _db.SyncStatuses.Add(syncStatus);
            }

            syncStatus.LastSyncStartedAt = DateTime.UtcNow;
            syncStatus.Status = SyncState.InProgress;
            syncStatus.CurrentStepNumber = 0;
            syncStatus.TotalSteps = 3;
            syncStatus.ErrorMessage = null;
            await _db.SaveChangesAsync();
            await BroadcastStatusUpdateAsync(connectionId, syncStatus);

            // Get Meraki service for this connection
            var merakiService = _merakiFactory.CreateForConnection(connectionId);

            // Step 1: Sync organizations
            syncStatus.CurrentStep = "Syncing organizations";
            syncStatus.CurrentStepNumber = 1;
            await _db.SaveChangesAsync();
            await BroadcastStatusUpdateAsync(connectionId, syncStatus);
            await Task.Delay(500); // Visual delay for smoother UX
            await SyncOrganizationsAsync(connectionId, merakiService);

            // Step 2: Sync networks for all organizations
            syncStatus.CurrentStep = "Syncing networks";
            syncStatus.CurrentStepNumber = 2;
            await _db.SaveChangesAsync();
            await BroadcastStatusUpdateAsync(connectionId, syncStatus);
            await Task.Delay(500); // Visual delay for smoother UX
            await SyncNetworksAsync(connectionId, merakiService);

            // Step 3: Sync devices for all organizations
            syncStatus.CurrentStep = "Syncing devices";
            syncStatus.CurrentStepNumber = 3;
            await _db.SaveChangesAsync();
            await BroadcastStatusUpdateAsync(connectionId, syncStatus);
            await Task.Delay(500); // Visual delay for smoother UX
            await SyncDevicesAsync(connectionId, merakiService);

            // Mark sync as completed
            syncStatus.Status = SyncState.Completed;
            syncStatus.LastSyncCompletedAt = DateTime.UtcNow;
            syncStatus.CurrentStep = "Completed";
            await _db.SaveChangesAsync();
            await BroadcastStatusUpdateAsync(connectionId, syncStatus);

            _logger.LogInformation("Successfully completed Meraki data sync for connection {ConnectionId}", connectionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error syncing Meraki data for connection {ConnectionId}", connectionId);

            // Update sync status to failed
            var syncStatus = await _db.SyncStatuses.FirstOrDefaultAsync(s => s.ConnectionId == connectionId);
            if (syncStatus != null)
            {
                syncStatus.Status = SyncState.Failed;
                syncStatus.LastSyncCompletedAt = DateTime.UtcNow;
                syncStatus.ErrorMessage = ex.Message;
                await _db.SaveChangesAsync();
                await BroadcastStatusUpdateAsync(connectionId, syncStatus);
            }

            throw; // Re-throw to let caller handle
        }
    }

    private async Task SyncOrganizationsAsync(int connectionId, IMerakiService merakiService)
    {
        _logger.LogInformation("Syncing organizations for connection {ConnectionId}", connectionId);

        // Fetch organizations from API
        var apiOrgs = await merakiService.GetOrganizationsAsync();
        if (apiOrgs == null)
        {
            _logger.LogWarning("No organizations returned from API for connection {ConnectionId}", connectionId);
            return;
        }

        // Get existing cached organizations for this connection
        var cachedOrgs = await _db.CachedOrganizations
            .Where(o => o.ConnectionId == connectionId)
            .ToListAsync();

        var now = DateTime.UtcNow;
        var apiOrgIds = apiOrgs.Select(o => o.Id).ToHashSet();

        // Smart merge: Insert new, update existing, mark missing as deleted
        foreach (var apiOrg in apiOrgs)
        {
            var existing = cachedOrgs.FirstOrDefault(c => c.ConnectionId == connectionId && c.OrganizationId == apiOrg.Id);
            if (existing != null)
            {
                // Update existing
                existing.Name = apiOrg.Name;

                // Regenerate QR code only if URL changed
                if (_qrCodeService.ShouldRegenerateQRCode(existing.Url, apiOrg.Url))
                {
                    existing.Url = apiOrg.Url;
                    existing.QRCodeDataUri = !string.IsNullOrWhiteSpace(apiOrg.Url)
                        ? _qrCodeService.GenerateQRCodeDataUri(apiOrg.Url)
                        : null;
                }
                // Also generate QR code if it's missing (migration scenario)
                else if (string.IsNullOrWhiteSpace(existing.QRCodeDataUri) && !string.IsNullOrWhiteSpace(existing.Url))
                {
                    existing.QRCodeDataUri = _qrCodeService.GenerateQRCodeDataUri(existing.Url);
                }

                existing.IsDeleted = false;
                existing.LastSyncedAt = now;
                _db.CachedOrganizations.Update(existing);
            }
            else
            {
                // Insert new - generate QR code if URL exists
                var qrCodeDataUri = !string.IsNullOrWhiteSpace(apiOrg.Url)
                    ? _qrCodeService.GenerateQRCodeDataUri(apiOrg.Url)
                    : null;

                _db.CachedOrganizations.Add(new CachedOrganization
                {
                    ConnectionId = connectionId,
                    OrganizationId = apiOrg.Id,
                    Name = apiOrg.Name,
                    Url = apiOrg.Url,
                    QRCodeDataUri = qrCodeDataUri,
                    IsDeleted = false,
                    LastSyncedAt = now,
                    CreatedAt = now
                });
            }
        }

        // Mark organizations not in API response as deleted
        foreach (var cached in cachedOrgs.Where(c => !apiOrgIds.Contains(c.OrganizationId) && !c.IsDeleted))
        {
            cached.IsDeleted = true;
            cached.LastSyncedAt = now;
            _db.CachedOrganizations.Update(cached);
        }

        await _db.SaveChangesAsync();
        _logger.LogInformation("Synced {Count} organizations for connection {ConnectionId}", apiOrgs.Count, connectionId);
    }

    private async Task SyncNetworksAsync(int connectionId, IMerakiService merakiService)
    {
        _logger.LogInformation("Syncing networks for connection {ConnectionId}", connectionId);

        // Get all active organizations for this connection
        var orgs = await _db.CachedOrganizations
            .Where(o => o.ConnectionId == connectionId && !o.IsDeleted)
            .ToListAsync();

        var allApiNetworks = new List<Network>();

        // Fetch networks for each organization
        foreach (var org in orgs)
        {
            try
            {
                var networks = await merakiService.GetNetworksAsync(org.OrganizationId);
                if (networks != null)
                {
                    allApiNetworks.AddRange(networks);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching networks for organization {OrgId}", org.OrganizationId);
                // Continue with other organizations
            }
        }

        // Get existing cached networks for this connection
        var cachedNetworks = await _db.CachedNetworks
            .Where(n => n.ConnectionId == connectionId)
            .ToListAsync();

        var now = DateTime.UtcNow;
        var apiNetworkIds = allApiNetworks.Select(n => n.Id).ToHashSet();

        // Smart merge: Insert new, update existing, mark missing as deleted
        foreach (var apiNetwork in allApiNetworks)
        {
            var existing = cachedNetworks.FirstOrDefault(c => c.ConnectionId == connectionId && c.NetworkId == apiNetwork.Id);
            if (existing != null)
            {
                // Update existing
                existing.Name = apiNetwork.Name;
                existing.OrganizationId = apiNetwork.OrganizationId;
                existing.ProductTypesJson = apiNetwork.ProductTypes != null ? JsonSerializer.Serialize(apiNetwork.ProductTypes) : null;
                existing.TagsJson = apiNetwork.Tags != null ? JsonSerializer.Serialize(apiNetwork.Tags) : null;
                existing.TimeZone = apiNetwork.TimeZone;

                // Regenerate QR code only if URL changed
                if (_qrCodeService.ShouldRegenerateQRCode(existing.Url, apiNetwork.Url))
                {
                    existing.Url = apiNetwork.Url;
                    existing.QRCodeDataUri = !string.IsNullOrWhiteSpace(apiNetwork.Url)
                        ? _qrCodeService.GenerateQRCodeDataUri(apiNetwork.Url)
                        : null;
                }
                // Also generate QR code if it's missing (migration scenario)
                else if (string.IsNullOrWhiteSpace(existing.QRCodeDataUri) && !string.IsNullOrWhiteSpace(existing.Url))
                {
                    existing.QRCodeDataUri = _qrCodeService.GenerateQRCodeDataUri(existing.Url);
                }

                existing.IsDeleted = false;
                existing.LastSyncedAt = now;
                _db.CachedNetworks.Update(existing);
            }
            else
            {
                // Insert new - generate QR code if URL exists
                var qrCodeDataUri = !string.IsNullOrWhiteSpace(apiNetwork.Url)
                    ? _qrCodeService.GenerateQRCodeDataUri(apiNetwork.Url)
                    : null;

                _db.CachedNetworks.Add(new CachedNetwork
                {
                    ConnectionId = connectionId,
                    OrganizationId = apiNetwork.OrganizationId,
                    NetworkId = apiNetwork.Id,
                    Name = apiNetwork.Name,
                    Url = apiNetwork.Url,
                    QRCodeDataUri = qrCodeDataUri,
                    ProductTypesJson = apiNetwork.ProductTypes != null ? JsonSerializer.Serialize(apiNetwork.ProductTypes) : null,
                    TagsJson = apiNetwork.Tags != null ? JsonSerializer.Serialize(apiNetwork.Tags) : null,
                    TimeZone = apiNetwork.TimeZone,
                    IsDeleted = false,
                    LastSyncedAt = now,
                    CreatedAt = now
                });
            }
        }

        // Mark networks not in API response as deleted
        foreach (var cached in cachedNetworks.Where(c => !apiNetworkIds.Contains(c.NetworkId) && !c.IsDeleted))
        {
            cached.IsDeleted = true;
            cached.LastSyncedAt = now;
            _db.CachedNetworks.Update(cached);
        }

        await _db.SaveChangesAsync();
        _logger.LogInformation("Synced {Count} networks for connection {ConnectionId}", allApiNetworks.Count, connectionId);
    }

    private async Task SyncDevicesAsync(int connectionId, IMerakiService merakiService)
    {
        _logger.LogInformation("Syncing devices for connection {ConnectionId}", connectionId);

        // Get all active organizations for this connection
        var orgs = await _db.CachedOrganizations
            .Where(o => o.ConnectionId == connectionId && !o.IsDeleted)
            .ToListAsync();

        var allApiDevices = new List<Device>();

        // Fetch devices for each organization
        foreach (var org in orgs)
        {
            try
            {
                var devices = await merakiService.GetOrganizationDevicesAsync(org.OrganizationId);
                if (devices != null)
                {
                    allApiDevices.AddRange(devices);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching devices for organization {OrgId}", org.OrganizationId);
                // Continue with other organizations
            }
        }

        // Get existing cached devices for this connection
        var cachedDevices = await _db.CachedDevices
            .Where(d => d.ConnectionId == connectionId)
            .ToListAsync();

        var now = DateTime.UtcNow;
        var apiDeviceSerials = allApiDevices.Where(d => d.Serial != null).Select(d => d.Serial!).ToHashSet();

        // Smart merge: Insert new, update existing, mark missing as deleted
        foreach (var apiDevice in allApiDevices.Where(d => d.Serial != null))
        {
            var existing = cachedDevices.FirstOrDefault(c => c.ConnectionId == connectionId && c.Serial == apiDevice.Serial);
            if (existing != null)
            {
                // Update existing
                existing.Name = apiDevice.Name;
                existing.Model = apiDevice.Model;
                existing.ProductType = apiDevice.ProductType;
                existing.NetworkId = apiDevice.NetworkId;

                // Regenerate QR code only if Serial changed (extremely rare, but handle it)
                if (_qrCodeService.ShouldRegenerateQRCode(existing.Serial, apiDevice.Serial))
                {
                    existing.Serial = apiDevice.Serial!;
                    existing.QRCodeDataUri = _qrCodeService.GenerateQRCodeDataUri(apiDevice.Serial!);
                }
                // Also generate QR code if it's missing (migration scenario)
                else if (string.IsNullOrWhiteSpace(existing.QRCodeDataUri))
                {
                    existing.QRCodeDataUri = _qrCodeService.GenerateQRCodeDataUri(existing.Serial);
                }

                existing.IsDeleted = false;
                existing.LastSyncedAt = now;
                _db.CachedDevices.Update(existing);
            }
            else
            {
                // Insert new - always generate QR code for device serial
                var qrCodeDataUri = _qrCodeService.GenerateQRCodeDataUri(apiDevice.Serial!);

                _db.CachedDevices.Add(new CachedDevice
                {
                    ConnectionId = connectionId,
                    Serial = apiDevice.Serial!,
                    QRCodeDataUri = qrCodeDataUri,
                    Name = apiDevice.Name,
                    Model = apiDevice.Model,
                    ProductType = apiDevice.ProductType,
                    NetworkId = apiDevice.NetworkId,
                    IsDeleted = false,
                    LastSyncedAt = now,
                    CreatedAt = now
                });
            }
        }

        // Mark devices not in API response as deleted
        foreach (var cached in cachedDevices.Where(c => !apiDeviceSerials.Contains(c.Serial) && !c.IsDeleted))
        {
            cached.IsDeleted = true;
            cached.LastSyncedAt = now;
            _db.CachedDevices.Update(cached);
        }

        await _db.SaveChangesAsync();
        _logger.LogInformation("Synced {Count} devices for connection {ConnectionId}", allApiDevices.Count, connectionId);
    }
}
