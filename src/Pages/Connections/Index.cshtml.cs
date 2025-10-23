using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace QRStickers.Pages.Connections;

[Authorize]
public class IndexModel : PageModel
{
    private readonly QRStickersDbContext _db;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(QRStickersDbContext db, ILogger<IndexModel> logger)
    {
        _db = db;
        _logger = logger;
    }

    public List<Connection> Connections { get; set; } = new();

    public async Task OnGetAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            return;
        }

        Connections = await _db.Connections
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        _logger.LogInformation("Loaded {Count} connections for user {UserId}", Connections.Count, userId);
    }
}
