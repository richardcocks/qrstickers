# Phase 5: Device Data Export & Multi-Device Support - Planning

**Date Created:** 2025-10-22
**Status:** PLANNING - READY FOR DEVELOPMENT
**Estimated Duration:** 2-3 weeks
**Complexity:** HIGH (depends on Phase 4 foundation)

---

## Overview

Phase 5 extends Phase 4's template preview/export functionality with **actual device data integration** and **multi-device batch export**. Users can export stickers with real device information from the Devices/Networks pages, and export multiple devices at once with automatic tiling/layout.

This phase transforms the system from a design tool to an operational tool for generating actual device stickers at scale.

---

## Goals

1. **Device Data Integration**
   - Replace placeholder data with actual device info
   - Merge device/network/connection data into templates
   - Support all data binding types with real values

2. **Multi-Device Export**
   - Batch export multiple devices from Devices/Networks pages
   - Automatic layout/tiling (grid, rows, columns)
   - Consistent formatting across multiple exports

3. **Template Matching & Fallback**
   - Match appropriate template by device type/model
   - Fallback to default template if no match
   - User can override template selection

4. **Enhanced Export Options**
   - Export directly from Devices/Networks pages (no manual template selection)
   - Progress indicators for batch operations
   - Export quality/format options

5. **PDF Export (Stretch Goal)**
   - PDF output format (printable stickers)
   - Page layout/margins configuration
   - Print-ready quality

---

## Scope

### In Scope (Phase 5)

- ✅ Device data API/retrieval
- ✅ Template data binding with real device fields
- ✅ Export button on Devices page
- ✅ Export button on Networks page
- ✅ Multi-device selection UI
- ✅ Batch export modal/dialog
- ✅ Multi-device PDF layout (tiling algorithm)
- ✅ Template matching by device type
- ✅ Fallback template logic
- ✅ Progress/status indicators
- ✅ QR code generation with real data (if possible client-side)
- ✅ Export history/logging (optional)

### Out of Scope (Phase 6+)

- ❌ Server-side QR code rendering (complex, deferred)
- ❌ Real image loading from CORS sources (phase 5.5 candidate)
- ❌ Bulk database operations/scheduling
- ❌ Email/cloud integration
- ❌ Advanced template customization per device
- ❌ Export templates (preset export settings)

---

## Deliverables

### Phase 5.1: Device Data Integration (Days 1-3)

**New/Modified Files:**
- `Pages/Templates/ExportHelper.cs` - Server helper for device data retrieval
- `wwwroot/js/device-export.js` - Device export logic
- `wwwroot/js/template-matcher.js` - Template matching algorithm

**Features:**
- Retrieve device/network/connection info from database
- Build comprehensive data context (device + connection + global)
- Handle missing fields gracefully (provide defaults)
- Real QR code generation or server API call
- Support all binding types with actual values

**Data Context Structure:**
```javascript
{
  device: {
    id: 123,
    serial: 'MS-1234-ABCD-5678',
    name: 'Switch-Main-Office',
    mac: '00:1A:2B:3C:4D:5E',
    model: 'MS225-48FP',
    ipAddress: '192.168.1.10',
    tags: ['production', 'main-office'],
    tags_str: 'production, main-office',
    // ... other device fields
  },
  network: {
    id: 456,
    name: 'Production',
    // ... network fields
  },
  connection: {
    displayName: 'Meraki - Production',
    companyLogoUrl: 'https://...',
    // ... connection fields
  },
  global: {
    supportUrl: 'support.example.com',
    supportPhone: '+1-555-0100',
    // ... global variables
  }
}
```

---

### Phase 5.2: Single Device Export from Devices Page (Days 2-3)

**Modified Files:**
- `Pages/Devices/Index.cshtml` - Add Export button to device row
- `Pages/Devices/Index.cshtml.cs` - Export action handler
- `designer.css` - Action button styling

**Features:**
- Export button on each device row (or bulk select)
- Single-click export with matched template
- Modal showing selected device + template + export options
- PNG/SVG/PDF format options
- Same export settings as Phase 4

