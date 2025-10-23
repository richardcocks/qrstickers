namespace QRStickers.Models;

/// <summary>
/// Response model for image list endpoint
/// </summary>
public class ImageListResponse
{
    /// <summary>
    /// List of uploaded images for the connection
    /// </summary>
    public List<ImageDto> Images { get; set; } = new();

    /// <summary>
    /// Quota information for the connection
    /// </summary>
    public QuotaDto Quota { get; set; } = new();
}

/// <summary>
/// DTO for uploaded image metadata
/// </summary>
public class ImageDto
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public string DataUri { get; set; } = null!;
    public int WidthPx { get; set; }
    public int HeightPx { get; set; }
    public string MimeType { get; set; } = null!;
    public long FileSizeBytes { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime UploadedAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
}

/// <summary>
/// DTO for quota information
/// </summary>
public class QuotaDto
{
    public int ImagesUsed { get; set; }
    public int ImagesLimit { get; set; }
    public long StorageUsed { get; set; }
    public long StorageLimit { get; set; }
}
