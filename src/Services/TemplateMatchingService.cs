using Microsoft.EntityFrameworkCore;
using QRStickers.Meraki;

namespace QRStickers.Services;

/// <summary>
/// Matches devices to appropriate sticker templates based on ProductType → Template mappings
/// Uses simplified connection-based defaults instead of complex priority matching
/// </summary>
public class TemplateMatchingService
{
    private readonly QRStickersDbContext _db;
    private readonly ILogger<TemplateMatchingService> _logger;

    public TemplateMatchingService(QRStickersDbContext db, ILogger<TemplateMatchingService> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Finds the best matching template for a device
    /// Logic: 1) Connection default for ProductType, 2) Fallback to any available template
    /// </summary>
    public async Task<TemplateMatchResult> FindTemplateForDeviceAsync(
        CachedDevice device,
        ApplicationUser user)
    {
        _logger.LogInformation($"[Template] Matching template for device {device.Name} (ProductType: {device.ProductType})");

        var result = await PerformTemplateMatchAsync(device, user);

        return result;
    }

    private async Task<TemplateMatchResult> PerformTemplateMatchAsync(
        CachedDevice device,
        ApplicationUser user)
    {
        // 1. Try connection default for this ProductType
        if (!string.IsNullOrEmpty(device.ProductType))
        {
            var defaultMapping = await _db.ConnectionDefaultTemplates
                .AsNoTracking()
                .Include(d => d.Template)
                .Where(d => d.ConnectionId == device.ConnectionId)
                .Where(d => d.ProductType.ToLower() == device.ProductType.ToLower())
                .Where(d => d.TemplateId != null) // Must have a template assigned
                .FirstOrDefaultAsync();

            if (defaultMapping?.Template != null)
            {
                _logger.LogInformation($"[Template] Found connection default: {defaultMapping.Template.Name} (ProductType: {device.ProductType})");
                return new TemplateMatchResult
                {
                    Template = defaultMapping.Template,
                    MatchReason = "connection_default",
                    Confidence = 1.0,
                    MatchedBy = device.ProductType
                };
            }
        }

        // 2. Fallback to any available template
        var anyTemplate = await _db.StickerTemplates
            .AsNoTracking()
            .Where(t => t.ConnectionId == device.ConnectionId || t.ConnectionId == null)
            .OrderByDescending(t => t.IsSystemTemplate) // Prefer system templates as fallback
            .FirstOrDefaultAsync();

        if (anyTemplate != null)
        {
            _logger.LogWarning($"[Template] No default mapping found for ProductType '{device.ProductType}', using fallback: {anyTemplate.Name}");
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
        // Get all templates (connection-specific + system)
        var templates = await _db.StickerTemplates
            .AsNoTracking()
            .Where(t =>
                (t.ConnectionId == null || t.ConnectionId == device.ConnectionId) &&
                (excludeTemplateId == null || t.Id != excludeTemplateId)
            )
            .ToListAsync();

        return templates;
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
    /// Reason for the match: connection_default, fallback
    /// </summary>
    public required string MatchReason { get; set; }

    /// <summary>
    /// Confidence score (1.0 = perfect match, 0.1 = fallback)
    /// </summary>
    public required double Confidence { get; set; }

    /// <summary>
    /// What was matched against (ProductType, "fallback", etc.)
    /// </summary>
    public required string MatchedBy { get; set; }
}
