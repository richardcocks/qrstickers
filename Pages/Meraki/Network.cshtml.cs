using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QRStickers.Meraki;
using System.Security.Claims;

namespace QRStickers.Pages.Meraki;

[Authorize]
public class NetworkModel : PageModel
{
    private readonly QRStickersDbContext _db;
    private readonly ILogger<NetworkModel> _logger;

    public NetworkModel(QRStickersDbContext db, ILogger<NetworkModel> logger)
    {
        _db = db;
        _logger = logger;
    }

    public Connection? Connection { get; set; }
    public CachedNetwork? Network { get; set; }
    public List<CachedDevice> Devices { get; set; } = new();
    public HashSet<string> ProductTypesPresent { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int? connectionId, string? networkId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            return RedirectToPage("/Identity/Account/Login");
        }

        // Validate parameters
        if (!connectionId.HasValue || string.IsNullOrEmpty(networkId))
        {
            return RedirectToPage("/Connections/Index");
        }

        // Get the connection
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
            // Load the network for this connection
            Network = await _db.CachedNetworks
                .Where(n => n.ConnectionId == Connection.Id && n.NetworkId == networkId && !n.IsDeleted)
                .FirstOrDefaultAsync();

            if (Network == null)
            {
                _logger.LogWarning("Network {NetworkId} not found for connection {ConnectionId}", networkId, connectionId);
                return RedirectToPage("/Meraki/Connection", new { connectionId = connectionId });
            }

            // Load devices for this network
            Devices = await _db.CachedDevices
                .Where(d => d.ConnectionId == Connection.Id && d.NetworkId == networkId && !d.IsDeleted)
                .OrderBy(d => d.Name)
                .ToListAsync();

            // Get product types present in devices
            ProductTypesPresent = Devices
                .Where(d => d.ProductType != null)
                .Select(d => d.ProductType!)
                .Distinct()
                .ToHashSet();

            _logger.LogInformation("Loaded network details for {NetworkName} (ID: {NetworkId}) with {DeviceCount} devices",
                Network.Name, Network.NetworkId, Devices.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving network details for network {NetworkId}", networkId);
        }

        return Page();
    }
}
