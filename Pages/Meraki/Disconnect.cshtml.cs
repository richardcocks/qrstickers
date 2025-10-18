using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace QRStickers.Pages.Meraki;

[Authorize]
public class DisconnectModel : PageModel
{
    private readonly QRStickersDbContext _db;
    private readonly AccessTokenCache _tokenCache;
    private readonly ILogger<DisconnectModel> _logger;

    public DisconnectModel(QRStickersDbContext db, AccessTokenCache tokenCache, ILogger<DisconnectModel> logger)
    {
        _db = db;
        _tokenCache = tokenCache;
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
                // Delete all cached Meraki data (in order of foreign key dependencies)

                // 1. Delete cached devices
                var devicesDeleted = await _db.CachedDevices
                    .Where(d => d.UserId == userId)
                    .ExecuteDeleteAsync();

                // 2. Delete cached networks
                var networksDeleted = await _db.CachedNetworks
                    .Where(n => n.UserId == userId)
                    .ExecuteDeleteAsync();

                // 3. Delete cached organizations
                var orgsDeleted = await _db.CachedOrganizations
                    .Where(o => o.UserId == userId)
                    .ExecuteDeleteAsync();

                // 4. Delete sync status
                var syncStatus = await _db.SyncStatuses.FindAsync(userId);
                if (syncStatus != null)
                {
                    _db.SyncStatuses.Remove(syncStatus);
                    await _db.SaveChangesAsync();
                }

                // 5. Remove OAuth token from database
                _db.OAuthTokens.Remove(token);
                await _db.SaveChangesAsync();

                // 6. Clear cached access token
                _tokenCache.RemoveToken(userId);

                _logger.LogInformation(
                    "Meraki account disconnected for user {userId}. Deleted: {orgs} organizations, {networks} networks, {devices} devices, sync status, OAuth token",
                    userId, orgsDeleted, networksDeleted, devicesDeleted);
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
