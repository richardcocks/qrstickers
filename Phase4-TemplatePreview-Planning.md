# Phase 4: Template Preview & Export (PNG/SVG) - Planning

**Date Created:** 2025-10-22
**Status:** PLANNING - APPROVED
**Estimated Duration:** 2-3 days

---

## Overview

Phase 4 implements template preview and export functionality, allowing users to see how their sticker designs look with realistic placeholder data and export them in PNG/SVG formats. This is a focused MVP that sets the foundation for later phases (device export with actual data, PDF export, etc.).

---

## Goals

1. **Template Preview with Sample Data**
   - Show users how their template looks with realistic placeholder values
   - Support all data binding types (device.*, connection.*, global.*)
   - Render QR codes with placeholder data

2. **Export Formats (Phase 4)**
   - PNG: Raster format for quick preview/sharing
   - SVG: Vector format for further editing
   - (PDF deferred to later phase)

3. **User Control**
   - Choose export format
   - Configure format-specific options (DPI for PNG, background, etc.)
   - Live preview before export
   - One-click download

---

## Scope

### In Scope (Phase 4)
- ✅ Placeholder data generation for all binding types
- ✅ Export modal/UI on Designer page
- ✅ PNG export with DPI selector (96, 150, 300)
- ✅ SVG export with Fabric.js
- ✅ Live preview in modal
- ✅ Transparent background option for PNG
- ✅ File download handler
- ✅ Template Index preview button (optional)

### Out of Scope (Phase 5+)
- ❌ Device data merging
- ❌ Multi-device export
- ❌ PDF export
- ❌ Export from Devices/Networks pages
- ❌ Bulk export
- ❌ Export history/logging

---

## Deliverables

### Phase 4.1: Placeholder Engine (Day 1 - Morning)

**New Files:**
1. `wwwroot/js/export-preview.js` - Client-side export and placeholder logic

**Features:**
- Generate realistic placeholder values for each data binding type
- Support data binding patterns:
  - `{{device.serial}}` → "MS-1234-ABCD-5678"
  - `{{device.name}}` → "Example Switch"
  - `{{device.mac}}` → "00:1A:2B:3C:4D:5E"
  - `{{device.model}}` → "MS225-48FP"
  - `{{device.tags}}` → "production, datacenter"
  - `{{connection.name}}` → "Main Office"
  - `{{global.supportUrl}}` → "support.example.com"
  - `{{global.supportPhone}}` → "+1-555-0100"
- Extract all `{{...}}` patterns from template JSON
- Render preview with placeholders merged into template

---

### Phase 4.2: Export Modal UI (Day 1 - Afternoon)

**Modified Files:**
1. `Pages/Templates/Designer.cshtml` - Add Export button and modal HTML
2. `wwwroot/css/designer.css` - Modal styling

**Features:**
- Export button in Designer toolbar (next to Save button)
- Modal dialog with:
  - Format selector (PNG or SVG radio buttons)
  - PNG-specific options section:
    - Resolution: 96 DPI, 150 DPI, 300 DPI (radio buttons)
    - Background: White or Transparent (radio buttons)
  - Live preview pane showing rendered sticker with placeholder data
  - Cancel and Download buttons
- Modal updates preview when format/options change
- Responsive design matching existing Designer UI

---

### Phase 4.3: PNG Export Implementation (Day 2 - Morning)

**Modified Files:**
1. `wwwroot/js/export-preview.js` - PNG export logic
2. `wwwroot/js/designer.js` - Integrate export modal

**Implementation:**
```javascript
function exportPNG(canvas, dpi) {
    // DPI to multiplier: 96 DPI = 1x, 150 DPI = 1.5625x, 300 DPI = 3.125x
    const multiplier = dpi / 96;

    // Export using Fabric.js canvas.toDataURL
    const dataUrl = canvas.toDataURL({
        format: 'png',
        multiplier: multiplier,
        enableRetinaScaling: false
    });

    // Trigger browser download
    downloadFile(dataUrl, 'template-preview.png', 'image/png');
}

function downloadFile(dataUrl, fileName, mimeType) {
    const link = document.createElement('a');
    link.href = dataUrl;
    link.download = fileName;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);
}
```

