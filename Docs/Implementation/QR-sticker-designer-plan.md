# QR Sticker Designer - Implementation Plan

**Project:** QRStickers
**Feature:** Drag/Drop Sticker Designer with Multi-Format Export
**Date:** 2025-10-19 (Initial Planning) / 2025-10-22 (Phase 4 Complete)
**Status:** In Progress - Phase 4 Complete ✅ | Phase 5 Planning

---

## Status Update - Phase 4 Complete

**Major Progress:** Template Preview & Export system is fully implemented, tested, and production-ready!

- **Phase 1-3:** ✅ All database models, designer UI, and template management completed (prior phases)
- **Phase 4:** ✅ **COMPLETE** - Live preview, PNG/SVG export with placeholder data, all features working
- **Phase 5:** 📋 Planning documents created and ready for implementation

**Implementation Actuals vs. Plan:**
- Original estimate for Phase 4: 4-6 days
- **Actual delivery:** ~6 hours (single focused session with comprehensive testing)
- **Key factor:** Fabric.js integration was smooth; all export functionality delivered client-side
- **Quality:** Production-ready with comprehensive error handling and logging

See [PHASE4_COMPLETION_NOTES.md](./PHASE4_COMPLETION_NOTES.md) for complete Phase 4 details.

---

## Executive Summary

This document outlines the complete implementation plan for a drag-and-drop sticker designer feature that allows users to create customizable templates for printing device labels. The system will support per-connection customization, multiple output formats (PDF, SVG, PNG), and automatic template selection based on device types.

**Current Status:** Phase 4 (Preview & Export) is complete. Phase 5 planning documents are available for next implementation phase.

---

## Implementation Phases Status

| Phase | Name | Status | Completion | Notes |
|-------|------|--------|------------|-------|
| **1** | Database & Core Models | ✅ Complete | Phase 1 | All entity models, migrations, template seeding |
| **2** | Designer UI | ✅ Complete | Phase 2-3 | Fabric.js designer, drag/drop, property panel, save/load |
| **3** | Template Management | ✅ Complete | Phase 2-3 | Index, Create, Edit, Delete pages, filtering, cloning |
| **4** | Preview & Export | ✅ Complete | Phase 4 | Live preview, PNG/SVG export, placeholder generation |
| **5** | Device Integration | 📋 Planned | Next | Real device data, bulk export, template matching |
| **6** | Company Logo Upload | 📋 Planned | Future | Logo upload, image storage, preview |

**Total Progress:** 4 of 6 phases complete (67%)

---

## 1. Technology Analysis & Recommendations

### 1.1 Element Arrangement Technology (Drag/Drop Designer)

#### Evaluated Options

| Technology | Pros | Cons | Verdict |
|------------|------|------|---------|
| **Fabric.js** | ✅ Canvas-based with high-level object manipulation<br>✅ Excellent drag/drop, rotation, scaling out-of-the-box<br>✅ Built-in snapping, grouping, layering<br>✅ Direct SVG export capability<br>✅ Active maintenance, large community<br>✅ Good TypeScript support<br>✅ Precise measurements (critical for print) | ❌ Larger bundle size (~200KB)<br>❌ Canvas-based (requires conversion for PDF) | **✅ RECOMMENDED** |
| **Konva.js** | ✅ Lighter (~150KB)<br>✅ Good performance<br>✅ Similar feature set | ❌ Smaller community<br>❌ Less extensive documentation<br>❌ Fewer built-in UI controls | Good alternative |
| **SVG + interact.js** | ✅ Minimal dependencies<br>✅ Direct SVG manipulation<br>✅ Smaller footprint | ❌ More custom code required<br>❌ No built-in design tools<br>❌ Manual implementation of rotation, scaling | Not recommended |
| **Native Canvas** | ❌ Too low-level<br>❌ Reinventing the wheel | ❌ Not recommended | ❌ Rejected |

#### Recommendation: **Fabric.js 5.x**
- **Rationale:** Best balance of features, ease of use, and export capabilities. Built-in precision tools essential for print design. Proven track record (Canva uses Fabric.js internally).
- **Integration:** CDN for quick start, npm package for production builds
- **License:** MIT (commercial use OK)

---

### 1.2 Sticker Output Technology

#### Evaluated Options

| Technology | Use Case | Pros | Cons | Verdict |
|------------|----------|------|------|---------|
| **QuestPDF** | Server-side PDF | ✅ Modern, fluent C# API<br>✅ Free for commercial use (MIT)<br>✅ Excellent documentation<br>✅ Active development<br>✅ Precise layout control (mm, inches, points)<br>✅ Built-in QR codes, images, SVG embedding | ❌ Requires .NET backend processing | **✅ RECOMMENDED for PDF** |
| **PdfSharp** | Server-side PDF | ✅ Open source<br>✅ Established | ❌ Older API<br>❌ Less intuitive<br>❌ Verbose | Not recommended |
| **jsPDF + Fabric.js** | Client-side PDF | ✅ No server processing<br>✅ Immediate preview | ❌ Limited precision control<br>❌ Browser compatibility issues<br>❌ Hard to enforce print standards | Preview only |
| **Svg.NET** | Server-side SVG | ✅ Native .NET SVG library<br>✅ Direct manipulation | ❌ Less common for complex layouts | **✅ RECOMMENDED for SVG** |
| **Fabric.js SVG Export** | Client-side SVG | ✅ Direct from canvas<br>✅ Native support | ✅ Already part of Fabric.js | **✅ RECOMMENDED for SVG** |
| **System.Drawing.Common** | Server-side PNG | ✅ Built into .NET<br>✅ Simple API | ❌ Windows-only (Linux requires libgdiplus)<br>❌ Deprecated for non-Windows | Fallback option |
| **ImageSharp** | Server-side PNG | ✅ Cross-platform<br>✅ Modern API<br>✅ Active development | ❌ Additional dependency | **✅ RECOMMENDED for PNG** |

