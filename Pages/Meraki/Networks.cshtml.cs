using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QRStickers.Meraki;
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

    public int? ConnectionId { get; set; }
    public string? OrganizationId { get; set; }
    public List<CachedNetwork>? Networks { get; set; }
    public Dictionary<string, int> DeviceCounts { get; set; } = new();
    public DateTime? LastSyncedAt { get; set; }

    public async Task<IActionResult> OnGetAsync(int? connectionId, string? orgId)
    {
        if (string.IsNullOrEmpty(orgId))
        {
            return RedirectToPage("/Meraki/Organizations", new { connectionId });
        }

        ConnectionId = connectionId;
        OrganizationId = orgId;

        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return Page();
            }

            // Get connection (default to first if not specified)
            var connection = connectionId.HasValue
                ? await _db.Connections.OfType<MerakiConnection>().FirstOrDefaultAsync(c => c.Id == connectionId.Value && c.UserId == userId)
                : await _db.Connections.OfType<MerakiConnection>().FirstOrDefaultAsync(c => c.UserId == userId);

            if (connection == null)
            {
                return RedirectToPage("/Connections/Create");
            }

            ConnectionId = connection.Id;

            // Load networks from cache
            Networks = await _db.CachedNetworks
                .Where(n => n.ConnectionId == connection.Id && n.OrganizationId == orgId && !n.IsDeleted)
                .OrderBy(n => n.Name)
                .ToListAsync();

            // Get device counts from cache
            var deviceCounts = await _db.CachedDevices
                .Where(d => d.ConnectionId == connection.Id && !d.IsDeleted && d.NetworkId != null)
                .GroupBy(d => d.NetworkId!)
                .Select(g => new { NetworkId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.NetworkId, x => x.Count);

            DeviceCounts = deviceCounts;

            // Get last sync time
            var syncStatus = await _db.SyncStatuses.FirstOrDefaultAsync(s => s.ConnectionId == connection.Id);
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
