# Phase 6 Prerequisite: ProductType Template Filtering

**Status:** Planned
**Dependencies:** Phase 5 (Template Management System)
**Related Features:** Phase 6 - Template Selection During Export

## Overview

Currently, templates (`StickerTemplate`) are device-type-agnostic - any template can be used with any device. While `ConnectionDefaultTemplate` maps ProductTypes to default templates, there's no mechanism to restrict which templates are compatible with which device types.

This prerequisite adds **ProductType compatibility filtering** to templates, allowing:
- Templates to declare which Meraki device types they support
- Export UI to show only compatible/recommended templates
- Better user experience by preventing incompatible template selections
- Flexible filtering (templates can support multiple ProductTypes)

## User Stories

1. **As a user**, I want to mark my "Rack Mount Switch" template as only compatible with switches, so I don't accidentally use it for wireless APs.

2. **As a user**, I want to create a "Universal Device Label" template that works with all device types, so I have a fallback option.

3. **As a system**, I want to filter template options during export based on device ProductType, so users see relevant templates first.

## Current State Analysis

### Existing ProductType Usage

**Meraki Device Types** (from API):
- `wireless` - Access Points
- `switch` - Network Switches
- `appliance` - Security Appliances/Firewalls
- `camera` - Security Cameras
- `sensor` - Environmental Sensors
- `cellularGateway` - Cellular Gateways

**Current Implementation:**
- ✅ `CachedDevice.ProductType` stores device type (from Meraki API)
- ✅ `ConnectionDefaultTemplate` maps ProductType → default TemplateId per connection
- ❌ `StickerTemplate` has NO ProductType compatibility field
- ❌ No filtering logic based on ProductType compatibility

### Current Template Model (src/StickerTemplate.cs)

```csharp
public class StickerTemplate
{
    public int Id { get; set; }
    public int? ConnectionId { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsSystemTemplate { get; set; }
    public double PageWidth { get; set; }
    public double PageHeight { get; set; }
    public string TemplateJson { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? LastUsedAt { get; set; }

    // No ProductType compatibility!
}
```

## Proposed Solution

### 1. Database Schema Changes

Add a new field to `StickerTemplate` to store compatible ProductTypes:

```csharp
/// <summary>
/// JSON array of compatible Meraki ProductTypes (e.g., ["wireless", "switch"])
/// NULL or empty array means compatible with ALL types (universal template)
/// </summary>
[Column(TypeName = "nvarchar(max)")]
public string? CompatibleProductTypes { get; set; }
```

**Why JSON instead of separate table?**
- ProductTypes are static (from Meraki API, rarely change)
- Small set of values (6 types currently)
- Simpler queries and schema
- Easy to serialize/deserialize in C# and JavaScript

### 2. Helper Methods on StickerTemplate

Add convenience methods for working with ProductType compatibility:

```csharp
// In StickerTemplate.cs
using System.Text.Json;

/// <summary>
/// Gets the list of compatible ProductTypes.
/// Returns null if template is compatible with all types.
/// </summary>
[NotMapped]
public List<string>? GetCompatibleProductTypes()
{
    if (string.IsNullOrWhiteSpace(CompatibleProductTypes))
        return null; // Universal template

    return JsonSerializer.Deserialize<List<string>>(CompatibleProductTypes);
}

/// <summary>
/// Sets the compatible ProductTypes for this template.
/// Pass null or empty list to make template universal.
/// </summary>
public void SetCompatibleProductTypes(List<string>? productTypes)
{
    if (productTypes == null || productTypes.Count == 0)
    {
        CompatibleProductTypes = null;
        return;
    }

    CompatibleProductTypes = JsonSerializer.Serialize(productTypes);
}

/// <summary>
/// Checks if this template is compatible with the given ProductType.
/// Returns true for universal templates (no restrictions).
/// </summary>
public bool IsCompatibleWith(string productType)
{
    var compatibleTypes = GetCompatibleProductTypes();

    // Null means universal (compatible with all)
    if (compatibleTypes == null)
        return true;

    return compatibleTypes.Contains(productType, StringComparer.OrdinalIgnoreCase);
}
```

### 3. Migration Plan

**New Migration: `AddProductTypeFilteringToTemplates`**

```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.AddColumn<string>(
        name: "CompatibleProductTypes",
        table: "StickerTemplates",
        type: "nvarchar(max)",
        nullable: true);
}

protected override void Down(MigrationBuilder migrationBuilder)
{
    migrationBuilder.DropColumn(
        name: "CompatibleProductTypes",
        table: "StickerTemplates");
}
```

**Data Migration for Existing Templates:**

After running the migration, existing templates will have `CompatibleProductTypes = NULL`, making them universal (compatible with all types). This is safe and backward-compatible.

