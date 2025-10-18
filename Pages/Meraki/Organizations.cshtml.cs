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
    public List<CachedOrganization>? Organizations { get; set; }
    public Dictionary<string, int> NetworkCounts { get; set; } = new();
    public DateTime? LastSyncedAt { get; set; }

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

            // Load organizations from cache (fast!)
            Organizations = await _db.CachedOrganizations
                .Where(o => o.UserId == userId && !o.IsDeleted)
                .OrderBy(o => o.Name)
                .ToListAsync();

            // Get network counts from cache
            var networkCounts = await _db.CachedNetworks
                .Where(n => n.UserId == userId && !n.IsDeleted)
                .GroupBy(n => n.OrganizationId)
                .Select(g => new { OrganizationId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.OrganizationId, x => x.Count);

            NetworkCounts = networkCounts;

            // Get last sync time
            var syncStatus = await _db.SyncStatuses.FindAsync(userId);
            LastSyncedAt = syncStatus?.LastSyncCompletedAt;

            _logger.LogInformation("Loaded {Count} organizations from cache for user {UserId}", Organizations.Count, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving organizations from cache");
            Organizations = null;
        }
    }
}
