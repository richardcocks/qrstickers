using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRStickers.Data;
using QRStickers.Models;
using QRStickers.Services;

namespace QRStickers.Controllers;

/// <summary>
/// Controller for usage tracking operations
/// </summary>
[ApiController]
[Route("api/usage")]
[Authorize]
public class UsageController : ControllerBase
{
    private readonly DeviceExportHelper _exportHelper;
    private readonly QRStickersDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<UsageController> _logger;

    public UsageController(
        DeviceExportHelper exportHelper,
        QRStickersDbContext db,
        UserManager<ApplicationUser> userManager,
        ILogger<UsageController> logger)
    {
        _exportHelper = exportHelper;
        _db = db;
        _userManager = userManager;
        _logger = logger;
    }

    /// <summary>
    /// Track usage of templates and images (updates LastUsedAt timestamps)
    /// </summary>
    /// <param name="request">Usage tracking request containing template ID and image IDs</param>
    /// <returns>Success status (never fails to avoid blocking exports)</returns>
    [HttpPost("track")]
    public async Task<IActionResult> TrackUsage([FromBody] UsageTrackRequest request)
    {
        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            // Track image usage (updates LastUsedAt for all referenced images)
            if (request.ImageIds?.Length > 0)
            {
                await _exportHelper.TrackImageUsageAsync(request.ImageIds);
                _logger.LogDebug("[Usage Tracking] Updated LastUsedAt for {Count} images", request.ImageIds.Length);
            }

            // Track template usage
            if (request.TemplateId > 0)
            {
                var template = await _db.StickerTemplates
                    .FirstOrDefaultAsync(t => t.Id == request.TemplateId);

                if (template != null)
                {
                    template.LastUsedAt = DateTime.UtcNow;
                    await _db.SaveChangesAsync();
                    _logger.LogDebug("[Usage Tracking] Updated LastUsedAt for template {TemplateId}", request.TemplateId);
                }
            }

            return Ok(new { success = true });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Usage Tracking] Error tracking usage");
            // Silent failure - don't block exports
            return Ok(new { success = false, error = ex.Message });
        }
    }
}
