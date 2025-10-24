using Microsoft.EntityFrameworkCore;
using QRStickers.Meraki;

namespace QRStickers;

/// <summary>
/// Scoped service that provides Meraki API access with automatic token refresh.
/// Handles OAuth token management per connection.
/// </summary>
public class MerakiService : IMerakiService
{
    private readonly int _connectionId;
    private readonly MerakiApiClient _apiClient;
    private readonly QRStickersDbContext _db;
    private readonly MerakiAccessTokenCache _tokenCache;
    private readonly ILogger<MerakiService> _logger;

    public MerakiService(
        int connectionId,
        MerakiApiClient apiClient,
        QRStickersDbContext db,
        MerakiAccessTokenCache tokenCache,
        ILogger<MerakiService> logger)
    {
        _connectionId = connectionId;
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
        if (_tokenCache.TryGetToken(_connectionId, out var cachedToken, out var expiresAt))
        {
            return cachedToken!;
        }

        // Need to get a fresh access token from refresh token
        var oauthToken = await _db.MerakiOAuthTokens.FirstOrDefaultAsync(t => t.ConnectionId == _connectionId);

        if (oauthToken == null)
        {
            throw new InvalidOperationException($"No OAuth token found for connection {_connectionId}");
        }

        // Check if refresh token is expired
        if (DateTime.UtcNow > oauthToken.RefreshTokenExpiresAt)
        {
            throw new InvalidOperationException($"Refresh token expired for connection {_connectionId}. User must re-authenticate.");
        }

        // Refresh the access token
        _logger.LogInformation("Refreshing access token for connection {ConnectionId}", _connectionId);
        var refreshResult = await _apiClient.RefreshAccessTokenAsync(oauthToken.RefreshToken);

        if (refreshResult == null)
        {
            throw new InvalidOperationException($"Failed to refresh access token for connection {_connectionId}");
        }

        var (newAccessToken, newRefreshToken, expiresIn) = refreshResult.Value;

        // Update refresh token in database if it changed (some OAuth providers rotate refresh tokens)
        if (newRefreshToken != null && newRefreshToken != oauthToken.RefreshToken)
        {
            _logger.LogInformation("Refresh token rotated for connection {ConnectionId}, updating database", _connectionId);
            oauthToken.RefreshToken = newRefreshToken;
            oauthToken.UpdatedAt = DateTime.UtcNow;
            _db.MerakiOAuthTokens.Update(oauthToken);
            await _db.SaveChangesAsync();
        }

        // Cache the new access token in singleton cache (persists across requests)
        var tokenExpiresAt = DateTime.UtcNow.AddSeconds(expiresIn);
        _tokenCache.CacheToken(_connectionId, newAccessToken, tokenExpiresAt);

        _logger.LogInformation("Access token refreshed for connection {ConnectionId}, expires at {ExpiresAt}", _connectionId, tokenExpiresAt);

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
