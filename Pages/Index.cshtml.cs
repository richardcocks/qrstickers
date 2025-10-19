using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace QRStickers.Pages;

public class IndexModel : PageModel
{
    private readonly QRStickersDbContext _db;

    public IndexModel(QRStickersDbContext db)
    {
        _db = db;
    }

    public List<Connection> Connections { get; set; } = new();
    public int ConnectionCount { get; set; }

    public async Task OnGetAsync()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId != null)
            {
                Connections = await _db.Connections
                    .Where(c => c.UserId == userId)
                    .OrderByDescending(c => c.CreatedAt)
                    .ToListAsync();

                ConnectionCount = Connections.Count;
            }
        }
    }
}
