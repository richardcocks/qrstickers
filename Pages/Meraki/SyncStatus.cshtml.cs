using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace QRStickers.Pages.Meraki;

[Authorize]
public class SyncStatusModel : PageModel
{
    private readonly QRStickersDbContext _db;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SyncStatusModel> _logger;

    public SyncStatusModel(QRStickersDbContext db, IServiceProvider serviceProvider, ILogger<SyncStatusModel> logger)
    {
        _db = db;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public SyncState Status { get; set; }
    public string? CurrentStep { get; set; }
    public int CurrentStepNumber { get; set; }
    public int TotalSteps { get; set; }
    public string? ErrorMessage { get; set; }
    public DateTime? LastSyncStartedAt { get; set; }
    public DateTime? LastSyncCompletedAt { get; set; }

    public async Task<IActionResult> OnGetAsync(bool trigger = false)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            return RedirectToPage("/Identity/Account/Login");
        }

        // If trigger=true, start a new sync in the background
        if (trigger)
        {
            _logger.LogInformation("Manual sync triggered for user {UserId}", userId);

            // Trigger background sync (fire and forget)
            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var syncOrchestrator = scope.ServiceProvider.GetRequiredService<MerakiSyncOrchestrator>();
                    await syncOrchestrator.SyncUserDataAsync(userId);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in manual background sync for user {UserId}", userId);
                }
            });
        }

        var syncStatus = await _db.SyncStatuses.FindAsync(userId);
        if (syncStatus == null)
        {
            // No sync status yet - sync may not have started
            Status = SyncState.NotStarted;
            CurrentStep = "Waiting for sync to start...";
            CurrentStepNumber = 0;
            TotalSteps = 3;
            return Page();
        }

        Status = syncStatus.Status;
        CurrentStep = syncStatus.CurrentStep;
        CurrentStepNumber = syncStatus.CurrentStepNumber;
        TotalSteps = syncStatus.TotalSteps;
        ErrorMessage = syncStatus.ErrorMessage;
        LastSyncStartedAt = syncStatus.LastSyncStartedAt;
        LastSyncCompletedAt = syncStatus.LastSyncCompletedAt;

        // If sync is completed, redirect to organizations page after a short delay
        if (Status == SyncState.Completed)
        {
            _logger.LogInformation("Sync completed for user {UserId}, will redirect to organizations", userId);
        }

        return Page();
    }
}