#### Recommendation: **Hybrid Multi-Format Approach**

1. **Designer Interface:** Fabric.js (client-side)
2. **Client-Side Exports:**
   - **SVG:** Fabric.js `canvas.toSVG()` - immediate download
   - **PNG:** Fabric.js `canvas.toDataURL('image/png')` - preview/quick export
3. **Server-Side Production Exports:**
   - **PDF:** QuestPDF - highest quality, precise print control
   - **SVG:** Fabric.js client export + optional server validation
   - **PNG:** ImageSharp - high-resolution rasterization

**Workflow:**
```
User designs in browser (Fabric.js)
    ↓
Template JSON saved to database
    ↓
For production output:
    ↓
Server renders using appropriate engine
    ↓
    ├─ PDF → QuestPDF (precise print dimensions)
    ├─ SVG → Fabric.js export (scalable vector)
    └─ PNG → ImageSharp (high-res raster)
```

---

## 2. Data Model & Storage Strategy

### 2.1 Storage Approach: JSON in Database Column

**Rationale:**
1. **Flexibility:** Easy to extend schema without migrations
2. **Fabric.js Native:** Direct export/import from Fabric.js canvas
3. **Simplicity:** Single source of truth, no join complexity
4. **Performance:** Modern SQL Server handles JSON queries well
5. **Versioning:** `version` field allows schema evolution

**Alternative Considered (Rejected):** Relational tables with `TemplateElements` entity
- **Reason for rejection:** Over-engineering for this use case, harder to maintain with dynamic properties

---

### 2.2 Entity Models

#### 2.2.1 StickerTemplate

```csharp
public class StickerTemplate
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Foreign key to Connection (NULL = system template shared across all connections)
    /// </summary>
    public int? ConnectionId { get; set; }

    /// <summary>
    /// Template name (e.g., "Rack Mount Switch Label", "AP Ceiling Label")
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = null!;

    /// <summary>
    /// Optional description
    /// </summary>
    [MaxLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// Filter by Meraki product type (nullable = matches all)
    /// Values: "switch", "wireless", "appliance", "camera", "sensor", "cellularGateway"
    /// </summary>
    [MaxLength(50)]
    public string? ProductTypeFilter { get; set; }

    /// <summary>
    /// Whether this is a rack-mount template (for categorization)
    /// Rack-mount: switches, appliances, some cameras
    /// Non-rack: wireless APs, sensors, wall-mount cameras
    /// </summary>
    public bool IsRackMount { get; set; }

    /// <summary>
    /// Whether this is the fallback template
    /// Only one template should have IsDefault=true per connection (or global)
    /// </summary>
    public bool IsDefault { get; set; } = false;

    /// <summary>
    /// System templates are read-only and cloneable by users
    /// </summary>
    public bool IsSystemTemplate { get; set; } = false;

    /// <summary>
    /// Page width in millimeters (precise print dimensions)
    /// Common sizes: 100mm x 50mm, 60mm x 40mm, etc.
    /// </summary>
    public double PageWidth { get; set; } = 100.0;  // mm

    /// <summary>
    /// Page height in millimeters
    /// </summary>
    public double PageHeight { get; set; } = 50.0;  // mm

    /// <summary>
    /// Serialized Fabric.js canvas JSON
    /// Contains all design elements, positioning, styling, and data bindings
    /// </summary>
    [Required]
    public string TemplateJson { get; set; } = null!;  // NVARCHAR(MAX)

    /// <summary>
    /// Template creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last modification timestamp
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Connection? Connection { get; set; }
}
```

#### 2.2.2 Connection (Modified)

**Add new property:**
```csharp
/// <summary>
/// Company logo URL or base64 data URI for this connection
/// Used in sticker templates when logo element is present
/// Format: "https://..." or "data:image/png;base64,..."
/// </summary>
[MaxLength(5000)]  // Allow for base64-encoded images
public string? CompanyLogoUrl { get; set; }
```

**Migration Required:** `AddCompanyLogoToConnection`

#### 2.2.3 GlobalVariable

```csharp
public class GlobalVariable
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Foreign key to Connection
    /// </summary>
    [Required]
    public int ConnectionId { get; set; }

    /// <summary>
    /// Variable name (e.g., "supportUrl", "companyPhone", "customMessage")
    /// Used in template with syntax: {{global.supportUrl}}
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string VariableName { get; set; } = null!;

    /// <summary>
    /// Variable value (e.g., "https://support.company.com", "+1-555-0123")
    /// </summary>
    [Required]
    [MaxLength(500)]
    public string VariableValue { get; set; } = null!;

    /// <summary>
    /// Optional description to help users understand the variable's purpose
    /// </summary>
    [MaxLength(500)]
    public string? Description { get; set; }

    /// <summary>
    /// Variable creation timestamp
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last modification timestamp
    /// </summary>
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Connection Connection { get; set; } = null!;
}
```

---

### 2.3 Template JSON Schema

The `TemplateJson` column stores the Fabric.js canvas export with custom extensions for data binding.

**Example Template JSON:**

