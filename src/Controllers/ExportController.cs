using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using QRStickers.Models;
using QRStickers.Services;

namespace QRStickers.Controllers;

/// <summary>
/// Controller for PDF export operations
/// </summary>
[ApiController]
[Route("api/export/pdf")]
[Authorize]
public class ExportController : ControllerBase
{
    private readonly PdfExportService _pdfService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<ExportController> _logger;

    public ExportController(
        PdfExportService pdfService,
        UserManager<ApplicationUser> userManager,
        ILogger<ExportController> logger)
    {
        _pdfService = pdfService;
        _userManager = userManager;
        _logger = logger;
    }

    /// <summary>
    /// Bulk PDF export endpoint (Phase 5.5)
    /// Generates a PDF document containing multiple device stickers
    /// </summary>
    /// <param name="request">PDF export request containing images and layout settings</param>
    /// <returns>PDF file download</returns>
    [HttpPost("bulk")]
    public async Task<IActionResult> BulkPdfExport([FromBody] PdfExportRequest request)
    {
        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            // Validate request
            if (request.Images == null || !request.Images.Any())
                return BadRequest(new { error = "No images provided" });

            if (request.Images.Count > 100)
                return BadRequest(new { error = "Maximum 100 devices per PDF export" });

            // Generate PDF
            var pdfBytes = await _pdfService.GenerateBulkPdfAsync(request);

            // Generate filename
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var filename = $"devices-{request.Images.Count}-{timestamp}.pdf";

            // Return PDF file
            return File(pdfBytes, "application/pdf", filename);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[PDF Export] Error generating PDF: {ErrorMessage}", ex.Message);
            return Problem(detail: ex.Message, statusCode: 500);
        }
    }
}
