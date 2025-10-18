using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace QRStickers.Pages.Meraki;

[Authorize]
public class NetworksModel : PageModel
{
    private readonly MerakiApiClient _merakiClient;
    private readonly QRStickersDbContext _db;
    private readonly ILogger<NetworksModel> _logger;

    public NetworksModel(MerakiApiClient merakiClient, QRStickersDbContext db, ILogger<NetworksModel> logger)
    {
        _merakiClient = merakiClient;
        _db = db;
        _logger = logger;
    }

    public bool HasToken { get; set; }
    public string? OrganizationId { get; set; }
    public List<Network>? Networks { get; set; }
    public Dictionary<string, int> DeviceCounts { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(string? orgId)
    {
        if (string.IsNullOrEmpty(orgId))
        {
            return RedirectToPage("/Meraki/Organizations");
        }

        OrganizationId = orgId;

        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                HasToken = false;
                return Page();
            }

            var token = await _db.OAuthTokens.FirstOrDefaultAsync(t => t.UserId == userId);

            if (token == null)
            {
                HasToken = false;
                return Page();
            }

            HasToken = true;

            // Check if token is expired and refresh if needed
            if (DateTime.UtcNow > token.ExpiresAt && token.RefreshToken != null)
            {
                var refreshResult = await _merakiClient.RefreshAccessTokenAsync(token.RefreshToken);
                if (refreshResult != null)
                {
                    var (newAccessToken, newRefreshToken, newExpiresIn) = refreshResult.Value;
                    token.AccessToken = newAccessToken;
                    token.RefreshToken = newRefreshToken;
                    token.ExpiresAt = DateTime.UtcNow.AddSeconds(newExpiresIn);
                    token.UpdatedAt = DateTime.UtcNow;
                    _db.OAuthTokens.Update(token);
                    await _db.SaveChangesAsync();
                    _logger.LogInformation("Token refreshed for user {userId}", userId);
                }
            }

            // Get networks for this organization
            Networks = await _merakiClient.GetNetworksAsync(token.AccessToken, orgId);

            // Get devices for this organization to calculate counts per network
            var devices = await _merakiClient.GetOrganizationDevicesAsync(token.AccessToken, orgId);
            if (devices != null)
            {
                DeviceCounts = devices
                    .Where(d => d.NetworkId != null)
                    .GroupBy(d => d.NetworkId!)
                    .ToDictionary(g => g.Key, g => g.Count());
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving networks for organization {OrganizationId}", orgId);
            Networks = null;
        }

        return Page();
    }
}
