using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QRStickers.Meraki;
using QRStickers.Models;
using QRStickers.Services;
using System.Security.Claims;
using System.Text.Json;

namespace QRStickers.Pages.Meraki;

[Authorize]
public class NetworkModel : PageModel
{
    private readonly QRStickersDbContext _db;
    private readonly ILogger<NetworkModel> _logger;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public NetworkModel(
        QRStickersDbContext db,
        ILogger<NetworkModel> logger,
        IWebHostEnvironment webHostEnvironment)
    {
        _db = db;
        _logger = logger;
        _webHostEnvironment = webHostEnvironment;
    }

    public Connection? Connection { get; set; }
    public CachedNetwork? Network { get; set; }
    public List<CachedDevice> Devices { get; set; } = new();
    public HashSet<string> ProductTypesPresent { get; set; } = new();

    // Vite bundle path for export-shared (resolved from manifest.json)
    public string ExportSharedBundlePath { get; set; } = string.Empty;

    public async Task<IActionResult> OnGetAsync(int? connectionId, string? networkId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            return RedirectToPage("/Identity/Account/Login");
        }

        // Load Vite manifest to resolve bundle path
        try
        {
            var webRoot = _webHostEnvironment.WebRootPath;
            var manifestPath = Path.Combine(webRoot, "dist", ".vite", "manifest.json");

            if (!System.IO.File.Exists(manifestPath))
            {
                _logger.LogWarning("Vite manifest not found at {ManifestPath}, using fallback", manifestPath);
                ExportSharedBundlePath = "assets/export-shared-[hash].js"; // Fallback
            }
            else
            {
                var manifestJson = await System.IO.File.ReadAllTextAsync(manifestPath);
                var manifest = JsonSerializer.Deserialize<Dictionary<string, ViteManifestEntry>>(
                    manifestJson,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );

                if (manifest != null && manifest.TryGetValue("src/pages/export-shared/export-shared.entry.ts", out var entry))
                {
                    ExportSharedBundlePath = entry.File;
                    _logger.LogInformation("Export-shared bundle resolved to: {BundlePath}", ExportSharedBundlePath);
                }
                else
                {
                    _logger.LogWarning("Export-shared entry not found in Vite manifest");
                    ExportSharedBundlePath = "assets/export-shared-[hash].js"; // Fallback
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load Vite manifest");
            ExportSharedBundlePath = "assets/export-shared-[hash].js"; // Fallback
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
                LogSanitizer.Sanitize(Network.Name), Network.NetworkId, Devices.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving network details for network {NetworkId}", networkId);
        }

        return Page();
    }
}
