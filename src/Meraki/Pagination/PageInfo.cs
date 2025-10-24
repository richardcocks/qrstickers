namespace QRStickers.Meraki.Pagination;

/// <summary>
/// Contains pagination metadata extracted from Link headers
/// </summary>
public class PageInfo
{
    /// <summary>
    /// URL for the first page (if available)
    /// </summary>
    public string? First { get; set; }

    /// <summary>
    /// URL for the last page (if available)
    /// </summary>
    public string? Last { get; set; }

    /// <summary>
    /// URL for the previous page (if available)
    /// </summary>
    public string? Prev { get; set; }

    /// <summary>
    /// URL for the next page (if available)
    /// </summary>
    public string? Next { get; set; }

    /// <summary>
    /// Indicates if there are more pages available
    /// </summary>
    public bool HasNextPage => !string.IsNullOrWhiteSpace(Next);

    /// <summary>
    /// Indicates if there are previous pages available
    /// </summary>
    public bool HasPrevPage => !string.IsNullOrWhiteSpace(Prev);
}
