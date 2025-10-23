namespace QRStickers.Models;

/// <summary>
/// Request model for bulk PDF export endpoint
/// </summary>
public class PdfExportRequest
{
    /// <summary>
    /// List of device sticker images to include in PDF
    /// </summary>
    public List<DeviceImageData> Images { get; set; } = new();

    /// <summary>
    /// Layout type: "auto-fit" (maximize per page) or "one-per-page"
    /// </summary>
    public string Layout { get; set; } = "auto-fit";

    /// <summary>
    /// Page size: "A4", "A5", "A6", "4x6", "Letter", "Legal"
    /// </summary>
    public string PageSize { get; set; } = "A4";
}

/// <summary>
/// Individual device sticker image data
/// </summary>
public class DeviceImageData
{
    /// <summary>
    /// PNG image as base64-encoded string
    /// </summary>
    public string ImageBase64 { get; set; } = null!;

    /// <summary>
    /// Sticker width in millimeters
    /// </summary>
    public double WidthMm { get; set; }

    /// <summary>
    /// Sticker height in millimeters
    /// </summary>
    public double HeightMm { get; set; }

    /// <summary>
    /// Device name (for metadata/debugging)
    /// </summary>
    public string DeviceName { get; set; } = null!;

    /// <summary>
    /// Device serial number (for metadata/debugging)
    /// </summary>
    public string DeviceSerial { get; set; } = null!;
}
