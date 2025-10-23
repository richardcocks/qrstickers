# Phase 5.3: Multi-Device Export - Implementation Plan

**Date Created:** 2025-10-22
**Status:** PLANNING - Ready for Development
**Estimated Duration:** 4-6 hours
**Complexity:** MEDIUM (builds on Phase 5 MVP foundation)
**Dependencies:** Phase 5 MVP (complete), JSZip library

---

## Executive Summary

Phase 5.3 adds **bulk device export** capability, allowing users to select multiple devices from the Network page and export them all at once. This is a high-value feature for users managing large device inventories who need to print sticker sheets or generate bulk exports for asset management.

**User Value:**
- Export 10-50 devices in seconds instead of clicking export 50 times
- Generate print-ready sticker sheets with automatic grid layout
- Download individual stickers as a ZIP file for separate use
- See real-time progress during bulk operations

**Technical Approach:**
- Client-side batching (no server changes needed initially)
- JSZip library for ZIP file generation
- Optional: Grid layout for print sheets
- Progress indicator with cancel capability

---

## Goals

1. **Checkbox Selection** - Users can select multiple devices from the table
2. **Bulk Export Button** - "Export Selected (N)" button enabled when devices selected
3. **Export Modal** - Show selected devices, template matches, and export options
4. **Individual + ZIP** - Export each device separately, package as ZIP download
5. **Grid Layout (Stretch)** - Tile multiple stickers on single canvas for printing
6. **Progress Indicator** - Real-time feedback (Processing device 5 of 12...)
7. **Error Handling** - Skip failed devices, report at end

---

## Success Criteria

By end of Phase 5.3:
- ✅ Checkboxes appear on device rows
- ✅ "Select All" / "Deselect All" functionality works
- ✅ Bulk export button shows selection count
- ✅ Modal displays selected devices with template matches
- ✅ Individual export → ZIP download works
- ✅ Progress indicator updates in real-time
- ✅ Can cancel operation mid-export
- ✅ Error handling reports failed devices
- ✅ Performance acceptable for 50 devices (< 30 seconds)
- ✅ All manual tests pass

---

## Technical Architecture

### Data Flow

```
User selects devices (checkboxes)
        ↓
Clicks "Export Selected (5)"
        ↓
Multi-device export modal opens
        ↓
Shows: Device list, template matches, export options
        ↓
User confirms settings
        ↓
For each device:
    ├─ Fetch device data (cached if available)
    ├─ Match template
    ├─ Render canvas with device data
    ├─ Export to PNG/SVG
    ├─ Add to ZIP file
    └─ Update progress indicator
        ↓
Generate final ZIP blob
        ↓
Download ZIP file
        ↓
Show completion summary (X succeeded, Y failed)
```

### Export Strategies

**Strategy A: Individual Files → ZIP (MVP)**
- Export each device as separate PNG/SVG
- Add all files to ZIP archive
- Download single ZIP file
- **Pros:** Simple, works with different templates per device
- **Cons:** User must unzip, not print-ready

**Strategy B: Grid Layout → Single Image (Stretch Goal)**
- Calculate grid layout (e.g., 3×4 = 12 stickers per page)
- Create large canvas with all stickers tiled
- Export as single PNG/PDF
- **Pros:** Print-ready, professional sheets
- **Cons:** More complex, requires same template for all

**Recommendation:** Implement Strategy A first (MVP), add Strategy B in Phase 5.4

---

## Implementation Steps

### Step 1: Add Checkbox UI (30 minutes)

**File:** `Pages/Meraki/Network.cshtml`

**Changes:**
1. Add checkbox column to device table header
2. Add checkbox to each device row
3. Add "Select All" / "Deselect All" toggle

