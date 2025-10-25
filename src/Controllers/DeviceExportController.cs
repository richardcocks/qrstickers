using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRStickers.Data;
using QRStickers.Meraki;
using QRStickers.Models;
using QRStickers.Services;

namespace QRStickers.Controllers;

/// <summary>
/// Controller for device export operations (single device and bulk exports)
/// </summary>
[ApiController]
[Route("api/export")]
[Authorize]
public class DeviceExportController : ControllerBase
{
    private readonly DeviceExportHelper _exportHelper;
    private readonly TemplateMatchingService _templateMatcher;
    private readonly TemplateService _templateService;
    private readonly QRStickersDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<DeviceExportController> _logger;

    public DeviceExportController(
        DeviceExportHelper exportHelper,
        TemplateMatchingService templateMatcher,
        TemplateService templateService,
        QRStickersDbContext db,
        UserManager<ApplicationUser> userManager,
        ILogger<DeviceExportController> logger)
    {
        _exportHelper = exportHelper;
        _templateMatcher = templateMatcher;
        _templateService = templateService;
        _db = db;
        _userManager = userManager;
        _logger = logger;
    }

    /// <summary>
    /// Get export data for a single device
    /// </summary>
    /// <param name="deviceId">Device ID to export</param>
    /// <param name="connectionId">Connection ID the device belongs to</param>
    /// <param name="includeAlternates">Include alternate template options</param>
    /// <returns>Device export data with matched template and optional alternates</returns>
    [HttpGet("device/{deviceId}")]
    public async Task<IActionResult> GetDeviceExport(
        int deviceId,
        [FromQuery] int connectionId,
        [FromQuery] bool includeAlternates)
    {
        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            var exportData = await _exportHelper.GetDeviceExportDataAsync(deviceId, connectionId, user);

            // Get template match
            var templateMatch = await _templateMatcher.FindTemplateForDeviceAsync(exportData.Device, user);
            exportData.MatchedTemplate = templateMatch.Template;

            // Get alternate templates if requested
            object? alternateTemplates = null;
            if (includeAlternates && !string.IsNullOrEmpty(exportData.Device.ProductType))
            {
                var filterResult = await _templateService.GetTemplatesForExportAsync(
                    connectionId,
                    exportData.Device.ProductType
                );

                alternateTemplates = DeviceExportHelper.BuildTemplateOptionsArray(filterResult, templateMatch.Template.Id);
            }

            return Ok(new
            {
                success = true,
                data = new
                {
                    device = new
                    {
                        id = exportData.Device.Id,
                        serial = exportData.Device.Serial,
                        name = exportData.Device.Name,
                        model = exportData.Device.Model,
                        productType = exportData.Device.ProductType,
                        networkId = exportData.Device.NetworkId,
                        connectionId = exportData.Device.ConnectionId,
                        qrCode = exportData.Device.QRCodeDataUri
                    },
                    network = exportData.Network != null ? new
                    {
                        id = exportData.Network.Id,
                        networkId = exportData.Network.NetworkId,
                        name = exportData.Network.Name,
                        organizationId = exportData.Network.OrganizationId,
                        qrCode = exportData.Network.QRCodeDataUri
                    } : null,
                    organization = exportData.Organization != null ? new
                    {
                        id = exportData.Organization.Id,
                        organizationId = exportData.Organization.OrganizationId,
                        name = exportData.Organization.Name,
                        url = exportData.Organization.Url,
                        qrCode = exportData.Organization.QRCodeDataUri
                    } : null,
                    connection = new
                    {
                        id = exportData.Connection.Id,
                        displayName = exportData.Connection.DisplayName,
                        type = exportData.Connection.GetType().Name
                    },
                    globalVariables = exportData.GlobalVariables,
                    uploadedImages = exportData.UploadedImages.Select(img => new
                    {
                        id = img.Id,
                        name = img.Name,
                        dataUri = img.DataUri,
                        widthPx = img.WidthPx,
                        heightPx = img.HeightPx
                    }).ToList(),
                    matchedTemplate = new
                    {
                        id = templateMatch.Template.Id,
                        name = templateMatch.Template.Name,
                        templateJson = templateMatch.Template.TemplateJson,
                        pageWidth = templateMatch.Template.PageWidth,
                        pageHeight = templateMatch.Template.PageHeight,
                        matchReason = templateMatch.MatchReason,
                        confidence = templateMatch.Confidence
                    },
                    alternateTemplates = alternateTemplates
                }
            });
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (ArgumentException ex)
        {
            return NotFound(new { error = ex.Message });
        }
        catch (Exception)
        {
            return StatusCode(500);
        }
    }

