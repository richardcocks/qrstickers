# Phase 4: Multi-Format Export - Planning

**Date Created:** 2025-10-22
**Status:** PLANNING
**Estimated Duration:** 3-5 days

---

## Overview

Phase 4 implements multi-format export functionality, allowing users to export their sticker designs in multiple formats (PDF, SVG, PNG) for different use cases. This phase bridges the designer with external tools and workflows.

---

## Goals

1. **Export Formats Support**
   - PDF (vector format for printing at high quality)
   - SVG (vector format for web/further editing)
   - PNG (raster format for quick preview/sharing)

2. **User Control**
   - Choose export format
   - Configure export options per format
   - Download directly or save to cloud
   - Preview before export

3. **Production Quality**
   - High-resolution output (300 DPI for print)
   - Proper color handling
   - Accurate scaling from design to output
   - Text rendering fidelity

---

## Deliverables

### Phase 4.1: Export Infrastructure (Days 1-2)

**New Files:**
1. `Pages/Templates/Export.cshtml` - Export configuration page
2. `Pages/Templates/Export.cshtml.cs` - Export logic page model
3. `wwwroot/js/export.js` - Client-side export helpers
4. `Services/ExportService.cs` - Server-side export service (abstract)
5. `Services/PdfExportService.cs` - PDF export implementation
6. `Services/SvgExportService.cs` - SVG export implementation
7. `Services/PngExportService.cs` - PNG export implementation

**Modifications:**
1. `Program.cs` - Register export services
2. `Pages/Templates/Designer.cshtml` - Add export button/menu
3. `QRStickersDbContext.cs` - Optional: Add ExportLog table

**Features:**
- ✅ Export configuration UI (format, DPI, paper size, etc.)
- ✅ Abstract export service interface
- ✅ Format-specific implementations
- ✅ Server-side export generation
- ✅ Download handler
- ✅ Basic export logging (optional)

---

### Phase 4.2: PDF Export (Days 2-3)

**Library Choice:**
- **iText7** (commercial, most features) or **SelectPdf** or **HtmlRenderer.Core**
- **Recommendation:** Start with HTML-to-PDF approach using `Playwright` (free, reliable)

**Implementation:**
1. Convert Fabric.js canvas to SVG or HTML
2. Use Playwright/Chrome headless to render PDF
3. Handle:
   - Multiple sticker layout (tiles on page)
   - Custom paper sizes
   - Margins and bleeds
   - QR code generation with proper resolution
   - Text rendering with font embedding

**Features:**
- ✅ Single sticker export
- ✅ Multiple stickers per page (3x2, 4x4, etc.)
- ✅ A4/Letter/Custom paper sizes
- ✅ Margins and bleeds for cutting
- ✅ Print-optimized colors
- ✅ High DPI (300 DPI for print)

---

### Phase 4.3: SVG Export (Days 2-3)

**Library Choice:**
- **Fabric.js built-in SVG export** (already have Fabric.js)
- Or custom SVG generation from template JSON

**Implementation:**
1. Use Fabric.js `canvas.toSVG()` method
2. Clean up output (remove Fabric-specific attributes)
3. Embed images as base64
4. Ensure SVG is editable in Adobe Illustrator, Inkscape, etc.

**Features:**
- ✅ Vector-based export (scalable)
- ✅ Editable in design tools
- ✅ QR code included
- ✅ Embedded fonts (optional)
- ✅ Layer information (optional)

---

### Phase 4.4: PNG Export (Days 3-4)

**Library Choice:**
- **Fabric.js `canvas.toDataURL('image/png')`** (built-in)
- Or **Playwright** for higher resolution

**Implementation:**
1. Export canvas as PNG using Fabric.js
2. Configure DPI/resolution
3. Handle transparency
4. Generate preview thumbnail

**Features:**
- ✅ Raster export for quick preview
- ✅ Transparent background option
- ✅ Multiple resolution options (72/96/150/300 DPI)
- ✅ Quick download (no server processing)

---

