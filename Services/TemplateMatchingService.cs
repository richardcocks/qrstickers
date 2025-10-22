using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using QRStickers.Meraki;

namespace QRStickers.Services;

/// <summary>
/// Matches devices to appropriate sticker templates based on device type/model
/// Uses priority-based matching with fallbacks
/// </summary>
public class TemplateMatchingService
{
    private readonly QRStickersDbContext _db;
    private readonly ILogger<TemplateMatchingService> _logger;
    private readonly IMemoryCache _cache;

    public TemplateMatchingService(QRStickersDbContext db, ILogger<TemplateMatchingService> logger, IMemoryCache cache)
    {
        _db = db;
        _logger = logger;
        _cache = cache;
    }

    /// <summary>
    /// Finds the best matching template for a device
    /// Priority: 1) Model match, 2) Type match, 3) User default, 4) System default, 5) Fallback
    /// </summary>
    public async Task<TemplateMatchResult> FindTemplateForDeviceAsync(
        CachedDevice device,
        ApplicationUser user)
    {
        _logger.LogInformation($"[Template] Matching template for device {device.Name} (model: {device.Model})");

        // Check cache first
        var cacheKey = $"template_match_{device.Id}_{user.Id}";
        if (_cache.TryGetValue(cacheKey, out TemplateMatchResult? cached))
        {
            _logger.LogDebug($"[Template] Cache hit for device {device.Id}");
            return cached!;
        }

        var result = await PerformTemplateMatchAsync(device, user);

        // Cache result for 30 minutes
        _cache.Set(cacheKey, result, TimeSpan.FromMinutes(30));

        return result;
    }

    private async Task<TemplateMatchResult> PerformTemplateMatchAsync(
        CachedDevice device,
        ApplicationUser user)
    {
        // 1. Try exact model match
        var modelMatch = await _db.TemplateDeviceModels
            .AsNoTracking()
            .Include(m => m.Template)
            .Where(m => m.Template.ConnectionId == null || m.Template.ConnectionId == device.ConnectionId)
            .Where(m => m.DeviceModel == device.Model)
            .OrderBy(m => m.Priority)
            .FirstOrDefaultAsync();

        if (modelMatch != null)
        {
            _logger.LogInformation($"[Template] Found model match: {modelMatch.Template.Name}");
            return new TemplateMatchResult
            {
                Template = modelMatch.Template,
                MatchReason = "model_match",
                Confidence = 1.0,
                MatchedBy = device.Model ?? "unknown"
            };
        }

        // 2. Try device type match (using TemplateDeviceTypes mapping table)
        var deviceType = DeriveDeviceType(device.Model);
        var typeMatch = await _db.TemplateDeviceTypes
            .AsNoTracking()
            .Include(t => t.Template)
            .Where(t => t.Template.ConnectionId == null || t.Template.ConnectionId == device.ConnectionId)
            .Where(t => t.DeviceType == deviceType)
            .OrderBy(t => t.Priority)
            .FirstOrDefaultAsync();

        if (typeMatch != null)
        {
            _logger.LogInformation($"[Template] Found type match: {typeMatch.Template.Name} (type: {deviceType})");
            return new TemplateMatchResult
            {
                Template = typeMatch.Template,
                MatchReason = "type_match",
                Confidence = 0.8,
                MatchedBy = deviceType
            };
        }

        // 2.5. Try ProductTypeFilter match (templates with matching product type filter)
        var productType = device.ProductType?.ToLower();
        if (!string.IsNullOrEmpty(productType))
        {
            var productTypeMatch = await _db.StickerTemplates
                .AsNoTracking()
                .Where(t => t.ConnectionId == device.ConnectionId)
                .Where(t => t.ProductTypeFilter != null && t.ProductTypeFilter.ToLower() == productType)
                .OrderByDescending(t => t.IsDefault) // Prioritize default templates for this product type
                .ThenBy(t => t.Id)
                .FirstOrDefaultAsync();

            if (productTypeMatch != null)
            {
                _logger.LogInformation($"[Template] Found product type filter match: {productTypeMatch.Name} (productType: {productType})");
                return new TemplateMatchResult
                {
                    Template = productTypeMatch,
                    MatchReason = "type_match",
                    Confidence = 0.75,
                    MatchedBy = productType
                };
            }
        }

        // 3. Try user's default template (for this connection)
        var userDefaultTemplate = await _db.StickerTemplates
            .AsNoTracking()
            .Where(t => t.ConnectionId == device.ConnectionId && t.IsDefault)
            .FirstOrDefaultAsync();

        if (userDefaultTemplate != null)
        {
            _logger.LogInformation($"[Template] Using user default template: {userDefaultTemplate.Name}");
            return new TemplateMatchResult
            {
                Template = userDefaultTemplate,
                MatchReason = "user_default",
                Confidence = 0.5,
                MatchedBy = "user_default"
            };
        }

        // 4. Try system default template
        var defaultTemplate = await _db.StickerTemplates
            .AsNoTracking()
            .Where(t => t.IsSystemTemplate && t.IsDefault)
            .FirstOrDefaultAsync();

        if (defaultTemplate != null)
        {
            _logger.LogInformation($"[Template] Using system default template: {defaultTemplate.Name}");
            return new TemplateMatchResult
            {
                Template = defaultTemplate,
                MatchReason = "system_default",
                Confidence = 0.3,
                MatchedBy = "system_default"
            };
        }

        // 5. Fallback to any available template
        var anyTemplate = await _db.StickerTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync();

        if (anyTemplate != null)
        {
            _logger.LogWarning($"[Template] No matching template found, using first available: {anyTemplate.Name}");
            return new TemplateMatchResult
            {
                Template = anyTemplate,
                MatchReason = "fallback",
                Confidence = 0.1,
                MatchedBy = "fallback"
            };
        }

        _logger.LogError($"[Template] No templates available at all!");
        throw new InvalidOperationException("No templates available for export");
    }

    /// <summary>
    /// Gets list of alternative templates that could work for this device
    /// </summary>
    public async Task<List<StickerTemplate>> GetAlternateTemplatesAsync(
        CachedDevice device,
        ApplicationUser user,
        int? excludeTemplateId = null)
    {
        var deviceType = DeriveDeviceType(device.Model);

        // Get all templates (model matches + type matches)
        var templates = await _db.StickerTemplates
            .AsNoTracking()
            .Where(t =>
                (t.ConnectionId == null || t.ConnectionId == device.ConnectionId) &&
                (excludeTemplateId == null || t.Id != excludeTemplateId)
            )
            .ToListAsync();

        return templates;
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

/// <summary>
/// Result of template matching for a device
/// </summary>
public class TemplateMatchResult
{
    /// <summary>
    /// The matched template
    /// </summary>
    public required StickerTemplate Template { get; set; }

    /// <summary>
    /// Reason for the match: model_match, type_match, user_default, system_default, fallback
    /// </summary>
    public required string MatchReason { get; set; }

    /// <summary>
    /// Confidence score (1.0 = perfect match, 0.1 = fallback)
    /// </summary>
    public required double Confidence { get; set; }

    /// <summary>
    /// What was matched against (model name, type, etc.)
    /// </summary>
    public required string MatchedBy { get; set; }
}
