using QRCoder;

namespace QRStickers.Services;

/// <summary>
/// Service for generating QR codes as base64 data URIs
/// </summary>
public class QRCodeGenerationService
{
    private readonly QRCodeGenerator _qrGenerator;
    private readonly ILogger<QRCodeGenerationService>? _logger;

    public QRCodeGenerationService(
        QRCodeGenerator qrGenerator,
        ILogger<QRCodeGenerationService> logger)
    {
        _qrGenerator = qrGenerator ?? throw new ArgumentNullException(nameof(qrGenerator));
        _logger = logger;
    }

    /// <summary>
    /// Generates a QR code as a base64 data URI
    /// </summary>
    /// <param name="content">Content to encode in QR code (URL, serial number, etc.)</param>
    /// <param name="sizePx">Size of QR code in pixels (default: 400x400)</param>
    /// <returns>Data URI string in format "data:image/png;base64,..." or null if generation fails</returns>
    public string? GenerateQRCodeDataUri(string content, int sizePx = 400)
    {
        if (string.IsNullOrWhiteSpace(content))
        {
            _logger?.LogWarning("[QR Generation] Cannot generate QR code: content is null or empty");
            return null;
        }

        try
        {
            // Generate QR code data with error correction level Q (25% error correction)
            using var qrCodeData = _qrGenerator.CreateQrCode(content, QRCodeGenerator.ECCLevel.Q);

            // Calculate pixels per module based on actual QR code module count
            // QRCodeData.ModuleMatrix gives us the actual size (e.g., 49x49 for longer content)
            var moduleCount = qrCodeData.ModuleMatrix.Count;
            var pixelsPerModule = Math.Max(1, sizePx / moduleCount);

            _logger?.LogDebug("[QR Generation] QR code has {ModuleCount} modules, using {PixelsPerModule} pixels/module for target {TargetSize}px",
                moduleCount, pixelsPerModule, sizePx);

            // Generate PNG bytes with calculated pixels per module
            using var qrCode = new PngByteQRCode(qrCodeData);
            byte[] qrCodeImage = qrCode.GetGraphic(pixelsPerModule);

            // Convert to base64 data URI
            var base64 = Convert.ToBase64String(qrCodeImage);
            var dataUri = $"data:image/png;base64,{base64}";

            _logger?.LogDebug("[QR Generation] Generated QR code for content: {Content} ({Size} bytes)",
                content.Length > 50 ? content.Substring(0, 50) + "..." : content,
                base64.Length);

            return dataUri;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "[QR Generation] Error generating QR code for content: {Content}", content);
            return null;
        }
    }

    /// <summary>
    /// Checks if a QR code needs regeneration (content changed)
    /// </summary>
    /// <param name="oldContent">Previous QR code content</param>
    /// <param name="newContent">New QR code content</param>
    /// <returns>True if QR code should be regenerated</returns>
    public bool ShouldRegenerateQRCode(string? oldContent, string? newContent)
    {
        // Regenerate if:
        // 1. Old content was null but new content exists (new QR code needed)
        // 2. Old content exists but new content is null (QR code should be removed)
        // 3. Content has changed
        if (oldContent == null && newContent != null) return true;
        if (oldContent != null && newContent == null) return true;
        if (oldContent != newContent) return true;

        return false; // No change
    }
}
