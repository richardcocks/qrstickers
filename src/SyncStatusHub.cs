using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace QRStickers;

/// <summary>
/// SignalR hub for broadcasting real-time sync status updates to clients
/// </summary>
[Authorize]
public class SyncStatusHub : Hub
{
    /// <summary>
    /// Allow client to join a connection-specific group to receive updates
    /// </summary>
    public async Task JoinConnectionGroup(int connectionId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, GetGroupName(connectionId));
    }

    /// <summary>
    /// Allow client to leave a connection-specific group
    /// </summary>
    public async Task LeaveConnectionGroup(int connectionId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GetGroupName(connectionId));
    }

    /// <summary>
    /// Get the group name for a specific connection
    /// </summary>
    public static string GetGroupName(int connectionId) => $"sync_connection_{connectionId}";
}