    /// <summary>
    /// Bulk device export data endpoint (Phase 6.1 - Performance Optimization)
    /// Fetches export data for multiple devices in a single request with reference-based deduplication
    /// </summary>
    /// <param name="request">Bulk export request containing device IDs and connection ID</param>
    /// <returns>Deduplicated export data for multiple devices</returns>
    [HttpPost("bulk-devices")]
    public async Task<IActionResult> BulkDeviceExport([FromBody] BulkDeviceExportRequest request)
    {
        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            // Validate request
            if (request.DeviceIds == null || request.DeviceIds.Length == 0)
                return BadRequest(new { error = "No device IDs provided" });

            if (request.DeviceIds.Length > 100)
                return BadRequest(new { error = "Maximum 100 devices per request" });

            _logger.LogInformation("[Bulk Export] Fetching data for {DeviceCount} devices, connection {ConnectionId}",
                request.DeviceIds.Length, request.ConnectionId);

            // Verify connection ownership
            var connection = await _db.Connections
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == request.ConnectionId && c.UserId == user.Id);

            if (connection == null)
                return NotFound(new { error = "Connection not found or access denied" });

            // Fetch all devices in a single query with eager loading
            var devices = await _db.CachedDevices
                .AsNoTracking()
                .Where(d => request.DeviceIds.Contains(d.Id) && d.ConnectionId == request.ConnectionId)
                .ToListAsync();

            if (devices.Count != request.DeviceIds.Length)
                return NotFound(new { error = "Some devices not found or not owned by connection" });

            // Fetch template matches for all devices (batch optimized)
            var templateMatches = await _templateMatcher.FindTemplatesForDevicesBatchAsync(
                devices,
                request.ConnectionId,
                user
            );

            // Group devices by ProductType and fetch templates once per type
            var devicesByProductType = devices
                .Where(d => !string.IsNullOrEmpty(d.ProductType))
                .GroupBy(d => d.ProductType!)
                .ToList();

            var templatesByProductType = new Dictionary<string, TemplateFilterResult>();
            foreach (var group in devicesByProductType)
            {
                var filterResult = await _templateService.GetTemplatesForExportAsync(
                    request.ConnectionId,
                    group.Key
                );
                templatesByProductType[group.Key] = filterResult;
            }

            // Fetch networks and organizations in bulk
            var networkIds = devices.Select(d => d.NetworkId).Distinct().ToList();
            var networks = await _db.CachedNetworks
                .AsNoTracking()
                .Where(n => networkIds.Contains(n.NetworkId) && n.ConnectionId == request.ConnectionId)
                .ToListAsync();

            var orgIds = networks.Select(n => n.OrganizationId).Distinct().ToList();
            var organizations = await _db.CachedOrganizations
                .AsNoTracking()
                .Where(o => orgIds.Contains(o.OrganizationId) && o.ConnectionId == request.ConnectionId)
                .ToListAsync();

            // Fetch global variables
            var globalVariables = await _db.GlobalVariables
                .AsNoTracking()
                .Where(gv => gv.ConnectionId == request.ConnectionId)
                .ToDictionaryAsync(gv => gv.VariableName, gv => gv.VariableValue);

            // Fetch uploaded images
            var uploadedImages = await _db.UploadedImages
                .AsNoTracking()
                .Where(i => i.ConnectionId == request.ConnectionId && !i.IsDeleted)
                .ToListAsync();

            // Build reference-based response
            var response = DeviceExportHelper.BuildBulkExportResponse(
                devices,
                templateMatches,
                templatesByProductType,
                networks,
                organizations,
                connection,
                globalVariables,
                uploadedImages
            );

            _logger.LogInformation("[Bulk Export] Successfully prepared data for {DeviceCount} devices", devices.Count);

            return Ok(new
            {
                success = true,
                data = response
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Bulk Export] Error fetching bulk device data: {ErrorMessage}", ex.Message);
            return Problem(detail: ex.Message, statusCode: 500);
        }
    }
}
