namespace QRStickers.Meraki.Pagination;

/// <summary>
/// Represents a paginated response from the Meraki API
/// </summary>
/// <typeparam name="T">The type of items in the response</typeparam>
public class PaginatedResponse<T>
{
    /// <summary>
    /// The items returned in this page
    /// </summary>
    public List<T> Items { get; set; } = new();

    /// <summary>
    /// Pagination metadata from Link headers
    /// </summary>
    public PageInfo PageInfo { get; set; } = new();

    /// <summary>
    /// Creates an empty paginated response
    /// </summary>
    public PaginatedResponse()
    {
    }

    /// <summary>
    /// Creates a paginated response with items and page info
    /// </summary>
    public PaginatedResponse(List<T> items, PageInfo pageInfo)
    {
        Items = items ?? new List<T>();
        PageInfo = pageInfo ?? new PageInfo();
    }
}
