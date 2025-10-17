/// <summary>
/// Represents an OAuth token stored for a user
/// </summary>
public class OAuthToken
{
    public int Id { get; set; }
    public string? UserId { get; set; }
    public string AccessToken { get; set; } = null!;
    public string? RefreshToken { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}