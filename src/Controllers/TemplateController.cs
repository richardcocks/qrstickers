using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using QRStickers.Models;
using QRStickers.Services;

namespace QRStickers.Controllers;

/// <summary>
/// Controller for template matching operations
/// </summary>
[ApiController]
[Route("api/templates")]
[Authorize]
public class TemplateController : ControllerBase
{
    private readonly DeviceExportHelper _exportHelper;
    private readonly TemplateMatchingService _templateMatcher;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<TemplateController> _logger;

    public TemplateController(
        DeviceExportHelper exportHelper,
        TemplateMatchingService templateMatcher,
        UserManager<ApplicationUser> userManager,
        ILogger<TemplateController> logger)
    {
        _exportHelper = exportHelper;
        _templateMatcher = templateMatcher;
        _userManager = userManager;
        _logger = logger;
    }

    /// <summary>
    /// Find matching template for a device with alternate options
    /// </summary>
    /// <param name="deviceId">Device ID to match template for</param>
    /// <param name="connectionId">Connection ID the device belongs to</param>
    /// <returns>Matched template and list of alternate templates</returns>
    [HttpGet("match")]
    public async Task<IActionResult> MatchTemplate(
        [FromQuery] int deviceId,
        [FromQuery] int connectionId)
    {
        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            var exportData = await _exportHelper.GetDeviceExportDataAsync(deviceId, connectionId, user);
            var templateMatch = await _templateMatcher.FindTemplateForDeviceAsync(exportData.Device, user);
            var alternates = await _templateMatcher.GetAlternateTemplatesAsync(
                exportData.Device, user, templateMatch.Template.Id);

            return Ok(new
            {
                success = true,
                data = new
                {
                    matchedTemplate = new
                    {
                        id = templateMatch.Template.Id,
                        name = templateMatch.Template.Name,
                        matchReason = templateMatch.MatchReason,
                        confidence = templateMatch.Confidence
                    },
                    alternateTemplates = alternates.Select(t => new
                    {
                        id = t.Id,
                        name = t.Name
                    }).ToList()
                }
            });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (ArgumentException)
        {
            return NotFound();
        }
    }
}
