using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QRStickers;

/// <summary>
/// Maps a StickerTemplate to device types (e.g., "switch", "ap", "gateway")
/// Used for template matching - templates can be matched to device types/categories
/// </summary>
public class TemplateDeviceType
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Foreign key to StickerTemplate
    /// </summary>
    [Required]
    public int TemplateId { get; set; }

    /// <summary>
    /// Device type identifier (e.g., "switch", "ap", "gateway", "firewall", "camera", "sensor")
    /// Maps to CachedDevice.Type field or derived from device properties
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string DeviceType { get; set; } = null!;

    /// <summary>
    /// Priority/order for template matching
    /// Lower value = higher priority (0 = highest)
    /// </summary>
    public int Priority { get; set; } = 0;

    /// <summary>
    /// Creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last modification timestamp
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("TemplateId")]
    public StickerTemplate Template { get; set; } = null!;
}
