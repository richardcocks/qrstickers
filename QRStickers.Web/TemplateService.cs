using Microsoft.EntityFrameworkCore;
using QRStickers.Meraki;

namespace QRStickers;

/// <summary>
/// Service for selecting the appropriate sticker template for a device.
/// Implements priority-based template selection algorithm.
/// </summary>
public class TemplateService
{
    private readonly QRStickersDbContext _db;

    public TemplateService(QRStickersDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Gets the best matching template for a device based on ConnectionDefaultTemplates mapping
    /// 1. Connection default for device ProductType
    /// 2. Any available template (connection-specific or system)
    /// </summary>
    /// <param name="device">The device to generate a sticker for</param>
    /// <param name="connectionId">The connection ID</param>
    /// <returns>The selected template</returns>
    /// <exception cref="InvalidOperationException">Thrown if no templates exist</exception>
    public async Task<StickerTemplate> GetTemplateForDeviceAsync(
        CachedDevice device,
        int connectionId)
    {
        // 1. Try connection default for this ProductType
        if (!string.IsNullOrEmpty(device.ProductType))
        {
            var defaultMapping = await _db.ConnectionDefaultTemplates
                .Include(d => d.Template)
                .Where(d => d.ConnectionId == connectionId)
                .Where(d => d.ProductType.ToLower() == device.ProductType.ToLower())
                .Where(d => d.TemplateId != null)
                .FirstOrDefaultAsync();

            if (defaultMapping?.Template != null)
            {
                return defaultMapping.Template;
            }
        }

        // 2. Fallback to any available template
        var template = await _db.StickerTemplates
            .Where(t => t.ConnectionId == connectionId || t.IsSystemTemplate)
            .OrderByDescending(t => t.IsSystemTemplate)
            .FirstOrDefaultAsync();

        if (template == null)
        {
            throw new InvalidOperationException(
                "No templates available. Ensure SystemTemplateSeeder has run.");
        }

        return template;
    }

    /// <summary>
    /// Gets all templates for a specific connection (including system templates)
    /// </summary>
    /// <param name="connectionId">The connection ID</param>
    /// <returns>List of templates ordered by name</returns>
    public async Task<List<StickerTemplate>> GetTemplatesForConnectionAsync(int connectionId)
    {
        return await _db.StickerTemplates
            .Where(t => t.ConnectionId == connectionId || t.IsSystemTemplate)
            .OrderBy(t => t.Name)
            .ToListAsync();
    }

    /// <summary>
    /// Gets a specific template by ID
    /// </summary>
    /// <param name="templateId">The template ID</param>
    /// <returns>The template, or null if not found</returns>
    public async Task<StickerTemplate?> GetTemplateByIdAsync(int templateId)
    {
        return await _db.StickerTemplates.FindAsync(templateId);
    }

    /// <summary>
    /// Clones an existing template to a new connection-specific template
    /// </summary>
    /// <param name="templateId">The template ID to clone</param>
    /// <param name="targetConnectionId">The connection ID to assign the clone to</param>
    /// <param name="newName">Optional new name for the cloned template</param>
    /// <returns>The newly created template</returns>
    /// <exception cref="InvalidOperationException">Thrown if source template not found</exception>
    public async Task<StickerTemplate> CloneTemplateAsync(
        int templateId,
        int targetConnectionId,
        string? newName = null)
    {
        var sourceTemplate = await _db.StickerTemplates.FindAsync(templateId);
        if (sourceTemplate == null)
        {
            throw new InvalidOperationException($"Template {templateId} not found.");
        }

        var clonedTemplate = new StickerTemplate
        {
            Name = newName ?? $"{sourceTemplate.Name} (Copy)",
            Description = sourceTemplate.Description,
            ConnectionId = targetConnectionId,
            PageWidth = sourceTemplate.PageWidth,
            PageHeight = sourceTemplate.PageHeight,
            IsSystemTemplate = false, // Always user template
            TemplateJson = sourceTemplate.TemplateJson,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.StickerTemplates.Add(clonedTemplate);
        await _db.SaveChangesAsync();

        return clonedTemplate;
    }
}
