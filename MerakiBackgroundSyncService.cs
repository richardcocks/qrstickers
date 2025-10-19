using Microsoft.EntityFrameworkCore;

namespace QRStickers;

/// <summary>
/// Background service that periodically syncs Meraki data for all users
/// </summary>
public class MerakiBackgroundSyncService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<MerakiBackgroundSyncService> _logger;
    private readonly IConfiguration _configuration;

    public MerakiBackgroundSyncService(
        IServiceProvider serviceProvider,
        ILogger<MerakiBackgroundSyncService> logger,
        IConfiguration configuration)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Meraki background sync service started");

        // Check if background sync is enabled
        var enableBackgroundSync = _configuration.GetValue<bool>("MerakiSync:EnableBackgroundSync", true);
        if (!enableBackgroundSync)
        {
            _logger.LogInformation("Background sync is disabled in configuration");
            return;
        }

        // Get sync interval from configuration (default: 6 hours)
        var syncIntervalHours = _configuration.GetValue<int>("MerakiSync:PeriodicSyncIntervalHours", 6);
        var syncInterval = TimeSpan.FromHours(syncIntervalHours);

        _logger.LogInformation("Background sync will run every {Hours} hours", syncIntervalHours);

        // Wait 5 minutes before first sync to allow application to stabilize
        await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await SyncAllUsersAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in background sync cycle");
            }

            // Wait for next sync interval
            await Task.Delay(syncInterval, stoppingToken);
        }

        _logger.LogInformation("Meraki background sync service stopped");
    }

    private async Task SyncAllUsersAsync()
    {
        _logger.LogInformation("Starting background sync for all connections");

        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<QRStickersDbContext>();

        // Get all connections with valid OAuth tokens
        var connectionIds = await db.MerakiOAuthTokens
            .Where(t => t.RefreshTokenExpiresAt > DateTime.UtcNow) // Only sync connections with valid refresh tokens
            .Select(t => t.ConnectionId)
            .Distinct()
            .ToListAsync();

        _logger.LogInformation("Found {Count} connections with valid Meraki tokens", connectionIds.Count);

        foreach (var connectionId in connectionIds)
        {
            try
            {
                _logger.LogInformation("Syncing data for connection {ConnectionId}", connectionId);

                // Create a new scope for each connection to avoid DbContext lifetime issues
                using var connectionScope = _serviceProvider.CreateScope();
                var syncOrchestrator = connectionScope.ServiceProvider.GetRequiredService<MerakiSyncOrchestrator>();

                await syncOrchestrator.SyncConnectionDataAsync(connectionId);

                _logger.LogInformation("Successfully synced data for connection {ConnectionId}", connectionId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error syncing data for connection {ConnectionId}", connectionId);
                // Continue with next connection
            }
        }

        _logger.LogInformation("Completed background sync for all connections");
    }
}
