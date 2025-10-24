using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QRStickers.Meraki;

namespace QRStickers.Data;

/// <summary>
/// Seeds the database with a demo user account and demo connection with fake data
/// Used for marketing screenshots and demonstrations
/// </summary>
public static class DemoAccountSeeder
{
    private const string DemoEmail = "demo@qrstickers.example.com";
    private const string DemoPassword = "Demo123!"; // Simple password for demo purposes
    private const string DemoDisplayName = "Demo User";
    private const string DemoConnectionName = "Demo Account";

    /// <summary>
    /// Seeds or updates demo account
    /// </summary>
    public static async Task SeedDemoAccountAsync(
        QRStickersDbContext db,
        UserManager<ApplicationUser> userManager)
    {
        // Check if demo user already exists
        var demoUser = await userManager.FindByEmailAsync(DemoEmail);

        if (demoUser == null)
        {
            // Create demo user
            demoUser = new ApplicationUser
            {
                UserName = DemoEmail,
                Email = DemoEmail,
                EmailConfirmed = true, // Auto-confirm for demo
                DisplayName = DemoDisplayName
            };

            var result = await userManager.CreateAsync(demoUser, DemoPassword);

            if (!result.Succeeded)
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to create demo user: {errors}");
            }
        }

        // Check if demo connection already exists
        var existingDemoConnection = await db.Connections
            .FirstOrDefaultAsync(c => c.UserId == demoUser.Id && c.IsDemo);

        if (existingDemoConnection != null)
        {
            // Demo connection already exists, no need to re-seed
            return;
        }

        // Create demo connection
        var demoConnection = new MerakiConnection
        {
            UserId = demoUser.Id,
            DisplayName = DemoConnectionName,
            IsDemo = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        db.Connections.Add(demoConnection);
        await db.SaveChangesAsync();

        // Seed default template mappings (like OAuth connections do)
        await SeedConnectionDefaultTemplatesAsync(db, demoConnection.Id);

        // Sync demo data (this will populate CachedOrganizations, CachedNetworks, CachedDevices)
        // We need to create a scoped orchestrator instance
        // Note: This will be called from Program.cs where services are available
    }

    /// <summary>
    /// Syncs demo data for the demo connection (called after connection is created)
    /// </summary>
    public static async Task SyncDemoDataAsync(
        QRStickersDbContext db,
        MerakiSyncOrchestrator orchestrator)
    {
        // Find the demo connection
        var demoConnection = await db.Connections
            .FirstOrDefaultAsync(c => c.IsDemo);

        if (demoConnection == null)
        {
            return; // No demo connection to sync
        }

        // Run sync for demo connection (will use DemoMerakiService)
        await orchestrator.SyncConnectionDataAsync(demoConnection.Id);
    }

    /// <summary>
    /// Seeds ConnectionDefaultTemplates for a newly created connection
    /// Maps ProductTypes to system templates (rack vs ceiling/wall)
    /// Same logic as Callback.cshtml.cs to ensure consistency
    /// </summary>
    private static async Task SeedConnectionDefaultTemplatesAsync(QRStickersDbContext db, int connectionId)
    {
        // Get system template IDs
        var rackTemplate = await db.StickerTemplates
            .Where(t => t.IsSystemTemplate && t.Name == "Rack Mount Default")
            .FirstOrDefaultAsync();

        var ceilingTemplate = await db.StickerTemplates
            .Where(t => t.IsSystemTemplate && t.Name == "Ceiling/Wall Mount Default")
            .FirstOrDefaultAsync();

        if (rackTemplate == null || ceilingTemplate == null)
        {
            // Templates not seeded yet, skip (this can happen on first run before SystemTemplateSeeder runs)
            return;
        }

        // Define ProductType → Template mappings
        var productTypeMappings = new[]
        {
            ("switch", rackTemplate.Id),
            ("appliance", rackTemplate.Id),
            ("wireless", ceilingTemplate.Id),
            ("camera", ceilingTemplate.Id),
            ("sensor", ceilingTemplate.Id),
            ("cellularGateway", ceilingTemplate.Id)
        };

        // Create ConnectionDefaultTemplate entries
        foreach (var (productType, templateId) in productTypeMappings)
        {
            db.ConnectionDefaultTemplates.Add(new ConnectionDefaultTemplate
            {
                ConnectionId = connectionId,
                ProductType = productType,
                TemplateId = templateId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
        }

        await db.SaveChangesAsync();
    }
}