**HTML Structure:**
```html
<table class="device-table">
    <thead>
        <tr>
            <th style="width: 40px;">
                <input type="checkbox" id="selectAllDevices" onchange="toggleSelectAll()" />
            </th>
            <th>Device Name</th>
            <th>Serial</th>
            <th>Model</th>
            <th>Status</th>
            <th>Action</th>
        </tr>
    </thead>
    <tbody>
        @foreach (var device in Model.Devices)
        {
            <tr>
                <td>
                    <input type="checkbox"
                           class="device-checkbox"
                           data-device-id="@device.Id"
                           data-device-name="@device.Name"
                           data-connection-id="@Model.ConnectionId"
                           onchange="onDeviceSelectionChanged()" />
                </td>
                <td>@device.Name</td>
                <!-- ... other columns ... -->
            </tr>
        }
    </tbody>
</table>
```

---

### Step 2: Bulk Export Button (30 minutes)

**File:** `Pages/Meraki/Network.cshtml`

**Changes:**
1. Add bulk export button below table (initially disabled)
2. Update button text with selection count
3. Enable/disable based on selection

**HTML:**
```html
<div class="bulk-actions" style="margin-top: 20px;">
    <button id="bulkExportBtn"
            class="btn-primary"
            onclick="openBulkExportModal()"
            disabled>
        📦 Export Selected (0)
    </button>
    <span id="selectionSummary" class="selection-summary"></span>
</div>
```

**JavaScript (in multi-device-export.js):**
```javascript
function onDeviceSelectionChanged() {
    const selected = getSelectedDevices();
    const bulkBtn = document.getElementById('bulkExportBtn');

    bulkBtn.disabled = selected.length === 0;
    bulkBtn.textContent = `📦 Export Selected (${selected.length})`;
}

function getSelectedDevices() {
    const checkboxes = document.querySelectorAll('.device-checkbox:checked');
    return Array.from(checkboxes).map(cb => ({
        id: parseInt(cb.getAttribute('data-device-id')),
        name: cb.getAttribute('data-device-name'),
        connectionId: parseInt(cb.getAttribute('data-connection-id'))
    }));
}
```

---

### Step 3: Multi-Device Export Modal (1 hour)

**File:** `wwwroot/js/multi-device-export.js` (NEW)

**Modal Structure:**
```html
<div class="modal bulk-export-modal">
    <div class="modal-content" style="max-width: 800px;">
        <div class="modal-header">
            <h2>Bulk Export: <span id="deviceCount">0</span> Devices</h2>
            <button class="modal-close-btn">✕</button>
        </div>
        <div class="modal-body">
            <!-- Device List Section -->
            <div class="selected-devices-section">
                <h3>Selected Devices</h3>
                <div id="deviceList" class="device-list">
                    <!-- Populated with device cards showing name, serial, matched template -->
                </div>
            </div>

            <!-- Export Options Section -->
            <div class="export-options-section">
                <h3>Export Settings</h3>

                <label><strong>Output Format:</strong></label>
                <div class="radio-group">
                    <label>
                        <input type="radio" name="bulk-format" value="zip-png" checked>
                        Individual PNG Files (ZIP)
                    </label>
                    <label>
                        <input type="radio" name="bulk-format" value="zip-svg">
                        Individual SVG Files (ZIP)
                    </label>
                    <!-- Stretch goal: -->
                    <label>
                        <input type="radio" name="bulk-format" value="grid-png" disabled>
                        Grid Layout (Single PNG) - Coming soon
                    </label>
                </div>

                <label><strong>DPI (for PNG):</strong></label>
                <select id="bulkDpi">
                    <option value="96">96 DPI (Web)</option>
                    <option value="150">150 DPI (Medium)</option>
                    <option value="300" selected>300 DPI (Print)</option>
                </select>

                <label><strong>Background:</strong></label>
                <div class="radio-group">
                    <label>
                        <input type="radio" name="bulk-background" value="white" checked>
                        White
                    </label>
                    <label>
                        <input type="radio" name="bulk-background" value="transparent">
                        Transparent
                    </label>
                </div>
            </div>

            <!-- Progress Section (hidden initially) -->
            <div id="exportProgress" class="progress-section" style="display: none;">
                <h3>Exporting...</h3>
                <div class="progress-bar">
                    <div class="progress-fill" id="progressBar" style="width: 0%;"></div>
                </div>
                <p id="progressText">Processing device 1 of 10...</p>
                <button id="cancelExportBtn" class="btn-secondary">Cancel</button>
            </div>
        </div>
        <div class="modal-footer">
            <button onclick="closeBulkExportModal()" class="btn-secondary">Cancel</button>
            <button onclick="startBulkExport()" class="btn-primary">Start Export</button>
        </div>
    </div>
</div>
```

