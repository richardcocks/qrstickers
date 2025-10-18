using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace QRStickers.Pages.Meraki;

[Authorize]
public class OrganizationsModel : PageModel
{
    private readonly MerakiServiceFactory _merakiFactory;
    private readonly QRStickersDbContext _db;
    private readonly ILogger<OrganizationsModel> _logger;

    public OrganizationsModel(MerakiServiceFactory merakiFactory, QRStickersDbContext db, ILogger<OrganizationsModel> logger)
    {
        _merakiFactory = merakiFactory;
        _db = db;
        _logger = logger;
    }

    public bool HasToken { get; set; }
    public List<Organization>? Organizations { get; set; }
    public Dictionary<string, int> NetworkCounts { get; set; } = new();

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

            // Get organizations using MerakiService
            // The service automatically handles access token refresh
            var merakiService = _merakiFactory.CreateForUser(userId);
            Organizations = await merakiService.GetOrganizationsAsync();

            // Get network counts for each organization
            // This pre-warms the cache for faster network page loads
            if (Organizations != null && Organizations.Any())
            {
                foreach (var org in Organizations)
                {
                    try
                    {
                        var networks = await merakiService.GetNetworksAsync(org.Id);
                        NetworkCounts[org.Id] = networks?.Count ?? 0;
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error retrieving network count for organization {OrganizationId}", org.Id);
                        NetworkCounts[org.Id] = 0; // Default to 0 on error
                    }
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving organizations");
            Organizations = null;
        }
    }
}
