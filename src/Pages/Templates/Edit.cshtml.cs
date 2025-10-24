using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QRStickers.Services;
using System.Security.Claims;

namespace QRStickers.Pages.Templates;

[Authorize]
public class EditModel : PageModel
{
    private readonly QRStickersDbContext _db;
    private readonly ILogger<EditModel> _logger;

    public EditModel(QRStickersDbContext db, ILogger<EditModel> logger)
    {
        _db = db;
        _logger = logger;
    }

    [BindProperty]
    public int Id { get; set; }

    [BindProperty]
    public string Name { get; set; } = null!;

    [BindProperty]
    public string? Description { get; set; }

    // Read-only properties (cannot be edited)
    public int ConnectionId { get; set; }
    public string ConnectionDisplayName { get; set; } = null!;
    public double PageWidth { get; set; }
    public double PageHeight { get; set; }
    public bool IsSystemTemplate { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
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

        // System templates cannot be edited - must clone
        if (template.IsSystemTemplate)
        {
            TempData["ErrorMessage"] = "System templates cannot be edited. Please clone the template first.";
            return RedirectToPage("/Templates/Index");
        }

        // Verify user owns this template (via connection)
        if (template.ConnectionId.HasValue)
        {
            if (template.Connection?.UserId != userId)
            {
                return Forbid();
            }
        }
        else
        {
            // Template has no connection - shouldn't happen for user templates
            _logger.LogWarning("Template {TemplateId} has no ConnectionId but is not a system template", id);
            return Forbid();
        }

        // Populate form fields
        Id = template.Id;
        Name = template.Name;
        Description = template.Description;

        // Read-only display values
        ConnectionId = template.ConnectionId.Value;
        ConnectionDisplayName = template.Connection?.DisplayName ?? "Unknown";
        PageWidth = template.PageWidth;
        PageHeight = template.PageHeight;
        IsSystemTemplate = template.IsSystemTemplate;

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            return Unauthorized();
        }

        var template = await _db.StickerTemplates
            .Include(t => t.Connection)
            .FirstOrDefaultAsync(t => t.Id == Id);

        if (template == null)
        {
            TempData["ErrorMessage"] = "Template not found.";
            return RedirectToPage("/Templates/Index");
        }

        // System templates cannot be edited
        if (template.IsSystemTemplate)
        {
            TempData["ErrorMessage"] = "System templates cannot be edited.";
            return RedirectToPage("/Templates/Index");
        }

        // Verify user owns this template
        if (template.ConnectionId.HasValue)
        {
            if (template.Connection?.UserId != userId)
            {
                return Forbid();
            }
        }
        else
        {
            return Forbid();
        }

        // Validate name
        if (string.IsNullOrWhiteSpace(Name) || Name.Length > 200)
        {
            ModelState.AddModelError(nameof(Name), "Template name is required and must be 200 characters or less.");
        }

        if (!ModelState.IsValid)
        {
            // Reload display properties
            ConnectionId = template.ConnectionId.Value;
            ConnectionDisplayName = template.Connection?.DisplayName ?? "Unknown";
            PageWidth = template.PageWidth;
            PageHeight = template.PageHeight;
            IsSystemTemplate = template.IsSystemTemplate;
            return Page();
        }

        // Update editable properties only
        template.Name = Name;
        template.Description = Description;
        template.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync();

        _logger.LogInformation("User {UserId} updated template {TemplateId} '{TemplateName}'",
            userId, template.Id, LogSanitizer.Sanitize(template.Name));

        TempData["SuccessMessage"] = $"Template '{template.Name}' updated successfully!";
        return RedirectToPage("/Templates/Index");
    }
}