**Features:**
- PNG export via Fabric.js `toDataURL('image/png')`
- Support 3 DPI levels:
  - 96 DPI (1x) - Web resolution, fast
  - 150 DPI (1.56x) - Medium quality
  - 300 DPI (3.125x) - Print resolution, larger file
- Background option:
  - White: Opaque white background
  - Transparent: PNG with alpha channel
- Automatic file naming: `template-preview.png`
- Browser download handler (no server processing needed)

---

### Phase 4.4: SVG Export Implementation (Day 2 - Afternoon)

**Modified Files:**
1. `wwwroot/js/export-preview.js` - SVG export logic

**Implementation:**
```javascript
function exportSVG(canvas) {
    // Export using Fabric.js canvas.toSVG()
    const svgString = canvas.toSVG();

    // Create blob and download
    const blob = new Blob([svgString], { type: 'image/svg+xml' });
    const url = URL.createObjectURL(blob);
    downloadFile(url, 'template-preview.svg', 'image/svg+xml');
    URL.revokeObjectURL(url);
}
```

**Features:**
- SVG export via Fabric.js `toSVG()`
- Vector format (scalable without quality loss)
- Editable in design tools (Adobe Illustrator, Inkscape, Figma, etc.)
- Automatic file naming: `template-preview.svg`
- Browser download handler

---

### Phase 4.5: Live Preview & Polish (Day 3)

**Modified Files:**
1. `wwwroot/js/export-preview.js` - Preview update logic
2. `wwwroot/css/designer.css` - Preview pane styling
3. `Pages/Templates/Designer.cshtml` - Preview pane HTML

**Features:**
- Preview pane in export modal shows:
  - Sticker with placeholder data rendered
  - Boundary box showing print area
  - Actual export preview (what user will download)
- Real-time preview updates when:
  - Format changes (PNG/SVG)
  - PNG DPI changes
  - Background option changes
- Preview loads instantly (client-side rendering)
- Modal remains open after download (allows multiple exports)
- Clean, professional styling matching Designer UI

---

## Technical Architecture

### Placeholder Generation

**Data Types:**
- Device fields: serial, name, mac, model, tags, ipAddress, etc.
- Connection fields: name, displayName, location
- Global variables: supportUrl, supportPhone, website, etc.

**Algorithm:**
1. Extract template JSON from canvas
2. Find all `{{variable.field}}` patterns using regex
3. Build placeholder map with realistic sample values
4. Create preview canvas with merged placeholders
5. Render preview to export modal

**Realistic Placeholders:**
```javascript
const placeholders = {
    'device.serial': 'MS-1234-ABCD-5678',      // Cisco Meraki format
    'device.name': 'Example Switch',            // Descriptive name
    'device.mac': '00:1A:2B:3C:4D:5E',        // Valid MAC format
    'device.model': 'MS225-48FP',              // Actual Meraki model
    'device.ipAddress': '192.168.1.10',        // Valid IP
    'device.tags': 'production, datacenter',   // Comma-separated
    'connection.name': 'Main Office',          // Business location
    'connection.displayName': 'HQ Network',    // User-friendly name
    'global.supportUrl': 'support.example.com',
    'global.supportPhone': '+1-555-0100'
};
```

### Export Flow

```
Designer Page
    ↓
[Export Button Click]
    ↓
Export Modal Opens
    ├─ Load current canvas state
    ├─ Generate placeholder data
    ├─ Create preview canvas
    └─ Render preview in modal
    ↓
User Selects Format (PNG/SVG)
    ├─ Update preview
    └─ Show format-specific options
    ↓
User Configures Options (DPI, background, etc.)
    └─ Update preview in real-time
    ↓
[Download Button]
    ├─ Render export (PNG or SVG)
    ├─ Generate file (client-side)
    └─ Browser downloads
```

### Client-Side Only

**Benefits:**
- No server processing needed (fast)
- Works offline
- No file storage required
- Instant preview updates
- Scalable (no server load)

**Limitations:**
- Browser memory constraints (very large exports may fail)
- No PDF support (requires server processing with Playwright/library)
- Limited color management

---

## Data Model

No database changes needed for Phase 4. Placeholder generation is entirely client-side and in-memory.

**Optional Future (Phase 5):** ExportLog table to track export history and usage analytics.

---

## UI Design

