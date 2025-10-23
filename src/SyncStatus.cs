using System.ComponentModel.DataAnnotations;

namespace QRStickers;

/// <summary>
/// Sync status enum
/// </summary>
public enum SyncState
{
    NotStarted = 0,
    InProgress = 1,
    Completed = 2,
    Failed = 3
}

/// <summary>
/// Tracks Meraki data sync status per connection
/// </summary>
public class SyncStatus
{
    /// <summary>
    /// Primary key
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Foreign key to Connection (MerakiConnection) - one sync status per connection
    /// </summary>
    [Required]
    public int ConnectionId { get; set; }

    /// <summary>
    /// When the last sync started
    /// </summary>
    public DateTime? LastSyncStartedAt { get; set; }

    /// <summary>
    /// When the last sync completed (successfully or with failure)
    /// </summary>
    public DateTime? LastSyncCompletedAt { get; set; }

    /// <summary>
    /// Current sync status
    /// </summary>
    public SyncState Status { get; set; } = SyncState.NotStarted;

    /// <summary>
    /// Current step description (e.g., "Syncing organizations", "Syncing networks")
    /// </summary>
    public string? CurrentStep { get; set; }

    /// <summary>
    /// Current step number (e.g., 1 for orgs, 2 for networks, 3 for devices)
    /// </summary>
    public int CurrentStepNumber { get; set; } = 0;

    /// <summary>
    /// Total number of steps (typically 3: orgs, networks, devices)
    /// </summary>
    public int TotalSteps { get; set; } = 3;

    /// <summary>
    /// Error message if sync failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Navigation property to connection
    /// </summary>
    public Connection Connection { get; set; } = null!;
}
