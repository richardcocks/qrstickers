namespace QRStickers;

/// <summary>
/// Represents an OAuth refresh token stored for a user
/// </summary>
public class OAuthToken
{
    public int Id { get; set; }

    /// <summary>
    /// Foreign key to ApplicationUser
    /// </summary>
    public string UserId { get; set; } = null!;

    /// <summary>
    /// Navigation property to the user who owns this token
    /// </summary>
    public ApplicationUser User { get; set; } = null!;

    /// <summary>
    /// OAuth refresh token (long-lived, 90 days)
    /// Access tokens are ephemeral and not stored
    /// </summary>
    public string RefreshToken { get; set; } = null!;

    /// <summary>
    /// When the refresh token expires (typically CreatedAt + 90 days for Meraki)
    /// </summary>
    public DateTime RefreshTokenExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}