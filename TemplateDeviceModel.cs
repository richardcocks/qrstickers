using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QRStickers;

/// <summary>
/// Maps a StickerTemplate to specific device models (e.g., MS225-48FP, MR32)
/// Used for template matching - templates can be matched to specific device models
/// </summary>
public class TemplateDeviceModel
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Foreign key to StickerTemplate
    /// </summary>
    [Required]
    public int TemplateId { get; set; }

    /// <summary>
    /// Device model identifier (e.g., "MS225-48FP", "MR32", "MX64W")
    /// Maps to CachedDevice.Model field
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string DeviceModel { get; set; } = null!;

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
