using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;

namespace QRStickers.Meraki;

/// <summary>
/// Cached Meraki network data per connection
/// </summary>
public class CachedNetwork
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Foreign key to Connection (MerakiConnection)
    /// </summary>
    [Required]
    public int ConnectionId { get; set; }

    /// <summary>
    /// Foreign key to CachedOrganization (reference by OrganizationId string)
    /// </summary>
    [Required]
    public string OrganizationId { get; set; } = null!;

    /// <summary>
    /// Meraki network ID
    /// </summary>
    [Required]
    public string NetworkId { get; set; } = null!;

    /// <summary>
    /// Network name
    /// </summary>
    [Required]
    public string Name { get; set; } = null!;

    /// <summary>
    /// Network dashboard URL (nullable)
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// Product types as JSON array string (e.g., ["appliance", "switch"])
    /// </summary>
    public string? ProductTypesJson { get; set; }

    /// <summary>
    /// Tags as JSON array string
    /// </summary>
    public string? TagsJson { get; set; }

    /// <summary>
    /// Network timezone
    /// </summary>
    public string? TimeZone { get; set; }

    /// <summary>
    /// Soft delete flag - true if network no longer exists in Meraki
    /// </summary>
    public bool IsDeleted { get; set; } = false;

    /// <summary>
    /// Last time this network was synced from Meraki API
    /// </summary>
    public DateTime LastSyncedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When this record was first created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property to connection
    /// </summary>
    public Connection Connection { get; set; } = null!;

    /// <summary>
    /// Navigation property to organization
    /// </summary>
    public CachedOrganization Organization { get; set; } = null!;

    /// <summary>
    /// Navigation property to cached devices
    /// </summary>
    public ICollection<CachedDevice> Devices { get; set; } = new List<CachedDevice>();

    /// <summary>
    /// Product types deserialized from JSON (not mapped to database)
    /// </summary>
    [NotMapped]
    public List<string>? ProductTypes
    {
        get
        {
            if (string.IsNullOrEmpty(ProductTypesJson))
                return null;
            try
            {
                return JsonSerializer.Deserialize<List<string>>(ProductTypesJson);
            }
            catch
            {
                return null;
            }
        }
    }

    /// <summary>
    /// Tags deserialized from JSON (not mapped to database)
    /// </summary>
    [NotMapped]
    public List<string>? Tags
    {
        get
        {
            if (string.IsNullOrEmpty(TagsJson))
                return null;
            try
            {
                return JsonSerializer.Deserialize<List<string>>(TagsJson);
            }
            catch
            {
                return null;
            }
        }
    }
}
