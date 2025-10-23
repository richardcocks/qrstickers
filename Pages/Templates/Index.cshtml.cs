using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace QRStickers.Pages.Templates;

[Authorize]
public class IndexModel : PageModel
{
    private readonly QRStickersDbContext _db;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(QRStickersDbContext db, ILogger<IndexModel> logger)
    {
        _db = db;
        _logger = logger;
    }

    public List<StickerTemplate> Templates { get; set; } = new();
    public List<Connection> UserConnections { get; set; } = new();

    // Filter properties
    public int? SelectedConnectionId { get; set; }
    public bool? ShowSystemTemplates { get; set; } = true;
    public string? SearchQuery { get; set; }

    public async Task<IActionResult> OnGetAsync(
        int? connectionId,
        bool? systemTemplates,
        string? search)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null)
        {
            return Unauthorized();
        }

        // Load user's connections
        UserConnections = await _db.Connections
            .Where(c => c.UserId == userId)
            .OrderBy(c => c.DisplayName)
            .ToListAsync();

        // Build query - system templates OR user's connection templates
        var query = _db.StickerTemplates
            .Include(t => t.Connection)
            .Where(t =>
                t.IsSystemTemplate ||
                (t.ConnectionId.HasValue &&
                 UserConnections.Select(c => c.Id).Contains(t.ConnectionId.Value))
            );

        // Apply filters
        if (connectionId.HasValue)
        {
            query = query.Where(t => t.ConnectionId == connectionId);
        }

        if (systemTemplates.HasValue && !systemTemplates.Value)
        {
            query = query.Where(t => !t.IsSystemTemplate);
        }

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(t =>
                t.Name.Contains(search) ||
                (t.Description != null && t.Description.Contains(search))
            );
        }

        Templates = await query
            .OrderByDescending(t => t.IsSystemTemplate)
            .ThenBy(t => t.Name)
            .ToListAsync();

        // Set filter state for UI
        SelectedConnectionId = connectionId;
        ShowSystemTemplates = systemTemplates ?? true; // Default to true (show system templates)
        SearchQuery = search;

        return Page();
    }
}
