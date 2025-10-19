using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Claims;
using System.Text.Json;
using QRStickers.Meraki;

namespace QRStickers.Pages.Meraki;

[Authorize]
public class CallbackModel : PageModel
{
    private readonly MerakiApiClient _merakiClient;
    private readonly QRStickersDbContext _db;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CallbackModel> _logger;
    private readonly IMemoryCache _cache;

    public CallbackModel(MerakiApiClient merakiClient, QRStickersDbContext db, IServiceProvider serviceProvider, ILogger<CallbackModel> logger, IMemoryCache cache)
    {
        _merakiClient = merakiClient;
        _db = db;
        _serviceProvider = serviceProvider;
        _logger = logger;
        _cache = cache;
    }

    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public int? ConnectionId { get; set; }

    public async Task<IActionResult> OnGetAsync(string? code, string? state, string? nonce)
    {
        try
        {
            if (string.IsNullOrEmpty(code))
            {
                _logger.LogWarning("OAuth redirect received with no code");
                Success = false;
                ErrorMessage = "Authorization code is missing";
                return Page();
            }

            // Get user identifier from authenticated Identity user
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                _logger.LogWarning("User ID not found in claims");
                Success = false;
                ErrorMessage = "User authentication error";
                return Page();
            }

            // Parse state parameter to get displayName and nonce
            string displayName = "My Meraki Connection";
            string? stateNonce = null;

            if (!string.IsNullOrEmpty(state))
            {
                try
                {
                    var stateObj = JsonSerializer.Deserialize<JsonElement>(state);
                    if (stateObj.TryGetProperty("displayName", out var displayNameElement))
                    {
                        displayName = displayNameElement.GetString() ?? displayName;
                    }
                    if (stateObj.TryGetProperty("nonce", out var nonceElement))
                    {
                        stateNonce = nonceElement.GetString();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to parse state parameter, using default display name");
                }
            }

            // CSRF Protection: Validate nonce from state parameter
            if (string.IsNullOrEmpty(stateNonce))
            {
                _logger.LogWarning("OAuth callback received with missing nonce in state parameter. Possible CSRF attack.");
                Success = false;
                ErrorMessage = "Invalid OAuth state. Please try connecting again.";
                return Page();
            }

            if (!_cache.TryGetValue($"oauth_nonce_{stateNonce}", out _))
            {
                _logger.LogWarning("OAuth callback received with invalid or expired nonce. Possible CSRF attack.");
                Success = false;
                ErrorMessage = "Invalid or expired OAuth state. Please try connecting again.";
                return Page();
            }

            // Remove nonce from cache (single-use enforcement)
            _cache.Remove($"oauth_nonce_{stateNonce}");

            // Build the redirect URI (should match what's configured in Meraki OAuth)
            var redirectUri = Url.PageLink("/Meraki/Callback")!;

            // Exchange code for token
            var tokenResult = await _merakiClient.ExchangeCodeForTokenAsync(code, redirectUri);

            if (tokenResult == null)
            {
                _logger.LogWarning("Failed to exchange authorization code for token");
                Success = false;
                ErrorMessage = "Failed to exchange authorization code for access token";
                return Page();
            }

            var (accessToken, refreshToken, expiresIn) = tokenResult.Value;

            if (string.IsNullOrEmpty(refreshToken))
            {
                _logger.LogError("No refresh token received from OAuth provider");
                Success = false;
                ErrorMessage = "Failed to obtain refresh token";
                return Page();
            }

            // Create new Meraki connection
            var connection = new MerakiConnection
            {
                UserId = userId,
                DisplayName = displayName,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _db.Connections.Add(connection);
            await _db.SaveChangesAsync(); // Save to get connection ID

            _logger.LogInformation("Created new Meraki connection {ConnectionId} for user {UserId} with display name '{DisplayName}'",
                connection.Id, userId, displayName);

            // Store refresh token linked to connection
            var oauthToken = new MerakiOAuthToken
            {
                ConnectionId = connection.Id,
                RefreshToken = refreshToken,
                RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(90), // Meraki refresh tokens last 90 days
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _db.MerakiOAuthTokens.Add(oauthToken);
            await _db.SaveChangesAsync();

            ConnectionId = connection.Id;

            // Trigger background sync (fire and forget)
            var connectionId = connection.Id;
            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var syncOrchestrator = scope.ServiceProvider.GetRequiredService<MerakiSyncOrchestrator>();
                    await syncOrchestrator.SyncConnectionDataAsync(connectionId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in background sync for connection {ConnectionId}", connectionId);
                }
            });

            Success = true;

            // Redirect to sync status page with connectionId
            return RedirectToPage("/Meraki/SyncStatus", new { connectionId = connection.Id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in OAuth redirect handler");
            Success = false;
            ErrorMessage = "An error occurred while connecting your Meraki account";
            return Page();
        }
    }
}
