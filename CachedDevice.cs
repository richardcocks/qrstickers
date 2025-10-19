using System.ComponentModel.DataAnnotations;

namespace QRStickers;

/// <summary>
/// Cached Meraki device data per user
/// </summary>
public class CachedDevice
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Foreign key to Connection (MerakiConnection)
    /// </summary>
    [Required]
    public int ConnectionId { get; set; }

    /// <summary>
    /// Foreign key to CachedNetwork (reference by NetworkId string, nullable if device not assigned)
    /// </summary>
    public string? NetworkId { get; set; }

    /// <summary>
    /// Device serial number (unique identifier from Meraki)
    /// </summary>
    [Required]
    public string Serial { get; set; } = null!;

    /// <summary>
    /// Device name
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Device model
    /// </summary>
    public string? Model { get; set; }

    /// <summary>
    /// Soft delete flag - true if device no longer exists in Meraki
    /// </summary>
    public bool IsDeleted { get; set; } = false;

    /// <summary>
    /// Last time this device was synced from Meraki API
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
    /// Navigation property to network (nullable - device may not be assigned)
    /// </summary>
    public CachedNetwork? Network { get; set; }
}