### Export Modal

```
┌────────────────────────────────────────────────┐
│ Export Template Preview                    [X] │
├────────────────────────────────────────────────┤
│                                                │
│ Format:                                        │
│ ○ PNG (raster image, better for sharing)       │
│ ● SVG (vector, editable in design tools)       │
│                                                │
│ PNG Options:                                   │
│ Resolution:                                    │
│ ● 96 DPI (web)   ○ 150 DPI (medium)            │
│ ○ 300 DPI (print)                              │
│                                                │
│ Background:                                    │
│ ● White   ○ Transparent                        │
│                                                │
│ Preview:                                       │
│ ┌──────────────────────────────────────────┐   │
│ │                                          │   │
│ │    [Sticker rendered with placeholder    │   │
│ │     data showing here - 100×50mm area]   │   │
│ │                                          │   │
│ │    Serial: MS-1234-ABCD-5678             │   │
│ │    Name: Example Switch                  │   │
│ │    [QR Code]                             │   │
│ │                                          │   │
│ └──────────────────────────────────────────┘   │
│                                                │
│                [Cancel]  [Download]            │
└────────────────────────────────────────────────┘
```

### Export Button in Toolbar

Location: Designer top toolbar (next to Save button)
Icon/Text: "📥 Export" or similar
Action: Opens export modal

---

## Implementation Details

### Step 1: Placeholder Generation (30 minutes)
```javascript
function generatePlaceholders(templateJson) {
    const placeholders = {
        'device.serial': 'MS-1234-ABCD-5678',
        'device.name': 'Example Switch',
        'device.mac': '00:1A:2B:3C:4D:5E',
        'device.model': 'MS225-48FP',
        'device.ipAddress': '192.168.1.10',
        'device.tags': 'production, datacenter',
        'connection.name': 'Main Office',
        'global.supportUrl': 'support.example.com',
        'global.supportPhone': '+1-555-0100'
    };

    // Parse template to extract all {{variable.field}} patterns
    // Merge placeholders into template
    // Return merged template JSON
}
```

### Step 2: Create Preview Canvas (1 hour)
```javascript
function createPreviewCanvas(templateJson, width, height) {
    // Create temporary Fabric.js canvas
    // Load template objects from JSON
    // Return canvas for preview rendering
}
```

### Step 3: Export PNG (1 hour)
```javascript
function exportPNG(canvas, dpi, background) {
    const multiplier = dpi / 96;
    const options = {
        format: 'png',
        multiplier: multiplier,
        enableRetinaScaling: false,
        backgroundColor: background === 'white' ? '#ffffff' : 'transparent'
    };
    const dataUrl = canvas.toDataURL(options);
    downloadFile(dataUrl, 'template-preview.png');
}
```

### Step 4: Export SVG (30 minutes)
```javascript
function exportSVG(canvas) {
    const svg = canvas.toSVG();
    const blob = new Blob([svg], { type: 'image/svg+xml' });
    const url = URL.createObjectURL(blob);
    downloadFile(url, 'template-preview.svg');
    URL.revokeObjectURL(url);
}
```

### Step 5: Live Preview (1 hour)
```javascript
// Listen to format/option changes
document.querySelectorAll('.export-option-input').forEach(input => {
    input.addEventListener('change', updatePreview);
});

function updatePreview() {
    const format = getSelectedFormat();
    const options = getSelectedOptions();
    // Recreate preview canvas with current options
    // Re-render in preview pane
}
```

---

## Testing Plan

### Manual Testing

**PNG Export:**
- [ ] Export at 96 DPI - File size reasonable, quality good
- [ ] Export at 150 DPI - Larger file, better quality
- [ ] Export at 300 DPI - High quality, may be slower
- [ ] White background - Background opaque white
- [ ] Transparent background - Background transparent (alpha channel)
- [ ] File downloads with correct name and extension
- [ ] File opens in image viewer

**SVG Export:**
- [ ] SVG downloads with correct name and extension
- [ ] SVG opens in Adobe Illustrator
- [ ] SVG opens in Inkscape
- [ ] SVG opens in Figma
- [ ] SVG is editable in design tools
- [ ] All elements preserved (shapes, text, QR code)

