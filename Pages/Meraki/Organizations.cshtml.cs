using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace QRStickers.Pages.Meraki;

[Authorize]
public class OrganizationsModel : PageModel
{
    private readonly QRStickersDbContext _db;
    private readonly ILogger<OrganizationsModel> _logger;

    public OrganizationsModel(QRStickersDbContext db, ILogger<OrganizationsModel> logger)
    {
        _db = db;
        _logger = logger;
    }

    public List<Connection> UserConnections { get; set; } = new();
    public Connection? SelectedConnection { get; set; }
    public List<CachedOrganization>? Organizations { get; set; }
    public Dictionary<string, int> NetworkCounts { get; set; } = new();
    public DateTime? LastSyncedAt { get; set; }

    public async Task OnGetAsync(int? connectionId)
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                return;
            }

            // Load all user's Meraki connections
            UserConnections = await _db.Connections
                .OfType<MerakiConnection>()
                .Where(c => c.UserId == userId)
                .OrderBy(c => c.DisplayName)
                .Cast<Connection>()
                .ToListAsync();

            if (!UserConnections.Any())
            {
                return;
            }

            // Select connection: use specified connectionId, or default to first active connection
            if (connectionId.HasValue)
            {
                SelectedConnection = UserConnections.FirstOrDefault(c => c.Id == connectionId.Value);
            }

            SelectedConnection ??= UserConnections.FirstOrDefault(c => c.IsActive) ?? UserConnections.First();

            // Load organizations from cache for selected connection
            Organizations = await _db.CachedOrganizations
                .Where(o => o.ConnectionId == SelectedConnection.Id && !o.IsDeleted)
                .OrderBy(o => o.Name)
                .ToListAsync();

            // Get network counts from cache
            var networkCounts = await _db.CachedNetworks
                .Where(n => n.ConnectionId == SelectedConnection.Id && !n.IsDeleted)
                .GroupBy(n => n.OrganizationId)
                .Select(g => new { OrganizationId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.OrganizationId, x => x.Count);

            NetworkCounts = networkCounts;

            // Get last sync time
            var syncStatus = await _db.SyncStatuses
                .FirstOrDefaultAsync(s => s.ConnectionId == SelectedConnection.Id);
            LastSyncedAt = syncStatus?.LastSyncCompletedAt;

            _logger.LogInformation("Loaded {Count} organizations from cache for connection {ConnectionId}",
                Organizations.Count, SelectedConnection.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving organizations from cache");
            Organizations = null;
        }
    }
}
