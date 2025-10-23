# Phase 3: Template Management - Implementation Plan

**Project:** QRStickers
**Feature:** Template Management UI
**Date Created:** 2025-10-21
**Status:** Planning Phase
**Phase Status:** Ready to Begin

---

## Executive Summary

Phase 3 implements the template management interface, allowing users to:
- Browse all templates (system and user-created) in a searchable, filterable list
- Create new templates from scratch or by cloning existing ones
- Clone system templates for customization
- Delete user-created templates with confirmation
- Navigate seamlessly between template management and the designer

This phase builds on Phase 2's designer implementation and prepares the foundation for Phase 4 (multi-format export).

---

## What's Already Complete (Phase 2)

✅ **Designer UI** - Fully functional Fabric.js canvas with drag/drop
✅ **Template Persistence** - Save/load templates to/from database via AJAX
✅ **StickerTemplate Entity** - Database model with all required properties
✅ **TemplateService** - Template selection logic for devices
✅ **System Templates** - Seeder with 2 default templates
✅ **Designer GET/POST** - Template load and save handlers
✅ **CRUD Operations** - Basic template creation and updates

---

## Phase 3 Deliverables

### 1. Template Index Page (`Pages/Templates/Index.cshtml[.cs]`)

**Purpose:** Central dashboard for all templates

**Features:**
- Display grid/list of all templates available to user (system + connection-specific)
- Filterable by:
  - Connection (dropdown)
  - Product type (all/switch/wireless/appliance/camera/sensor)
  - Rack mount vs. ceiling/wall mount
  - Show/hide system templates (checkbox)
- Search by template name and description
- Visual indicators:
  - System template badge (read-only, cloneable)
  - Default template star icon
  - Template dimensions (e.g., "100mm x 50mm")
  - Connection name
  - Created date
  - Rack/ceiling icon
- Action buttons per template:
  - **Edit** - Opens Designer (user templates only)
  - **Clone** - Creates copy and opens Designer (all templates)
  - **Delete** - Confirmation dialog (user templates only)
  - **Preview** - Canvas preview in modal (nice-to-have)
- "New Template" button (primary action, top-right)
- Empty state message ("No templates found")

**Layout:**
- Responsive card grid (3 cols desktop, 2 tablet, 1 mobile)
- Top toolbar with filters and search
- Sorting: System templates first, then by default, then by name

**Implementation Details:**

```csharp
[Authorize]
public class IndexModel : PageModel
{
    private readonly QRStickersDbContext _db;
    private readonly ILogger<IndexModel> _logger;

    public List<StickerTemplate> Templates { get; set; } = new();
    public List<Connection> UserConnections { get; set; } = new();

    // Filter properties
    public int? SelectedConnectionId { get; set; }
    public string? ProductTypeFilter { get; set; }
    public bool? IsRackMount { get; set; }
    public bool? ShowSystemTemplates { get; set; } = true;
    public string? SearchQuery { get; set; }

    public async Task<IActionResult> OnGetAsync(
        int? connectionId,
        string? productType,
        bool? rackMount,
        bool? systemTemplates,
        string? search)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

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
            query = query.Where(t => t.ConnectionId == connectionId);

        if (!string.IsNullOrEmpty(productType))
            query = query.Where(t => t.ProductTypeFilter == productType);

        if (rackMount.HasValue)
            query = query.Where(t => t.IsRackMount == rackMount.Value);

        if (systemTemplates.HasValue && !systemTemplates.Value)
            query = query.Where(t => !t.IsSystemTemplate);

        if (!string.IsNullOrEmpty(search))
            query = query.Where(t =>
                t.Name.Contains(search) ||
                (t.Description != null && t.Description.Contains(search))
            );

        Templates = await query
            .OrderByDescending(t => t.IsSystemTemplate)
            .ThenByDescending(t => t.IsDefault)
            .ThenBy(t => t.Name)
            .ToListAsync();

        SelectedConnectionId = connectionId;
        ProductTypeFilter = productType;
        IsRackMount = rackMount;
        ShowSystemTemplates = systemTemplates ?? true;
        SearchQuery = search;

        return Page();
    }
}
```

---

