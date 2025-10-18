using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace QRStickers.Pages.Meraki;

[Authorize]
public class NetworksModel : PageModel
{
    private readonly MerakiServiceFactory _merakiFactory;
    private readonly QRStickersDbContext _db;
    private readonly ILogger<NetworksModel> _logger;

    public NetworksModel(MerakiServiceFactory merakiFactory, QRStickersDbContext db, ILogger<NetworksModel> logger)
    {
        _merakiFactory = merakiFactory;
        _db = db;
        _logger = logger;
    }

    public bool HasToken { get; set; }
    public string? OrganizationId { get; set; }
    public List<CachedNetwork>? Networks { get; set; }
    public Dictionary<string, int> DeviceCounts { get; set; } = new();
    public DateTime? LastSyncedAt { get; set; }

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

            // Load networks from cache (fast!)
            Networks = await _db.CachedNetworks
                .Where(n => n.UserId == userId && n.OrganizationId == orgId && !n.IsDeleted)
                .OrderBy(n => n.Name)
                .ToListAsync();

            // Get device counts from cache
            var deviceCounts = await _db.CachedDevices
                .Where(d => d.UserId == userId && !d.IsDeleted && d.NetworkId != null)
                .GroupBy(d => d.NetworkId!)
                .Select(g => new { NetworkId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.NetworkId, x => x.Count);

            DeviceCounts = deviceCounts;

            // Get last sync time
            var syncStatus = await _db.SyncStatuses.FindAsync(userId);
            LastSyncedAt = syncStatus?.LastSyncCompletedAt;

            _logger.LogInformation("Loaded {Count} networks from cache for organization {OrgId}", Networks.Count, orgId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving networks from cache for organization {OrganizationId}", orgId);
            Networks = null;
        }

        return Page();
    }
}
