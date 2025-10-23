using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace QRStickers.Pages;

public class IndexModel : PageModel
{
    private readonly QRStickersDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public IndexModel(QRStickersDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    public List<Connection> Connections { get; set; } = new();
    public int ConnectionCount { get; set; }
    public string? DisplayName { get; set; }

    public async Task OnGetAsync()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId != null)
            {
                // Load user's display name
                var user = await _userManager.FindByIdAsync(userId);
                if (user != null)
                {
                    DisplayName = user.DisplayName ?? user.Email;
                }

                Connections = await _db.Connections
                    .Where(c => c.UserId == userId)
                    .OrderByDescending(c => c.CreatedAt)
                    .ToListAsync();

                ConnectionCount = Connections.Count;
            }
        }
    }
}