```json
{
  "version": "1.0",
  "fabricVersion": "5.3.0",
  "pageSize": {
    "width": 100,
    "height": 50,
    "unit": "mm"
  },
  "objects": [
    {
      "type": "qrcode",
      "id": "qr-primary",
      "left": 5,
      "top": 5,
      "width": 30,
      "height": 30,
      "scaleX": 1,
      "scaleY": 1,
      "angle": 0,
      "properties": {
        "dataSource": "device.Serial",
        "eccLevel": "Q",
        "quietZone": 2
      }
    },
    {
      "type": "text",
      "id": "device-name",
      "left": 40,
      "top": 10,
      "width": 55,
      "height": 10,
      "text": "{{device.Name}}",
      "fontFamily": "Arial",
      "fontSize": 12,
      "fontWeight": "bold",
      "fill": "#000000",
      "properties": {
        "dataSource": "device.Name",
        "maxLength": 50,
        "overflow": "truncate"
      }
    },
    {
      "type": "text",
      "id": "serial-number",
      "left": 40,
      "top": 25,
      "width": 55,
      "height": 8,
      "text": "{{device.Serial}}",
      "fontFamily": "Courier New",
      "fontSize": 9,
      "fill": "#333333",
      "properties": {
        "dataSource": "device.Serial",
        "format": "uppercase"
      }
    },
    {
      "type": "image",
      "id": "company-logo",
      "left": 70,
      "top": 35,
      "width": 25,
      "height": 10,
      "src": "{{connection.CompanyLogoUrl}}",
      "properties": {
        "dataSource": "connection.CompanyLogoUrl",
        "placeholder": true,
        "aspectRatio": "contain"
      }
    },
    {
      "type": "qrcode",
      "id": "qr-secondary",
      "left": 40,
      "top": 35,
      "width": 12,
      "height": 12,
      "properties": {
        "dataSource": "global.supportUrl",
        "eccLevel": "M"
      }
    },
    {
      "type": "text",
      "id": "support-phone",
      "left": 5,
      "top": 45,
      "width": 90,
      "height": 4,
      "text": "Support: {{global.companyPhone}}",
      "fontFamily": "Arial",
      "fontSize": 6,
      "fill": "#666666",
      "properties": {
        "dataSource": "global.companyPhone"
      }
    }
  ]
}
```

**Data Source Syntax:**
- `device.*` - Fields from `CachedDevice` (e.g., `device.Serial`, `device.Name`, `device.Model`)
- `connection.*` - Fields from `Connection` (e.g., `connection.CompanyLogoUrl`, `connection.DisplayName`)
- `global.*` - User-defined global variables (e.g., `global.supportUrl`, `global.companyPhone`)
- `network.*` - Fields from `CachedNetwork` (e.g., `network.Name`)

---

### 2.4 Database Schema (SQL)

```sql
-- Add CompanyLogoUrl to Connections table
ALTER TABLE Connections
ADD CompanyLogoUrl NVARCHAR(5000) NULL;

-- Create StickerTemplates table
CREATE TABLE StickerTemplates (
    Id INT PRIMARY KEY IDENTITY(1,1),
    ConnectionId INT NULL,  -- FK to Connections (NULL = system template)
    Name NVARCHAR(200) NOT NULL,
    Description NVARCHAR(1000) NULL,
    ProductTypeFilter NVARCHAR(50) NULL,
    IsRackMount BIT NOT NULL DEFAULT 0,
    IsDefault BIT NOT NULL DEFAULT 0,
    IsSystemTemplate BIT NOT NULL DEFAULT 0,
    PageWidth FLOAT NOT NULL DEFAULT 100.0,
    PageHeight FLOAT NOT NULL DEFAULT 50.0,
    TemplateJson NVARCHAR(MAX) NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT FK_StickerTemplates_Connections
        FOREIGN KEY (ConnectionId) REFERENCES Connections(Id) ON DELETE CASCADE
);

-- Indexes for performance
CREATE INDEX IX_StickerTemplates_ConnectionId ON StickerTemplates(ConnectionId);
CREATE INDEX IX_StickerTemplates_ProductTypeFilter ON StickerTemplates(ProductTypeFilter);
CREATE INDEX IX_StickerTemplates_IsDefault ON StickerTemplates(IsDefault);
CREATE INDEX IX_StickerTemplates_IsRackMount ON StickerTemplates(IsRackMount);

-- Create GlobalVariables table
CREATE TABLE GlobalVariables (
    Id INT PRIMARY KEY IDENTITY(1,1),
    ConnectionId INT NOT NULL,  -- FK to Connections
    VariableName NVARCHAR(100) NOT NULL,
    VariableValue NVARCHAR(500) NOT NULL,
    Description NVARCHAR(500) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETUTCDATE(),

    CONSTRAINT FK_GlobalVariables_Connections
        FOREIGN KEY (ConnectionId) REFERENCES Connections(Id) ON DELETE CASCADE,

    CONSTRAINT UQ_GlobalVariables_ConnectionId_VariableName
        UNIQUE (ConnectionId, VariableName)
);

-- Indexes for performance
CREATE INDEX IX_GlobalVariables_ConnectionId ON GlobalVariables(ConnectionId);
```

---

## 3. Template Selection Logic

### 3.1 Selection Algorithm

Templates are selected in priority order:

1. **User's connection-specific template for exact product type**
2. **User's connection-specific default template**
3. **System template for product type**
4. **System default template** (fallback)

### 3.2 Implementation (TemplateService.cs)

