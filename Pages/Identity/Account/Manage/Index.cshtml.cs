using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace QRStickers.Pages.Identity.Account.Manage;

[Authorize]
public class IndexModel : PageModel
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public IndexModel(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
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
}
