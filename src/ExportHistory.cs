using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using QRStickers.Meraki;

namespace QRStickers;

/// <summary>
/// Tracks all sticker export operations for analytics and user history
/// Used to log and monitor export activity across the application
/// </summary>
public class ExportHistory
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Foreign key to ApplicationUser who performed the export
    /// </summary>
    [Required]
    [MaxLength(450)]
    public string UserId { get; set; } = null!;

    /// <summary>
    /// Foreign key to StickerTemplate used for export (nullable - for future use)
    /// </summary>
    public int? TemplateId { get; set; }

    /// <summary>
    /// Foreign key to CachedDevice exported (nullable - could be multiple devices)
    /// </summary>
    public int? DeviceId { get; set; }

    /// <summary>
    /// Foreign key to Connection associated with this export
    /// </summary>
    public int? ConnectionId { get; set; }

    /// <summary>
    /// Export file format (PNG, SVG, PDF)
    /// </summary>
    [MaxLength(10)]
    public string? ExportFormat { get; set; }

    /// <summary>
    /// DPI for PNG exports (96, 150, 300, etc.)
    /// </summary>
    public int? ExportDpi { get; set; }

    /// <summary>
    /// Background type (white, transparent)
    /// </summary>
    [MaxLength(20)]
    public string? BackgroundType { get; set; }

    /// <summary>
    /// Timestamp when export was performed
    /// </summary>
    public DateTime ExportedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// File size in bytes (approximate for download tracking)
    /// </summary>
    public int? FileSize { get; set; }

    /// <summary>
    /// File path if stored server-side (null if client-side download)
    /// </summary>
    [MaxLength(500)]
    public string? FilePath { get; set; }

    /// <summary>
    /// User-Agent from browser (for analytics/support)
    /// </summary>
    [MaxLength(500)]
    public string? UserAgent { get; set; }

    /// <summary>
    /// Client IP address (for analytics/security)
    /// </summary>
    [MaxLength(50)]
    public string? IpAddress { get; set; }

    // Navigation properties
    [ForeignKey("UserId")]
    public ApplicationUser? User { get; set; }

    [ForeignKey("TemplateId")]
    public StickerTemplate? Template { get; set; }

    [ForeignKey("DeviceId")]
    public CachedDevice? Device { get; set; }

    [ForeignKey("ConnectionId")]
    public Connection? Connection { get; set; }
}
