using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QRStickers.Meraki;
using QRStickers.Services;
using System.Security.Claims;

namespace QRStickers.Pages.Connections;

[Authorize]
public class DeleteModel : PageModel
{
    private readonly QRStickersDbContext _db;
    private readonly MerakiAccessTokenCache _tokenCache;
    private readonly ILogger<DeleteModel> _logger;

    public DeleteModel(QRStickersDbContext db, MerakiAccessTokenCache tokenCache, ILogger<DeleteModel> logger)
    {
        _db = db;
        _tokenCache = tokenCache;
        _logger = logger;
    }

    public Connection? Connection { get; set; }

    public async Task<IActionResult> OnGetAsync(int? connectionId)
    {
        if (!connectionId.HasValue)
        {
            return RedirectToPage("/Connections/Index");
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            return RedirectToPage("/Identity/Account/Login");
        }

        Connection = await _db.Connections
            .FirstOrDefaultAsync(c => c.Id == connectionId.Value && c.UserId == userId);

        if (Connection == null)
        {
            return NotFound();
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int? connectionId)
    {
        if (!connectionId.HasValue)
        {
            return RedirectToPage("/Connections/Index");
        }

        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            return RedirectToPage("/Identity/Account/Login");
        }

        var connection = await _db.Connections
            .FirstOrDefaultAsync(c => c.Id == connectionId.Value && c.UserId == userId);

        if (connection == null)
        {
            return NotFound();
        }

        try
        {
            // Remove from access token cache if present
            _tokenCache.RemoveToken(connection.Id);

            // Delete connection (cascade will delete related OAuth tokens, cached data, sync status)
            _db.Connections.Remove(connection);
            await _db.SaveChangesAsync();

            _logger.LogInformation("Deleted connection {ConnectionId} ({DisplayName}) for user {UserId}",
                connection.Id, LogSanitizer.Sanitize(connection.DisplayName), userId);

            return RedirectToPage("/Connections/Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting connection {ConnectionId}", connectionId);
            ModelState.AddModelError(string.Empty, "An error occurred while deleting the connection.");

            Connection = connection;
            return Page();
        }
    }
}
