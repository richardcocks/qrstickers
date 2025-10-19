using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QRStickers.Meraki;
using System.Security.Claims;

namespace QRStickers.Pages.Meraki;

[Authorize]
public class ConnectionModel : PageModel
{
    private readonly QRStickersDbContext _db;
    private readonly ILogger<ConnectionModel> _logger;

    public ConnectionModel(QRStickersDbContext db, ILogger<ConnectionModel> logger)
    {
        _db = db;
        _logger = logger;
    }

    public Connection? Connection { get; set; }
    public CachedOrganization? Organization { get; set; }
    public List<CachedNetwork> Networks { get; set; } = new();
    public Dictionary<string, int> DeviceCounts { get; set; } = new();
    public DateTime? LastSyncedAt { get; set; }

    public async Task<IActionResult> OnGetAsync(int? connectionId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            return RedirectToPage("/Identity/Account/Login");
        }

        // Get the specified connection, or redirect if not found
        if (!connectionId.HasValue)
        {
            return RedirectToPage("/Connections/Index");
        }

        Connection = await _db.Connections
            .OfType<MerakiConnection>()
            .FirstOrDefaultAsync(c => c.Id == connectionId.Value && c.UserId == userId);

        if (Connection == null)
        {
            _logger.LogWarning("Connection {ConnectionId} not found for user {UserId}", connectionId, userId);
            return RedirectToPage("/Connections/Index");
        }

        try
        {
            // Load the single organization for this connection
            Organization = await _db.CachedOrganizations
                .Where(o => o.ConnectionId == Connection.Id && !o.IsDeleted)
                .FirstOrDefaultAsync();

            if (Organization != null)
            {
                // Load networks for this connection
                Networks = await _db.CachedNetworks
                    .Where(n => n.ConnectionId == Connection.Id && !n.IsDeleted)
                    .OrderBy(n => n.Name)
                    .ToListAsync();

                // Get device counts from cache
                var deviceCounts = await _db.CachedDevices
                    .Where(d => d.ConnectionId == Connection.Id && !d.IsDeleted && d.NetworkId != null)
                    .GroupBy(d => d.NetworkId!)
                    .Select(g => new { NetworkId = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.NetworkId, x => x.Count);

                DeviceCounts = deviceCounts;
            }

            // Get last sync time
            var syncStatus = await _db.SyncStatuses.FirstOrDefaultAsync(s => s.ConnectionId == Connection.Id);
            LastSyncedAt = syncStatus?.LastSyncCompletedAt;

            _logger.LogInformation("Loaded connection details for {ConnectionName} (ID: {ConnectionId})",
                Connection.DisplayName, Connection.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving connection details for connection {ConnectionId}", connectionId);
        }

        return Page();
    }
}
