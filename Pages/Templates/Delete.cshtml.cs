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
    public List<ConnectionDefaultTemplate> UsageInfo { get; set; } = new();
    public List<StickerTemplate> ReplacementOptions { get; set; } = new();

    [BindProperty]
    public int? ReplacementTemplateId { get; set; }

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

        // Check if template is referenced in ConnectionDefaultTemplates
        UsageInfo = await _db.ConnectionDefaultTemplates
            .Where(d => d.TemplateId == id)
            .ToListAsync();

        // If template is in use, load replacement options
        if (UsageInfo.Any())
        {
            // Get connection ID for loading appropriate replacements
            var connectionId = Template.ConnectionId;

            // Load replacement options: system templates + user's other templates (excluding this one)
            ReplacementOptions = await _db.StickerTemplates
                .Where(t => t.Id != id) // Exclude template being deleted
                .Where(t => t.IsSystemTemplate || t.ConnectionId == connectionId)
                .OrderByDescending(t => t.IsSystemTemplate)
                .ThenBy(t => t.Name)
                .ToListAsync();
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

        // Check if template is in use
        var usageInfo = await _db.ConnectionDefaultTemplates
            .Where(d => d.TemplateId == id)
            .ToListAsync();

        if (usageInfo.Any())
        {
            // Template is in use - require replacement selection
            if (!ReplacementTemplateId.HasValue)
            {
                // No replacement selected - reload page with error
                Template = template;
                UsageInfo = usageInfo;

                var connectionId = template.ConnectionId;
                ReplacementOptions = await _db.StickerTemplates
                    .Where(t => t.Id != id)
                    .Where(t => t.IsSystemTemplate || t.ConnectionId == connectionId)
                    .OrderByDescending(t => t.IsSystemTemplate)
                    .ThenBy(t => t.Name)
                    .ToListAsync();

                ModelState.AddModelError("ReplacementTemplateId", "Please select a replacement template.");
                return Page();
            }

            // Update all references to use the replacement
            foreach (var defaultTemplate in usageInfo)
            {
                defaultTemplate.TemplateId = ReplacementTemplateId.Value;
                defaultTemplate.UpdatedAt = DateTime.UtcNow;
            }

            await _db.SaveChangesAsync();

            _logger.LogInformation("Updated {Count} default template references from template {Id} to {ReplacementId}",
                usageInfo.Count, id, ReplacementTemplateId.Value);
        }

        _logger.LogInformation("Deleting template {Id} '{Name}' for user {UserId}",
            template.Id, template.Name, userId);

        _db.StickerTemplates.Remove(template);
        await _db.SaveChangesAsync();

        var successMessage = usageInfo.Any()
            ? $"Template '{template.Name}' deleted successfully. {usageInfo.Count} default(s) updated with replacement."
            : $"Template '{template.Name}' deleted successfully.";

        TempData["SuccessMessage"] = successMessage;
        return RedirectToPage("/Templates/Index");
    }

    public string GetDeviceTypeName(string productType)
    {
        return productType.ToLower() switch
        {
            "switch" => "Switches",
            "appliance" => "Appliances",
            "wireless" => "Wireless APs",
            "camera" => "Cameras",
            "sensor" => "Sensors",
            "cellulargateway" => "Cellular Gateways",
            _ => productType
        };
    }
}
