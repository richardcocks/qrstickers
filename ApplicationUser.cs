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
}
