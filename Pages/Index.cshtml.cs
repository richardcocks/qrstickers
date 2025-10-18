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

    public bool HasMerakiToken { get; set; }
    public DateTime? RefreshTokenExpiresAt { get; set; }

    public async Task OnGetAsync()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId != null)
            {
                var token = await _db.OAuthTokens.FirstOrDefaultAsync(t => t.UserId == userId);
                HasMerakiToken = token != null;
                RefreshTokenExpiresAt = token?.RefreshTokenExpiresAt;
            }
        }
    }
}
