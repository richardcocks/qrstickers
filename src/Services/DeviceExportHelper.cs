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

    /// <summary>
    /// Builds an array of template options from filter result for API response
    /// Used by export endpoints to provide alternate template options
    /// </summary>
    public static object[] BuildTemplateOptionsArray(TemplateFilterResult filterResult, int matchedTemplateId)
    {
        var options = new List<object>();

        // Add recommended template (if different from matched)
        if (filterResult.RecommendedTemplate != null &&
            filterResult.RecommendedTemplate.Id != matchedTemplateId)
        {
            options.Add(new
            {
                template = new
                {
                    id = filterResult.RecommendedTemplate.Id,
                    name = filterResult.RecommendedTemplate.Name,
                    templateJson = filterResult.RecommendedTemplate.TemplateJson,
                    pageWidth = filterResult.RecommendedTemplate.PageWidth,
                    pageHeight = filterResult.RecommendedTemplate.PageHeight
                },
                category = "recommended",
                isRecommended = true,
                isCompatible = true,
                compatibilityNote = "Default template for this device type"
            });
        }

        // Add compatible templates
        foreach (var template in filterResult.CompatibleTemplates)
        {
            if (template.Id == matchedTemplateId) continue; // Skip matched

            options.Add(new
            {
                template = new
                {
                    id = template.Id,
                    name = template.Name,
                    templateJson = template.TemplateJson,
                    pageWidth = template.PageWidth,
                    pageHeight = template.PageHeight
                },
                category = "compatible",
                isRecommended = false,
                isCompatible = true,
                compatibilityNote = "Compatible with this device type"
            });
        }

        // Add incompatible templates (with warning)
        foreach (var template in filterResult.IncompatibleTemplates)
        {
            options.Add(new
            {
                template = new
                {
                    id = template.Id,
                    name = template.Name,
                    templateJson = template.TemplateJson,
                    pageWidth = template.PageWidth,
                    pageHeight = template.PageHeight
                },
                category = "incompatible",
                isRecommended = false,
                isCompatible = false,
                compatibilityNote = "Not designed for this device type"
            });
        }

        return options.ToArray();
    }

    /// <summary>
    /// Builds a reference-based bulk export response with deduplicated templates and shared data
    /// Optimizes payload size by using reference keys instead of duplicating data
    /// </summary>
    public static object BuildBulkExportResponse(
        List<CachedDevice> devices,
        Dictionary<int, TemplateMatchResult> templateMatches,
        Dictionary<string, TemplateFilterResult> templatesByProductType,
        List<CachedNetwork> networks,
        List<CachedOrganization> organizations,
        Connection connection,
        Dictionary<string, string> globalVariables,
        List<UploadedImage> uploadedImages)
    {
        // Build unique template dictionary
        var templateDict = new Dictionary<int, object>();
        var networkDict = new Dictionary<string, object>();
        var orgDict = new Dictionary<string, object>();
        var imageDict = new Dictionary<int, object>();

        // Add all matched templates
        foreach (var match in templateMatches.Values)
        {
            AddTemplateIfNotExists(templateDict, match.Template);
        }

        // Add all alternate templates (avoiding duplicates)
        foreach (var filterResult in templatesByProductType.Values)
        {
            AddTemplateIfNotExists(templateDict, filterResult.RecommendedTemplate);

            if (filterResult.CompatibleTemplates != null)
            {
                foreach (var t in filterResult.CompatibleTemplates)
                    AddTemplateIfNotExists(templateDict, t);
            }

            if (filterResult.IncompatibleTemplates != null)
            {
                foreach (var t in filterResult.IncompatibleTemplates)
                    AddTemplateIfNotExists(templateDict, t);
            }
        }

        // Build network dictionary
        foreach (var network in networks)
        {
            var networkRef = $"net_{network.Id}";
            if (!networkDict.ContainsKey(networkRef))
            {
                networkDict[networkRef] = new
                {
                    id = network.Id,
                    networkId = network.NetworkId,
                    name = network.Name,
                    organizationRef = $"org_{network.OrganizationId}",
                    qrCode = network.QRCodeDataUri
                };
            }
        }

        // Build organization dictionary
        foreach (var org in organizations)
        {
            var orgRef = $"org_{org.Id}";
            if (!orgDict.ContainsKey(orgRef))
            {
                orgDict[orgRef] = new
                {
                    id = org.Id,
                    organizationId = org.OrganizationId,
                    name = org.Name,
                    url = org.Url,
                    qrCode = org.QRCodeDataUri
                };
            }
        }

        // Build image dictionary
        foreach (var img in uploadedImages)
        {
            imageDict[img.Id] = new
            {
                id = img.Id,
                name = img.Name,
                dataUri = img.DataUri,
                widthPx = img.WidthPx,
                heightPx = img.HeightPx
            };
        }

        // Build device dictionary with references
        var deviceDict = new Dictionary<int, object>();
        var templateOptionsDict = new Dictionary<int, object[]>();

        foreach (var device in devices)
        {
            var matchedTemplate = templateMatches[device.Id].Template;
            var matchedTemplateRef = $"tpl_{matchedTemplate.Id}";

            // Find network for this device
            var network = networks.FirstOrDefault(n => n.NetworkId == device.NetworkId);
            var networkRef = network != null ? $"net_{network.Id}" : null;

            deviceDict[device.Id] = new
            {
                id = device.Id,
                name = device.Name,
                serial = device.Serial,
                model = device.Model,
                productType = device.ProductType,
                networkId = device.NetworkId,
                connectionId = device.ConnectionId,
                qrCode = device.QRCodeDataUri,
                networkRef = networkRef,
                matchedTemplateRef = matchedTemplateRef
            };

            // Build template options for this device
            if (!string.IsNullOrEmpty(device.ProductType) &&
                templatesByProductType.TryGetValue(device.ProductType, out var filterResult))
            {
                var options = new List<object>();

                // Add recommended if different from matched
                if (filterResult.RecommendedTemplate != null &&
                    filterResult.RecommendedTemplate.Id != matchedTemplate.Id)
                {
                    options.Add(new
                    {
                        templateRef = $"tpl_{filterResult.RecommendedTemplate.Id}",
                        category = "recommended",
                        compatibilityNote = "Default template for this device type"
                    });
                }

                // Add compatible templates
                if (filterResult.CompatibleTemplates != null)
                {
                    foreach (var t in filterResult.CompatibleTemplates)
                    {
                        if (t.Id == matchedTemplate.Id) continue;
                        options.Add(new
                        {
                            templateRef = $"tpl_{t.Id}",
                            category = "compatible",
                            compatibilityNote = "Compatible with this device type"
                        });
                    }
                }

                // Add incompatible templates
                if (filterResult.IncompatibleTemplates != null)
                {
                    foreach (var t in filterResult.IncompatibleTemplates)
                    {
                        options.Add(new
                        {
                            templateRef = $"tpl_{t.Id}",
                            category = "incompatible",
                            compatibilityNote = "Not designed for this device type"
                        });
                    }
                }

                if (options.Count > 0)
                {
                    templateOptionsDict[device.Id] = options.ToArray();
                }
            }
        }

        // Convert template dict to use "tpl_{id}" keys for consistency
        var templateDictWithRefs = templateDict.ToDictionary(
            kvp => $"tpl_{kvp.Key}",
            kvp => kvp.Value
        );

        return new
        {
            devices = deviceDict,
            templates = templateDictWithRefs,
            networks = networkDict,
            organizations = orgDict,
            connection = new
            {
                id = connection.Id,
                displayName = connection.DisplayName,
                type = connection.GetType().Name
            },
            globalVariables = globalVariables,
            uploadedImages = imageDict,
            templateOptions = templateOptionsDict
        };
    }

    /// <summary>
    /// Adds a template to the dictionary if it doesn't already exist
    /// Helper method for BuildBulkExportResponse
    /// </summary>
    private static void AddTemplateIfNotExists(Dictionary<int, object> dict, StickerTemplate? template)
    {
        if (template != null && !dict.ContainsKey(template.Id))
        {
            dict[template.Id] = new
            {
                id = template.Id,
                name = template.Name,
                templateJson = template.TemplateJson,
                pageWidth = template.PageWidth,
                pageHeight = template.PageHeight
            };
        }
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