**Optional:** Create a migration seed script to set ProductType compatibility based on template names:
- Templates with "switch", "rack" in name → `["switch"]`
- Templates with "AP", "wireless", "ceiling" in name → `["wireless"]`
- Templates with "camera" in name → `["camera"]`
- Others → NULL (universal)

### 4. Template Designer UI Changes

**Template Designer Page** (src/Pages/Templates/Designer.cshtml)

Add a ProductType selector section in the template metadata form:

```html
<!-- After template name/description fields -->
<div class="form-group">
    <label>Compatible Device Types</label>
    <p class="text-muted small">Select which Meraki device types this template is designed for. Leave all unchecked for universal templates.</p>

    <div class="checkbox-group">
        <label class="checkbox-inline">
            <input type="checkbox" name="CompatibleProductTypes" value="wireless" /> Wireless (Access Points)
        </label>
        <label class="checkbox-inline">
            <input type="checkbox" name="CompatibleProductTypes" value="switch" /> Switches
        </label>
        <label class="checkbox-inline">
            <input type="checkbox" name="CompatibleProductTypes" value="appliance" /> Appliances (Firewalls)
        </label>
        <label class="checkbox-inline">
            <input type="checkbox" name="CompatibleProductTypes" value="camera" /> Cameras
        </label>
        <label class="checkbox-inline">
            <input type="checkbox" name="CompatibleProductTypes" value="sensor" /> Sensors
        </label>
        <label class="checkbox-inline">
            <input type="checkbox" name="CompatibleProductTypes" value="cellularGateway" /> Cellular Gateways
        </label>
    </div>
</div>
```

**Backend Handling** (src/Pages/Templates/Designer.cshtml.cs):

```csharp
public async Task<IActionResult> OnPostAsync()
{
    // ... existing validation ...

    // Get selected ProductTypes from form
    var selectedProductTypes = Request.Form["CompatibleProductTypes"].ToList();

    // Set compatibility (empty list becomes null = universal)
    template.SetCompatibleProductTypes(
        selectedProductTypes.Count > 0 ? selectedProductTypes : null
    );

    // ... save template ...
}
```

### 5. Template Service Updates

**New Methods in TemplateService** (src/TemplateService.cs):

```csharp
/// <summary>
/// Gets templates compatible with the specified ProductType for a connection.
/// Includes both explicitly compatible templates and universal templates (null compatibility).
/// </summary>
public async Task<List<StickerTemplate>> GetCompatibleTemplatesAsync(
    int connectionId,
    string productType)
{
    var userId = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userId == null) return new List<StickerTemplate>();

    // Get all templates for this connection (custom + system)
    var allTemplates = await _context.StickerTemplates
        .Where(t => t.ConnectionId == connectionId || t.IsSystemTemplate)
        .ToListAsync();

    // Filter to compatible templates
    return allTemplates
        .Where(t => t.IsCompatibleWith(productType))
        .OrderBy(t => t.Name)
        .ToList();
}

/// <summary>
/// Gets templates with compatibility metadata for filtering in export UI.
/// Returns templates grouped by compatibility status.
/// </summary>
public async Task<TemplateFilterResult> GetTemplatesForExportAsync(
    int connectionId,
    string deviceProductType)
{
    var userId = _httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userId == null) throw new UnauthorizedAccessException();

    // Get recommended template (from ConnectionDefaultTemplate)
    var recommendedTemplate = await GetRecommendedTemplateAsync(connectionId, deviceProductType);

    // Get all available templates
    var allTemplates = await _context.StickerTemplates
        .Where(t => t.ConnectionId == connectionId || t.IsSystemTemplate)
        .ToListAsync();

    return new TemplateFilterResult
    {
        RecommendedTemplate = recommendedTemplate,
        CompatibleTemplates = allTemplates
            .Where(t => t.IsCompatibleWith(deviceProductType) && t.Id != recommendedTemplate?.Id)
            .OrderBy(t => t.Name)
            .ToList(),
        IncompatibleTemplates = allTemplates
            .Where(t => !t.IsCompatibleWith(deviceProductType))
            .OrderBy(t => t.Name)
            .ToList()
    };
}

private async Task<StickerTemplate?> GetRecommendedTemplateAsync(int connectionId, string productType)
{
    var defaultMapping = await _context.ConnectionDefaultTemplates
        .Include(cdt => cdt.Template)
        .FirstOrDefaultAsync(cdt =>
            cdt.ConnectionId == connectionId &&
            cdt.ProductType == productType &&
            cdt.TemplateId != null);

    return defaultMapping?.Template;
}
```

**New DTO Class**:

