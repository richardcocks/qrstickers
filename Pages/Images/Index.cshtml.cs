using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QRStickers.Services;
using System.Security.Claims;

namespace QRStickers.Pages.Images;

[Authorize]
public class IndexModel : PageModel
{
    private readonly QRStickersDbContext _db;
    private readonly ImageUploadValidator _validator;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(QRStickersDbContext db, ImageUploadValidator validator, ILogger<IndexModel> logger)
    {
        _db = db;
        _validator = validator;
        _logger = logger;
    }

    public int ConnectionId { get; set; }
    public Connection Connection { get; set; } = null!;
    public List<UploadedImage> Images { get; set; } = new();
    public QuotaInfo Quota { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int? connectionId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            return RedirectToPage("/Identity/Account/Login");
        }

        // Get connection (first connection if not specified)
        var connection = connectionId.HasValue
            ? await _db.Connections.FirstOrDefaultAsync(c => c.Id == connectionId.Value && c.UserId == userId)
            : await _db.Connections.FirstOrDefaultAsync(c => c.UserId == userId);

        if (connection == null)
        {
            _logger.LogWarning("User {UserId} has no connections or connection {ConnectionId} not found",
                userId, connectionId);
            return RedirectToPage("/Connections/Create");
        }

        Connection = connection;
        ConnectionId = connection.Id;

        // Get images for this connection (excluding soft-deleted by default)
        Images = await _db.UploadedImages
            .Where(i => i.ConnectionId == connection.Id && !i.IsDeleted)
            .OrderByDescending(i => i.UploadedAt)
            .ToListAsync();

        // Get quota info
        Quota = await _validator.GetQuotaInfoAsync(connection.Id);

        return Page();
    }
}
