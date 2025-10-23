using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace QRStickers;

/// <summary>
/// Application user extending ASP.NET Core Identity user
/// </summary>
public class ApplicationUser : IdentityUser
{
    /// <summary>
    /// OAuth/integration connections owned by this user (Meraki, LogicMonitor, etc.)
    /// </summary>
    public ICollection<Connection> Connections { get; set; } = new List<Connection>();

    /// <summary>
    /// Optional display name for personalized greeting (falls back to email if not set)
    /// </summary>
    [MaxLength(100)]
    public string? DisplayName { get; set; }

    /// <summary>
    /// Most recent login timestamp
    /// </summary>
    public DateTime? LastLoginAt { get; set; }

    /// <summary>
    /// Previous login timestamp (before the current one) for security awareness
    /// </summary>
    public DateTime? PreviousLoginAt { get; set; }

    /// <summary>
    /// Timestamp when the current session started
    /// </summary>
    public DateTime? CurrentSessionStartedAt { get; set; }
}
