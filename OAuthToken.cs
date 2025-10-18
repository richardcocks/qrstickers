namespace QRStickers;

/// <summary>
/// Represents an OAuth token stored for a user
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

    public string AccessToken { get; set; } = null!;
    public string? RefreshToken { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}