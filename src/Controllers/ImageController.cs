using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRStickers.Data;
using QRStickers.Models;
using QRStickers.Services;

namespace QRStickers.Controllers;

/// <summary>
/// Controller for image upload and management operations (Phase 6.1)
/// </summary>
[ApiController]
[Route("api/images")]
[Authorize]
public class ImageController : ControllerBase
{
    private readonly ImageUploadValidator _validator;
    private readonly QRStickersDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<ImageController> _logger;

    public ImageController(
        ImageUploadValidator validator,
        QRStickersDbContext db,
        UserManager<ApplicationUser> userManager,
        ILogger<ImageController> logger)
    {
        _validator = validator;
        _db = db;
        _userManager = userManager;
        _logger = logger;
    }

    /// <summary>
    /// Upload a new image for use in sticker templates
    /// </summary>
    /// <param name="request">Image upload request containing data URI and metadata</param>
    /// <returns>Uploaded image details</returns>
    [HttpPost("upload")]
    public async Task<IActionResult> UploadImage([FromBody] ImageUploadRequest request)
    {
        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            // Validate upload
            var validationResult = await _validator.ValidateUploadAsync(
                request.ConnectionId,
                request.Name,
                request.DataUri,
                request.WidthPx,
                request.HeightPx,
                user.Id);

            if (!validationResult.IsValid)
            {
                return BadRequest(new { success = false, error = validationResult.ErrorMessage });
            }

            // Create uploaded image entity
            var uploadedImage = new UploadedImage
            {
                ConnectionId = request.ConnectionId,
                Name = request.Name,
                Description = request.Description,
                DataUri = request.DataUri,
                WidthPx = request.WidthPx,
                HeightPx = request.HeightPx,
                MimeType = validationResult.MimeType!,
                FileSizeBytes = validationResult.FileSizeBytes,
                IsDeleted = false,
                UploadedAt = DateTime.UtcNow
            };

            _db.UploadedImages.Add(uploadedImage);
            await _db.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                data = new
                {
                    id = uploadedImage.Id,
                    name = uploadedImage.Name,
                    dataUri = uploadedImage.DataUri,
                    widthPx = uploadedImage.WidthPx,
                    heightPx = uploadedImage.HeightPx,
                    mimeType = uploadedImage.MimeType,
                    fileSizeBytes = uploadedImage.FileSizeBytes,
                    uploadedAt = uploadedImage.UploadedAt
                }
            });
        }
        catch (Exception ex)
        {
            return Problem(detail: "Upload failed: " + ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// Get all images for a connection with quota information
    /// </summary>
    /// <param name="connectionId">Connection ID to get images for</param>
    /// <param name="includeDeleted">Include soft-deleted images in results</param>
    /// <returns>List of images with quota information</returns>
    [HttpGet]
    public async Task<IActionResult> GetImages(
        [FromQuery] int connectionId,
        [FromQuery] bool includeDeleted = false)
    {
        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            // Verify user owns connection
            var connection = await _db.Connections
                .FirstOrDefaultAsync(c => c.Id == connectionId && c.UserId == user.Id);

            if (connection == null)
                return Forbid();

            // Get images
            var query = _db.UploadedImages.Where(i => i.ConnectionId == connectionId);

            if (!includeDeleted)
                query = query.Where(i => !i.IsDeleted);

            var images = await query
                .OrderByDescending(i => i.UploadedAt)
                .ToListAsync();

            // Get quota info
            var quota = await _validator.GetQuotaInfoAsync(connectionId);

            var response = new ImageListResponse
            {
                Images = images.Select(i => new ImageDto
                {
                    Id = i.Id,
                    Name = i.Name,
                    Description = i.Description,
                    DataUri = i.DataUri,
                    WidthPx = i.WidthPx,
                    HeightPx = i.HeightPx,
                    MimeType = i.MimeType,
                    FileSizeBytes = i.FileSizeBytes,
                    IsDeleted = i.IsDeleted,
                    UploadedAt = i.UploadedAt,
                    LastUsedAt = i.LastUsedAt
                }).ToList(),
                Quota = new QuotaDto
                {
                    ImagesUsed = quota.ImagesUsed,
                    ImagesLimit = quota.ImagesLimit,
                    StorageUsed = quota.StorageUsed,
                    StorageLimit = quota.StorageLimit
                }
            };

            return Ok(new { success = true, data = response });
        }
        catch (Exception ex)
        {
            return Problem(detail: ex.Message, statusCode: 500);
        }
    }

    /// <summary>
    /// Delete an image (soft delete - replaces with transparent placeholder)
    /// </summary>
    /// <param name="id">Image ID to delete</param>
    /// <returns>Success message</returns>
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteImage(int id)
    {
        try
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
                return Unauthorized();

            // Find image
            var image = await _db.UploadedImages
                .Include(i => i.Connection)
                .FirstOrDefaultAsync(i => i.Id == id);

            if (image == null)
                return NotFound(new { success = false, error = "Image not found" });

            // Verify user owns the connection
            if (image.Connection.UserId != user.Id)
                return Forbid();

            // Soft delete: Replace DataUri with transparent 1×1 PNG
            const string transparentPng = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==";

            image.DataUri = transparentPng;
            image.IsDeleted = true;

            await _db.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Image deleted. Templates using this image will show a transparent placeholder."
            });
        }
        catch (Exception ex)
        {
            return Problem(detail: ex.Message, statusCode: 500);
        }
    }
}