### Phase 4.5: Export UI & Integration (Days 4-5)

**Components:**
1. **Export Modal/Page**
   - Format selector (radio buttons or tabs)
   - Format-specific options panel
   - Preview pane
   - Download button
   - Save to cloud button (future enhancement)

2. **Designer Page Integration**
   - Export button in top toolbar
   - Opens export modal
   - Pre-loads current template state

3. **Template Index Integration**
   - Export button on template cards
   - Quick export with default settings
   - Or open export modal for customization

**UX:**
- ✅ Export modal/page from Designer
- ✅ Format selector with descriptions
- ✅ Live preview as options change
- ✅ One-click download
- ✅ Export history (optional)

---

## Technical Architecture

### Export Service Pattern

```csharp
public interface IExportService
{
    ExportFormat Format { get; }
    Task<ExportResult> ExportAsync(
        StickerTemplate template,
        ExportOptions options,
        CancellationToken cancellationToken = default);
}

public class ExportOptions
{
    public int DPI { get; set; } = 96;
    public string PaperSize { get; set; } = "A4"; // A4, Letter, Custom
    public float MarginMm { get; set; } = 5;
    public bool IncludeBleeds { get; set; } = false;
    public int TilesPerPage { get; set; } = 1;
    public bool TransparentBg { get; set; } = false;
}

public class ExportResult
{
    public byte[] Content { get; set; }
    public string ContentType { get; set; }
    public string FileName { get; set; }
}
```

### Service Registration

```csharp
// Program.cs
builder.Services.AddScoped<IPdfExportService, PdfExportService>();
builder.Services.AddScoped<ISvgExportService, SvgExportService>();
builder.Services.AddScoped<IPngExportService, PngExportService>();
```

### Data Flow

```
Designer.cshtml → Export Modal
                 ↓
              Format Select
                 ↓
         Format-Specific Options
                 ↓
              Export.cshtml.cs
                 ↓
          ExportService.ExportAsync()
                 ↓
         PDF/SVG/PngExportService
                 ↓
            Generate Output
                 ↓
           File Download
```

---

## Implementation Phases

### Phase 4.1: Foundation (Days 1-2)
- [x] Create export infrastructure
- [x] Design export service pattern
- [x] Create Export.cshtml UI
- [x] Implement format selector
- [x] Handle file downloads

### Phase 4.2: PDF Export (Days 2-3)
- [x] Choose PDF library
- [x] Implement canvas-to-PDF conversion
- [x] Support paper sizes
- [x] Handle high-DPI output
- [x] Test with various templates

### Phase 4.3: SVG Export (Days 2-3)
- [x] Use Fabric.js SVG export
- [x] Clean up SVG output
- [x] Embed resources
- [x] Verify tool compatibility

### Phase 4.4: PNG Export (Days 3-4)
- [x] Implement canvas-to-PNG
- [x] Support resolution options
- [x] Handle transparency
- [x] Generate thumbnails

### Phase 4.5: UI & Integration (Days 4-5)
- [x] Export button in Designer
- [x] Export modal/page
- [x] Live preview
- [x] Test all formats
- [x] Document export features

---

## Feature Breakdown

### PDF Export Features

**Core:**
- Single sticker on page
- Multiple stickets per page (grid layout)
- A4, Letter, Custom paper sizes
- Configurable margins
- Configurable DPI (72, 150, 300)

**Advanced:**
- Bleed lines (for cutting)
- Crop marks
- Multiple pages (if designing multiple stickets)
- Print profiles (CMYK vs RGB)
- Embedded fonts

**Export Options Form:**
- Paper Size: Dropdown (A4, Letter, A3, A5, Custom)
- Margin (mm): Slider or input
- DPI: Radio buttons (72, 150, 300)
- Bleed (mm): Input
- Tiles per page: Dropdown (1, 4, 6, 9, 16)
- Include crop marks: Checkbox
- CMYK mode: Checkbox

---

### SVG Export Features

**Core:**
- Vector output (scalable)
- Embedded images
- QR code included
- Editable in design tools

