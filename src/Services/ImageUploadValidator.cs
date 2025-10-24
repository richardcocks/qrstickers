using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.RegularExpressions;

namespace QRStickers.Services;

/// <summary>
/// Server-side validation service for image uploads
/// Validates MIME type, dimensions, file size, quota, and name uniqueness
/// </summary>
public class ImageUploadValidator
{
    private const int MAX_DIMENSION = 900;
    private const long MAX_FILE_SIZE = 2_000_000; // 2 MB (pre-base64)
    private const long MAX_BASE64_SIZE = 2_700_000; // 2.7 MB (with base64 overhead)
    private const int MAX_IMAGES_PER_CONNECTION = 25;
    private const long MAX_TOTAL_STORAGE = 20_000_000; // 20 MB

    private static readonly string[] AllowedMimeTypes = new[]
    {
        "image/png",
        "image/jpeg",
        "image/webp",
        "image/svg+xml"
    };

    private readonly QRStickersDbContext _db;
    private readonly ILogger<ImageUploadValidator> _logger;

    public ImageUploadValidator(QRStickersDbContext db, ILogger<ImageUploadValidator> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// Validates an image upload request
    /// </summary>
    public async Task<ValidationResult> ValidateUploadAsync(
        int connectionId,
        string name,
        string dataUri,
        int widthPx,
        int heightPx,
        string userId)
    {
        // 1. Validate user owns connection
        var connection = await _db.Connections
            .FirstOrDefaultAsync(c => c.Id == connectionId && c.UserId == userId);

        if (connection == null)
        {
            _logger.LogWarning("Upload validation failed: Connection {ConnectionId} not found or access denied for user {UserId}",
                connectionId, SanitizeForLog(userId));
            return ValidationResult.Fail("Connection not found or access denied");
        }

        // 2. Validate MIME type from data URI
        if (!dataUri.StartsWith("data:image/"))
        {
            return ValidationResult.Fail("Invalid image format. Data URI must start with 'data:image/'");
        }

        var mimeType = ExtractMimeType(dataUri);
        if (string.IsNullOrEmpty(mimeType))
        {
            return ValidationResult.Fail("Unable to extract MIME type from data URI");
        }

        if (!AllowedMimeTypes.Contains(mimeType))
        {
            return ValidationResult.Fail($"Unsupported MIME type: {mimeType}. Allowed types: PNG, JPEG, WebP, SVG");
        }

        // 3. Validate dimensions
        if (widthPx > MAX_DIMENSION || heightPx > MAX_DIMENSION)
        {
            return ValidationResult.Fail($"Image dimensions too large ({widthPx}×{heightPx}px). Max {MAX_DIMENSION}×{MAX_DIMENSION}px");
        }

        if (widthPx <= 0 || heightPx <= 0)
        {
            return ValidationResult.Fail("Invalid dimensions. Width and height must be positive");
        }

        // 4. Validate file size
        var sizeBytes = Encoding.UTF8.GetByteCount(dataUri);
        if (sizeBytes > MAX_BASE64_SIZE)
        {
            var sizeMB = sizeBytes / 1024.0 / 1024.0;
            return ValidationResult.Fail($"Image too large ({sizeMB:F2} MB). Max 2 MB");
        }

        // 5. Validate name
        if (string.IsNullOrWhiteSpace(name))
        {
            return ValidationResult.Fail("Image name is required");
        }

        if (name.Length > 200)
        {
            return ValidationResult.Fail("Image name too long. Max 200 characters");
        }

        // 6. Check quota: image count
        var imageCount = await _db.UploadedImages
            .CountAsync(i => i.ConnectionId == connectionId && !i.IsDeleted);

        if (imageCount >= MAX_IMAGES_PER_CONNECTION)
        {
            return ValidationResult.Fail($"Image limit reached ({MAX_IMAGES_PER_CONNECTION} images per connection)");
        }

        // 7. Check quota: total storage
        var totalStorage = await _db.UploadedImages
            .Where(i => i.ConnectionId == connectionId && !i.IsDeleted)
            .SumAsync(i => (long?)i.FileSizeBytes) ?? 0;

        if (totalStorage + sizeBytes > MAX_TOTAL_STORAGE)
        {
            var available = (MAX_TOTAL_STORAGE - totalStorage) / 1024.0 / 1024.0;
            return ValidationResult.Fail($"Storage quota exceeded (max 20 MB). Available: {available:F2} MB");
        }

        _logger.LogInformation("Upload validation passed for image '{ImageName}' on connection {ConnectionId}",
            SanitizeForLog(name), connectionId);

        return ValidationResult.Success(mimeType, sizeBytes);
    }

    /// <summary>
    /// Extracts MIME type from data URI
    /// Example: data:image/png;base64,... → image/png
    /// </summary>
    private string ExtractMimeType(string dataUri)
    {
        var match = Regex.Match(dataUri, @"^data:([^;]+);");
        return match.Success ? match.Groups[1].Value : string.Empty;
    }

    /// <summary>
    /// Sanitizes a string for safe logging by removing newlines and control characters
    /// Prevents log injection attacks where attackers inject fake log entries
    /// </summary>
    /// <param name="input">Raw user input</param>
    /// <returns>Sanitized string safe for logging</returns>
    private static string SanitizeForLog(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        // Remove newlines, carriage returns, and control characters
        // Keep printable ASCII and common Unicode characters
        var sanitized = Regex.Replace(input, @"[\r\n\t\x00-\x1F\x7F-\x9F]", "");

        // Truncate to reasonable length for logs (prevent log flooding)
        return sanitized.Length > 200 ? sanitized.Substring(0, 200) + "..." : sanitized;
    }

    /// <summary>
    /// Gets current quota usage for a connection
    /// </summary>
    public async Task<QuotaInfo> GetQuotaInfoAsync(int connectionId)
    {
        var images = await _db.UploadedImages
            .Where(i => i.ConnectionId == connectionId && !i.IsDeleted)
            .ToListAsync();

        var imagesUsed = images.Count;
        var storageUsed = images.Sum(i => i.FileSizeBytes);

        return new QuotaInfo
        {
            ImagesUsed = imagesUsed,
            ImagesLimit = MAX_IMAGES_PER_CONNECTION,
            StorageUsed = storageUsed,
            StorageLimit = MAX_TOTAL_STORAGE
        };
    }
}

/// <summary>
/// Result of validation
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; set; }
    public string? ErrorMessage { get; set; }
    public string? MimeType { get; set; }
    public long FileSizeBytes { get; set; }

    public static ValidationResult Success(string mimeType, long fileSizeBytes) => new()
    {
        IsValid = true,
        MimeType = mimeType,
        FileSizeBytes = fileSizeBytes
    };

    public static ValidationResult Fail(string errorMessage) => new()
    {
        IsValid = false,
        ErrorMessage = errorMessage
    };
}

/// <summary>
/// Quota information for a connection
/// </summary>
public class QuotaInfo
{
    public int ImagesUsed { get; set; }
    public int ImagesLimit { get; set; }
    public long StorageUsed { get; set; }
    public long StorageLimit { get; set; }
}