### 2. Template Create Wizard (`Pages/Templates/Create.cshtml[.cs]`)

**Purpose:** Simplified workflow to create new templates

**Features:**
- 2-step process (can be single page with collapsible sections)

**Step 1: Template Properties**
- Template name (required, default: "New Template")
- Description (optional, 1000 char max)
- Select connection (required dropdown, user's connections only)
- Page size:
  - Quick presets: "100×50mm (Rack)", "60×60mm (Ceiling/Wall)", "Custom"
  - Custom width/height number inputs (mm)
- Product type filter (optional dropdown)
- Is rack mount? (checkbox)
- Set as default? (checkbox with validation)

**Step 2: Starting Point**
- Option A: "Start from blank canvas" (radio button)
- Option B: "Clone system template" (radio button + list/grid)
  - Shows system templates with preview cards
  - Pre-selects if only 2 templates exist

**Result:** Creates template and redirects to `/Templates/Designer?id={newId}`

**Implementation Details:**

```csharp
[Authorize]
public class CreateModel : PageModel
{
    private readonly QRStickersDbContext _db;

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

    public async Task<IActionResult> OnGetAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        UserConnections = await _db.Connections
            .Where(c => c.UserId == userId)
            .ToListAsync();

        SystemTemplates = await _db.StickerTemplates
            .Where(t => t.IsSystemTemplate)
            .ToListAsync();

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        // Validate connection ownership
        var connection = await _db.Connections
            .FirstOrDefaultAsync(c => c.Id == ConnectionId && c.UserId == userId);

        if (connection == null)
        {
            ModelState.AddModelError("", "Invalid connection selected.");
            return Page();
        }

        StickerTemplate newTemplate;

        if (CloneFromTemplateId.HasValue)
        {
            var sourceTemplate = await _db.StickerTemplates
                .FindAsync(CloneFromTemplateId.Value);

            if (sourceTemplate == null) return NotFound();

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
        }
        else
        {
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
        }

        _db.StickerTemplates.Add(newTemplate);
        await _db.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Template '{newTemplate.Name}' created!";
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
```

---

### 3. Template Delete Page (`Pages/Templates/Delete.cshtml[.cs]`)

**Purpose:** Safe deletion with confirmation

**Features:**
- Show template details (name, description, dimensions, created date, connection)
- Canvas preview (if feasible)
- Confirmation message: "Are you sure you want to delete this template?"
- Warning message (if default template): "This is your default template. After deletion, devices will fall back to system templates."
- Cannot delete system templates (redirect with error)
- Buttons: "Delete" (red/danger), "Cancel" (returns to Index)

**Implementation Details:**

```csharp
[Authorize]
public class DeleteModel : PageModel
{
    private readonly QRStickersDbContext _db;
    private readonly ILogger<DeleteModel> _logger;

    public StickerTemplate Template { get; set; } = null!;
    public string? WarningMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        Template = await _db.StickerTemplates
            .Include(t => t.Connection)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (Template == null) return NotFound();

        // System templates cannot be deleted
        if (Template.IsSystemTemplate)
        {
            TempData["ErrorMessage"] = "Cannot delete system templates.";
            return RedirectToPage("/Templates/Index");
        }

        // Verify user owns the connection
        if (Template.Connection?.UserId != userId)
            return Forbid();

        if (Template.IsDefault)
            WarningMessage = "This is your default template. After deletion, devices will fall back to system templates.";

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int id)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (userId == null) return Unauthorized();

        var template = await _db.StickerTemplates
            .Include(t => t.Connection)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (template == null) return NotFound();

        // Verify ownership and not system template
        if (template.IsSystemTemplate || template.Connection?.UserId != userId)
            return Forbid();

        _logger.LogInformation("Deleting template {Id} '{Name}' for user {UserId}",
            template.Id, template.Name, userId);

        _db.StickerTemplates.Remove(template);
        await _db.SaveChangesAsync();

        TempData["SuccessMessage"] = $"Template '{template.Name}' deleted successfully.";
        return RedirectToPage("/Templates/Index");
    }
}
```

---

### 4. Clone Functionality

**Purpose:** Allow users to clone system templates for customization

**Implementation:** Add method to TemplateService

```csharp
public async Task<StickerTemplate> CloneTemplateAsync(
    int templateId,
    int targetConnectionId,
    string? newName = null)
{
    var sourceTemplate = await _db.StickerTemplates.FindAsync(templateId);
    if (sourceTemplate == null)
        throw new InvalidOperationException($"Template {templateId} not found.");

    var clonedTemplate = new StickerTemplate
    {
        Name = newName ?? $"{sourceTemplate.Name} (Copy)",
        Description = sourceTemplate.Description,
        ConnectionId = targetConnectionId,
        PageWidth = sourceTemplate.PageWidth,
        PageHeight = sourceTemplate.PageHeight,
        ProductTypeFilter = sourceTemplate.ProductTypeFilter,
        IsRackMount = sourceTemplate.IsRackMount,
        IsDefault = false, // Never default on clone
        IsSystemTemplate = false, // Always user template
        TemplateJson = sourceTemplate.TemplateJson,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    _db.StickerTemplates.Add(clonedTemplate);
    await _db.SaveChangesAsync();

    return clonedTemplate;
}
```

**Usage:**
- "Clone" button on each template card in Index
- POST to new endpoint: `/Templates/Clone?id={templateId}&connectionId={connectionId}`
- Validates template exists and connection belongs to user
- Creates new template with "(Copy)" suffix
- Redirects to Designer with new template ID

---

### 5. Navigation Updates

**Modify:** `Pages/Shared/_Layout.cshtml`

Add link in navbar between "Connections" and sync status:

```html
@if (User.Identity?.IsAuthenticated == true)
{
    <li><a href="/">Home</a></li>
    <li><a href="/Connections">Connections</a></li>
    <li><a href="/Templates">Templates</a></li>  <!-- ADD THIS -->
    <li>@await Component.InvokeAsync("SyncStatus")</li>
    <!-- ... logout ... -->
}
```

**Result:** Clear navigation to template management from any authenticated page

---

### 6. Designer Integration

**Modifications to Designer:**

1. **Add breadcrumb navigation**
   - "Templates > {Template Name}"
   - Makes it clear user is editing a template

2. **Add "Back to Templates" button**
   - In toolbar, near Save button
   - Returns to `/Templates/Index`

3. **Add "Save and Close" button**
   - Saves template AND returns to Index
   - Better UX than separate actions

4. **Handle system template editing**
   - Detect if editing system template
   - Show banner: "This is a read-only system template. To customize it, clone it first."
   - When saving system template: either block or auto-create clone

5. **Template title in page header**
   - Show template name prominently
   - Indicate if system or user template

---

## Files Summary

### New Files to Create (6)

1. **`Pages/Templates/Index.cshtml`** - Template list view
2. **`Pages/Templates/Index.cshtml.cs`** - Template list logic
3. **`Pages/Templates/Create.cshtml`** - Create wizard view
4. **`Pages/Templates/Create.cshtml.cs`** - Create wizard logic
5. **`Pages/Templates/Delete.cshtml`** - Delete confirmation view
6. **`Pages/Templates/Delete.cshtml.cs`** - Delete confirmation logic

### Files to Modify (3)

1. **`Pages/Shared/_Layout.cshtml`** - Add Templates navigation link
2. **`Pages/Templates/Designer.cshtml`** - Add breadcrumb, back button, title
3. **`Pages/Templates/Designer.cshtml.cs`** - Add clone/system template handling
4. **`TemplateService.cs`** - Add `CloneTemplateAsync()` method

### Optional Styling

- **`wwwroot/css/templates.css`** - Template list specific styles (or extend designer.css)

---

## Implementation Tasks

### Task 1: Create Template Index Page (4-6 hours)

**Subtasks:**
- [ ] Create `Index.cshtml.cs` with filtering logic
- [ ] Test query filters individually
- [ ] Create `Index.cshtml` with responsive card grid
- [ ] Add search box functionality
- [ ] Style cards and layout
- [ ] Add action buttons (Edit, Clone, Delete)
- [ ] Test authorization (users only see own templates)

**Acceptance Criteria:**
- All templates visible in grid format
- Filters work correctly
- Search finds templates by name and description
- Action buttons present and functional
- Empty state displays when no templates

---

### Task 2: Create Template Creation Wizard (3-4 hours)

**Subtasks:**
- [ ] Create `Create.cshtml.cs` with form binding
- [ ] Add blank template creation logic
- [ ] Add clone template logic
- [ ] Create `Create.cshtml` with 2-step form
- [ ] Add quick preset buttons (100×50mm, 60×60mm)
- [ ] Style wizard UI
- [ ] Test redirect to Designer

**Acceptance Criteria:**
- Can create blank template with custom dimensions
- Can create from system template clone
- Correct properties set (IsSystemTemplate=false, IsDefault=false)
- Redirects to Designer with new template ID
- Template JSON is valid

---

### Task 3: Create Template Delete Page (2-3 hours)

**Subtasks:**
- [ ] Create `Delete.cshtml.cs` with deletion logic
- [ ] Add authorization checks
- [ ] Add warning for default templates
- [ ] Create `Delete.cshtml` confirmation UI
- [ ] Add style for confirmation buttons
- [ ] Test redirect to Index after deletion

**Acceptance Criteria:**
- Cannot delete system templates (shows error)
- Cannot delete other user's templates (shows 403)
- Warning shows for default template
- Deletion removes from database
- Redirect to Index after success

---

### Task 4: Add Clone Functionality (2 hours)

**Subtasks:**
- [ ] Add `CloneTemplateAsync()` to TemplateService
- [ ] Create clone endpoint or integrate with Index
- [ ] Add "Clone" button to Index page
- [ ] Test clone creates correct properties
- [ ] Test redirect to Designer

**Acceptance Criteria:**
- Clone creates new template with same design
- Clone name has "(Copy)" suffix
- Clone is always user template (IsSystemTemplate=false)
- Clone is never default (IsDefault=false)
- Redirect to Designer with new template ID

---

### Task 5: Update Navigation (30 minutes)

**Subtasks:**
- [ ] Modify `_Layout.cshtml`
- [ ] Add Templates link to navbar
- [ ] Test link visible when authenticated
- [ ] Test link hidden when not authenticated

**Acceptance Criteria:**
- Templates link visible in navbar for authenticated users
- Templates link hidden for anonymous users
- Link navigates to `/Templates/Index`

---

### Task 6: Enhance Designer Integration (2-3 hours)

**Subtasks:**
- [ ] Add breadcrumb navigation to Designer
- [ ] Add "Back to Templates" button
- [ ] Add "Save and Close" button
- [ ] Add template title display
- [ ] Add system template warning banner
- [ ] Test navigation between pages

**Acceptance Criteria:**
- Breadcrumb shows current template name
- Back button navigates to Index
- Save and Close saves then navigates
- System template banner displays
- System templates marked as read-only

---

## Testing Checklist

### Functional Testing
- [ ] View template list displays all templates (system + user)
- [ ] Filters work: connection, product type, rack mount, search
- [ ] Search finds templates by name and description
- [ ] Empty state displays when no results
- [ ] Can create blank template with custom dimensions
- [ ] Can clone system template with custom settings
- [ ] Cloned template has "(Copy)" in name
- [ ] Cloned template is user template, not default
- [ ] Can edit user template (opens Designer)
- [ ] Cannot edit system template directly
- [ ] Can delete user template with confirmation
- [ ] Cannot delete system template (shows error)
- [ ] Default template warning appears
- [ ] Navigation link present and functional
- [ ] Breadcrumb shows in Designer
- [ ] Back and Save & Close buttons work

### Authorization Testing
- [ ] Users only see their own connection's templates
- [ ] Cannot delete other users' templates (403)
- [ ] Cannot edit other users' templates (403)
- [ ] Cannot access Delete page for templates they don't own
- [ ] Connection dropdown only shows user's connections
- [ ] System templates visible to all users (read-only)

### Edge Cases
- [ ] Template with special characters in name displays correctly
- [ ] Very long template names truncate appropriately
- [ ] Deleting default template clears IsDefault flag
- [ ] Cloning preserves all design elements (test with complex template)
- [ ] Missing template JSON handled gracefully
- [ ] Concurrent creates don't cause issues
- [ ] Page size presets work correctly (100×50, 60×60)
- [ ] Templates sorted correctly (system first, then default, then by name)

### Performance Testing
- [ ] Index page loads quickly with 50+ templates
- [ ] Filters execute in < 500ms
- [ ] Clone operation completes in < 1s
- [ ] Delete completes in < 1s

### UI/UX Testing
- [ ] Layout is responsive (mobile, tablet, desktop)
- [ ] Buttons are clearly labeled and positioned
- [ ] Error messages are helpful
- [ ] Success messages are clear
- [ ] Form validation works (required fields, limits)
- [ ] Navigation is intuitive

---

## Database

**No new migrations required** - All tables already exist from Phase 1:
- `StickerTemplates` table (already created)
- `Connections` table (already has required fields)
- `GlobalVariables` table (already created)

**Existing indexes used:**
- `IX_StickerTemplates_ConnectionId` - for connection filtering
- `IX_StickerTemplates_ProductTypeFilter` - for product type filtering
- `IX_StickerTemplates_IsDefault` - for default template queries
- `IX_StickerTemplates_IsRackMount` - for rack mount filtering

---

## Success Criteria

Phase 3 is **complete** when:

✅ Users can view all templates in a searchable, filterable list
✅ Users can create new templates from scratch with custom dimensions
✅ Users can clone system templates for customization
✅ Users can delete their own templates with confirmation
✅ Users cannot modify or delete system templates
✅ Navigation to Templates exists in layout
✅ Designer seamlessly integrates with template management
✅ All authorization checks prevent unauthorized access
✅ Templates can be sorted and filtered effectively
✅ UI is responsive and user-friendly

---

## Estimated Timeline

| Task | Hours | Timeline |
|------|-------|----------|
| Index Page | 5 | Day 1 |
| Create Wizard | 3.5 | Day 1 |
| Delete Page | 2.5 | Day 1-2 |
| Clone Functionality | 2 | Day 2 |
| Navigation Updates | 0.5 | Day 2 |
| Designer Enhancements | 2.5 | Day 2 |
| Testing & Bug Fixes | 4 | Day 2-3 |
| **TOTAL** | **19.5 hours** | **~2.5 days** |

---

## Definition of Done

A feature is complete when:
1. Code is written and follows project conventions
2. All acceptance criteria met
3. Authorization checks implemented
4. Error handling in place
5. Tested manually (checklist items pass)
6. No console errors or warnings
7. Responsive on desktop, tablet, mobile
8. Documentation updated (if needed)

---

## Risk Assessment

| Risk | Impact | Mitigation |
|------|--------|------------|
| Complex filtering logic | Medium | Test each filter individually, use LINQ debugging |
| Authorization edge cases | High | Test with multiple users, verify all checks |
| Template JSON corruption during clone | Medium | Validate JSON on save, use same JSON string |
| Performance with many templates | Low | Indexes already created, pagination can be added later |
| Browser back button issues | Low | Use proper navigation, test thoroughly |

---

## Next Steps

After Phase 3 completion, proceed to:

1. **Phase 4:** Multi-Format Export (PDF/SVG/PNG)
   - PDF generation with QuestPDF
   - Rendering templates with device data
   - Batch export as ZIP

2. **Phase 5:** Device Integration
   - Add "Generate Stickers" button to device list
   - Multi-device selection
   - Batch generation workflow

3. **Phase 6:** Company Logo Upload
   - File upload UI
   - Logo storage (base64 or Azure Blob)
   - Logo preview in Designer

---

## Notes

- **Reuse existing infrastructure:** All database tables, services, and models already exist
- **Consistent UI/UX:** Follow designer.css patterns and layout conventions
- **Authorization-first:** Every operation checks user ownership
- **Fail-safe deletions:** Confirmation required for destructive actions
- **Future-proof:** Design allows easy addition of bulk operations, export, etc.

---

## Document Metadata

**Version:** 1.0
**Status:** Planning Phase - Ready to Implement
**Created:** 2025-10-21
**Last Modified:** 2025-10-21
**Author:** Claude
**Next Review:** After Phase 3 Task 1 completion

---

**END OF PHASE 3 PLAN**