```csharp
public class TemplateFilterResult
{
    public StickerTemplate? RecommendedTemplate { get; set; }
    public List<StickerTemplate> CompatibleTemplates { get; set; } = new();
    public List<StickerTemplate> IncompatibleTemplates { get; set; } = new();
}
```

### 6. Template Matching Service Updates

**Update TemplateMatchingService** (src/Services/TemplateMatchingService.cs):

The existing matching logic should incorporate compatibility:

```csharp
public async Task<TemplateMatchResult> FindTemplateForDeviceAsync(CachedDevice device, int connectionId)
{
    // Priority 1: ConnectionDefaultTemplate for this ProductType
    var defaultTemplate = await _context.ConnectionDefaultTemplates
        .Include(cdt => cdt.Template)
        .FirstOrDefaultAsync(cdt =>
            cdt.ConnectionId == connectionId &&
            cdt.ProductType == device.ProductType &&
            cdt.TemplateId != null);

    if (defaultTemplate?.Template != null)
    {
        return new TemplateMatchResult
        {
            Template = defaultTemplate.Template,
            MatchConfidence = 1.0,
            MatchReason = $"Default template for {device.ProductType} devices"
        };
    }

    // Priority 2: Compatible templates (NEW: filter by compatibility)
    var compatibleTemplates = await _context.StickerTemplates
        .Where(t => t.ConnectionId == connectionId || t.IsSystemTemplate)
        .ToListAsync();

    var compatibleTemplate = compatibleTemplates
        .FirstOrDefault(t => t.IsCompatibleWith(device.ProductType));

    if (compatibleTemplate != null)
    {
        return new TemplateMatchResult
        {
            Template = compatibleTemplate,
            MatchConfidence = 0.6,
            MatchReason = $"Compatible with {device.ProductType} devices"
        };
    }

    // Priority 3: Universal fallback (any template)
    var fallbackTemplate = compatibleTemplates.FirstOrDefault();

    if (fallbackTemplate != null)
    {
        return new TemplateMatchResult
        {
            Template = fallbackTemplate,
            MatchConfidence = 0.1,
            MatchReason = "Universal fallback template (may not be optimized for this device type)"
        };
    }

    throw new InvalidOperationException($"No templates available for device {device.Name}");
}
```

## Implementation Steps

### Step 1: Database Schema (Backend)
1. Add `CompatibleProductTypes` property to `StickerTemplate.cs`
2. Add helper methods (`GetCompatibleProductTypes`, `SetCompatibleProductTypes`, `IsCompatibleWith`)
3. Create EF Core migration: `dotnet ef migrations add AddProductTypeFilteringToTemplates`
4. Review migration SQL script
5. Apply migration: `dotnet ef database update`

### Step 2: Service Layer Updates
1. Update `TemplateService.cs`:
   - Add `GetCompatibleTemplatesAsync()`
   - Add `GetTemplatesForExportAsync()`
   - Add `TemplateFilterResult` DTO class
2. Update `TemplateMatchingService.cs`:
   - Modify `FindTemplateForDeviceAsync()` to use compatibility checking
   - Update match confidence and reasons

### Step 3: Template Designer UI
1. Update `Pages/Templates/Designer.cshtml`:
   - Add ProductType checkbox group to form
2. Update `Pages/Templates/Designer.cshtml.cs`:
   - Handle `CompatibleProductTypes` form data
   - Call `SetCompatibleProductTypes()` on save
   - Populate checkboxes on GET for editing existing templates

### Step 4: Template List UI (Optional Enhancement)
1. Update `Pages/Templates/Index.cshtml`:
   - Display ProductType compatibility badges/icons for each template
   - Add filter dropdown to show only templates for specific ProductType

### Step 5: Testing
1. **Unit Tests** (new file: `QRStickers.Tests/Models/StickerTemplateTests.cs`):
   - Test `IsCompatibleWith()` for various scenarios
   - Test universal templates (null compatibility)
   - Test JSON serialization/deserialization
2. **Integration Tests** (new file: `QRStickers.Tests/Services/TemplateFilteringTests.cs`):
   - Test `GetCompatibleTemplatesAsync()`
   - Test `GetTemplatesForExportAsync()`
   - Verify recommended/compatible/incompatible grouping
3. **Manual Testing**:
   - Create template with specific ProductType compatibility
   - Verify checkboxes save/load correctly
   - Verify universal templates work with all device types
   - Verify filtering in template selection (Phase 6 feature)

## Migration Strategy for Existing Data

**After migration, all existing templates will have `CompatibleProductTypes = NULL` (universal).**

This is safe and backward-compatible:
- ✅ Existing templates continue to work with all device types
- ✅ No breaking changes to export workflow
- ✅ Users can gradually add ProductType restrictions as needed

**Optional: Smart Migration Script**

If desired, create a data migration to intelligently set ProductType compatibility based on template names:

