namespace QRStickers.Models;

/// <summary>
/// Request model for image upload endpoint
/// </summary>
public class ImageUploadRequest
{
    /// <summary>
    /// Connection ID that will own this image
    /// </summary>
    public int ConnectionId { get; set; }

    /// <summary>
    /// User-friendly name for the image (e.g., "CompanyLogo", "FloorPlanB2")
    /// Must match regex: ^[A-Za-z0-9_-]+$
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Optional description for the image
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Image data URI (data:image/png;base64,... or data:image/jpeg;base64,...)
    /// </summary>
    public string DataUri { get; set; } = null!;

    /// <summary>
    /// Image width in pixels
    /// </summary>
    public int WidthPx { get; set; }

    /// <summary>
    /// Image height in pixels
    /// </summary>
    public int HeightPx { get; set; }
}