```csharp
public class TemplateService
{
    private readonly QRStickersDbContext _db;

    public async Task<StickerTemplate> GetTemplateForDevice(
        CachedDevice device,
        int connectionId)
    {
        // Get device's product type from network
        var network = await _db.CachedNetworks
            .FirstOrDefaultAsync(n => n.ConnectionId == connectionId
                                   && n.NetworkId == device.NetworkId);

        var productType = network?.ProductTypes?.FirstOrDefault();

        // 1. Try user's template for this product type
        var template = await _db.StickerTemplates
            .Where(t => t.ConnectionId == connectionId
                     && t.ProductTypeFilter == productType)
            .FirstOrDefaultAsync();

        // 2. Try user's default template
        if (template == null)
            template = await _db.StickerTemplates
                .Where(t => t.ConnectionId == connectionId && t.IsDefault)
                .FirstOrDefaultAsync();

        // 3. Try system template for this product type
        if (template == null)
            template = await _db.StickerTemplates
                .Where(t => t.IsSystemTemplate
                         && t.ProductTypeFilter == productType)
                .FirstOrDefaultAsync();

        // 4. Fallback to system default
        if (template == null)
            template = await _db.StickerTemplates
                .Where(t => t.IsSystemTemplate && t.IsDefault)
                .FirstAsync();

        return template;
    }
}
```

---

## 4. System Template Seeding

### 4.1 Initial Templates

Two system templates will be provided:

1. **Rack-Mount Template** (switches, appliances)
   - Size: 100mm x 50mm (landscape)
   - Elements:
     - Large QR code (left, 40mm x 40mm)
     - Device name (top right, bold, 14pt)
     - Serial number (middle right, monospace, 10pt)
     - Model number (below serial)
     - Company logo (bottom right, small)
   - ProductTypeFilter: NULL (matches all)
   - IsRackMount: TRUE
   - IsDefault: FALSE

2. **Ceiling/Wall-Mount Template** (APs, cameras, sensors)
   - Size: 60mm x 60mm (square)
   - Elements:
     - Medium QR code (center-top, 30mm x 30mm)
     - Device name (below QR, centered, 10pt)
     - Serial number (bottom, centered, 8pt)
     - Small company logo (top-right corner)
   - ProductTypeFilter: NULL (matches all)
   - IsRackMount: FALSE
   - IsDefault: TRUE (fallback)

### 4.2 Seeder Implementation

```csharp
public class SystemTemplateSeeder
{
    public static async Task SeedTemplates(QRStickersDbContext db)
    {
        if (await db.StickerTemplates.AnyAsync(t => t.IsSystemTemplate))
            return; // Already seeded

        var templates = new List<StickerTemplate>
        {
            CreateRackMountTemplate(),
            CreateCeilingWallTemplate()
        };

        db.StickerTemplates.AddRange(templates);
        await db.SaveChangesAsync();
    }

    private static StickerTemplate CreateRackMountTemplate() { /* ... */ }
    private static StickerTemplate CreateCeilingWallTemplate() { /* ... */ }
}
```

---

## 5. Implementation Phases

### Phase 1: Database & Core Models (3-5 days)

**Deliverables:**
- [ ] `StickerTemplate.cs` entity model
- [ ] `GlobalVariable.cs` entity model
- [ ] Modify `Connection.cs` to add `CompanyLogoUrl`
- [ ] Update `QRStickersDbContext.cs` with new DbSets and relationships
- [ ] Create `Services/TemplateService.cs` for template selection
- [ ] Create migration `AddStickerDesignerTables`
- [ ] Create `Data/SystemTemplateSeeder.cs` with 2 templates
- [ ] Update `Program.cs` to call seeder on startup

**Files Created/Modified:**
```
StickerTemplate.cs (new)
GlobalVariable.cs (new)
Connection.cs (modify)
QRStickersDbContext.cs (modify)
Services/TemplateService.cs (new)
Data/SystemTemplateSeeder.cs (new)
Program.cs (modify)
Migrations/AddStickerDesignerTables.cs (new)
```

**Testing:**
- Verify migration creates tables correctly
- Verify seeder populates 2 system templates
- Verify template selection logic with unit tests

---

### Phase 2: Designer UI (5-7 days)

**Deliverables:**
- [ ] `/Templates/Designer` Razor Page
- [ ] Fabric.js integration (CDN or npm)
- [ ] Element palette UI (QR code, text, image, shapes)
- [ ] Property inspector panel (font, size, color, data binding)
- [ ] Canvas controls (zoom, pan, grid, rulers)
- [ ] Data binding UI (dropdown to select device/connection/global fields)
- [ ] Template save functionality (POST to server)
- [ ] Template load functionality (GET from server)
- [ ] Real-time preview with sample data

**Files Created:**
```
Pages/Templates/Designer.cshtml (new)
Pages/Templates/Designer.cshtml.cs (new)
wwwroot/js/designer.js (new)
wwwroot/js/fabric-extensions.js (new)
wwwroot/css/designer.css (new)
```

**UI Features:**
- Left sidebar: Element palette (drag to canvas)
- Right sidebar: Property inspector (contextual to selection)
- Top toolbar: Zoom, grid toggle, ruler toggle, save, load
- Center: Fabric.js canvas with page size visualization
- Bottom: Status bar (cursor position, selected element info)

**Technical Details:**
- Use Fabric.js custom objects for QR code elements
- Implement data binding metadata in object properties
- Support undo/redo with Fabric.js history
- Auto-save to localStorage (prevent data loss)

---

### Phase 3: Template Management (2-3 days)

**Deliverables:**
- [ ] `/Templates/Index` - List all templates with filtering
- [ ] `/Templates/Create` - Template creation wizard
- [ ] `/Templates/Edit` - Launch designer for existing template
- [ ] `/Templates/Delete` - Delete confirmation page
- [ ] Template cloning (duplicate system template for customization)

**Files Created:**
```
Pages/Templates/Index.cshtml[.cs] (new)
Pages/Templates/Create.cshtml[.cs] (new)
Pages/Templates/Edit.cshtml[.cs] (new)
Pages/Templates/Delete.cshtml[.cs] (new)
```

