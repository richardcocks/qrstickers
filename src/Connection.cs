using System.ComponentModel.DataAnnotations;

namespace QRStickers;

/// <summary>
/// Base class for all OAuth/integration connections (Meraki, LogicMonitor, etc.)
/// Uses Table-per-Hierarchy (TPH) with discriminator pattern
/// </summary>
public abstract class Connection
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Foreign key to ApplicationUser who owns this connection
    /// </summary>
    [Required]
    public string UserId { get; set; } = null!;

    /// <summary>
    /// User-defined display name (e.g., "Work Meraki Account", "Home LogicMonitor")
    /// Prevents log injection via newlines and control characters
    /// </summary>
    [Required]
    [MaxLength(100)]
    [RegularExpression(@"^[a-zA-Z0-9\s\-_.()]+$", ErrorMessage = "Display name contains invalid characters. Allowed: letters, numbers, spaces, -_.()")]
    public string DisplayName { get; set; } = null!;

    /// <summary>
    /// Connection type discriminator (set by derived classes)
    /// Values: "Meraki", "LogicMonitor", etc.
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string ConnectionType { get; set; } = null!;

    /// <summary>
    /// Whether this connection is currently active (user can disable temporarily)
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Whether this is a demo connection with fake/placeholder data (for marketing screenshots)
    /// Demo connections skip OAuth and sync with generated data instead of real API calls
    /// </summary>
    public bool IsDemo { get; set; } = false;

    /// <summary>
    /// When this connection was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this connection was last updated
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Company logo URL or base64 data URI for this connection
    /// Used in sticker templates when logo element is present
    /// Format: "https://..." or "data:image/png;base64,..."
    /// </summary>
    [MaxLength(5000)]  // Allow for base64-encoded images
    public string? CompanyLogoUrl { get; set; }

    /// <summary>
    /// Navigation property to the user who owns this connection
    /// </summary>
    public ApplicationUser User { get; set; } = null!;

    /// <summary>
    /// Navigation property to default templates per device ProductType
    /// </summary>
    public ICollection<ConnectionDefaultTemplate> DefaultTemplates { get; set; } = new List<ConnectionDefaultTemplate>();
}
