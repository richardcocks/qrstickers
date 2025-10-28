using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using QRStickers.Models;
using QRStickers.Services;
using System.Security.Claims;
using System.Text.Json;

namespace QRStickers.Pages.Templates;

[Authorize]
public class DesignerModel : PageModel
{
    private readonly QRStickersDbContext _db;
    private readonly ILogger<DesignerModel> _logger;
    private readonly IOptions<DesignerSettings> _designerSettings;
    private readonly IWebHostEnvironment _webHostEnvironment;

    public DesignerModel(
        QRStickersDbContext db,
        ILogger<DesignerModel> logger,
        IOptions<DesignerSettings> designerSettings,
        IWebHostEnvironment webHostEnvironment)
    {
        _db = db;
        _logger = logger;
        _designerSettings = designerSettings;
        _webHostEnvironment = webHostEnvironment;
    }

    [BindProperty]
    public StickerTemplate Template { get; set; } = null!;

    public List<Connection> UserConnections { get; set; } = new();
    public List<GlobalVariable> GlobalVariables { get; set; } = new();
    public List<UploadedImage> UploadedImages { get; set; } = new();
    public bool IsEditMode { get; set; }
    public bool IsSystemTemplate { get; set; }

    // Designer canvas margin settings
    public int MarginTop => _designerSettings.Value.DefaultMargins.Top;
    public int MarginLeft => _designerSettings.Value.DefaultMargins.Left;
    public int MarginBottom => _designerSettings.Value.DefaultMargins.Bottom;
    public int MarginRight => _designerSettings.Value.DefaultMargins.Right;

    // Vite bundle path (resolved from manifest.json)
    public string DesignerBundlePath { get; set; } = string.Empty;

    /// <summary>
    /// Load existing template for editing, or create new template
    /// </summary>
    public async Task<IActionResult> OnGetAsync(int? id, int? connectionId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            return Unauthorized();
        }

        // Load Vite manifest to get designer bundle path
        try
        {
            var manifestPath = Path.Combine(_webHostEnvironment.WebRootPath, "dist", ".vite", "manifest.json");

            _logger.LogInformation("Loading Vite manifest from: {ManifestPath}", manifestPath);

            if (!System.IO.File.Exists(manifestPath))
            {
                _logger.LogWarning("Vite manifest not found at {ManifestPath}, using fallback", manifestPath);
                DesignerBundlePath = "assets/designer-Bx9c5oHc.js"; // Fallback to current hash
            }
            else
            {
                var manifestJson = await System.IO.File.ReadAllTextAsync(manifestPath);

                // Use case-insensitive deserialization (Vite manifest uses lowercase property names)
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                var manifest = JsonSerializer.Deserialize<Dictionary<string, ViteManifestEntry>>(manifestJson, options);

                if (manifest != null && manifest.TryGetValue("src/pages/designer/designer.entry.ts", out var entry))
                {
                    DesignerBundlePath = entry.File;
                    _logger.LogInformation("Designer bundle resolved to: {BundlePath}", DesignerBundlePath);
                }
                else
                {
                    _logger.LogWarning("Designer entry not found in Vite manifest");
                    DesignerBundlePath = "assets/designer-BpCILmFg.js"; // Fallback to current hash
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load Vite manifest");
            DesignerBundlePath = "assets/designer-BpCILmFg.js"; // Fallback to current hash
        }

        // Load user's connections for dropdown
        UserConnections = await _db.Connections
            .Where(c => c.UserId == userId)
            .OrderBy(c => c.DisplayName)
            .ToListAsync();

        if (id.HasValue)
        {
            // Edit mode - load existing template
            var template = await _db.StickerTemplates
                .Include(t => t.Connection)
                .FirstOrDefaultAsync(t => t.Id == id.Value);

            if (template == null)
            {
                return NotFound();
            }

            // Verify user has access to this template
            if (template.ConnectionId.HasValue)
            {
                // User template - must belong to this user's connection
                if (template.Connection?.UserId != userId)
                {
                    return Forbid();
                }
            }

            Template = template;
            IsEditMode = true;
            IsSystemTemplate = template.IsSystemTemplate;

            // Load global variables for this connection (if applicable)
            if (template.ConnectionId.HasValue)
            {
                GlobalVariables = await _db.GlobalVariables
                    .Where(v => v.ConnectionId == template.ConnectionId.Value)
                    .OrderBy(v => v.VariableName)
                    .ToListAsync();

                // Load uploaded images for this connection
                UploadedImages = await _db.UploadedImages
                    .Where(i => i.ConnectionId == template.ConnectionId.Value && !i.IsDeleted)
                    .OrderBy(i => i.Name)
                    .ToListAsync();
            }
        }
        else
        {
            // Create mode - new template
            Template = new StickerTemplate
            {
                Name = "New Template",
                Description = "",
                PageWidth = 100.0,
                PageHeight = 50.0,
                IsSystemTemplate = false,
                ConnectionId = connectionId,
                TemplateJson = CreateBlankTemplateJson()
            };

            IsEditMode = false;

            // Load global variables if connection specified
            if (connectionId.HasValue)
            {
                GlobalVariables = await _db.GlobalVariables
                    .Where(v => v.ConnectionId == connectionId.Value)
                    .OrderBy(v => v.VariableName)
                    .ToListAsync();

                // Load uploaded images for this connection
                UploadedImages = await _db.UploadedImages
                    .Where(i => i.ConnectionId == connectionId.Value && !i.IsDeleted)
                    .OrderBy(i => i.Name)
                    .ToListAsync();
            }
        }

        return Page();
    }

    /// <summary>
    /// Save template (create new or update existing)
    /// </summary>
    public async Task<IActionResult> OnPostAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            return Unauthorized();
        }

