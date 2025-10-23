using System.ComponentModel.DataAnnotations;

namespace QRStickers;

/// <summary>
/// Represents a custom image uploaded by a user for a specific connection
/// Images are stored as data URIs and can be used in sticker templates
/// </summary>
public class UploadedImage
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Foreign key to Connection (connection-scoped assets)
    /// </summary>
    [Required]
    public int ConnectionId { get; set; }

    /// <summary>
    /// User-friendly name for the image (e.g., "CompanyLogo", "FloorPlanB2")
    /// Used in data binding: {{customImage.CompanyLogo}}
    /// Must match regex: ^[A-Za-z0-9_-]+$
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = null!;

    /// <summary>
    /// Optional description for the image
    /// </summary>
    [MaxLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// Image data URI (data:image/png;base64,... or data:image/jpeg;base64,...)
    /// Set to transparent 1×1 PNG on soft delete
    /// </summary>
    [Required]
    public string DataUri { get; set; } = null!;  // NVARCHAR(MAX) in SQL Server

    /// <summary>
    /// Original width in pixels (for aspect ratio preservation)
    /// </summary>
    [Required]
    public int WidthPx { get; set; }

    /// <summary>
    /// Original height in pixels (for aspect ratio preservation)
    /// </summary>
    [Required]
    public int HeightPx { get; set; }

    /// <summary>
    /// MIME type (image/png, image/jpeg, image/webp, image/svg+xml)
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string MimeType { get; set; } = null!;

    /// <summary>
    /// File size in bytes (for quota management)
    /// Includes base64 overhead (~33% larger than original file)
    /// </summary>
    [Required]
    public long FileSizeBytes { get; set; }

    /// <summary>
    /// Soft delete flag - true if image deleted but may be referenced by templates
    /// When true, DataUri is replaced with transparent 1×1 PNG
    /// </summary>
    public bool IsDeleted { get; set; } = false;

    /// <summary>
    /// Upload timestamp
    /// </summary>
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last time this image was used in an export (for orphan detection in Phase 6.3)
    /// </summary>
    public DateTime? LastUsedAt { get; set; }

    /// <summary>
    /// Navigation property to the connection that owns this image
    /// </summary>
    public Connection Connection { get; set; } = null!;
}
