using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QRStickers.Meraki;
using QRStickers.Services;

namespace QRStickers.Pages.Identity.Account.Manage;

[Authorize]
public class IndexModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly QRStickersDbContext _db;
    private readonly MerakiAccessTokenCache _tokenCache;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        QRStickersDbContext db,
        MerakiAccessTokenCache tokenCache,
        ILogger<IndexModel> logger)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _db = db;
        _tokenCache = tokenCache;
        _logger = logger;
    }

    [BindProperty]
    public InputModel Input { get; set; } = null!;

    public string? Email { get; set; }
    public DateTime? CurrentSessionStartedAt { get; set; }
    public DateTime? PreviousLoginAt { get; set; }

    [TempData]
    public string? StatusMessage { get; set; }

    public class InputModel
    {
        [Display(Name = "Display Name")]
        [StringLength(100, ErrorMessage = "The {0} must be at most {1} characters long.")]
        public string? DisplayName { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "Password")]
        public string? DeletePassword { get; set; }
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound("Unable to load user.");
        }

        Email = user.Email;
        CurrentSessionStartedAt = user.CurrentSessionStartedAt;
        PreviousLoginAt = user.PreviousLoginAt;

        Input = new InputModel
        {
            DisplayName = user.DisplayName
        };

        return Page();
    }

    public async Task<IActionResult> OnPostUpdateProfileAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound("Unable to load user.");
        }

        if (!ModelState.IsValid)
        {
            await OnGetAsync();
            return Page();
        }

        if (Input.DisplayName != user.DisplayName)
        {
            user.DisplayName = Input.DisplayName;
            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                StatusMessage = "Error: Unexpected error when trying to update profile.";
                return RedirectToPage();
            }
        }

        StatusMessage = "Your profile has been updated";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRevokeOtherSessionsAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound("Unable to load user.");
        }

        // Update security stamp to invalidate all existing auth cookies
        await _userManager.UpdateSecurityStampAsync(user);

        // Immediately re-sign in the current user to keep this session active
        await _signInManager.RefreshSignInAsync(user);

        StatusMessage = "All other sessions have been revoked. You remain logged in on this device.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAccountAsync()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null)
        {
            return NotFound("Unable to load user.");
        }

        // Validate the password field is provided
        if (string.IsNullOrWhiteSpace(Input.DeletePassword))
        {
            ModelState.AddModelError(string.Empty, "Password is required to delete your account.");
            await OnGetAsync();
            return Page();
        }

        // Verify the password
        var passwordCheck = await _signInManager.CheckPasswordSignInAsync(user, Input.DeletePassword, false);
        if (!passwordCheck.Succeeded)
        {
            ModelState.AddModelError(string.Empty, "Incorrect password. Please try again.");
            await OnGetAsync();
            return Page();
        }

        try
        {
            _logger.LogInformation("User {UserId} ({Email}) is deleting their account", user.Id, LogSanitizer.Sanitize(user.Email));

            // Load all connections to clear from token cache
            var connections = await _db.Connections
                .Where(c => c.UserId == user.Id)
                .ToListAsync();

            // Clear in-memory token cache for all connections
            foreach (var connection in connections)
            {
                _tokenCache.RemoveToken(connection.Id);
                _logger.LogInformation("Removed cached token for connection {ConnectionId}", connection.Id);
            }

            // Delete the user (this will CASCADE delete all related data)
            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                _logger.LogError("Failed to delete user {UserId}: {Errors}", user.Id, string.Join(", ", result.Errors.Select(e => e.Description)));
                ModelState.AddModelError(string.Empty, "Unable to delete account. Please try again later.");
                await OnGetAsync();
                return Page();
            }

            _logger.LogInformation("User {UserId} ({Email}) successfully deleted their account", user.Id, LogSanitizer.Sanitize(user.Email));

            // Sign out the user
            await _signInManager.SignOutAsync();

            // Redirect to home page with success message
            TempData["StatusMessage"] = "Your account has been deleted.";
            return RedirectToPage("/Index");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting user account {UserId}", user.Id);
            ModelState.AddModelError(string.Empty, "Unable to delete account. Please try again later.");
            await OnGetAsync();
            return Page();
        }
    }
}
