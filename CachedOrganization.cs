using System.ComponentModel.DataAnnotations;

namespace QRStickers;

/// <summary>
/// Cached Meraki organization data per user
/// </summary>
public class CachedOrganization
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Foreign key to Connection (MerakiConnection)
    /// </summary>
    [Required]
    public int ConnectionId { get; set; }

    /// <summary>
    /// Meraki organization ID
    /// </summary>
    [Required]
    public string OrganizationId { get; set; } = null!;

    /// <summary>
    /// Organization name
    /// </summary>
    [Required]
    public string Name { get; set; } = null!;

    /// <summary>
    /// Organization dashboard URL (nullable)
    /// </summary>
    public string? Url { get; set; }

    /// <summary>
    /// Soft delete flag - true if organization no longer exists in Meraki
    /// </summary>
    public bool IsDeleted { get; set; } = false;

    /// <summary>
    /// Last time this organization was synced from Meraki API
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
    /// Navigation property to cached networks
    /// </summary>
    public ICollection<CachedNetwork> Networks { get; set; } = new List<CachedNetwork>();
}
