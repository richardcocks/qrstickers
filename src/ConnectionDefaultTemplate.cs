using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QRStickers;

/// <summary>
/// Maps device ProductTypes to default templates per connection.
/// Replaces the old IsDefault/IsRackMount system with explicit per-connection mappings.
/// </summary>
public class ConnectionDefaultTemplate
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Foreign key to Connection
    /// </summary>
    [Required]
    public int ConnectionId { get; set; }

    /// <summary>
    /// Meraki ProductType (e.g., "wireless", "switch", "appliance", "camera", "sensor", "cellularGateway")
    /// Maps to CachedDevice.ProductType field
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string ProductType { get; set; } = null!;

    /// <summary>
    /// Foreign key to StickerTemplate (nullable - null means no default set for this product type)
    /// </summary>
    public int? TemplateId { get; set; }

    /// <summary>
    /// Creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last modification timestamp
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("ConnectionId")]
    public Connection Connection { get; set; } = null!;

    [ForeignKey("TemplateId")]
    public StickerTemplate? Template { get; set; }
}