**Key Functions:**
```javascript
// Open modal with selected devices
async function openBulkExportModal() {
    const selected = getSelectedDevices();
    // Fetch template matches for all devices (batch API or individual)
    // Populate device list with template info
    // Show modal
}

// Populate device list with cards
function renderDeviceList(devices, templateMatches) {
    // Create card for each device showing:
    // - Device name, serial, model
    // - Matched template name
    // - Match confidence badge
}
```

---

### Step 4: Bulk Export Logic (1.5 hours)

**File:** `wwwroot/js/multi-device-export.js`

**Core Export Function:**
```javascript
async function startBulkExport() {
    console.log('[Bulk Export] Starting bulk export');

    const selected = getSelectedDevices();
    const format = document.querySelector('input[name="bulk-format"]:checked').value;
    const dpi = parseInt(document.getElementById('bulkDpi').value);
    const background = document.querySelector('input[name="bulk-background"]:checked').value;

    // Hide options, show progress
    document.querySelector('.export-options-section').style.display = 'none';
    document.getElementById('exportProgress').style.display = 'block';

    // Initialize ZIP
    const zip = new JSZip();
    const exportedFiles = [];
    const failedDevices = [];
    let cancelled = false;

    // Set up cancel handler
    document.getElementById('cancelExportBtn').onclick = () => {
        cancelled = true;
    };

    // Export each device
    for (let i = 0; i < selected.length; i++) {
        if (cancelled) {
            console.log('[Bulk Export] Export cancelled by user');
            break;
        }

        const device = selected[i];
        updateProgress(i + 1, selected.length, device.name);

        try {
            // Fetch device data and template
            const exportData = await fetchDeviceExportData(device.id, device.connectionId);

            // Create device data map
            const deviceDataMap = createDeviceDataMap(exportData);

            // Render canvas with device data
            const blob = await renderDeviceToBlob(
                exportData.matchedTemplate,
                deviceDataMap,
                format.includes('png') ? 'png' : 'svg',
                { dpi, background }
            );

            // Add to ZIP
            const filename = generateFilename(device, format, dpi);
            zip.file(filename, blob);
            exportedFiles.push(filename);

        } catch (error) {
            console.error(`[Bulk Export] Failed to export device ${device.name}:`, error);
            failedDevices.push({ device: device.name, error: error.message });
        }
    }

    if (!cancelled) {
        // Generate ZIP file
        console.log('[Bulk Export] Generating ZIP file');
        const zipBlob = await zip.generateAsync({ type: 'blob' });

        // Download ZIP
        const zipFilename = `device-stickers-${selected.length}-devices-${Date.now()}.zip`;
        downloadBlob(zipBlob, zipFilename);

        // Show completion summary
        showCompletionSummary(exportedFiles.length, failedDevices);
    }

    closeBulkExportModal();
}

// Helper: Render device to blob
async function renderDeviceToBlob(template, deviceDataMap, format, options) {
    const templateJson = JSON.parse(template.templateJson);

    // Clone and merge template with device data
    const mergedTemplate = JSON.parse(JSON.stringify(templateJson));
    replacePlaceholdersInTemplate(mergedTemplate, deviceDataMap);

    // Create temporary canvas
    const tempCanvas = document.createElement('canvas');

    // Render using export-preview.js functions
    const fabricCanvas = createAndRenderPreviewCanvas(
        tempCanvas,
        mergedTemplate,
        template.pageWidth,
        template.pageHeight,
        true, // Export at full resolution
        options
    );

    // Convert to blob
    return new Promise((resolve) => {
        if (format === 'png') {
            fabricCanvas.lowerCanvasEl.toBlob(resolve, 'image/png');
        } else {
            const svgData = fabricCanvas.toSVG();
            const blob = new Blob([svgData], { type: 'image/svg+xml' });
            resolve(blob);
        }
    });
}

// Helper: Generate filename
function generateFilename(device, format, dpi) {
    const serial = (device.serial || device.name || 'device').toLowerCase().replace(/[^a-z0-9]/g, '-');
    const ext = format.includes('png') ? 'png' : 'svg';
    const dpiStr = format.includes('png') ? `-${dpi}dpi` : '';
    return `sticker-${serial}${dpiStr}.${ext}`;
}

// Helper: Update progress
function updateProgress(current, total, deviceName) {
    const percent = Math.round((current / total) * 100);
    document.getElementById('progressBar').style.width = `${percent}%`;
    document.getElementById('progressText').textContent =
        `Processing device ${current} of ${total}: ${deviceName}`;
}

// Helper: Show completion summary
function showCompletionSummary(successCount, failed) {
    let message = `✓ Successfully exported ${successCount} device(s)`;
    if (failed.length > 0) {
        message += `\n⚠ Failed to export ${failed.length} device(s):\n`;
        failed.forEach(f => {
            message += `  • ${f.device}: ${f.error}\n`;
        });
    }
    alert(message); // TODO: Use nicer notification
}
```

