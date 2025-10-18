using System.Collections.Concurrent;

namespace QRStickers;

/// <summary>
/// Singleton service that caches Meraki access tokens across requests.
/// Prevents unnecessary token refreshes and helps avoid hitting Meraki's live token limit.
/// </summary>
public class AccessTokenCache
{
    private readonly ConcurrentDictionary<string, TokenCacheEntry> _cache = new();
    private readonly ILogger<AccessTokenCache> _logger;

    private record TokenCacheEntry(string AccessToken, DateTime ExpiresAt);

    public AccessTokenCache(ILogger<AccessTokenCache> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Attempts to get a cached access token for the specified user
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="accessToken">The cached access token if found and valid</param>
    /// <param name="expiresAt">When the token expires</param>
    /// <param name="bufferMinutes">Minimum minutes before expiry to consider token valid (default 5)</param>
    /// <returns>True if a valid cached token was found</returns>
    public bool TryGetToken(string userId, out string? accessToken, out DateTime expiresAt, int bufferMinutes = 5)
    {
        if (_cache.TryGetValue(userId, out var entry))
        {
            // Check if token expires in more than bufferMinutes
            if (entry.ExpiresAt > DateTime.UtcNow.AddMinutes(bufferMinutes))
            {
                accessToken = entry.AccessToken;
                expiresAt = entry.ExpiresAt;
                _logger.LogDebug("Cache hit for user {UserId}, token expires at {ExpiresAt}", userId, expiresAt);
                return true;
            }

            _logger.LogDebug("Cache entry expired for user {UserId}, needs refresh", userId);
        }

        accessToken = null;
        expiresAt = DateTime.MinValue;
        return false;
    }

    /// <summary>
    /// Caches an access token for the specified user
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <param name="accessToken">The access token to cache</param>
    /// <param name="expiresAt">When the token expires</param>
    public void CacheToken(string userId, string accessToken, DateTime expiresAt)
    {
        var entry = new TokenCacheEntry(accessToken, expiresAt);
        _cache[userId] = entry;
        _logger.LogInformation("Cached access token for user {UserId}, expires at {ExpiresAt}", userId, expiresAt);
    }

    /// <summary>
    /// Removes the cached token for the specified user
    /// Call this when a user disconnects their Meraki account
    /// </summary>
    /// <param name="userId">The user ID</param>
    public void RemoveToken(string userId)
    {
        if (_cache.TryRemove(userId, out _))
        {
            _logger.LogInformation("Removed cached token for user {UserId}", userId);
        }
    }

    /// <summary>
    /// Gets the number of cached tokens (for monitoring)
    /// </summary>
    public int CachedTokenCount => _cache.Count;
}
