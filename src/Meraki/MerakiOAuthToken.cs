namespace QRStickers.Meraki;

/// <summary>
/// Represents a Meraki OAuth refresh token for a specific connection
/// </summary>
public class MerakiOAuthToken
{
    public int Id { get; set; }

    /// <summary>
    /// Foreign key to Connection (MerakiConnection)
    /// </summary>
    public int ConnectionId { get; set; }

    /// <summary>
    /// Navigation property to the connection that owns this token
    /// </summary>
    public Connection Connection { get; set; } = null!;

    /// <summary>
    /// OAuth refresh token (long-lived, 90 days)
    /// Access tokens are ephemeral and not stored in the database
    /// </summary>
    public string RefreshToken { get; set; } = null!;

    /// <summary>
    /// When the refresh token expires (typically CreatedAt + 90 days for Meraki)
    /// </summary>
    public DateTime RefreshTokenExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