**UI Changes:**
- Add "📥 Export" action button in device list
- Show device name in export modal
- Display matched template name
- Allow template override (dropdown)

---

### Phase 5.3: Multi-Device Export (Days 4-6)

**Modified Files:**
- `Pages/Devices/Index.cshtml` - Add multi-select UI
- `Pages/Devices/Index.cshtml.cs` - Multi-export handler
- `wwwroot/js/multi-device-export.js` - Multi-device logic
- `wwwroot/css/device-list.css` - Select/action styling

**Features:**
- Checkbox selection for multiple devices
- Bulk export button (enabled only when devices selected)
- Multi-device export modal with:
  - Device list preview
  - Layout options (grid, rows, columns)
  - Page size selection
  - Format options (PNG, SVG, PDF)
- Progress indicator (X of Y exported)
- Download consolidated file or zip

**Layout Algorithm:**
```
Input: Array of devices, page size, layout type
Output: Positioned stickers ready for export

Grid Layout:
- Calculate tiles per row = floor(page_width / sticker_width)
- Calculate rows = ceil(total_devices / tiles_per_row)
- Position each sticker in grid

Row Layout:
- One device per row
- Repeat across pages as needed

Column Layout:
- One device per column
- Repeat across pages as needed
```

---

### Phase 5.4: Template Matching & Fallback (Days 3-5)

**New File:**
- `Pages/Templates/TemplateMatchingService.cs` - Template matching logic

**Algorithm:**
```
For each device:
  1. Check if template matches device.model
  2. If not found, check if template matches device.type
  3. If not found, check user's default template preference
  4. If not found, use system default template
  5. If still not found, skip device with error message
```

**Features:**
- Template metadata (model/type matching)
- User-defined template preferences per device type
- Fallback chain (specific → type → user default → system default)
- Error handling for no available template
- User override in export modal

---

### Phase 5.5: PDF Export (Days 5-7, Stretch Goal)

**New Files:**
- `Pages/Templates/PdfExportService.cs` - Server-side PDF generation
- `wwwroot/js/pdf-export.js` - Client-side PDF handler

**Implementation Options:**

**Option A: Client-Side (jsPDF/html2pdf)**
- Pros: No server dependency, instant
- Cons: Limited layout control, font issues, no print preview
- Estimated effort: 2-3 hours

**Option B: Server-Side (Playwright/Puppeteer)**
- Pros: Full browser rendering, perfect print quality
- Cons: Server dependency, slower, requires Node.js
- Estimated effort: 4-6 hours

**Recommendation:** Option A for MVP, Option B in Phase 6 if needed

