using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace QRStickers.Pages.Templates;

[Authorize]
public class DeleteModel : PageModel
{
    private readonly QRStickersDbContext _db;
    private readonly ILogger<DeleteModel> _logger;

    public DeleteModel(QRStickersDbContext db, ILogger<DeleteModel> logger)
    {
        _db = db;
        _logger = logger;
    }

    public StickerTemplate? Template { get; set; } = null!;
    public string? WarningMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            return Unauthorized();
        }

        Template = await _db.StickerTemplates
            .Include(t => t.Connection)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (Template == null)
        {
            return NotFound();
        }

        // System templates cannot be deleted
        if (Template.IsSystemTemplate)
        {
            TempData["ErrorMessage"] = "Cannot delete system templates. System templates are read-only.";
            return RedirectToPage("/Templates/Index");
        }

        // Verify user owns the connection
        if (Template.Connection?.UserId != userId)
        {
            return Forbid();
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            return Unauthorized();
        }

        var template = await _db.StickerTemplates
            .Include(t => t.Connection)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (template == null)
        {
            return NotFound();
        }

        // Verify ownership and not system template
        if (template.IsSystemTemplate)
        {
            TempData["ErrorMessage"] = "Cannot delete system templates.";
            return RedirectToPage("/Templates/Index");
        }

        if (template.Connection?.UserId != userId)
        {
            return Forbid();
        }

        _logger.LogInformation("Deleting template {Id} '{Name}' for user {UserId}",
            template.Id, template.Name, userId);

        _db.StickerTemplates.Remove(template);
        await _db.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Template '{template.Name}' deleted successfully.";
        return RedirectToPage("/Templates/Index");
    }
}
