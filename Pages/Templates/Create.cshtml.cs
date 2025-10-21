using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace QRStickers.Pages.Templates;

[Authorize]
public class CreateModel : PageModel
{
    private readonly QRStickersDbContext _db;
    private readonly ILogger<CreateModel> _logger;

    public CreateModel(QRStickersDbContext db, ILogger<CreateModel> logger)
    {
        _db = db;
        _logger = logger;
    }

    [BindProperty]
    public string Name { get; set; } = "New Template";

    [BindProperty]
    public string? Description { get; set; }

    [BindProperty]
    public int ConnectionId { get; set; }

    [BindProperty]
    public double PageWidth { get; set; } = 100.0;

    [BindProperty]
    public double PageHeight { get; set; } = 50.0;

    [BindProperty]
    public string? ProductTypeFilter { get; set; }

    [BindProperty]
    public bool IsRackMount { get; set; }

    [BindProperty]
    public bool IsDefault { get; set; }

    [BindProperty]
    public int? CloneFromTemplateId { get; set; }

    public List<Connection> UserConnections { get; set; } = new();
    public List<StickerTemplate> SystemTemplates { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int? cloneFrom)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            return Unauthorized();
        }

        UserConnections = await _db.Connections
            .Where(c => c.UserId == userId)
            .OrderBy(c => c.DisplayName)
            .ToListAsync();

        if (UserConnections.Count == 0)
        {
            TempData["ErrorMessage"] = "You must have at least one connection to create a template.";
            return RedirectToPage("/Connections/Index");
        }

        SystemTemplates = await _db.StickerTemplates
            .Where(t => t.IsSystemTemplate)
            .OrderBy(t => t.Name)
            .ToListAsync();

        // Pre-select connection if user only has one
        if (UserConnections.Count == 1)
        {
            ConnectionId = UserConnections[0].Id;
        }

        // Pre-populate if cloning
        if (cloneFrom.HasValue)
        {
            CloneFromTemplateId = cloneFrom;
            var sourceTemplate = await _db.StickerTemplates.FindAsync(cloneFrom.Value);
            if (sourceTemplate != null)
            {
                Name = $"{sourceTemplate.Name} (Copy)";
                Description = sourceTemplate.Description;
                PageWidth = sourceTemplate.PageWidth;
                PageHeight = sourceTemplate.PageHeight;
                ProductTypeFilter = sourceTemplate.ProductTypeFilter;
                IsRackMount = sourceTemplate.IsRackMount;
            }
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            return Unauthorized();
        }

        // Validate connection ownership
        var connection = await _db.Connections
            .FirstOrDefaultAsync(c => c.Id == ConnectionId && c.UserId == userId);

        if (connection == null)
        {
            ModelState.AddModelError("", "Invalid connection selected.");
            UserConnections = await _db.Connections
                .Where(c => c.UserId == userId)
                .ToListAsync();
            SystemTemplates = await _db.StickerTemplates
                .Where(t => t.IsSystemTemplate)
                .ToListAsync();
            return Page();
        }

        // Validate page dimensions
        if (PageWidth < 10 || PageWidth > 500)
        {
            ModelState.AddModelError(nameof(PageWidth), "Page width must be between 10mm and 500mm.");
        }

        if (PageHeight < 10 || PageHeight > 500)
        {
            ModelState.AddModelError(nameof(PageHeight), "Page height must be between 10mm and 500mm.");
        }

        if (!ModelState.IsValid)
        {
            UserConnections = await _db.Connections
                .Where(c => c.UserId == userId)
                .ToListAsync();
            SystemTemplates = await _db.StickerTemplates
                .Where(t => t.IsSystemTemplate)
                .ToListAsync();
            return Page();
        }

        StickerTemplate newTemplate;

        if (CloneFromTemplateId.HasValue)
        {
            // Clone from existing template
            var sourceTemplate = await _db.StickerTemplates
                .FindAsync(CloneFromTemplateId.Value);

            if (sourceTemplate == null)
            {
                TempData["ErrorMessage"] = "Source template not found.";
                return RedirectToPage("/Templates/Index");
            }

            newTemplate = new StickerTemplate
            {
                Name = Name,
                Description = Description,
                ConnectionId = ConnectionId,
                PageWidth = sourceTemplate.PageWidth,
                PageHeight = sourceTemplate.PageHeight,
                ProductTypeFilter = ProductTypeFilter,
                IsRackMount = IsRackMount,
                IsDefault = IsDefault,
                IsSystemTemplate = false,
                TemplateJson = sourceTemplate.TemplateJson,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _logger.LogInformation("Cloning template {SourceId} to new template '{Name}' for user {UserId}",
                sourceTemplate.Id, Name, userId);
        }
        else
        {
            // Create blank template
            newTemplate = new StickerTemplate
            {
                Name = Name,
                Description = Description,
                ConnectionId = ConnectionId,
                PageWidth = PageWidth,
                PageHeight = PageHeight,
                ProductTypeFilter = ProductTypeFilter,
                IsRackMount = IsRackMount,
                IsDefault = IsDefault,
                IsSystemTemplate = false,
                TemplateJson = CreateBlankTemplateJson(PageWidth, PageHeight),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _logger.LogInformation("Creating new blank template '{Name}' for user {UserId}", Name, userId);
        }

        _db.StickerTemplates.Add(newTemplate);
        await _db.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Template '{newTemplate.Name}' created successfully!";
        return RedirectToPage("/Templates/Designer", new { id = newTemplate.Id });
    }

    private static string CreateBlankTemplateJson(double width, double height)
    {
        return $@"{{
  ""version"": ""1.0"",
  ""fabricVersion"": ""5.3.0"",
  ""pageSize"": {{
    ""width"": {width},
    ""height"": {height},
    ""unit"": ""mm""
  }},
  ""objects"": []
}}";
    }
}
