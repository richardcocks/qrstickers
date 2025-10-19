using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace QRStickers.ViewComponents;

public class SyncStatusViewComponent : ViewComponent
{
    private readonly QRStickersDbContext _db;

    public SyncStatusViewComponent(QRStickersDbContext db)
    {
        _db = db;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(userId))
        {
            return View(new SyncStatusViewModel { IsVisible = false });
        }

        // Check if user has any connections
        var hasConnection = await _db.Connections.AnyAsync(c => c.UserId == userId);

        if (!hasConnection)
        {
            return View(new SyncStatusViewModel
            {
                IsVisible = true,
                ColorClass = "sync-status-none",
                StatusText = "No connections",
                TimeAgo = null
            });
        }

        // Get most recent sync status across all user's connections
        var connectionIds = await _db.Connections
            .Where(c => c.UserId == userId)
            .Select(c => c.Id)
            .ToListAsync();

        var syncStatus = await _db.SyncStatuses
            .Where(s => connectionIds.Contains(s.ConnectionId))
            .OrderByDescending(s => s.LastSyncCompletedAt)
            .FirstOrDefaultAsync();

        if (syncStatus == null || syncStatus.LastSyncCompletedAt == null)
        {
            return View(new SyncStatusViewModel
            {
                IsVisible = true,
                ColorClass = "sync-status-none",
                StatusText = "Not synced yet",
                TimeAgo = null
            });
        }

        var lastSyncTime = syncStatus.LastSyncCompletedAt.Value;
        var timeSinceSync = DateTime.UtcNow - lastSyncTime;
        var timeAgo = FormatTimeAgo(timeSinceSync);

        // Determine color based on sync state and time
        string colorClass;
        string statusText;

        if (syncStatus.Status == SyncState.Failed)
        {
            colorClass = "sync-status-failed";
            statusText = "Sync failed";
        }
        else if (syncStatus.Status == SyncState.InProgress)
        {
            colorClass = "sync-status-progress";
            statusText = "Syncing...";
        }
        else if (timeSinceSync.TotalHours < 1)
        {
            colorClass = "sync-status-recent";
            statusText = "Synced";
        }
        else
        {
            colorClass = "sync-status-old";
            statusText = "Synced";
        }

        return View(new SyncStatusViewModel
        {
            IsVisible = true,
            ColorClass = colorClass,
            StatusText = statusText,
            TimeAgo = timeAgo,
            LastSyncTime = lastSyncTime
        });
    }

    private string FormatTimeAgo(TimeSpan timeSpan)
    {
        if (timeSpan.TotalSeconds < 30)
            return "just now";
        if (timeSpan.TotalMinutes < 1)
            return $"{(int)timeSpan.TotalSeconds} seconds ago";
        if (timeSpan.TotalMinutes < 2)
            return "1 minute ago";
        if (timeSpan.TotalMinutes < 60)
            return $"{(int)timeSpan.TotalMinutes} minutes ago";
        if (timeSpan.TotalHours < 2)
            return "1 hour ago";
        if (timeSpan.TotalHours < 24)
            return $"{(int)timeSpan.TotalHours} hours ago";
        if (timeSpan.TotalDays < 2)
            return "yesterday";
        if (timeSpan.TotalDays < 7)
            return $"{(int)timeSpan.TotalDays} days ago";
        if (timeSpan.TotalDays < 14)
            return "1 week ago";
        if (timeSpan.TotalDays < 30)
            return $"{(int)(timeSpan.TotalDays / 7)} weeks ago";
        if (timeSpan.TotalDays < 60)
            return "1 month ago";
        if (timeSpan.TotalDays < 365)
            return $"{(int)(timeSpan.TotalDays / 30)} months ago";

        return "over a year ago";
    }
}

public class SyncStatusViewModel
{
    public bool IsVisible { get; set; }
    public string ColorClass { get; set; } = "";
    public string StatusText { get; set; } = "";
    public string? TimeAgo { get; set; }
    public DateTime? LastSyncTime { get; set; }
}
