using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

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

    /// <summary>
    /// JSON array of compatible Meraki ProductTypes (e.g., ["wireless", "switch"])
    /// NULL or empty array means compatible with ALL types (universal template)
    /// </summary>
    [Column(TypeName = "nvarchar(max)")]
    public string? CompatibleProductTypes { get; set; }

    // Navigation properties
    public Connection? Connection { get; set; }

    // Helper methods for ProductType compatibility

    /// <summary>
    /// Gets the list of compatible ProductTypes.
    /// Returns null if template is compatible with all types (universal template).
    /// </summary>
    public List<string>? GetCompatibleProductTypes()
    {
        if (string.IsNullOrWhiteSpace(CompatibleProductTypes))
            return null; // Universal template

        try
        {
            return JsonSerializer.Deserialize<List<string>>(CompatibleProductTypes);
        }
        catch (JsonException)
        {
            // Invalid JSON - treat as universal template
            return null;
        }
    }

    /// <summary>
    /// Sets the compatible ProductTypes for this template.
    /// Pass null or empty list to make template universal.
    /// </summary>
    public void SetCompatibleProductTypes(List<string>? productTypes)
    {
        if (productTypes == null || productTypes.Count == 0)
        {
            CompatibleProductTypes = null;
            return;
        }

        CompatibleProductTypes = JsonSerializer.Serialize(productTypes);
    }

    /// <summary>
    /// Checks if this template is compatible with the given ProductType.
    /// Returns true for universal templates (no restrictions).
    /// </summary>
    public bool IsCompatibleWith(string productType)
    {
        var compatibleTypes = GetCompatibleProductTypes();

        // Null means universal (compatible with all)
        if (compatibleTypes == null)
            return true;

        return compatibleTypes.Contains(productType, StringComparer.OrdinalIgnoreCase);
    }
}
