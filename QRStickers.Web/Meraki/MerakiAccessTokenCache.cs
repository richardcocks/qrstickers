using System.Collections.Concurrent;

namespace QRStickers.Meraki;

/// <summary>
/// Singleton service that caches Meraki access tokens per connection.
/// Prevents unnecessary token refreshes and helps avoid hitting Meraki's live token limit.
/// </summary>
public class MerakiAccessTokenCache
{
    private readonly ConcurrentDictionary<int, TokenCacheEntry> _cache = new();
    private readonly ILogger<MerakiAccessTokenCache> _logger;

    private record TokenCacheEntry(string AccessToken, DateTime ExpiresAt);

    public MerakiAccessTokenCache(ILogger<MerakiAccessTokenCache> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Attempts to get a cached access token for the specified connection
    /// </summary>
    /// <param name="connectionId">The connection ID</param>
    /// <param name="accessToken">The cached access token if found and valid</param>
    /// <param name="expiresAt">When the token expires</param>
    /// <param name="bufferMinutes">Minimum minutes before expiry to consider token valid (default 5)</param>
    /// <returns>True if a valid cached token was found</returns>
    public bool TryGetToken(int connectionId, out string? accessToken, out DateTime expiresAt, int bufferMinutes = 5)
    {
        if (_cache.TryGetValue(connectionId, out var entry))
        {
            // Check if token expires in more than bufferMinutes
            if (entry.ExpiresAt > DateTime.UtcNow.AddMinutes(bufferMinutes))
            {
                accessToken = entry.AccessToken;
                expiresAt = entry.ExpiresAt;
                _logger.LogDebug("Cache hit for connection {ConnectionId}, token expires at {ExpiresAt}", connectionId, expiresAt);
                return true;
            }

            _logger.LogDebug("Cache entry expired for connection {ConnectionId}, needs refresh", connectionId);
        }

        accessToken = null;
        expiresAt = DateTime.MinValue;
        return false;
    }

    /// <summary>
    /// Caches an access token for the specified connection
    /// </summary>
    /// <param name="connectionId">The connection ID</param>
    /// <param name="accessToken">The access token to cache</param>
    /// <param name="expiresAt">When the token expires</param>
    public void CacheToken(int connectionId, string accessToken, DateTime expiresAt)
    {
        var entry = new TokenCacheEntry(accessToken, expiresAt);
        _cache[connectionId] = entry;
        _logger.LogInformation("Cached access token for connection {ConnectionId}, expires at {ExpiresAt}", connectionId, expiresAt);
    }

    /// <summary>
    /// Removes the cached token for the specified connection
    /// Call this when a connection is deleted or disconnected
    /// </summary>
    /// <param name="connectionId">The connection ID</param>
    public void RemoveToken(int connectionId)
    {
        if (_cache.TryRemove(connectionId, out _))
        {
            _logger.LogInformation("Removed cached token for connection {ConnectionId}", connectionId);
        }
    }

    /// <summary>
    /// Gets the number of cached tokens (for monitoring)
    /// </summary>
    public int CachedTokenCount => _cache.Count;
}
