using Microsoft.EntityFrameworkCore;

namespace QRStickers;

/// <summary>
/// Scoped service that provides Meraki API access with automatic token refresh.
/// Handles OAuth token management per request.
/// </summary>
public class MerakiService
{
    private readonly string _userId;
    private readonly MerakiApiClient _apiClient;
    private readonly QRStickersDbContext _db;
    private readonly AccessTokenCache _tokenCache;
    private readonly ILogger<MerakiService> _logger;

    public MerakiService(
        string userId,
        MerakiApiClient apiClient,
        QRStickersDbContext db,
        AccessTokenCache tokenCache,
        ILogger<MerakiService> logger)
    {
        _userId = userId ?? throw new ArgumentNullException(nameof(userId));
        _apiClient = apiClient ?? throw new ArgumentNullException(nameof(apiClient));
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _tokenCache = tokenCache ?? throw new ArgumentNullException(nameof(tokenCache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets a valid access token, refreshing if necessary
    /// </summary>
    private async Task<string> GetAccessTokenAsync()
    {
        // Check singleton cache first (persists across requests)
        if (_tokenCache.TryGetToken(_userId, out var cachedToken, out var expiresAt))
        {
            return cachedToken!;
        }

        // Need to get a fresh access token from refresh token
        var oauthToken = await _db.OAuthTokens.FirstOrDefaultAsync(t => t.UserId == _userId);

        if (oauthToken == null)
        {
            throw new InvalidOperationException($"No OAuth token found for user {_userId}");
        }

        // Check if refresh token is expired
        if (DateTime.UtcNow > oauthToken.RefreshTokenExpiresAt)
        {
            throw new InvalidOperationException($"Refresh token expired for user {_userId}. User must re-authenticate.");
        }

        // Refresh the access token
        _logger.LogInformation("Refreshing access token for user {UserId}", _userId);
        var refreshResult = await _apiClient.RefreshAccessTokenAsync(oauthToken.RefreshToken);

        if (refreshResult == null)
        {
            throw new InvalidOperationException($"Failed to refresh access token for user {_userId}");
        }

        var (newAccessToken, newRefreshToken, expiresIn) = refreshResult.Value;

        // Update refresh token in database if it changed (some OAuth providers rotate refresh tokens)
        if (newRefreshToken != null && newRefreshToken != oauthToken.RefreshToken)
        {
            _logger.LogInformation("Refresh token rotated for user {UserId}, updating database", _userId);
            oauthToken.RefreshToken = newRefreshToken;
            oauthToken.UpdatedAt = DateTime.UtcNow;
            _db.OAuthTokens.Update(oauthToken);
            await _db.SaveChangesAsync();
        }

        // Cache the new access token in singleton cache (persists across requests)
        var tokenExpiresAt = DateTime.UtcNow.AddSeconds(expiresIn);
        _tokenCache.CacheToken(_userId, newAccessToken, tokenExpiresAt);

        _logger.LogInformation("Access token refreshed for user {UserId}, expires at {ExpiresAt}", _userId, tokenExpiresAt);

        return newAccessToken;
    }

    /// <summary>
    /// Get organizations for the authenticated user
    /// </summary>
    public async Task<List<Organization>?> GetOrganizationsAsync()
    {
        var accessToken = await GetAccessTokenAsync();
        return await _apiClient.GetOrganizationsAsync(accessToken);
    }

    /// <summary>
    /// Get networks for a specific organization
    /// </summary>
    public async Task<List<Network>?> GetNetworksAsync(string organizationId)
    {
        var accessToken = await GetAccessTokenAsync();
        return await _apiClient.GetNetworksAsync(accessToken, organizationId);
    }

    /// <summary>
    /// Get devices for a specific organization
    /// </summary>
    public async Task<List<Device>?> GetOrganizationDevicesAsync(string organizationId)
    {
        var accessToken = await GetAccessTokenAsync();
        return await _apiClient.GetOrganizationDevicesAsync(accessToken, organizationId);
    }
}