**Export Options Form:**
- Embed fonts: Checkbox
- Embed images: Checkbox
- Preserve layer structure: Checkbox
- Include Fabric metadata: Checkbox

---

### PNG Export Features

**Core:**
- Raster output for preview
- Fast (client-side or minimal server processing)
- Transparent background option
- Multiple resolution options

**Export Options Form:**
- Resolution (DPI): Radio buttons (72, 96, 150, 300)
- Background: Radio (white, transparent, custom color)
- Scale: Percentage input (100%, 150%, 200%)

---

## Dependencies & Libraries

### PDF Export
- **Option A:** `SelectPdf` (commercial, $399) - Most features, reliable
- **Option B:** `Playwright` with Chrome headless (free) - HTML to PDF, very reliable
- **Option C:** `iText7` (AGPL/commercial) - Powerful but complex
- **Recommendation:** Start with Playwright (free, reliable, well-supported)

Install:
```bash
dotnet add package Microsoft.Playwright
dotnet exec pwsh install-playwright.ps1
```

### SVG Export
- Built into Fabric.js (already have it)
- No additional dependencies needed

### PNG Export
- Built into Fabric.js (already have it)
- Client-side: No server processing needed
- Server-side: Optional Playwright for higher DPI

### NuGet Packages
- `Microsoft.Playwright` - HTML/Canvas to PDF conversion
- `QRCoder` - Already have it (QR code generation)

---

## Data Model

### Optional: ExportLog Table

```csharp
public class ExportLog
{
    public int Id { get; set; }
    public int TemplateId { get; set; }
    public StickerTemplate Template { get; set; }
    public string ExportFormat { get; set; } // "PDF", "SVG", "PNG"
    public Dictionary<string, string> Options { get; set; } // JSON of export options
    public DateTime ExportedAt { get; set; }
    public int DownloadCount { get; set; } = 0;
}
```

**Purpose:** Track export usage for analytics and debugging

---

## UI Mockup

### Export Modal (in Designer)

```
┌─────────────────────────────────────────────┐
│ Export Design                           [X] │
├─────────────────────────────────────────────┤
│                                             │
│ Format:                                     │
│ ○ PDF (vector, print-optimized)             │
│ ○ SVG (editable, scalable)                  │
│ ○ PNG (raster, quick preview)               │
│                                             │
│ PDF Options:                                │
│ Paper Size: [A4 ▼]                          │
│ Resolution: ○ 72  ○ 150  ● 300 DPI          │
│ Margin: [5 mm]                              │
│ □ Include Bleed                             │
│                                             │
│ Preview: [Shows sticker on paper]           │
│                                             │
│                 [Cancel] [Download PDF]    │
└─────────────────────────────────────────────┘
```

---

## Testing Plan

### Unit Tests
- [ ] Export service pattern
- [ ] Format conversion logic
- [ ] DPI/resolution calculations
- [ ] File naming conventions

### Integration Tests
- [ ] PDF export from Designer
- [ ] SVG export from Index
- [ ] PNG export with various options
- [ ] File download handling
- [ ] Authorization (user can only export own templates)

### Manual Testing
- [ ] Export PDF from Designer
- [ ] Export SVG and edit in Illustrator
- [ ] Export PNG at various DPI
- [ ] Test with different template sizes
- [ ] Test with complex designs (many elements)
- [ ] Test with QR codes
- [ ] Test with images

---

## Browser Compatibility

### Client-Side Export (PNG)
- ✅ Chrome, Edge, Firefox, Safari (canvas.toDataURL)
- ✅ IE11+ (limited, but supported)

### Server-Side Export (PDF, SVG)
- ✅ All browsers (server generates, browser downloads)
- ⚠️ Large files may take time to generate

---

## Performance Considerations

### PDF Generation
- Playwright spins up Chrome headless process (~300ms)
- Consider: Caching, async processing, queue system for large exports

### SVG Generation
- Fabric.js SVG export: < 100ms typically
- Very fast, minimal server load

