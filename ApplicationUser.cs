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
}
