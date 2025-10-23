namespace QRStickers.Models;

/// <summary>
/// Request model for tracking usage of templates and images
/// </summary>
public record UsageTrackRequest
{
    /// <summary>
    /// Array of image IDs that were used in the export
    /// </summary>
    public int[] ImageIds { get; init; } = Array.Empty<int>();

    /// <summary>
    /// ID of the template that was used for the export
    /// </summary>
    public int TemplateId { get; init; }
}
