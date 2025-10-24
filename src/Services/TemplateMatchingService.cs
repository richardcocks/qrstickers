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
        _logger.LogInformation($"[Template] Matching template for device {LogSanitizer.Sanitize(device.Name)} (ProductType: {LogSanitizer.Sanitize(device.ProductType)})");

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
                _logger.LogInformation($"[Template] Found connection default: {LogSanitizer.Sanitize(defaultMapping.Template.Name)} (ProductType: {LogSanitizer.Sanitize(device.ProductType)})");
                return new TemplateMatchResult
                {
                    Template = defaultMapping.Template,
                    MatchReason = "connection_default",
                    Confidence = 1.0,
                    MatchedBy = device.ProductType
                };
            }
        }

        // 2. Try compatible templates (NEW: filter by ProductType compatibility)
        if (!string.IsNullOrEmpty(device.ProductType))
        {
            var allTemplates = await _db.StickerTemplates
                .AsNoTracking()
                .Where(t => t.ConnectionId == device.ConnectionId || t.ConnectionId == null)
                .ToListAsync();

            var compatibleTemplate = allTemplates
                .FirstOrDefault(t => t.IsCompatibleWith(device.ProductType));

            if (compatibleTemplate != null)
            {
                _logger.LogInformation($"[Template] Found compatible template: {LogSanitizer.Sanitize(compatibleTemplate.Name)} (ProductType: {LogSanitizer.Sanitize(device.ProductType)})");
                return new TemplateMatchResult
                {
                    Template = compatibleTemplate,
                    MatchReason = "compatible",
                    Confidence = 0.6,
                    MatchedBy = device.ProductType
                };
            }
        }

        // 3. Universal fallback (any template, with warning if incompatible)
        var fallbackTemplate = await _db.StickerTemplates
            .AsNoTracking()
            .Where(t => t.ConnectionId == device.ConnectionId || t.ConnectionId == null)
            .OrderByDescending(t => t.IsSystemTemplate) // Prefer system templates as fallback
            .FirstOrDefaultAsync();

        if (fallbackTemplate != null)
        {
            var isCompatible = string.IsNullOrEmpty(device.ProductType) || fallbackTemplate.IsCompatibleWith(device.ProductType);
            if (!isCompatible)
            {
                _logger.LogWarning($"[Template] Using incompatible fallback template '{LogSanitizer.Sanitize(fallbackTemplate.Name)}' for ProductType '{LogSanitizer.Sanitize(device.ProductType)}' (may not be optimized)");
            }
            else
            {
                _logger.LogWarning($"[Template] No default mapping found for ProductType '{LogSanitizer.Sanitize(device.ProductType)}', using fallback: {LogSanitizer.Sanitize(fallbackTemplate.Name)}");
            }

            return new TemplateMatchResult
            {
                Template = fallbackTemplate,
                MatchReason = isCompatible ? "fallback" : "fallback_incompatible",
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
    /// <param name="device">The device to find templates for</param>
    /// <param name="user">The user</param>
    /// <param name="excludeTemplateId">Optional template ID to exclude from results</param>
    /// <param name="compatibleOnly">If true, only return templates compatible with device ProductType</param>
    /// <returns>List of alternate templates</returns>
    public async Task<List<StickerTemplate>> GetAlternateTemplatesAsync(
        CachedDevice device,
        ApplicationUser user,
        int? excludeTemplateId = null,
        bool compatibleOnly = false)
    {
        // Get all templates (connection-specific + system)
        var templates = await _db.StickerTemplates
            .AsNoTracking()
            .Where(t =>
                (t.ConnectionId == null || t.ConnectionId == device.ConnectionId) &&
                (excludeTemplateId == null || t.Id != excludeTemplateId)
            )
            .ToListAsync();

        // Filter to compatible templates if requested
        if (compatibleOnly && !string.IsNullOrEmpty(device.ProductType))
        {
            templates = templates
                .Where(t => t.IsCompatibleWith(device.ProductType))
                .ToList();
        }

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
