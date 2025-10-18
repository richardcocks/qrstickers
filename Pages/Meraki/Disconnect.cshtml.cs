using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace QRStickers.Pages.Meraki;

[Authorize]
public class DisconnectModel : PageModel
{
    private readonly QRStickersDbContext _db;
    private readonly ILogger<DisconnectModel> _logger;

    public DisconnectModel(QRStickersDbContext db, ILogger<DisconnectModel> logger)
    {
        _db = db;
        _logger = logger;
    }

    public bool Success { get; set; }

    public async Task OnGetAsync()
    {
        try
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (userId == null)
            {
                Success = false;
                return;
            }

            var token = await _db.OAuthTokens.FirstOrDefaultAsync(t => t.UserId == userId);

            if (token != null)
            {
                _db.OAuthTokens.Remove(token);
                await _db.SaveChangesAsync();
                _logger.LogInformation("OAuth token removed for user {userId}", userId);
                Success = true;
            }
            else
            {
                Success = false;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during OAuth disconnect");
            Success = false;
        }
    }
}
