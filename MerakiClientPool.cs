using System.Collections.Concurrent;
using Meraki.Api;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace QRStickers;

/// <summary>
/// Manages a pool of reusable MerakiClient instances, one per user.
/// Handles OAuth refresh token management and automatic access token refresh.
/// Thread-safe and suitable for dependency injection.
/// </summary>
public class MerakiClientPool : IDisposable
{
    private readonly ConcurrentDictionary<string, ClientEntry> _clients = new();
    private readonly ILogger<MerakiClientPool> _logger;
    private readonly MerakiApiClient _oauthClient;
    private readonly IServiceProvider _serviceProvider;
    private bool _disposed;

    // Track access tokens in memory (userId -> (accessToken, expiresAt))
    private readonly ConcurrentDictionary<string, (string AccessToken, DateTime ExpiresAt)> _accessTokenCache = new();

    private record ClientEntry(MerakiClient Client, DateTime LastUsed);

    public MerakiClientPool(
        ILogger<MerakiClientPool> logger,
        MerakiApiClient oauthClient,
        IServiceProvider serviceProvider)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _oauthClient = oauthClient ?? throw new ArgumentNullException(nameof(oauthClient));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <summary>
    /// Gets or creates a MerakiClient for the specified user.
    /// Automatically handles access token refresh using stored refresh token.
    /// </summary>
    /// <param name="userId">The user ID</param>
    /// <returns>A reusable MerakiClient instance with valid access token</returns>
    public async Task<MerakiClient> GetClientForUserAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            throw new ArgumentException("User ID cannot be null or empty", nameof(userId));
        }

        if (_disposed)
        {
            throw new ObjectDisposedException(nameof(MerakiClientPool));
        }

        // Get valid access token (refresh if needed)
        var accessToken = await GetValidAccessTokenAsync(userId);

        if (accessToken == null)
        {
            throw new InvalidOperationException($"Unable to obtain access token for user {userId}");
        }

        // Get or create client for this user
        var entry = _clients.AddOrUpdate(
            userId,
            // Create new client
            key =>
            {
                _logger.LogInformation("Creating new MerakiClient for user {UserId}", userId);
                var options = new MerakiClientOptions
                {
                    ApiKey = accessToken,
                    UserAgent = "QRStickers/1.0.0",
                    HttpClientTimeoutSeconds = 600,
                    MaxAttemptCount = 5
                };
                return new ClientEntry(new MerakiClient(options, _logger), DateTime.UtcNow);
            },
            // Update existing client's access token if changed
            (key, existing) =>
            {
                // Note: MerakiClient doesn't support updating the API key after construction
                // So if access token changed, we need to dispose old client and create new one
                var cachedToken = _accessTokenCache.TryGetValue(userId, out var cached) ? cached.AccessToken : null;

                if (cachedToken != accessToken)
                {
                    _logger.LogInformation("Access token changed for user {UserId}, recreating MerakiClient", userId);
                    existing.Client.Dispose();

                    var options = new MerakiClientOptions
                    {
                        ApiKey = accessToken,
                        UserAgent = "QRStickers/1.0.0",
                        HttpClientTimeoutSeconds = 600,
                        MaxAttemptCount = 5
                    };
                    return new ClientEntry(new MerakiClient(options, _logger), DateTime.UtcNow);
                }

                return existing with { LastUsed = DateTime.UtcNow };
            }
        );

        return entry.Client;
    }

    /// <summary>
    /// Gets a valid access token for the user, refreshing if necessary
    /// </summary>
    private async Task<string?> GetValidAccessTokenAsync(string userId)
    {
        // Check if we have a cached access token that's still valid
        if (_accessTokenCache.TryGetValue(userId, out var cached))
        {
            // If token expires in more than 5 minutes, use it
            if (cached.ExpiresAt > DateTime.UtcNow.AddMinutes(5))
            {
                _logger.LogDebug("Using cached access token for user {UserId}", userId);
                return cached.AccessToken;
            }

            _logger.LogInformation("Cached access token expired for user {UserId}, refreshing", userId);
        }

        // Need to get a fresh access token
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<QRStickersDbContext>();

        var oauthToken = await db.OAuthTokens.FirstOrDefaultAsync(t => t.UserId == userId);

        if (oauthToken == null)
        {
            _logger.LogWarning("No OAuth token found for user {UserId}", userId);
            return null;
        }

        // Check if refresh token is expired
        if (DateTime.UtcNow > oauthToken.RefreshTokenExpiresAt)
        {
            _logger.LogWarning("Refresh token expired for user {UserId}", userId);
            return null;
        }

        // Refresh the access token
        var refreshResult = await _oauthClient.RefreshAccessTokenAsync(oauthToken.RefreshToken);

        if (refreshResult == null)
        {
            _logger.LogError("Failed to refresh access token for user {UserId}", userId);
            return null;
        }

        var (newAccessToken, newRefreshToken, expiresIn) = refreshResult.Value;

        // Update refresh token in database if it changed
        if (newRefreshToken != null && newRefreshToken != oauthToken.RefreshToken)
        {
            _logger.LogInformation("Refresh token rotated for user {UserId}, updating database", userId);
            oauthToken.RefreshToken = newRefreshToken;
            oauthToken.UpdatedAt = DateTime.UtcNow;
            db.OAuthTokens.Update(oauthToken);
            await db.SaveChangesAsync();
        }

        // Cache the new access token
        var expiresAt = DateTime.UtcNow.AddSeconds(expiresIn);
        _accessTokenCache[userId] = (newAccessToken, expiresAt);

        _logger.LogInformation("Access token refreshed for user {UserId}, expires at {ExpiresAt}", userId, expiresAt);

        return newAccessToken;
    }

    /// <summary>
    /// Gets the number of cached clients
    /// </summary>
    public int ClientCount => _clients.Count;

    /// <summary>
    /// Removes and disposes a client for a specific user.
    /// Call this when a user disconnects their Meraki account.
    /// </summary>
    public bool RemoveClientForUser(string userId)
    {
        _accessTokenCache.TryRemove(userId, out _);

        if (_clients.TryRemove(userId, out var entry))
        {
            _logger.LogInformation("Removing MerakiClient for user {UserId}", userId);
            entry.Client.Dispose();
            return true;
        }
        return false;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _logger.LogInformation("Disposing MerakiClientPool with {Count} clients", _clients.Count);

        foreach (var kvp in _clients)
        {
            try
            {
                kvp.Value.Client.Dispose();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error disposing MerakiClient for user {UserId}", kvp.Key);
            }
        }

        _clients.Clear();
        _accessTokenCache.Clear();
        _disposed = true;
    }
}