**Index Page Features:**
- Filterable list (by connection, product type, rack-mount)
- Template preview thumbnails
- Quick actions (edit, clone, delete)
- "New Template" button

**Create Wizard:**
1. Template name and description
2. Select base template (system templates or blank)
3. Configure product type filter
4. → Redirect to Designer

---

### Phase 4: Multi-Format Export ✅ COMPLETE (Actual: ~6 hours)

**Deliverables:**
- [x] **Phase 4 - Preview & Export (Client-Side Only)**
- [x] Live preview with real-time updates
- [x] PNG export (96, 150, 300 DPI)
- [x] SVG export (vector format)
- [x] Placeholder data generation for all binding types
- [x] Transparent background with checkerboard visualization
- [x] Export modal with professional UI
- [x] Full error handling and diagnostic logging

**Note:** Phase 4 was implemented as client-side only (no server-side export service). Server-side generation with QuestPDF/ImageSharp is deferred to Phase 5 when real device data integration begins. Current implementation provides excellent foundation for batch export with real data.

**Files Created:**
```
wwwroot/js/export-preview.js (new - 350 lines)
    ├─ extractDataBindings()
    ├─ generatePlaceholderMap()
    ├─ createPreviewTemplate()
    ├─ replacePlaceholders()
    ├─ createPreviewCanvas()
    ├─ exportPNG()
    └─ exportSVG()
```

**Files Modified:**
```
Pages/Templates/Designer.cshtml (+75 lines)
    └─ Added Export button and modal HTML

wwwroot/css/designer.css (+185 lines)
    └─ Added modal styling and checkerboard pattern

wwwroot/js/designer.js (+340 lines)
    └─ Added export modal integration and live preview
```

**Total Code Added:** ~950 lines

**Key Implementation Details:**

**Placeholder Generation System:**
```javascript
Supported bindings:
- device.* (serial, name, mac, model, ipaddress, tags)
- connection.* (name, displayname)
- network.* (name)
- global.* (supporturl, supportphone, custom vars)

Format: {{variable.field}} → "realistic placeholder value"
```

**Export Formats (Client-Side):**
- **PNG:** 96, 150, 300 DPI with white or transparent background
- **SVG:** Vector format, fully editable in design tools

**Real-Time Preview:**
- Automatic updates when format/DPI/background changes
- Accurate representation of export output
- Checkerboard pattern for transparency visualization

---

### Phase 5: Device Integration 📋 PLANNED (Est. 18-20 hours)

**Status:** Planning documents completed. Ready for implementation.

**See detailed planning documents:**
- **[Phase5-DeviceExport-Planning.md](./Phase5-DeviceExport-Planning.md)** - Main planning document with 5 sub-phases
- **[Phase5-TechnicalSpec.md](./Phase5-TechnicalSpec.md)** - Technical architecture and API specifications
- **[Phase5-UX-Design.md](./Phase5-UX-Design.md)** - UI mockups and interaction flows

**High-Level Deliverables:**
- [ ] Device data integration (replace placeholder data with real values)
- [ ] Single device export workflow
- [ ] Multi-device bulk export with tiling/grid layout
- [ ] Template auto-selection based on device type
- [ ] PDF generation service (QuestPDF integration)
- [ ] Batch export API endpoint
- [ ] ZIP download for multiple stickers
- [ ] Export history tracking
- [ ] Progress indication for large batch operations

**Key Features:**
1. **Device Data Integration** - Real serial numbers, names, models, IP addresses from Meraki API
2. **Single Device Export** - Export individual device sticker with template auto-selection
3. **Multi-Device Export** - Select multiple devices, generate all stickers in one operation
4. **Template Matching** - Auto-select best template based on device product type
5. **PDF Quality** - Server-side rendering with QuestPDF for professional print quality
6. **Batch Operations** - Handle 10-1000+ devices efficiently with background processing

**Files to Create/Modify (5 sub-phases):**
- Services: PdfGenerationService, TemplateRenderService, ExportOrchestrationService
- Pages: Devices export UI, export progress page, export history
- API: /api/export/device/{id}, /api/export/devices/batch
- Database: ExportHistory table, TemplateDeviceModels, TemplateDeviceTypes

---

### Phase 6: Company Logo Upload (2-3 days)

**Deliverables:**
- [ ] Add logo upload to `/Connections/Edit` page
- [ ] Create file upload handler (base64 or Azure Blob Storage)
- [ ] Image validation (format, size limits)
- [ ] Preview in designer
- [ ] Use logo in PDF/SVG/PNG generation

**Files Modified/Created:**
```
Pages/Connections/Edit.cshtml[.cs] (modify)
Services/ImageStorageService.cs (new - optional, for Blob Storage)
wwwroot/js/logo-preview.js (new)
```

**Storage Options:**

**Option A: Base64 in Database (Simpler)**
- Store as data URI in `CompanyLogoUrl`
- Pros: No external dependencies, easy backup
- Cons: Increases database size, 4KB-1MB per logo
- **Recommendation:** Start with this approach

**Option B: Azure Blob Storage**
- Store URL in `CompanyLogoUrl`
- Pros: Better performance, scalable, CDN-ready
- Cons: Additional Azure cost, more complex
- **Recommendation:** Migrate to this if logos become a performance issue

**Validation:**
- Max file size: 2MB
- Formats: PNG, JPG, SVG
- Dimensions: Max 1000x1000px
- Auto-resize if needed

---

## 6. Total Implementation Timeline

