using System.ComponentModel.DataAnnotations;

namespace QRStickers.Models;

/// <summary>
/// Request model for image upload endpoint
/// </summary>
public class ImageUploadRequest
{
    /// <summary>
    /// Connection ID that will own this image
    /// </summary>
    [Required]
    [Range(1, int.MaxValue, ErrorMessage = "Connection ID must be positive")]
    public int ConnectionId { get; set; }

    /// <summary>
    /// User-friendly name for the image (e.g., "CompanyLogo", "FloorPlanB2")
    /// Must match regex: ^[a-zA-Z0-9\s\-_.()]+$
    /// Prevents log injection via newlines and control characters
    /// </summary>
    [Required(ErrorMessage = "Image name is required")]
    [StringLength(200, MinimumLength = 1, ErrorMessage = "Image name must be 1-200 characters")]
    [RegularExpression(@"^[a-zA-Z0-9\s\-_.()]+$", ErrorMessage = "Image name contains invalid characters. Allowed: letters, numbers, spaces, -_.()")]
    public string Name { get; set; } = null!;

    /// <summary>
    /// Optional description for the image
    /// </summary>
    [StringLength(500, ErrorMessage = "Description must be 500 characters or less")]
    public string? Description { get; set; }

    /// <summary>
    /// Image data URI (data:image/png;base64,... or data:image/jpeg;base64,...)
    /// </summary>
    [Required(ErrorMessage = "Image data is required")]
    [RegularExpression(@"^data:image/(png|jpeg|webp|svg\+xml);base64,", ErrorMessage = "Invalid data URI format")]
    public string DataUri { get; set; } = null!;

    /// <summary>
    /// Image width in pixels
    /// </summary>
    [Required]
    [Range(1, 900, ErrorMessage = "Width must be 1-900 pixels")]
    public int WidthPx { get; set; }

    /// <summary>
    /// Image height in pixels
    /// </summary>
    [Required]
    [Range(1, 900, ErrorMessage = "Height must be 1-900 pixels")]
    public int HeightPx { get; set; }
}
