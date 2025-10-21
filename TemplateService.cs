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
    /// Gets the best matching template for a device based on priority order:
    /// 1. User's connection-specific template for exact product type
    /// 2. User's connection-specific default template
    /// 3. System template for product type
    /// 4. System default template (fallback)
    /// </summary>
    /// <param name="device">The device to generate a sticker for</param>
    /// <param name="connectionId">The connection ID</param>
    /// <returns>The selected template</returns>
    /// <exception cref="InvalidOperationException">Thrown if no default system template exists</exception>
    public async Task<StickerTemplate> GetTemplateForDeviceAsync(
        CachedDevice device,
        int connectionId)
    {
        // Get device's product type from network
        var network = await _db.CachedNetworks
            .FirstOrDefaultAsync(n => n.ConnectionId == connectionId
                                   && n.NetworkId == device.NetworkId);

        var productType = network?.ProductTypes?.FirstOrDefault();

        // 1. Try user's template for this product type
        var template = await _db.StickerTemplates
            .Where(t => t.ConnectionId == connectionId
                     && t.ProductTypeFilter == productType)
            .FirstOrDefaultAsync();

        // 2. Try user's default template
        if (template == null)
        {
            template = await _db.StickerTemplates
                .Where(t => t.ConnectionId == connectionId && t.IsDefault)
                .FirstOrDefaultAsync();
        }

        // 3. Try system template for this product type
        if (template == null)
        {
            template = await _db.StickerTemplates
                .Where(t => t.IsSystemTemplate
                         && t.ProductTypeFilter == productType)
                .FirstOrDefaultAsync();
        }

        // 4. Fallback to system default
        if (template == null)
        {
            template = await _db.StickerTemplates
                .Where(t => t.IsSystemTemplate && t.IsDefault)
                .FirstOrDefaultAsync();
        }

        // If still no template found, throw exception (should never happen if seeder ran)
        if (template == null)
        {
            throw new InvalidOperationException(
                "No default system template found. Ensure SystemTemplateSeeder has run.");
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
}