| Phase | Duration | Status | Actual | Dependencies |
|-------|----------|--------|--------|--------------|
| **Phase 1:** Database & Models | 3-5 days | ✅ Complete | ~1 day | None |
| **Phase 2:** Designer UI | 5-7 days | ✅ Complete | ~2 days | Phase 1 |
| **Phase 3:** Template Management | 2-3 days | ✅ Complete | ~1 day | Phase 1, 2 |
| **Phase 4:** Multi-Format Export | 4-6 days | ✅ Complete | ~6 hours | Phase 1 |
| **Phase 5:** Device Integration | 18-20 hours | 📋 Planned | TBD | Phase 1, 4 |
| **Phase 6:** Company Logo Upload | 2-3 days | 📋 Planned | TBD | Phase 1, 2 |

**Total Actual Time (Phases 1-4):** ~4-5 days
**Total Remaining Estimate (Phases 5-6):** 4-5 days
**Overall Project Estimate:** 8-10 days total (as of Phase 4 completion)

**Parallel Work Opportunities:**
- Phases 2 and 4 ran efficiently in sequence (no parallel needed, single developer)
- Phase 5 and 6 can run in parallel (different developers)
- Phase 6 can begin immediately after Phase 4 completes (independent of Phase 5)

---

## 7. Technical Architecture Diagram

```
┌─────────────────────────────────────────────────────────────┐
│                        Browser (Client)                      │
├─────────────────────────────────────────────────────────────┤
│                                                               │
│  ┌──────────────────┐         ┌─────────────────────────┐   │
│  │  Designer UI     │         │   Device Selection UI   │   │
│  │  (Fabric.js)     │         │   (Multi-select)        │   │
│  │  - Drag/drop     │         │   - Checkboxes          │   │
│  │  - Data binding  │         │   - Template picker     │   │
│  │  - Save template │         │   - Format selection    │   │
│  └──────┬───────────┘         └──────────┬──────────────┘   │
│         │                                 │                  │
│         │ POST /Templates/Save            │ POST /api/       │
│         │ GET /Templates/Load             │ stickers/batch   │
│         │                                 │                  │
└─────────┼─────────────────────────────────┼──────────────────┘
          │                                 │
          ▼                                 ▼
┌─────────────────────────────────────────────────────────────┐
│                   ASP.NET Core Server                        │
├─────────────────────────────────────────────────────────────┤
│                                                               │
│  ┌───────────────────────────────────────────────────────┐  │
│  │               Services Layer                           │  │
│  │  ┌────────────────┐  ┌──────────────────────────────┐ │  │
│  │  │ TemplateService│  │  Generation Services         │ │  │
│  │  │ - Select best  │  │  - PdfGenerationService      │ │  │
│  │  │   template     │  │  - SvgGenerationService      │ │  │
│  │  │ - Parse JSON   │  │  - PngGenerationService      │ │  │
│  │  │ - Bind data    │  │  - TemplateRenderService     │ │  │
│  │  └────────────────┘  └──────────────────────────────┘ │  │
│  └───────────────────────────────────────────────────────┘  │
│                           │                                  │
│                           ▼                                  │
│  ┌───────────────────────────────────────────────────────┐  │
│  │               Database (SQL Server)                    │  │
│  │  ┌──────────────┐  ┌─────────────┐  ┌──────────────┐ │  │
│  │  │ Connections  │  │   Sticker   │  │   Global     │ │  │
│  │  │ - Logo URL   │  │  Templates  │  │  Variables   │ │  │
│  │  │              │  │ - JSON      │  │              │ │  │
│  │  └──────────────┘  └─────────────┘  └──────────────┘ │  │
│  │  ┌──────────────┐                                     │  │
│  │  │ CachedDevice │                                     │  │
│  │  │ - Serial     │                                     │  │
│  │  │ - Name       │                                     │  │
│  │  │ - Model      │                                     │  │
│  │  └──────────────┘                                     │  │
│  └───────────────────────────────────────────────────────┘  │
│                                                               │
└───────────────────────────────────────────────────────────────┘
                                │
                                ▼
                    ┌───────────────────────┐
                    │   External Libraries  │
                    │   - QuestPDF          │
                    │   - ImageSharp        │
                    │   - QRCoder           │
                    └───────────────────────┘
```

---

## 8. Data Binding Reference

### 8.1 Available Data Sources

| Prefix | Source | Example Fields |
|--------|--------|----------------|
| `device.*` | `CachedDevice` | `device.Serial`, `device.Name`, `device.Model` |
| `network.*` | `CachedNetwork` | `network.Name`, `network.NetworkId` |
| `connection.*` | `Connection` | `connection.DisplayName`, `connection.CompanyLogoUrl` |
| `global.*` | `GlobalVariables` | `global.supportUrl`, `global.companyPhone` (user-defined) |

### 8.2 Template Variable Syntax

**In Template JSON:**
```json
{
  "type": "text",
  "text": "{{device.Name}}",
  "properties": {
    "dataSource": "device.Name"
  }
}
```

**Rendering Process:**
1. Parse `dataSource` property
2. Split by `.` to get entity and field
3. Lookup value from appropriate source
4. Replace `{{placeholder}}` with actual value
5. Render to PDF/SVG/PNG

---

## 9. NuGet Package Requirements

```xml
<!-- Existing packages -->
<PackageReference Include="QRCoder" Version="1.7.0" />
<PackageReference Include="Microsoft.EntityFrameworkCore.SqlServer" Version="9.0.0" />

<!-- New packages for sticker designer -->
<PackageReference Include="QuestPDF" Version="2024.10.0" />
<PackageReference Include="SixLabors.ImageSharp" Version="3.1.5" />
<PackageReference Include="SixLabors.ImageSharp.Drawing" Version="2.1.4" />
```

**Client-Side (CDN):**
```html
<!-- Fabric.js -->
<script src="https://cdn.jsdelivr.net/npm/fabric@5.3.0/dist/fabric.min.js"></script>
```