**Preview:**
- [ ] Preview updates when format changes
- [ ] Preview updates when DPI changes
- [ ] Preview updates when background changes
- [ ] Preview shows placeholder data correctly
- [ ] QR code renders in preview
- [ ] Preview renders quickly (< 1 second)

**Placeholder Data:**
- [ ] All binding types replaced with placeholders
- [ ] Realistic values (not just `{{device.serial}}` text)
- [ ] QR codes generate with placeholder data
- [ ] Text fields display placeholder values
- [ ] Missing global variables handled gracefully

**Edge Cases:**
- [ ] Template with no data bindings - Shows template as-is
- [ ] Template with many bindings - All replaced correctly
- [ ] Very large template - Export completes without error
- [ ] Template with images - Images preserved in PNG/SVG
- [ ] Template with complex shapes - Shapes rendered correctly

---

## Browser Compatibility

### Tested
- ✅ Chrome (latest) - **PRIMARY**
- ⚠️ Edge - Not yet tested
- ⚠️ Firefox - Not yet tested

### Requirements
- Canvas `toDataURL()` support - All modern browsers
- Canvas `toSVG()` (Fabric.js) - All modern browsers
- Blob API - All modern browsers
- Download attribute on links - All modern browsers

---

## Performance

### Expected Performance
- **Placeholder generation:** < 50ms
- **Preview canvas creation:** < 100ms
- **Preview render:** < 200ms
- **PNG export:** < 500ms (96 DPI), < 2s (300 DPI)
- **SVG export:** < 100ms
- **File download:** Instant (browser native)

### Potential Issues
- Large templates (many objects) may slow down preview
- 300 DPI PNG for large stickers may consume significant browser memory
- Solution: Show progress indicator, warn for large exports

---

## Security Considerations

### Input Validation
- ✅ Template ID belongs to user (already validated in Designer)
- ✅ No server processing (client-side only)
- ✅ No file storage (direct download)
- ✅ No user input beyond format/DPI selection

### Authorization
- ✅ User must be logged in (Designer page requires `[Authorize]`)
- ✅ User must own template (Designer enforces ownership)
- ✅ System templates can be exported (correct behavior)

---

## Success Criteria

By end of Phase 4:
- ✅ Export button visible in Designer toolbar
- ✅ Export modal opens with format selector
- ✅ PNG export works with 3 DPI options
- ✅ SVG export works and is editable in design tools
- ✅ Transparent background option works for PNG
- ✅ Live preview shows template with placeholder data
- ✅ Placeholders are realistic and merge correctly
- ✅ QR codes render with placeholder data
- ✅ Files download with correct MIME type and extension
- ✅ Modal remains open after download
- ✅ Works with all template sizes
- ✅ All manual tests pass

---

## Timeline & Effort

| Task | Duration | Notes |
|------|----------|-------|
| Placeholder generation logic | 1 hour | Extract patterns, create placeholders |
| Export modal UI | 1.5 hours | HTML, CSS, UX design |
| PNG export implementation | 1 hour | Fabric.js toDataURL, file download |
| SVG export implementation | 0.5 hours | Fabric.js toSVG, file download |
| Live preview functionality | 1 hour | Real-time updates, preview canvas |
| Testing & polish | 2-3 hours | Manual testing, bug fixes, refinement |
| **Total** | **7-8 hours** | **~1 full day of focused work** |

**Realistic estimate:** 2-3 days (includes buffer for issues, testing, refinement)

---

## Risks & Mitigation

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|-----------|
| QR code rendering with placeholders | Low | Medium | Test with various QR data sizes |
| Large template performance | Medium | Low | Show progress indicator, warn user |
| SVG compatibility issues | Low | Low | Test in multiple design tools |
| Transparent PNG browser support | Low | Low | Test on target browsers |
| File download not working | Low | High | Test across browsers, add fallback |

---

## Next Phase: Phase 5 - Device Export

See `Phase5-DeviceExport-Planning.md` (to be created later)

Phase 5 will implement:
- Export from Devices/Networks pages
- Merge template with actual device data
- Multi-device export with tiling
- Template matching and fallback logic
- (Potentially PDF export)

---

**Document Version:** 2.0 (Revised)
**Author:** Claude
**Status:** Planning Complete - Ready for Implementation
**Last Updated:** 2025-10-22
