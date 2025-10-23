using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace QRStickers.Pages.Templates;

[Authorize]
public class ConnectionDefaultsModel : PageModel
{
    private readonly QRStickersDbContext _db;
    private readonly ILogger<ConnectionDefaultsModel> _logger;

    public ConnectionDefaultsModel(QRStickersDbContext db, ILogger<ConnectionDefaultsModel> logger)
    {
        _db = db;
        _logger = logger;
    }

    public List<Connection> UserConnections { get; set; } = new();
    public List<StickerTemplate> AvailableTemplates { get; set; } = new();
    public Connection? SelectedConnection { get; set; }

    [BindProperty]
    public int SelectedConnectionId { get; set; }

    [BindProperty]
    public int? SwitchTemplateId { get; set; }

    [BindProperty]
    public int? ApplianceTemplateId { get; set; }

    [BindProperty]
    public int? WirelessTemplateId { get; set; }

    [BindProperty]
    public int? CameraTemplateId { get; set; }

    [BindProperty]
    public int? SensorTemplateId { get; set; }

    [BindProperty]
    public int? CellularGatewayTemplateId { get; set; }

    [BindProperty]
    public string? ReturnTo { get; set; }

    public async Task<IActionResult> OnGetAsync(int? connectionId, string? returnTo)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            return Unauthorized();
        }

        // Load user connections
        UserConnections = await _db.Connections
            .Where(c => c.UserId == userId)
            .OrderBy(c => c.DisplayName)
            .ToListAsync();

        if (UserConnections.Count == 0)
        {
            TempData["ErrorMessage"] = "You must have at least one connection to manage defaults.";
            return RedirectToPage("/Connections/Index");
        }

        // Determine selected connection
        if (connectionId.HasValue)
        {
            SelectedConnection = UserConnections.FirstOrDefault(c => c.Id == connectionId.Value);
            if (SelectedConnection == null)
            {
                TempData["ErrorMessage"] = "Connection not found or access denied.";
                return RedirectToPage("/Templates/Index");
            }
        }
        else
        {
            // Default to first connection
            SelectedConnection = UserConnections.First();
        }

        SelectedConnectionId = SelectedConnection.Id;
        ReturnTo = returnTo;

        // Load available templates (system + connection-specific)
        AvailableTemplates = await _db.StickerTemplates
            .Where(t => t.IsSystemTemplate || t.ConnectionId == SelectedConnectionId)
            .OrderByDescending(t => t.IsSystemTemplate)
            .ThenBy(t => t.Name)
            .ToListAsync();

        // Load existing defaults
        var defaults = await _db.ConnectionDefaultTemplates
            .Where(d => d.ConnectionId == SelectedConnectionId)
            .ToListAsync();

        // Populate form fields
        SwitchTemplateId = defaults.FirstOrDefault(d => d.ProductType == "switch")?.TemplateId;
        ApplianceTemplateId = defaults.FirstOrDefault(d => d.ProductType == "appliance")?.TemplateId;
        WirelessTemplateId = defaults.FirstOrDefault(d => d.ProductType == "wireless")?.TemplateId;
        CameraTemplateId = defaults.FirstOrDefault(d => d.ProductType == "camera")?.TemplateId;
        SensorTemplateId = defaults.FirstOrDefault(d => d.ProductType == "sensor")?.TemplateId;
        CellularGatewayTemplateId = defaults.FirstOrDefault(d => d.ProductType == "cellularGateway")?.TemplateId;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            return Unauthorized();
        }

        // Verify connection ownership
        var connection = await _db.Connections
            .FirstOrDefaultAsync(c => c.Id == SelectedConnectionId && c.UserId == userId);

        if (connection == null)
        {
            TempData["ErrorMessage"] = "Connection not found or access denied.";
            return RedirectToPage("/Templates/Index");
        }

        // Load existing defaults
        var existingDefaults = await _db.ConnectionDefaultTemplates
            .Where(d => d.ConnectionId == SelectedConnectionId)
            .ToListAsync();

        // Update or create defaults for each ProductType
        await UpdateOrCreateDefaultAsync(existingDefaults, "switch", SwitchTemplateId);
        await UpdateOrCreateDefaultAsync(existingDefaults, "appliance", ApplianceTemplateId);
        await UpdateOrCreateDefaultAsync(existingDefaults, "wireless", WirelessTemplateId);
        await UpdateOrCreateDefaultAsync(existingDefaults, "camera", CameraTemplateId);
        await UpdateOrCreateDefaultAsync(existingDefaults, "sensor", SensorTemplateId);
        await UpdateOrCreateDefaultAsync(existingDefaults, "cellularGateway", CellularGatewayTemplateId);

        await _db.SaveChangesAsync();

        _logger.LogInformation("User {UserId} updated default templates for connection {ConnectionId}",
            userId, SelectedConnectionId);

        TempData["SuccessMessage"] = $"Template defaults updated successfully for '{connection.DisplayName}'!";
        return RedirectToPage("/Templates/ConnectionDefaults", new { connectionId = SelectedConnectionId, returnTo = ReturnTo });
    }

    public async Task<IActionResult> OnPostUpdateDefaultAsync(int connectionId, string productType, int? templateId, string? returnTo)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            return new JsonResult(new { success = false, message = "Unauthorized" }) { StatusCode = 401 };
        }

        // Verify connection ownership
        var connection = await _db.Connections
            .FirstOrDefaultAsync(c => c.Id == connectionId && c.UserId == userId);

        if (connection == null)
        {
            return new JsonResult(new { success = false, message = "Connection not found or access denied" }) { StatusCode = 403 };
        }

        // Load existing defaults
        var existingDefaults = await _db.ConnectionDefaultTemplates
            .Where(d => d.ConnectionId == connectionId)
            .ToListAsync();

        // Update or create default for this ProductType
        await UpdateOrCreateDefaultAsync(existingDefaults, productType, templateId);
        await _db.SaveChangesAsync();

        _logger.LogInformation("User {UserId} updated default template for {ProductType} on connection {ConnectionId}",
            userId, productType, connectionId);

        return new JsonResult(new { success = true, message = "Default saved successfully" });
    }

    private async Task UpdateOrCreateDefaultAsync(List<ConnectionDefaultTemplate> existingDefaults, string productType, int? templateId)
    {
        var existing = existingDefaults.FirstOrDefault(d => d.ProductType == productType);

        if (existing != null)
        {
            // Update existing
            existing.TemplateId = templateId;
            existing.UpdatedAt = DateTime.UtcNow;
        }
        else
        {
            // Create new
            _db.ConnectionDefaultTemplates.Add(new ConnectionDefaultTemplate
            {
                ConnectionId = SelectedConnectionId,
                ProductType = productType,
                TemplateId = templateId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }
    }
}