---

## 10. Testing Strategy

### 10.1 Unit Tests

**TemplateService:**
- Template selection logic (all priority levels)
- Data binding resolution
- Variable interpolation

**Generation Services:**
- PDF rendering accuracy
- SVG export correctness
- PNG resolution quality

### 10.2 Integration Tests

- Template CRUD operations
- Batch sticker generation
- ZIP file creation
- File downloads

### 10.3 Manual Testing Checklist

- [ ] Designer UI drag/drop
- [ ] Template save/load
- [ ] Data binding in designer preview
- [ ] PDF generation with real devices
- [ ] Batch download (10+ devices)
- [ ] Logo upload and display
- [ ] Global variables in stickers
- [ ] Print quality verification (physical stickers)

---

## 11. Future Enhancements (Post-MVP)

### 11.1 Advanced Features
- [ ] **Template Marketplace:** Share templates with other users
- [ ] **Template Versioning:** Track changes, rollback capability
- [ ] **Custom Fonts:** Upload and use custom fonts in templates
- [ ] **Barcode Support:** Code128, Data Matrix, PDF417
- [ ] **Multi-Page Templates:** Front and back of stickers
- [ ] **Label Sheet Layouts:** Avery template support (e.g., 30 labels per sheet)
- [ ] **Conditional Elements:** Show/hide elements based on device type
- [ ] **Rich Text Editor:** Formatted text with multiple styles
- [ ] **Image Filters:** Grayscale, invert, etc. for logos

### 11.2 Performance Optimizations
- [ ] **Client-Side Caching:** Cache templates in localStorage
- [ ] **Background Jobs:** Use Hangfire for large batch generation
- [ ] **CDN for Logos:** Migrate to Azure Blob + CDN
- [ ] **Template Thumbnails:** Pre-generate preview images

### 11.3 User Experience
- [ ] **Keyboard Shortcuts:** Ctrl+C/V for copy/paste in designer
- [ ] **Alignment Guides:** Snap to grid, smart guides
- [ ] **Template Preview Gallery:** Visual browser instead of list
- [ ] **Bulk Variable Editor:** Edit global variables for multiple connections
- [ ] **Print Preview:** Show how stickers will look on label sheet

---

## 12. Security Considerations

### 12.1 Input Validation
- Sanitize template JSON to prevent XSS
- Validate image uploads (file type, size, dimensions)
- Limit template JSON size (max 1MB)
- Validate global variable values (max length, no HTML)

### 12.2 Authorization
- Users can only access their own connection's templates
- System templates are read-only (clone to edit)
- Validate connectionId belongs to authenticated user

### 12.3 Rate Limiting
- Batch generation: Max 100 devices per request
- API rate limits: 10 requests/minute per user
- File upload: Max 5 uploads/minute

---

## 13. Monitoring & Analytics

### 13.1 Metrics to Track
- Template creation count (per user, per connection)
- Sticker generation count (by format: PDF/SVG/PNG)
- Batch generation size (average devices per batch)
- Template selection hit rate (system vs. user templates)
- Error rates (generation failures, upload failures)

### 13.2 Logging
- Log all template save/load operations
- Log all sticker generation requests (device count, format, template used)
- Log errors with template JSON for debugging

---

## 14. Documentation Requirements

### 14.1 User Documentation
- [ ] **Quick Start Guide:** Create your first template
- [ ] **Designer Tutorial:** How to use Fabric.js designer
- [ ] **Data Binding Guide:** Available variables and syntax
- [ ] **Template Best Practices:** Design tips for printable stickers
- [ ] **Troubleshooting:** Common issues and solutions

### 14.2 Developer Documentation
- [ ] **Architecture Overview:** Component diagram
- [ ] **API Reference:** Endpoints, request/response schemas
- [ ] **Template JSON Schema:** Full specification
- [ ] **Extension Guide:** How to add new element types

---

## 15. Success Criteria

### 15.1 MVP Definition
✅ MVP is complete when:
- [ ] Users can create templates with drag/drop designer
- [ ] Templates support all 5 element types (QR, text, image, rectangle, line)
- [ ] Data binding works for device.*, connection.*, global.* variables
- [ ] Templates can be saved, edited, deleted
- [ ] 2 system templates exist (rack-mount, ceiling/wall-mount)
- [ ] Users can generate PDF, SVG, and PNG stickers
- [ ] Batch generation works for multiple devices (ZIP download)
- [ ] Company logos can be uploaded and appear in stickers

### 15.2 Quality Metrics
- [ ] Designer loads in < 2 seconds
- [ ] PDF generation completes in < 500ms per sticker
- [ ] Batch of 50 devices completes in < 30 seconds
- [ ] Zero layout errors in printed stickers (measured dimensions match design)
- [ ] 95% template save success rate

---

## 16. Risk Assessment

| Risk | Impact | Mitigation |
|------|--------|------------|
| **Fabric.js learning curve** | Medium | Budget extra time for Phase 2, use official docs extensively |
| **PDF precision issues** | High | Test with physical prints early, use QuestPDF mm units |
| **Large batch timeouts** | Medium | Implement background job queue for batches > 50 devices |
| **Template JSON corruption** | Medium | Validate JSON on save, implement versioning |
| **Logo storage scaling** | Low | Start with base64, migrate to Blob Storage if needed |
| **Browser compatibility** | Low | Test on Chrome, Edge, Firefox; Fabric.js has good support |

---

## 17. Open Questions & Lessons Learned