        // Validate connection ownership if specified
        if (Template.ConnectionId.HasValue)
        {
            var connection = await _db.Connections
                .FirstOrDefaultAsync(c => c.Id == Template.ConnectionId.Value);

            if (connection == null || connection.UserId != userId)
            {
                ModelState.AddModelError("", "Invalid connection selected.");
                return Page();
            }
        }

        // Validate template JSON is not empty
        if (string.IsNullOrWhiteSpace(Template.TemplateJson))
        {
            ModelState.AddModelError("", "Template design cannot be empty.");
            return Page();
        }

        // Validate template JSON size (max 5 MB to allow complex designs while preventing DoS)
        const int MAX_TEMPLATE_JSON_SIZE = 5_000_000; // 5 MB
        if (Template.TemplateJson.Length > MAX_TEMPLATE_JSON_SIZE)
        {
            var sizeMB = Template.TemplateJson.Length / 1024.0 / 1024.0;
            ModelState.AddModelError("", $"Template too complex ({sizeMB:F2} MB). Maximum size: 5 MB");
            return Page();
        }

        // Validate custom image limit (max 4 per template)
        try
        {
            var templateJson = System.Text.Json.JsonDocument.Parse(Template.TemplateJson);
            if (templateJson.RootElement.TryGetProperty("objects", out var objects) && objects.ValueKind == System.Text.Json.JsonValueKind.Array)
            {
                int customImageCount = 0;
                foreach (var obj in objects.EnumerateArray())
                {
                    if (obj.TryGetProperty("properties", out var properties) &&
                        properties.TryGetProperty("customImageId", out var customImageId) &&
                        customImageId.ValueKind != System.Text.Json.JsonValueKind.Null)
                    {
                        customImageCount++;
                    }
                }

                if (customImageCount > 4)
                {
                    ModelState.AddModelError("", $"Template cannot contain more than 4 custom images. Found: {customImageCount}");
                    return Page();
                }
            }
        }
        catch (System.Text.Json.JsonException ex)
        {
            _logger.LogWarning("Invalid template JSON format: {Error}", ex.Message);
            ModelState.AddModelError("", "Invalid template format.");
            return Page();
        }

        if (Template.Id == 0)
        {
            // Create new template
            Template.CreatedAt = DateTime.UtcNow;
            Template.UpdatedAt = DateTime.UtcNow;
            _db.StickerTemplates.Add(Template);
            _logger.LogInformation("Creating new template '{Name}' for user {UserId}", LogSanitizer.Sanitize(Template.Name), userId);
        }
        else
        {
            // Update existing template
            var existingTemplate = await _db.StickerTemplates
                .Include(t => t.Connection)
                .FirstOrDefaultAsync(t => t.Id == Template.Id);

            if (existingTemplate == null)
            {
                return NotFound();
            }

            // System templates cannot be modified - user must clone
            if (existingTemplate.IsSystemTemplate)
            {
                _logger.LogWarning("User {UserId} attempted to modify system template {TemplateId}",
                    userId, existingTemplate.Id);
                ModelState.AddModelError("", "Cannot modify system templates. Please clone the template first.");
                return BadRequest(new { error = "Cannot modify system templates. Please clone the template first." });
            }

            if (existingTemplate.ConnectionId.HasValue)
            {
                if (existingTemplate.Connection?.UserId != userId)
                {
                    return Forbid();
                }
            }

            // Update properties
            existingTemplate.Name = Template.Name;
            existingTemplate.Description = Template.Description;
            existingTemplate.PageWidth = Template.PageWidth;
            existingTemplate.PageHeight = Template.PageHeight;
            existingTemplate.TemplateJson = Template.TemplateJson;
            existingTemplate.UpdatedAt = DateTime.UtcNow;

            _logger.LogInformation("Updating template {Id} '{Name}' for user {UserId}",
                existingTemplate.Id, LogSanitizer.Sanitize(existingTemplate.Name), userId);
        }

        await _db.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Template '{Template.Name}' saved successfully!";
        return RedirectToPage("/Templates/Designer", new { id = Template.Id });
    }

    /// <summary>
    /// Creates a blank template JSON for new templates
    /// </summary>
    private static string CreateBlankTemplateJson()
    {
        return @"{
  ""version"": ""1.0"",
  ""fabricVersion"": ""5.3.0"",
  ""pageSize"": {
    ""width"": 100,
    ""height"": 50,
    ""unit"": ""mm""
  },
  ""objects"": []
}";
    }
}
