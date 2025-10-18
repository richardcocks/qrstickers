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
    private readonly ILogger<CallbackModel> _logger;

    public CallbackModel(MerakiApiClient merakiClient, QRStickersDbContext db, ILogger<CallbackModel> logger)
    {
        _merakiClient = merakiClient;
        _db = db;
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

            // Get user identifier from authenticated Identity user
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (userId == null)
            {
                _logger.LogWarning("User ID not found in claims");
                Success = false;
                ErrorMessage = "User authentication error";
                return Page();
            }

            // Store or update token in database
            var existingToken = await _db.OAuthTokens.FirstOrDefaultAsync(t => t.UserId == userId);

            if (existingToken != null)
            {
                existingToken.AccessToken = accessToken;
                existingToken.RefreshToken = refreshToken;
                existingToken.ExpiresAt = DateTime.UtcNow.AddSeconds(expiresIn);
                existingToken.UpdatedAt = DateTime.UtcNow;
                _db.OAuthTokens.Update(existingToken);
            }
            else
            {
                var newToken = new OAuthToken
                {
                    UserId = userId,
                    AccessToken = accessToken,
                    RefreshToken = refreshToken,
                    ExpiresAt = DateTime.UtcNow.AddSeconds(expiresIn),
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _db.OAuthTokens.Add(newToken);
            }

            await _db.SaveChangesAsync();
            _logger.LogInformation("OAuth token stored successfully for user {userId}", userId);

            Success = true;
            return Page();
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
