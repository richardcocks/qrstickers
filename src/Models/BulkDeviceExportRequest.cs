namespace QRStickers.Models;

/// <summary>
/// Request model for bulk device export data fetching
/// </summary>
public class BulkDeviceExportRequest
{
    /// <summary>
    /// List of device IDs to export
    /// </summary>
    public int[] DeviceIds { get; set; } = Array.Empty<int>();

    /// <summary>
    /// Connection ID that owns all devices
    /// </summary>
    public int ConnectionId { get; set; }
}
