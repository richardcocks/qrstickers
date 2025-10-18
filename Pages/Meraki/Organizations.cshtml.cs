using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Meraki.Api.Data;

namespace QRStickers.Pages.Meraki;

[Authorize]
public class OrganizationsModel : PageModel
{
    private readonly MerakiApiClient _merakiClient;
    private readonly MerakiClientPool _clientPool;
    private readonly QRStickersDbContext _db;
    private readonly ILogger<OrganizationsModel> _logger;

    public OrganizationsModel(MerakiApiClient merakiClient, MerakiClientPool clientPool, QRStickersDbContext db, ILogger<OrganizationsModel> logger)
    {
        _merakiClient = merakiClient;
        _clientPool = clientPool;
        _db = db;
        _logger = logger;
    }

    public bool HasToken { get; set; }
    public List<Organization>? Organizations { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                HasToken = false;
                return;
            }

            var token = await _db.OAuthTokens.FirstOrDefaultAsync(t => t.UserId == userId);

            if (token == null)
            {
                HasToken = false;
                return;
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

            // Get organizations using pooled client
            var client = _clientPool.GetClient(token.AccessToken);
            Organizations = await client.Organizations.GetOrganizationsAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving organizations");
            Organizations = null;
        }
    }
}