### PNG Generation
- Client-side: Instant (built into browser)
- Server-side (high DPI): ~100-200ms per PNG

### Optimization Strategies
- Cache exported files temporarily
- Use async/await for long-running exports
- Show progress indicator for large exports
- Consider background job queue for bulk exports (future)

---

## Security Considerations

### Input Validation
- ✅ Template ID must belong to user
- ✅ Export options must be within allowed ranges
- ✅ File names sanitized (no path traversal)
- ✅ Content-Type headers correct

### Authorization
- ✅ User can only export their own templates
- ✅ System templates can be exported by all users
- ✅ Export page requires `[Authorize]`

### File Handling
- ✅ Downloaded file size limited
- ✅ Temporary files cleaned up
- ✅ No sensitive data in exported files

---

## Future Enhancements (Phase 5+)

1. **Bulk Export**
   - Export multiple templates at once
   - Create ZIP file with all exports
   - Batch processing with progress

2. **Cloud Integration**
   - Export to Google Drive
   - Export to Dropbox
   - Email export

3. **Advanced PDF Features**
   - Crop marks and registration marks
   - CMYK color mode
   - Barcode support
   - Font embedding

4. **Template Variants**
   - Export multiple variations with data binding
   - Merge template with CSV data for batch printing

5. **Preview & Proof**
   - Interactive preview before download
   - Layered preview (see each element separately)
   - Print simulation (CMYK preview)

6. **Export History**
   - Track exports per template
   - One-click re-export with same options
   - Export analytics

---

## Success Criteria

By end of Phase 4:
- ✅ Users can export templates in 3 formats (PDF, SVG, PNG)
- ✅ PDF exports are print-ready (300 DPI)
- ✅ SVG exports are editable in design tools
- ✅ PNG exports are instant and high-quality
- ✅ Export options customizable per format
- ✅ Live preview before export
- ✅ All exports include QR codes and proper scaling
- ✅ Exports accessible from Designer and Index pages
- ✅ Proper authorization and security
- ✅ < 2 second export time (PDF may take longer)

---

## Risks & Mitigation

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|-----------|
| PDF lib licensing issues | Medium | High | Use Playwright (free), evaluate early |
| Large file generation timeout | Low | Medium | Add timeout handling, async processing |
| QR code rendering issues | Low | Medium | Test with various QR code sizes |
| Font rendering differences | Medium | Low | Embed fonts, test in target apps |
| Image embedding issues | Low | Medium | Test with various image formats |

---

## Timeline

- **Day 1-2:** Export infrastructure, Export.cshtml UI, service pattern
- **Day 2-3:** PDF export implementation with Playwright
- **Day 2-3:** SVG export (parallel) using Fabric.js
- **Day 3-4:** PNG export and high-DPI options
- **Day 4-5:** Designer integration, UI polish, comprehensive testing

**Total Estimated Time:** 4-5 development days

---

## Acceptance Criteria

- [ ] Users can export PDF from Designer
  - [ ] Configurable paper size, DPI, margins
  - [ ] Print-ready quality (300 DPI)
  - [ ] Multiple stickers per page option
- [ ] Users can export SVG from Designer
  - [ ] Editable in Adobe Illustrator
  - [ ] All elements preserved
  - [ ] QR codes included
- [ ] Users can export PNG from Designer
  - [ ] Fast download (no wait)
  - [ ] Multiple resolution options
  - [ ] Transparent background option
- [ ] Export button visible in Designer
- [ ] Export options customizable per format
- [ ] Authorization verified (user can only export own templates)
- [ ] File downloads work correctly
- [ ] All exports tested and verified

---

## Next Steps

1. **Confirm approach** with user (Playwright for PDF vs other options)
2. **Estimate timeline** (3-5 days)
3. **Schedule implementation** after Phase 3 complete
4. **Set up Playwright** environment if PDF via Playwright chosen
5. **Create first export test** (PDF from simple template)

---

**Document Version:** 1.0
**Author:** Claude
**Status:** Planning Phase - Ready for Kickoff
