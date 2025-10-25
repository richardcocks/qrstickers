using Microsoft.EntityFrameworkCore;
using QRStickers.Meraki;

namespace QRStickers.Services;

/// <summary>
/// Retrieves and prepares device data for export operations
/// Combines device, network, connection, and global variable data into a single context
/// </summary>
public class DeviceExportHelper
{
    private readonly QRStickersDbContext _db;
    private readonly ILogger<DeviceExportHelper> _logger;

    public DeviceExportHelper(QRStickersDbContext db, ILogger<DeviceExportHelper> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Retrieves all data needed to export a single device
    /// Throws UnauthorizedAccessException if user doesn't own the device
    /// </summary>
    public async Task<DeviceExportContext> GetDeviceExportDataAsync(
        int deviceId,
        int connectionId,
        ApplicationUser user)
    {
        _logger.LogInformation("[Export] Retrieving export data for device {DeviceId}, connection {ConnectionId}", deviceId, connectionId);

        // Retrieve device with eager loading
        var device = await _db.CachedDevices
            .AsNoTracking()
            .Include(d => d.Network)
            .Include(d => d.Connection)
            .Where(d => d.Id == deviceId && d.ConnectionId == connectionId)
            .FirstOrDefaultAsync();

        if (device == null)
        {
            _logger.LogWarning("[Export] Device {DeviceId} not found or doesn't belong to connection {ConnectionId}", deviceId, connectionId);
            throw new ArgumentException($"Device {deviceId} not found");
        }

        // Verify user ownership (device's connection must belong to user)
        if (device.Connection?.UserId != user.Id)
        {
            _logger.LogWarning("[Export] Unauthorized access attempt: User {UserId} tried to export device {DeviceId} from connection {ConnectionId}", user.Id, deviceId, connectionId);
            throw new UnauthorizedAccessException($"You don't have permission to export this device");
        }

        // Retrieve connection
        var connection = await _db.Connections
            .AsNoTracking()
            .Where(c => c.Id == connectionId && c.UserId == user.Id)
            .FirstOrDefaultAsync();

        if (connection == null)
        {
            _logger.LogWarning("[Export] Connection {ConnectionId} not found for user {UserId}", connectionId, user.Id);
            throw new ArgumentException($"Connection {connectionId} not found");
        }

        // Retrieve network
        var network = device.Network ?? await _db.CachedNetworks
            .AsNoTracking()
            .Where(n => n.NetworkId == device.NetworkId && n.ConnectionId == connectionId)
            .FirstOrDefaultAsync();

        // Retrieve organization (if network exists)
        CachedOrganization? organization = null;
        if (network != null)
        {
            organization = await _db.CachedOrganizations
                .AsNoTracking()
                .Where(o => o.OrganizationId == network.OrganizationId && o.ConnectionId == connectionId)
                .FirstOrDefaultAsync();
        }

        // Retrieve global variables for this connection
        var globalVariables = await GetGlobalVariablesAsync(connectionId);

        // Retrieve uploaded images for this connection
        var uploadedImages = await _db.UploadedImages
            .AsNoTracking()
            .Where(i => i.ConnectionId == connectionId && !i.IsDeleted)
            .ToListAsync();

        _logger.LogDebug("[Export] Retrieved {ImageCount} uploaded images for connection {ConnectionId}", uploadedImages.Count, connectionId);

        // Build the export context
        var context = new DeviceExportContext
        {
            Device = device,
            Network = network,
            Organization = organization,
            Connection = connection,
            GlobalVariables = globalVariables,
            UploadedImages = uploadedImages
        };

        _logger.LogInformation("[Export] Successfully retrieved export data for device {DeviceName} ({DeviceSerial})", LogSanitizer.Sanitize(device.Name), LogSanitizer.Sanitize(device.Serial));
        return context;
    }

    /// <summary>
    /// Retrieves all global variables configured for a connection
    /// Returns dictionary of variable name -> value
    /// </summary>
    public async Task<Dictionary<string, string>> GetGlobalVariablesAsync(int connectionId)
    {
        _logger.LogDebug("[Export] Retrieving global variables for connection {ConnectionId}", connectionId);

        var variables = await _db.GlobalVariables
            .AsNoTracking()
            .Where(v => v.ConnectionId == connectionId)
            .ToDictionaryAsync(v => v.VariableName, v => v.VariableValue);

        _logger.LogDebug("[Export] Retrieved {VariableCount} global variables for connection {ConnectionId}", variables.Count, connectionId);
        return variables;
    }

    /// <summary>
    /// Tracks usage of custom images (updates LastUsedAt timestamp)
    /// Call this after determining which images are referenced in the template
    /// </summary>
    public async Task TrackImageUsageAsync(int[] imageIds)
    {
        if (imageIds == null || imageIds.Length == 0)
            return;

        _logger.LogDebug("[Export] Tracking usage for {ImageCount} images", imageIds.Length);

        var images = await _db.UploadedImages
            .Where(i => imageIds.Contains(i.Id))
            .ToListAsync();

        foreach (var image in images)
        {
            image.LastUsedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        _logger.LogDebug("[Export] Updated LastUsedAt for {ImageCount} images", images.Count);
    }

    /// <summary>
    /// Retrieves data for multiple devices in a single query (batch export)
    /// Returns list of DeviceExportContext objects
    /// </summary>
    public async Task<List<DeviceExportContext>> GetBulkDeviceExportDataAsync(
        int[] deviceIds,
        int connectionId,
        ApplicationUser user)
    {
        _logger.LogInformation("[Export] Retrieving bulk export data for {DeviceCount} devices", deviceIds.Length);

        // Verify connection belongs to user
        var connection = await _db.Connections
            .AsNoTracking()
            .Where(c => c.Id == connectionId && c.UserId == user.Id)
            .FirstOrDefaultAsync();

        if (connection == null)
        {
            throw new UnauthorizedAccessException("Connection not found");
        }

        // Retrieve all devices
        var devices = await _db.CachedDevices
            .AsNoTracking()
            .Include(d => d.Network)
            .Include(d => d.Connection)
            .Where(d => deviceIds.Contains(d.Id) && d.ConnectionId == connectionId)
            .ToListAsync();

        if (devices.Count == 0)
        {
            _logger.LogWarning("[Export] No devices found for bulk export");
            throw new ArgumentException("No devices found");
        }

        // Verify all devices belong to user's connection
        if (devices.Any(d => d.Connection?.UserId != user.Id))
        {
            _logger.LogWarning("[Export] Unauthorized access attempt in bulk export");
            throw new UnauthorizedAccessException("Some devices don't belong to you");
        }

        // Retrieve global variables once
        var globalVariables = await GetGlobalVariablesAsync(connectionId);

        // Retrieve uploaded images once
        var uploadedImages = await _db.UploadedImages
            .AsNoTracking()
            .Where(i => i.ConnectionId == connectionId && !i.IsDeleted)
            .ToListAsync();

        // Retrieve all organizations for this connection
        var organizationIds = devices
            .Where(d => d.Network != null)
            .Select(d => d.Network!.OrganizationId)
            .Distinct()
            .ToList();

        var organizations = await _db.CachedOrganizations
            .AsNoTracking()
            .Where(o => organizationIds.Contains(o.OrganizationId) && o.ConnectionId == connectionId)
            .ToListAsync();

        // Build export contexts for all devices
        var contexts = devices.Select(device => {
            var organization = device.Network != null
                ? organizations.FirstOrDefault(o => o.OrganizationId == device.Network.OrganizationId)
                : null;

            return new DeviceExportContext
            {
                Device = device,
                Network = device.Network,
                Organization = organization,
                Connection = connection,
                GlobalVariables = globalVariables,
                UploadedImages = uploadedImages
            };
        }).ToList();

        _logger.LogInformation("[Export] Retrieved bulk export data for {DeviceCount} devices", contexts.Count);
        return contexts;
    }
}

/// <summary>
/// Container for all data needed to render and export a device sticker
/// </summary>
public class DeviceExportContext
{
    /// <summary>
    /// The device being exported
    /// </summary>
    public required CachedDevice Device { get; set; }

    /// <summary>
    /// Network this device belongs to (for device.network.* bindings)
    /// </summary>
    public CachedNetwork? Network { get; set; }

    /// <summary>
    /// Organization this device belongs to (for organization.* bindings)
    /// </summary>
    public CachedOrganization? Organization { get; set; }

    /// <summary>
    /// Connection this device belongs to (for connection.* bindings)
    /// </summary>
    public required Connection Connection { get; set; }

    /// <summary>
    /// Global variables for the connection (for global.* bindings)
    /// </summary>
    public required Dictionary<string, string> GlobalVariables { get; set; }

    /// <summary>
    /// Uploaded images for the connection (for customImage.* bindings)
    /// </summary>
    public List<UploadedImage> UploadedImages { get; set; } = new();

    /// <summary>
    /// The matched/selected template (set by template matching service)
    /// </summary>
    public StickerTemplate? MatchedTemplate { get; set; }

    /// <summary>
    /// Creates a JavaScript-friendly object for client-side template binding
    /// </summary>
    public object ToDeviceDataMap()
    {
        return new
        {
            device = new
            {
                id = Device.Id,
                serial = Device.Serial ?? string.Empty,
                name = Device.Name ?? "Unnamed Device",
                mac = string.Empty, // Not available in CachedDevice
                model = Device.Model ?? string.Empty,
                ipAddress = string.Empty, // Not available in CachedDevice
                ipaddress = string.Empty, // Lowercase alias
                type = DeriveDeviceType(Device.Model),
                productType = Device.ProductType ?? string.Empty,
                status = "synced", // Not available in CachedDevice
                firmware = string.Empty, // Not available in CachedDevice
                tags = new List<string>(), // Not available in CachedDevice
                tags_str = string.Empty,
                networkId = Device.NetworkId ?? string.Empty,
                connectionId = Device.ConnectionId
            },
            network = Network != null ? new
            {
                id = Network.Id,
                name = Network.Name ?? string.Empty,
                organizationId = Network.OrganizationId
            } : null,
            connection = new
            {
                id = Connection.Id,
                displayName = Connection.DisplayName ?? string.Empty,
                type = Connection.GetType().Name,
                companyLogoUrl = (Connection as MerakiConnection)?.CompanyLogoUrl ?? string.Empty
            },
            global = GlobalVariables
        };
    }

    /// <summary>
    /// Derives device type from model string (heuristic)
    /// Examples: MS225-48FP → switch, MR32 → ap, MX64W → gateway
    /// </summary>
    private static string DeriveDeviceType(string? model)
    {
        if (string.IsNullOrEmpty(model))
            return "unknown";

        model = model.ToUpperInvariant();

        // Meraki device type patterns
        if (model.StartsWith("MS") || model.StartsWith("C9"))
            return "switch";
        if (model.StartsWith("MR"))
            return "ap"; // Access Point
        if (model.StartsWith("MX"))
            return "gateway";
        if (model.StartsWith("Z") || model.Contains("CAPTIVE"))
            return "appliance";
        if (model.StartsWith("MV"))
            return "camera";
        if (model.StartsWith("MT"))
            return "sensor";
        if (model.StartsWith("MC"))
            return "cellular";

        return "unknown";
    }
}
