using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace QRStickers.Pages.Meraki;

[Authorize]
public class CallbackModel : PageModel
{
    private readonly MerakiApiClient _merakiClient;
    private readonly QRStickersDbContext _db;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CallbackModel> _logger;

    public CallbackModel(MerakiApiClient merakiClient, QRStickersDbContext db, IServiceProvider serviceProvider, ILogger<CallbackModel> logger)
    {
        _merakiClient = merakiClient;
        _db = db;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(string? code, string? state)
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

            // Build the redirect URI (should match what's configured in Meraki OAuth)
            var redirectUri = "https://qrstickers-htbteydbgjh0b9c4.uksouth-01.azurewebsites.net/Meraki/Callback";

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

            _logger.LogInformation("Token exchange successful. Access token length: {Length}, Expires in: {ExpiresIn} seconds",
                accessToken?.Length ?? 0, expiresIn);

            // Get user identifier from authenticated Identity user
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                _logger.LogWarning("User ID not found in claims");
                Success = false;
                ErrorMessage = "User authentication error";
                return Page();
            }

            if (string.IsNullOrEmpty(refreshToken))
            {
                _logger.LogError("No refresh token received from OAuth provider");
                Success = false;
                ErrorMessage = "Failed to obtain refresh token";
                return Page();
            }

            // Store or update ONLY refresh token in database
            // Access tokens are ephemeral and managed in-memory by MerakiClientPool
            var existingToken = await _db.OAuthTokens.FirstOrDefaultAsync(t => t.UserId == userId);

            if (existingToken != null)
            {
                existingToken.RefreshToken = refreshToken;
                existingToken.RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(90); // Meraki refresh tokens last 90 days
                existingToken.UpdatedAt = DateTime.UtcNow;
                _db.OAuthTokens.Update(existingToken);
                _logger.LogInformation("Updated refresh token for user {userId}, expires at {ExpiresAt}",
                    userId, existingToken.RefreshTokenExpiresAt);
            }
            else
            {
                var newToken = new OAuthToken
                {
                    UserId = userId,
                    RefreshToken = refreshToken,
                    RefreshTokenExpiresAt = DateTime.UtcNow.AddDays(90),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _db.OAuthTokens.Add(newToken);
                _logger.LogInformation("Created new refresh token for user {userId}, expires at {ExpiresAt}",
                    userId, newToken.RefreshTokenExpiresAt);
            }

            await _db.SaveChangesAsync();
            _logger.LogInformation("OAuth refresh token stored successfully for user {userId}", userId);

            // Trigger background sync (fire and forget)
            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var syncOrchestrator = scope.ServiceProvider.GetRequiredService<MerakiSyncOrchestrator>();
                    await syncOrchestrator.SyncUserDataAsync(userId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in background sync for user {UserId}", userId);
                }
            });

            Success = true;

            // Redirect to sync status page
            return RedirectToPage("/Meraki/SyncStatus");
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
