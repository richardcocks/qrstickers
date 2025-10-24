using System.ComponentModel.DataAnnotations;

namespace QRStickers;

/// <summary>
/// Represents a sticker template with customizable design elements.
/// Templates can be system-wide or connection-specific, with filtering by device type.
/// </summary>
public class StickerTemplate
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Foreign key to Connection (NULL = system template shared across all connections)
    /// </summary>
    public int? ConnectionId { get; set; }

    /// <summary>
    /// Template name (e.g., "Rack Mount Switch Label", "AP Ceiling Label")
    /// Prevents log injection via newlines and control characters
    /// </summary>
    [Required]
    [MaxLength(200)]
    [RegularExpression(@"^[a-zA-Z0-9\s\-_.()]+$", ErrorMessage = "Template name contains invalid characters. Allowed: letters, numbers, spaces, -_.()")]
    public string Name { get; set; } = null!;

    /// <summary>
    /// Optional description
    /// </summary>
    [MaxLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// System templates are read-only and cloneable by users
    /// </summary>
    public bool IsSystemTemplate { get; set; } = false;

    /// <summary>
    /// Page width in millimeters (precise print dimensions)
    /// Common sizes: 100mm x 50mm, 60mm x 40mm, etc.
    /// </summary>
    public double PageWidth { get; set; } = 100.0;  // mm

    /// <summary>
    /// Page height in millimeters
    /// </summary>
    public double PageHeight { get; set; } = 50.0;  // mm

    /// <summary>
    /// Serialized Fabric.js canvas JSON
    /// Contains all design elements, positioning, styling, and data bindings
    /// </summary>
    [Required]
    public string TemplateJson { get; set; } = null!;  // NVARCHAR(MAX)

    /// <summary>
    /// Template creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last modification timestamp
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last time this template was used in an export (for usage tracking)
    /// </summary>
    public DateTime? LastUsedAt { get; set; }

    // Navigation properties
    public Connection? Connection { get; set; }
}