```csharp
// After migration, run this seeder
public static async Task MigrateExistingTemplateCompatibility(QRStickersDbContext context)
{
    var templates = await context.StickerTemplates.ToListAsync();

    foreach (var template in templates)
    {
        var nameLower = template.Name.ToLower();

        if (nameLower.Contains("switch") || nameLower.Contains("rack"))
        {
            template.SetCompatibleProductTypes(new List<string> { "switch" });
        }
        else if (nameLower.Contains("wireless") || nameLower.Contains("ap") || nameLower.Contains("ceiling"))
        {
            template.SetCompatibleProductTypes(new List<string> { "wireless" });
        }
        else if (nameLower.Contains("camera"))
        {
            template.SetCompatibleProductTypes(new List<string> { "camera" });
        }
        // Others remain NULL (universal)
    }

    await context.SaveChangesAsync();
}
```

## Files to Modify

**Models:**
- `src/StickerTemplate.cs` - Add `CompatibleProductTypes` property and helper methods

**Services:**
- `src/TemplateService.cs` - Add filtering methods
- `src/Services/TemplateMatchingService.cs` - Update matching logic

**Pages:**
- `src/Pages/Templates/Designer.cshtml` - Add ProductType checkboxes
- `src/Pages/Templates/Designer.cshtml.cs` - Handle form data
- (Optional) `src/Pages/Templates/Index.cshtml` - Display compatibility badges

**Database:**
- New migration: `Migrations/YYYYMMDDHHMMSS_AddProductTypeFilteringToTemplates.cs`

**Tests:**
- `QRStickers.Tests/Models/StickerTemplateTests.cs` - NEW
- `QRStickers.Tests/Services/TemplateFilteringTests.cs` - NEW

## Challenges and Solutions

### Challenge 1: ProductType Value Consistency
**Problem:** ProductTypes come from Meraki API - what if they add new types?

**Solution:**
- Store as JSON array (flexible)
- UI checkboxes can be updated without schema changes
- Universal templates (NULL) automatically support new types

### Challenge 2: Migrating Existing Templates
**Problem:** Users may have existing templates with no ProductType set.

**Solution:**
- Default to NULL (universal) for backward compatibility
- Provide optional migration script based on template names
- Allow users to manually set compatibility later

### Challenge 3: UI Complexity in Designer
**Problem:** 6 checkboxes may clutter the designer form.

**Solution:**
- Place in collapsible/expandable section
- Default to expanded when creating new template
- Show summary badge when collapsed ("Compatible with: Switch, Wireless")

### Challenge 4: Performance for Large Template Libraries
**Problem:** Filtering large numbers of templates by ProductType.

**Solution:**
- JSON column is indexed (for SQL Server full-text search if needed)
- In-memory filtering is fast for typical template counts (< 100)
- Consider caching if performance issues arise

## Success Criteria

- ✅ Templates can store compatible ProductTypes as JSON array
- ✅ NULL compatibility means universal (works with all types)
- ✅ Helper methods correctly check compatibility
- ✅ Template designer UI allows selecting ProductTypes via checkboxes
- ✅ Existing templates default to universal (NULL) after migration
- ✅ TemplateService filters templates by compatibility
- ✅ TemplateMatchingService prioritizes compatible templates
- ✅ Unit tests pass for all compatibility scenarios
- ✅ No breaking changes to existing export workflow

## Future Enhancements

1. **Template Library Filtering**: Filter template list by ProductType
2. **Bulk Update**: Admin UI to bulk-set ProductType compatibility
3. **Smart Recommendations**: Suggest ProductTypes based on template content (e.g., QR code placeholders)
4. **Validation Warnings**: Warn users if template placeholders don't match device ProductType (e.g., "This template expects switch-specific fields")

## Risks and Mitigation

| Risk | Impact | Mitigation |
|------|--------|------------|
| Meraki adds new ProductTypes | Templates won't support new types | Universal templates (NULL) automatically support new types |
| User accidentally restricts template too much | Template not available for export | Clear UI messaging, easy to change compatibility |
| Migration breaks existing workflows | Users can't export | Default to NULL (universal) ensures backward compatibility |
| JSON deserialization errors | Runtime exceptions | Add try-catch with fallback to universal, log errors |

## Timeline Estimate

- Database schema + migration: **2 hours**
- Service layer updates: **3 hours**
- Template designer UI: **4 hours**
- Testing: **3 hours**
- **Total: ~12 hours** (1.5 days)

## Next Steps

1. Review and approve this plan
2. Implement Step 1 (database schema)
3. Implement Step 2 (service layer)
4. Implement Step 3 (template designer UI)
5. Complete testing
6. Proceed to Phase 6 main feature (Template Selection During Export)