**Features:**
- Page size selection (8.5x11", A4, etc.)
- Margin configuration
- Bleed area support
- Print-ready PDF
- Batch PDF generation (one file per device or consolidated)

---

## Technical Architecture

### Data Flow

```
Devices Page
    ↓
User selects device(s) and clicks "Export"
    ↓
Server retrieves device + connection + network data
    ↓
Client calls template matching service
    ↓
Selected template loaded with data bindings
    ↓
Data context merged into template
    ↓
Preview rendered in modal
    ↓
User confirms export settings
    ↓
Canvas rendered at target resolution
    ↓
PNG/SVG/PDF generated
    ↓
File(s) downloaded
```

### Component Interaction

```
ExportHelper.cs (Server)
  ├─ GetDeviceExportData(deviceId)
  ├─ GetBulkDeviceExportData(deviceIds[])
  └─ GetConnectionData(connectionId)

TemplateMatchingService.cs (Server)
  ├─ FindTemplateForDevice(device)
  ├─ FindTemplateByType(deviceType)
  └─ GetDefaultTemplate()

device-export.js (Client)
  ├─ openDeviceExportModal(device)
  ├─ loadTemplateWithDeviceData(template, deviceData)
  └─ exportDevicePNG/SVG/PDF()

multi-device-export.js (Client)
  ├─ getSelectedDevices()
  ├─ calculateMultiDeviceLayout(devices, layout)
  ├─ createMultiDeviceCanvas()
  └─ downloadMultiDeviceFile()

template-matcher.js (Client)
  ├─ matchTemplate(device, availableTemplates)
  └─ getFallbackTemplate()
```

### Database Schema Changes

**New Tables:**
```sql
-- Template matching metadata
CREATE TABLE TemplateDeviceTypes (
    Id INT PRIMARY KEY IDENTITY,
    TemplateId INT NOT NULL,
    DeviceType VARCHAR(100),
    Priority INT,
    CreatedAt DATETIME DEFAULT GETUTCDATE(),
    FOREIGN KEY (TemplateId) REFERENCES Templates(Id)
);

CREATE TABLE TemplateDeviceModels (
    Id INT PRIMARY KEY IDENTITY,
    TemplateId INT NOT NULL,
    DeviceModel VARCHAR(100),
    Priority INT,
    CreatedAt DATETIME DEFAULT GETUTCDATE(),
    FOREIGN KEY (TemplateId) REFERENCES Templates(Id)
);

-- Export history/logging
CREATE TABLE ExportHistory (
    Id INT PRIMARY KEY IDENTITY,
    UserId NVARCHAR(450),
    TemplateId INT,
    DeviceId INT,
    ExportFormat VARCHAR(10),  -- PNG, SVG, PDF
    ExportedAt DATETIME DEFAULT GETUTCDATE(),
    FileSize INT,
    FOREIGN KEY (UserId) REFERENCES AspNetUsers(Id),
    FOREIGN KEY (TemplateId) REFERENCES Templates(Id)
);

CREATE INDEX IX_ExportHistory_UserId ON ExportHistory(UserId);
CREATE INDEX IX_ExportHistory_ExportedAt ON ExportHistory(ExportedAt);
```

**Modified Tables:**
```sql
-- Add default template preference to user
ALTER TABLE AspNetUsers ADD DefaultTemplateId INT NULL;
ALTER TABLE AspNetUsers ADD CONSTRAINT FK_User_DefaultTemplate
    FOREIGN KEY (DefaultTemplateId) REFERENCES Templates(Id);

-- Add template metadata
ALTER TABLE Templates ADD IsDefaultTemplate BIT DEFAULT 0;
ALTER TABLE Templates ADD TemplateCategory VARCHAR(50);  -- 'device', 'network', 'connection'
ALTER TABLE Templates ADD ModelFilter VARCHAR(255);  -- JSON array of device models
ALTER TABLE Templates ADD TypeFilter VARCHAR(255);   -- JSON array of device types
```

---

## Implementation Details

### Step 1: Retrieve Device Data (2 hours)

```csharp
// ExportHelper.cs
public class ExportHelper {
    public async Task<DeviceExportContext> GetDeviceExportDataAsync(
        int deviceId,
        int connectionId,
        ApplicationUser user) {

        var device = await _db.CachedDevices
            .Where(d => d.Id == deviceId && d.Connection.UserId == user.Id)
            .FirstOrDefaultAsync();

        var connection = await _db.Connections
            .Where(c => c.Id == connectionId && c.UserId == user.Id)
            .FirstOrDefaultAsync();

        var network = await _db.CachedNetworks
            .Where(n => n.Device.Id == deviceId)
            .FirstOrDefaultAsync();

        return new DeviceExportContext {
            Device = device,
            Network = network,
            Connection = connection,
            GlobalVariables = _globalVars  // Pre-configured globals
        };
    }
}
```

### Step 2: Match Template (1 hour)

```csharp
// TemplateMatchingService.cs
public class TemplateMatchingService {
    public async Task<Template> FindTemplateForDeviceAsync(
        Device device,
        ApplicationUser user) {

        // Check device model match
        var template = await _db.Templates
            .Where(t => t.UserId == user.Id)
            .Where(t => EF.Functions.JsonContains(t.ModelFilter, device.Model))
            .OrderBy(t => t.Priority)
            .FirstOrDefaultAsync();

        if (template != null) return template;

        // Check device type match
        template = await _db.Templates
            .Where(t => t.UserId == user.Id)
            .Where(t => EF.Functions.JsonContains(t.TypeFilter, device.Type))
            .FirstOrDefaultAsync();

        if (template != null) return template;

        // Fall back to user's default
        template = await _db.Templates
            .Where(t => t.Id == user.DefaultTemplateId)
            .FirstOrDefaultAsync();

        // Fall back to system default
        return await _db.Templates
            .Where(t => t.IsSystemTemplate && t.IsDefaultTemplate)
            .FirstOrDefaultAsync();
    }
}
```

### Step 3: Export with Device Data (2 hours)

```javascript
// device-export.js

function openDeviceExportModal(device, template, deviceData) {
    // Create preview canvas with actual device data
    const previewCanvas = createPreviewCanvasWithDeviceData(
        template,
        deviceData
    );

    // Show in modal
    showExportModal(previewCanvas, device, template);
}

function createPreviewCanvasWithDeviceData(template, deviceData) {
    // Replace all {{bindings}} with actual values from deviceData
    const mergedTemplate = mergeDataIntoTemplate(template, deviceData);

    // Render to preview canvas
    return createAndRenderCanvas(mergedTemplate);
}
```

---

## Testing Plan

### Unit Tests
- [ ] Template matching with various device types
- [ ] Template matching with fallback scenarios
- [ ] Data binding with null/missing fields
- [ ] Multi-device layout algorithm (various device counts)
- [ ] QR code generation with device data

### Integration Tests
- [ ] Retrieve device data from database
- [ ] Template + device data merge
- [ ] Canvas rendering with real data
- [ ] PDF generation (if implemented)

### Manual Tests
- [ ] Export single device from Devices page
- [ ] Export multiple devices (batch)
- [ ] Template matching works correctly
- [ ] Fallback template used when no match
- [ ] PNG/SVG/PDF exports work
- [ ] QR codes contain correct device data
- [ ] Text bindings show real device info
- [ ] Multi-device layout looks correct
- [ ] Performance acceptable for 50+ devices

---

## UI/UX Design

### Devices Page Changes

```
Device List:
┌─────────────────────────────────────────────────────────────────┐
│ □ Device Name          Model          Serial      [📥 Export] │
├─────────────────────────────────────────────────────────────────┤
│ ☑ Switch-Main-A       MS225-48FP     MS-1234...  [📥 Export] │
│ ☑ Switch-Main-B       MS225-48FP     MS-5678...  [📥 Export] │
│ □ AP-Office-01        MR32           MR-ABCD...  [📥 Export] │
├─────────────────────────────────────────────────────────────────┤
│                                    [Bulk Export] (2 selected)   │
└─────────────────────────────────────────────────────────────────┘
```

### Export Modal

```
Single Device Export:
┌──────────────────────────────────────────────────────┐
│ Export: Switch-Main-A                          [X]  │
├──────────────────────────────────────────────────────┤
│                                                      │
│ Template: "Device Sticker (Default)"  [Change ▼]   │
│                                                      │
│ Device Info:                                        │
│ • Serial: MS-1234-ABCD-5678                        │
│ • Name: Switch-Main-A                               │
│ • Model: MS225-48FP                                │
│ • IP: 192.168.1.10                                 │
│                                                      │
│ Export Options:                                     │
│ ○ PNG (96 DPI)  ○ PNG (300 DPI)  ○ SVG  ○ PDF     │
│                                                      │
│ Preview: [Sticker with real device data]           │
│                                                      │
│              [Cancel]  [Export]                     │
└──────────────────────────────────────────────────────┘

Multi-Device Export:
┌──────────────────────────────────────────────────────┐
│ Batch Export: 5 Devices                        [X]  │
├──────────────────────────────────────────────────────┤
│                                                      │
│ Selected Devices:                                   │
│ ☑ Switch-Main-A (MS225-48FP)                       │
│ ☑ Switch-Main-B (MS225-48FP)                       │
│ ☑ AP-Office-01 (MR32)                              │
│ ☑ AP-Office-02 (MR32)                              │
│ ☑ AP-Office-03 (MR32)                              │
│                                                      │
│ Layout: ○ Grid (2x3)  ○ Rows  ○ Columns           │
│ Page Size: ○ Letter  ○ A4  ○ Custom               │
│ Format: ○ PNG  ○ SVG  ○ PDF                        │
│                                                      │
│ [Exporting: ████████░░ 8/10]                       │
│                                                      │
│              [Cancel]  [Download]                  │
└──────────────────────────────────────────────────────┘
```

---

## Browser Compatibility

- ✅ Chrome/Chromium (primary)
- ✅ Firefox
- ⚠️ Safari (test needed)
- ❌ IE 11 (not supported)

Requirements:
- Canvas API ✅
- Fetch API ✅
- Blob API ✅
- Download attribute ✅
- CSS Grid (for layout) ✅

---

## Performance Considerations

**Expected Performance:**
- Device data retrieval: < 200ms (per device)
- Template matching: < 50ms
- Preview generation: < 100ms
- Multi-device canvas (5 devices): < 500ms
- Export PNG (single): < 500ms
- Export PDF (5 devices): < 2000ms
- Batch export (10 devices): < 10 seconds total

**Optimizations:**
- Database query optimization (indexing, eager loading)
- Template matching caching (cache matches for X minutes)
- Canvas pooling (reuse canvas objects)
- Debounce preview updates
- Background worker for batch exports (stretch goal)

---

## Security Considerations

- ✅ User can only export their own devices
- ✅ Check device ownership before export
- ✅ Validate template ownership
- ✅ Rate limit exports (prevent abuse)
- ✅ No sensitive data in exported files
- ✅ Log all exports to ExportHistory

---

## Success Criteria

By end of Phase 5:
- ✅ Export button visible on Devices page
- ✅ Export button visible on Networks page
- ✅ Single device export works with real data
- ✅ Multi-device export works with layout options
- ✅ Template matching matches correctly
- ✅ Fallback template used when no match
- ✅ QR codes contain correct device data
- ✅ Text bindings show real device info (not placeholders)
- ✅ PNG/SVG exports work with real data
- ✅ PDF export works (if implemented)
- ✅ Multi-device layout looks professional
- ✅ Performance acceptable (< 10s for 10 devices)
- ✅ All manual tests pass
- ✅ Export history logged

---

## Timeline & Effort

| Task | Duration | Notes |
|------|----------|-------|
| Device data retrieval & API | 2 hours | GetDeviceExportData, GetBulkExportData |
| Template matching service | 2 hours | Matching algorithm, fallback logic |
| Single device export UI | 2 hours | Devices page button, modal |
| Multi-device selection UI | 2 hours | Checkboxes, bulk export button |
| Multi-device export logic | 3 hours | Layout algorithm, tiling |
| PDF export (if included) | 4 hours | Client-side jsPDF or server-side |
| Testing & refinement | 3-4 hours | Manual testing, bug fixes |
| **Total** | **18-20 hours** | **2-3 weeks with breaks** |

**Realistic estimate:** 2-3 weeks (assumes focused development)

---

## Risks & Mitigation

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|-----------|
| Template matching too complex | Medium | High | Start simple, iterate; use heuristics |
| Multi-device layout issues | Medium | Medium | Thorough testing with various counts |
| PDF generation complexity | Medium | Medium | Use client-side jsPDF MVP; upgrade later |
| Performance with many devices | Low | High | Implement progress indicator, background worker |
| Database query performance | Low | Medium | Add indexes, optimize queries early |
| QR code with real data fails | Low | Medium | Fallback to placeholder QR if needed |

---

## Dependencies

- **Phase 4 Foundation:** Export/preview system must be complete
- **Meraki API:** Already integrated, reuse for device data
- **jsPDF Library:** For PDF export (optional but recommended)
- **Database:** Migration script for new tables

---

## Next Steps

1. **Refine Requirements** with stakeholder
2. **Design Template Matching** algorithm details
3. **Plan Database Schema** changes
4. **Prototype Single Device Export** first
5. **Then Implement Multi-Device**
6. **Add PDF as stretch goal**

---

**Document Version:** 1.0 (Initial Planning)
**Author:** Claude
**Status:** Ready for Development - Waiting for Approval
**Last Updated:** 2025-10-22
