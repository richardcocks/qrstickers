using Microsoft.AspNetCore.Identity;

namespace QRStickers;

/// <summary>
/// Application user extending ASP.NET Core Identity user
/// </summary>
public class ApplicationUser : IdentityUser
{
    /// <summary>
    /// OAuth tokens associated with this user (e.g., Meraki)
    /// </summary>
    public ICollection<OAuthToken> OAuthTokens { get; set; } = new List<OAuthToken>();

    /// <summary>
    /// Cached Meraki organizations for this user
    /// </summary>
    public ICollection<CachedOrganization> CachedOrganizations { get; set; } = new List<CachedOrganization>();

    /// <summary>
    /// Cached Meraki networks for this user
    /// </summary>
    public ICollection<CachedNetwork> CachedNetworks { get; set; } = new List<CachedNetwork>();

    /// <summary>
    /// Cached Meraki devices for this user
    /// </summary>
    public ICollection<CachedDevice> CachedDevices { get; set; } = new List<CachedDevice>();

    /// <summary>
    /// Sync status tracking for Meraki data
    /// </summary>
    public SyncStatus? SyncStatus { get; set; }
}
