using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace QRStickers.Pages.Connections;

[Authorize]
public class CreateModel : PageModel
{
    [BindProperty]
    [Required]
    [MaxLength(100)]
    [Display(Name = "Display Name")]
    public string DisplayName { get; set; } = null!;

    [BindProperty]
    [Required]
    [Display(Name = "Connection Type")]
    public string ConnectionType { get; set; } = "Meraki";

    public void OnGet()
    {
        // Display the form
    }

    public IActionResult OnPost()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        // Redirect to the appropriate OAuth flow based on connection type
        if (ConnectionType == "Meraki")
        {
            return RedirectToPage("/Meraki/Connect", new { displayName = DisplayName });
        }

        // Future: Handle other connection types here
        ModelState.AddModelError(string.Empty, "Unsupported connection type");
        return Page();
    }
}