### Resolved Questions (from Phase 4):
1. **Canvas visibility with Fabric.js** ✅ RESOLVED
   - **Issue:** Live preview not rendering despite objects being loaded
   - **Root Cause:** Fabric.js upper-canvas blocking lower-canvas
   - **Solution:** Explicit inline styles + CSS !important flag
   - **Lesson:** Fabric.js CSS specificity requires understanding its canvas layering architecture

2. **Transparent background visualization** ✅ RESOLVED
   - **Issue:** Transparent and white backgrounds looked identical
   - **Solution:** Checkerboard pattern (standard transparency indicator)
   - **Lesson:** Users need visual feedback for abstract concepts like transparency

3. **Placeholder vs. Real Data** ✅ DECISION MADE
   - **Phase 4 approach:** Client-side placeholders for preview
   - **Phase 5 approach:** Server-side real data for actual exports
   - **Rationale:** Placeholder data allows template design without device dependency

### Remaining Questions for Phase 5:

1. **Background Layer Detection for Transparent Exports**
   - When template has white background rectangle, transparent export still shows white
   - Should Phase 5 include auto-detection to hide background layers on transparent export?
   - Risk: Could accidentally hide design elements user wants to keep
   - Recommendation: Add as optional feature with explicit user opt-in

2. **Multi-Device Layout Options**
   - Grid layout (3x3 stickers per page)?
   - Rows layout (1 sticker per row, multiple rows)?
   - Sheet layout (simulate Avery label sheets)?
   - Recommendation: Start with grid (most flexible), add sheets later

3. **Batch Export Performance**
   - Should batch operations use background job queue (Hangfire) for large sets?
   - What's the threshold? (10 devices, 50 devices, 100+ devices?)
   - Recommendation: Start with sync processing, add background jobs in Phase 6 if needed

### For Technical Investigation (Phase 5):
1. **QuestPDF font embedding:** Test if custom fonts work in Docker/Azure
2. **ImageSharp performance:** Benchmark high-res PNG generation (300 DPI)
3. **PDF precision:** Verify mm measurements match actual printed dimensions
4. **QR code generation:** Real QR codes vs. placeholder patterns in Phase 5

---

## 18. Appendix: File Structure

```
QRStickers/
├── StickerTemplate.cs (new)
├── GlobalVariable.cs (new)
├── Connection.cs (modified - add CompanyLogoUrl)
├── QRStickersDbContext.cs (modified - add DbSets)
├── Program.cs (modified - register services, call seeder)
│
├── Services/
│   ├── TemplateService.cs (new)
│   ├── PdfGenerationService.cs (new)
│   ├── SvgGenerationService.cs (new)
│   ├── PngGenerationService.cs (new)
│   ├── TemplateRenderService.cs (new)
│   └── ImageStorageService.cs (new - optional)
│
├── Data/
│   └── SystemTemplateSeeder.cs (new)
│
├── Controllers/
│   └── StickerApiController.cs (new - or minimal API)
│
├── Models/
│   ├── StickerRenderRequest.cs (new)
│   └── StickerRenderResponse.cs (new)
│
├── Pages/
│   ├── Templates/
│   │   ├── Index.cshtml[.cs] (new)
│   │   ├── Designer.cshtml[.cs] (new)
│   │   ├── Create.cshtml[.cs] (new)
│   │   ├── Edit.cshtml[.cs] (new)
│   │   └── Delete.cshtml[.cs] (new)
│   ├── Stickers/
│   │   ├── Generate.cshtml[.cs] (new)
│   │   └── Batch.cshtml[.cs] (new)
│   ├── Connections/
│   │   └── Edit.cshtml[.cs] (modified - add logo upload)
│   └── Meraki/
│       └── Connection.cshtml[.cs] (modified - add sticker button)
│
├── wwwroot/
│   ├── js/
│   │   ├── designer.js (new - Fabric.js integration)
│   │   ├── fabric-extensions.js (new - custom objects)
│   │   ├── template-renderer.js (new - preview logic)
│   │   ├── device-selection.js (new - multi-select UI)
│   │   └── logo-preview.js (new - upload preview)
│   └── css/
│       └── designer.css (new - designer styles)
│
└── Migrations/
    └── AddStickerDesignerTables.cs (new)
```

---

## Document Version

**Version:** 2.0
**Last Updated:** 2025-10-22
**Next Review:** After Phase 5 completion

---

## Change Log

| Date | Version | Changes | Author |
|------|---------|---------|--------|
| 2025-10-19 | 1.0 | Initial implementation plan created | Claude |
| 2025-10-22 | 2.0 | Phase 4 completion status update:<br>- Added status update section<br>- Added implementation phases status table<br>- Updated Phase 4 to show completion with actual effort (~6 hours)<br>- Updated Phase 5 with references to detailed planning documents<br>- Updated timeline with actual vs. estimated durations<br>- Resolved Phase 4 questions and documented lessons learned<br>- Total progress: 67% (4 of 6 phases complete) | Claude |

---

**END OF IMPLEMENTATION PLAN**

---

## Quick Reference: Key Deliverables by Phase

**Phase 4 (COMPLETE):**
- ✅ Export modal with live preview
- ✅ PNG/SVG export with placeholder data
- ✅ Real-time preview updates (format, DPI, background)
- ✅ Transparent background visualization
- ✅ ~950 lines of code across 4 files

**Phase 5 (NEXT - Planning Complete):**
- 📋 Device data integration
- 📋 Single/multi-device export
- 📋 Server-side PDF generation
- 📋 Batch export with ZIP download
- 📋 See [Phase5-DeviceExport-Planning.md](./Phase5-DeviceExport-Planning.md)

**Phase 6 (FUTURE):**
- 📋 Company logo upload and storage
- 📋 Logo preview in designer
- 📋 Logo integration in exports