---

### Step 5: Progress Indicator & Error Handling (1 hour)

**Features:**
- Real-time progress bar (0% → 100%)
- Current device name display
- Cancel button (stops export, downloads partial ZIP)
- Error collection (failed devices logged, doesn't stop export)
- Completion summary (success/failure counts)

**CSS (designer.css):**
```css
.progress-section {
    padding: 20px;
    background: #f5f5f5;
    border-radius: 8px;
    margin-top: 20px;
}

.progress-bar {
    width: 100%;
    height: 30px;
    background: #e0e0e0;
    border-radius: 15px;
    overflow: hidden;
    margin: 15px 0;
}

.progress-fill {
    height: 100%;
    background: linear-gradient(90deg, #4CAF50, #45a049);
    transition: width 0.3s ease;
}

#progressText {
    text-align: center;
    color: #555;
    font-size: 14px;
    margin: 10px 0;
}

#cancelExportBtn {
    display: block;
    margin: 20px auto 0;
}
```

---

### Step 6: Testing & Refinement (30-60 minutes)

**Manual Test Cases:**

1. **Selection:**
   - [ ] Click individual checkboxes
   - [ ] Select All works
   - [ ] Deselect All works
   - [ ] Bulk button shows correct count
   - [ ] Button disabled when no selection

2. **Modal:**
   - [ ] Modal opens with selected devices
   - [ ] Device list shows all selected devices
   - [ ] Template matches displayed correctly
   - [ ] Export options all functional

3. **Export:**
   - [ ] Export 2 devices → ZIP downloads
   - [ ] Export 5 devices → All files in ZIP
   - [ ] Export 10 devices → Performance acceptable
   - [ ] Export 50 devices → Completes in < 30 seconds
   - [ ] PNG files correct DPI
   - [ ] SVG files valid and openable
   - [ ] Filenames correct format

4. **Progress:**
   - [ ] Progress bar updates smoothly
   - [ ] Device name updates each iteration
   - [ ] Percentage accurate

5. **Error Handling:**
   - [ ] Failed device doesn't stop export
   - [ ] Completion summary shows failures
   - [ ] Cancel button stops export
   - [ ] Partial ZIP downloads if cancelled

6. **Edge Cases:**
   - [ ] Export 1 device (should work, odd but valid)
   - [ ] Export with mixed templates
   - [ ] Export with missing device data
   - [ ] Network error during export

---

## File Structure

### New Files (2)
```
wwwroot/js/multi-device-export.js          (+400 lines)
Phase5.3-MultiDeviceExport-Plan.md         (this document)
```

### Modified Files (2)
```
Pages/Meraki/Network.cshtml                (+50 lines - checkboxes, bulk button)
wwwroot/css/designer.css                   (+100 lines - progress styles)
```

### External Dependencies (1)
```
JSZip library: https://cdn.jsdelivr.net/npm/jszip@3.10.1/dist/jszip.min.js
Add to Network.cshtml scripts section
```

**Total New Code:** ~550 lines
**Total Modified:** ~150 lines
**Grand Total:** ~700 lines

---

## API Changes

**Option A: No Server Changes (Recommended for MVP)**
- Use existing `/api/export/device/{id}` endpoint
- Call in loop for each device (client-side batching)
- **Pros:** No backend work, works immediately
- **Cons:** Multiple HTTP requests (acceptable for < 50 devices)

**Option B: Batch Endpoint (Future Optimization)**
- New endpoint: `POST /api/export/devices/bulk`
- Request body: `{ deviceIds: [1,2,3,...], connectionId: 456 }`
- Response: Array of device data + templates
- **Pros:** Single HTTP request, faster
- **Cons:** Requires backend work

**Recommendation:** Start with Option A, add Option B if performance issues

---

## Dependencies

### JSZip Library (Required)

**CDN:**
```html
<script src="https://cdn.jsdelivr.net/npm/jszip@3.10.1/dist/jszip.min.js"></script>
```

**Usage:**
```javascript
const zip = new JSZip();
zip.file('filename.png', blob);
const zipBlob = await zip.generateAsync({ type: 'blob' });
```

**License:** MIT (free for commercial use)
**Size:** ~100KB minified
**Browser Support:** All modern browsers

---

## Performance Considerations

### Expected Performance

**Per Device:**
- Fetch data: ~50ms (cached after first request)
- Render canvas: ~100ms
- Export to blob: ~200ms
- Add to ZIP: ~10ms
- **Total: ~360ms per device**

**Bulk Export Times:**
- 5 devices: ~2 seconds
- 10 devices: ~4 seconds
- 25 devices: ~9 seconds
- 50 devices: ~18 seconds
- 100 devices: ~36 seconds

### Optimizations

1. **Canvas Pooling** - Reuse canvas instance instead of creating new ones
2. **Memory Management** - Dispose Fabric.js canvases after each export
3. **Parallel Processing** - Export 3-5 devices concurrently (advanced)
4. **Batch API** - Single request for all device data (future)
5. **Worker Threads** - Offload canvas rendering to Web Workers (advanced)

### Memory Management

```javascript
// Dispose canvas after each export
function renderDeviceToBlob(template, deviceDataMap, format, options) {
    const tempCanvas = document.createElement('canvas');
    const fabricCanvas = createAndRenderPreviewCanvas(...);

    return new Promise((resolve) => {
        fabricCanvas.lowerCanvasEl.toBlob((blob) => {
            fabricCanvas.dispose(); // ← CRITICAL: Free memory
            resolve(blob);
        }, 'image/png');
    });
}
```

---

## UI/UX Design

### Device Table with Checkboxes

```
┌─────────────────────────────────────────────────────────────────┐
│ ☐  Device Name          Serial         Model         [📥 Export]│
├─────────────────────────────────────────────────────────────────┤
│ ☑  Switch-Main-A       MS-1234...     MS225-48FP    [📥 Export]│
│ ☑  Switch-Main-B       MS-5678...     MS225-48FP    [📥 Export]│
│ ☐  AP-Office-01        MR-ABCD...     MR32          [📥 Export]│
│ ☑  AP-Office-02        MR-EFGH...     MR32          [📥 Export]│
├─────────────────────────────────────────────────────────────────┤
│                           [📦 Export Selected (3)]               │
└─────────────────────────────────────────────────────────────────┘
```

### Bulk Export Modal

```
┌──────────────────────────────────────────────────────┐
│ Bulk Export: 3 Devices                          [X] │
├──────────────────────────────────────────────────────┤
│                                                      │
│ Selected Devices:                                   │
│ ┌────────────────────────────────────────────────┐ │
│ │ ✓ Switch-Main-A (MS225-48FP)                   │ │
│ │   Template: Device Sticker - Switch            │ │
│ │   Match: Model (100%)                          │ │
│ ├────────────────────────────────────────────────┤ │
│ │ ✓ Switch-Main-B (MS225-48FP)                   │ │
│ │   Template: Device Sticker - Switch            │ │
│ │   Match: Model (100%)                          │ │
│ ├────────────────────────────────────────────────┤ │
│ │ ✓ AP-Office-02 (MR32)                          │ │
│ │   Template: Generic Device Sticker             │ │
│ │   Match: Type (80%)                            │ │
│ └────────────────────────────────────────────────┘ │
│                                                      │
│ Export Settings:                                    │
│ ○ Individual PNG Files (ZIP)                        │
│ ○ Individual SVG Files (ZIP)                        │
│ ○ Grid Layout (Single PNG) - Coming soon           │
│                                                      │
│ DPI: [300 DPI (Print) ▼]                           │
│ Background: ○ White  ○ Transparent                  │
│                                                      │
│              [Cancel]  [Start Export]               │
└──────────────────────────────────────────────────────┘
```

### Progress Indicator

```
┌──────────────────────────────────────────────────────┐
│ Exporting...                                    [X] │
├──────────────────────────────────────────────────────┤
│                                                      │
│ [███████████████████░░░░░░░░░] 75%                  │
│                                                      │
│ Processing device 3 of 4: AP-Office-02              │
│                                                      │
│                   [Cancel]                          │
│                                                      │
└──────────────────────────────────────────────────────┘
```

### Completion Summary

```
┌──────────────────────────────────────────────────────┐
│ Export Complete                                 [X] │
├──────────────────────────────────────────────────────┤
│                                                      │
│ ✓ Successfully exported 3 device(s)                 │
│                                                      │
│ Downloaded: device-stickers-3-devices.zip           │
│                                                      │
│                   [Close]                           │
│                                                      │
└──────────────────────────────────────────────────────┘
```

---

## Stretch Goals (Optional Enhancements)

### 1. Grid Layout Export (Phase 5.4)

**Feature:** Tile multiple stickers on single canvas for printing

**Algorithm:**
```javascript
function calculateGridLayout(stickerWidth, stickerHeight, deviceCount) {
    // A4 dimensions at 300 DPI: 2480 × 3508 pixels
    const pageWidth = 2480;
    const pageHeight = 3508;
    const margin = 50;

    const cols = Math.floor((pageWidth - margin * 2) / stickerWidth);
    const rows = Math.floor((pageHeight - margin * 2) / stickerHeight);
    const stickersPerPage = cols * rows;

    const pages = Math.ceil(deviceCount / stickersPerPage);

    return { cols, rows, pages, stickersPerPage };
}

function positionStickersInGrid(devices, layout) {
    const positions = [];
    let page = 0;
    let row = 0;
    let col = 0;

    devices.forEach((device, i) => {
        positions.push({
            device,
            page,
            x: margin + col * stickerWidth,
            y: margin + row * stickerHeight
        });

        col++;
        if (col >= layout.cols) {
            col = 0;
            row++;
            if (row >= layout.rows) {
                row = 0;
                page++;
            }
        }
    });

    return positions;
}
```

### 2. Template Override

**Feature:** User can change template for specific devices before export

**UI:** Dropdown next to each device in bulk modal
**Implementation:** Store per-device template overrides in state

### 3. Export Presets

**Feature:** Save export settings as presets (e.g., "Print Quality", "Web Optimized")

**Storage:** localStorage or database
**UI:** Dropdown to select preset in export modal

### 4. Background Export (Web Worker)

**Feature:** Offload canvas rendering to Web Worker for better performance

**Benefit:** UI stays responsive during long exports
**Complexity:** High (Fabric.js + OffscreenCanvas compatibility)

---

## Testing Plan

### Unit Tests (Optional)

- `calculateGridLayout()` with various device counts
- `generateFilename()` with edge cases
- `replacePlaceholdersInTemplate()` with bulk data

### Integration Tests

- Full export flow with 5 devices
- Progress indicator updates
- ZIP file generation and download
- Error handling with failed devices

### Manual Tests (Critical)

**Selection:**
1. Select 3 devices individually → Bulk button shows "Export Selected (3)"
2. Select All → All checkboxes checked
3. Deselect All → All checkboxes unchecked
4. Select 0 devices → Bulk button disabled

**Export:**
1. Export 2 devices as PNG ZIP → Downloads correct file
2. Open ZIP → Contains 2 PNG files with correct names
3. Export 5 devices as SVG ZIP → All SVG files valid
4. Export 10 devices at 300 DPI → Files are high resolution
5. Export with transparent background → PNG transparency works

**Progress:**
1. Progress bar animates from 0% to 100%
2. Device name updates for each device
3. Cancel button stops export mid-process
4. Completion summary shows correct counts

**Error Handling:**
1. Disconnect network mid-export → Failed devices logged, others succeed
2. Invalid device data → Skipped, doesn't crash
3. Cancel after 3 of 10 → Partial ZIP downloads

**Performance:**
1. Export 50 devices → Completes in < 30 seconds
2. Browser doesn't freeze during export
3. Memory usage acceptable (< 500MB)

---

## Known Limitations

1. **No Grid Layout** - Phase 5.3 only does individual files (grid is Phase 5.4)
2. **No PDF Export** - Requires server-side QuestPDF (Phase 5.5)
3. **Sequential Processing** - Exports one device at a time (parallel in future)
4. **Client-Side Only** - All processing in browser (no server batching yet)
5. **Memory Constraints** - Very large exports (100+ devices) may cause issues

---

## Risks & Mitigation

| Risk | Probability | Impact | Mitigation |
|------|-------------|--------|-----------|
| JSZip fails with large files | Low | High | Test with 50+ devices, add file size checks |
| Browser memory exhaustion | Medium | High | Dispose canvases after each export, limit to 50 devices |
| Slow export speed | Low | Medium | Optimize canvas rendering, add parallel processing |
| User cancels mid-export | High | Low | Handle gracefully, download partial ZIP |
| Mixed template sizes | Low | Medium | Document limitation, future: validate same template |

---

## Deployment Notes

**No Database Changes:** Phase 5.3 is 100% client-side.

**New Dependencies:**
- JSZip library (CDN script tag)

**File Deployment:**
- Deploy `multi-device-export.js` (new)
- Deploy updated `Network.cshtml`
- Deploy updated `designer.css`
- Clear browser cache recommended

**Rollback Plan:**
- Remove checkboxes and bulk button from `Network.cshtml`
- Remove `multi-device-export.js` script reference
- No data migration needed

---

## Timeline & Effort

| Task | Duration | Notes |
|------|----------|-------|
| Checkbox UI | 30 min | HTML + event handlers |
| Bulk export button | 30 min | UI + selection state |
| Multi-device modal | 1 hour | HTML structure + device list rendering |
| Bulk export logic | 1.5 hours | Loop, ZIP generation, download |
| Progress indicator | 1 hour | UI + real-time updates |
| Testing & refinement | 30-60 min | Manual testing, bug fixes |
| **Total** | **4.5-5.5 hours** | **~1 day of focused work** |

**Realistic estimate with breaks:** 1 full day or 2 half-days

---

## Next Steps After Phase 5.3

### Phase 5.4: Grid Layout Export
- Tile multiple stickers on single canvas
- Print-ready sheet generation
- Multi-page support

### Phase 5.5: Server-Side PDF Export
- QuestPDF integration
- Professional print quality
- Real QR code generation

### Phase 6: Company Logo Upload
- Logo upload UI
- Logo storage
- Logo in templates

---

## Success Metrics

After Phase 5.3 completion:
- ✅ Users can export 10 devices in < 5 seconds
- ✅ ZIP file downloads correctly with all files
- ✅ Zero crashes with 50 device export
- ✅ Progress indicator provides clear feedback
- ✅ Error handling prevents data loss
- ✅ User feedback: "This saves so much time!"

---

**Document Version:** 1.0 (Initial Planning)
**Author:** Claude
**Status:** PLANNING - Ready for Development
**Last Updated:** 2025-10-22
**Depends On:** Phase 5 MVP (complete ✅)
**Estimated Start:** After Phase 5 MVP testing complete
