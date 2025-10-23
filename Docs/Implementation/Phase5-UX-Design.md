# Phase 5: Device Data Export - UX/UI Design

**Date:** 2025-10-22
**Version:** 1.0
**Status:** Design Ready

---

## Table of Contents

1. [Design Principles](#design-principles)
2. [Devices Page Changes](#devices-page-changes)
3. [Export Modals](#export-modals)
4. [Multi-Device Interface](#multi-device-interface)
5. [Error States](#error-states)
6. [Success States](#success-states)
7. [Interaction Flows](#interaction-flows)

---

## Design Principles

1. **Consistency** - Match existing Designer UI patterns
2. **Clarity** - Make it obvious what will be exported
3. **Efficiency** - Minimal clicks for common operations
4. **Safety** - Confirm before exporting multiple devices
5. **Feedback** - Clear progress and status indication
6. **Accessibility** - Keyboard navigation, screen reader support

---

## Devices Page Changes

### Current State (Phase 4)

```
Devices Index
┌─────────────────────────────────────────────────────────────┐
│ My Devices                                                  │
├─────────────────────────────────────────────────────────────┤
│ Device Name        Model           Serial       Status      │
├─────────────────────────────────────────────────────────────┤
│ Switch-Main-A      MS225-48FP      MS-1234...   Online  ✓  │
│ Switch-Main-B      MS225-48FP      MS-5678...   Online  ✓  │
│ AP-Office-01       MR32            MR-ABCD...   Online  ✓  │
│ AP-Office-02       MR32            MR-EFGH...   Offline ⚠  │
│ Gateway-Primary    MX64W           MX-1111...   Online  ✓  │
└─────────────────────────────────────────────────────────────┘
```

### New State (Phase 5)

```
Devices Index with Bulk Export
┌──────────────────────────────────────────────────────────────┐
│ My Devices                                                   │
│ [Bulk Export] (disabled) | [Clear Selection]                 │
├──────────────────────────────────────────────────────────────┤
│ ☐ Device Name        Model           Serial       Status    │
├──────────────────────────────────────────────────────────────┤
│ ☐ Switch-Main-A      MS225-48FP      MS-1234...   Online ✓ │
│ ☐ Switch-Main-B      MS225-48FP      MS-5678...   Online ✓ │
│ ☐ AP-Office-01       MR32            MR-ABCD...   Online ✓ │
│ ☐ AP-Office-02       MR32            MR-EFGH...   Offline⚠ │
│ ☐ Gateway-Primary    MX64W           MX-1111...   Online ✓ │
├──────────────────────────────────────────────────────────────┤
│                              [Bulk Export] (0 selected)     │
└──────────────────────────────────────────────────────────────┘
```

### With Action Column

```
┌──────────────────────────────────────────────────────────────┐
│ My Devices                                                   │
├──────────────────────────────────────────────────────────────┤
│ Device Name        Model           Serial       Status  Action
├──────────────────────────────────────────────────────────────┤
│ Switch-Main-A      MS225-48FP      MS-1234...   Online  [···] │
│ Switch-Main-B      MS225-48FP      MS-5678...   Online  [···] │
│ AP-Office-01       MR32            MR-ABCD...   Online  [···] │
│ AP-Office-02       MR32            MR-EFGH...   Offline [···] │
│ Gateway-Primary    MX64W           MX-1111...   Online  [···] │
└──────────────────────────────────────────────────────────────┘

Action Menu (Click [···]):
┌─────────────────────┐
│ 📥 Export as PNG    │
│ 📥 Export as SVG    │
│ 📥 Export as PDF    │
│ ──────────────────  │
│ 📋 View Details     │
│ 🔗 Sync Device      │
└─────────────────────┘
```

---

## Export Modals

### Single Device Export Modal

```
Modal: Export Device
┌────────────────────────────────────────────────────────────┐
│ Export: Switch-Main-Office                            [X]  │
├────────────────────────────────────────────────────────────┤
│                                                            │
│ Device Information:                                        │
│ ┌──────────────────────────────────────────────────────┐  │
│ │ Name: Switch-Main-Office                             │  │
│ │ Serial: MS-1234-ABCD-5678                            │  │
│ │ Model: MS225-48FP                                    │  │
│ │ IP Address: 192.168.1.10                             │  │
│ │ Network: Production (6 other devices)                │  │
│ │ Status: Online                                       │  │
│ └──────────────────────────────────────────────────────┘  │
│                                                            │
│ Template Selection:                                        │
│ ┌──────────────────────────────────────────────────────┐  │
│ │ Matched Template: "Device Sticker - Switch"    [v]   │  │
│ │ (Matched by model: MS225-48FP)                       │  │
│ │                                                       │  │
│ │ Alternative Templates:                               │  │
│ │ • "Generic Device Sticker"                           │  │
│ │ • "Network Switch Label"                             │  │
│ └──────────────────────────────────────────────────────┘  │
│                                                            │
│ Export Settings:                                           │
│ ┌──────────────────────────────────────────────────────┐  │
│ │ Format:                                               │  │
│ │ ○ PNG (96 DPI)  ○ PNG (150 DPI)  ○ PNG (300 DPI)   │  │
│ │ ○ SVG           ○ PDF                                │  │
│ │                                                       │  │
│ │ Background:                                          │  │
│ │ ○ White         ○ Transparent                        │  │
│ └──────────────────────────────────────────────────────┘  │
│                                                            │
│ Live Preview:                                              │
│ ┌──────────────────────────────────────────────────────┐  │
│ │                                                       │  │
│ │    [Sticker preview with actual device data]        │  │
│ │    Name: Switch-Main-Office                         │  │
│ │    Serial: MS-1234-ABCD-5678                        │  │
│ │    [QR Code contains device serial]                 │  │
│ │                                                       │  │
│ └──────────────────────────────────────────────────────┘  │
│                                                            │
│                        [Cancel]  [Export]                 │
└────────────────────────────────────────────────────────────┘
```

### Multi-Device Export Modal

```
Modal: Batch Export Devices
┌────────────────────────────────────────────────────────────┐
│ Batch Export: 5 Devices Selected                      [X]  │
├────────────────────────────────────────────────────────────┤
│                                                            │
│ Selected Devices:                                          │
│ ┌──────────────────────────────────────────────────────┐  │
│ │ ☑ Switch-Main-A (MS225-48FP)    Matched Template ✓  │  │
│ │ ☑ Switch-Main-B (MS225-48FP)    Matched Template ✓  │  │
│ │ ☑ AP-Office-01 (MR32)           Matched Template ✓  │  │
│ │ ☑ AP-Office-02 (MR32)           Matched Template ✓  │  │
│ │ ☑ Gateway-Primary (MX64W)       Fallback Used ⚠     │  │
│ └──────────────────────────────────────────────────────┘  │
│                                                            │
│ Layout Options:                                            │
│ ┌──────────────────────────────────────────────────────┐  │
│ │ ○ Grid Layout (auto-fit)    → 2×3 grid on 1 page    │  │
│ │ ○ Rows (one per row)        → 6 pages needed        │  │
│ │ ○ Columns (one per column)  → 3 pages needed        │  │
│ └──────────────────────────────────────────────────────┘  │
│                                                            │
│ Export Settings:                                           │
│ ┌──────────────────────────────────────────────────────┐  │
│ │ Format: ○ PNG (300 DPI) ○ SVG ○ PDF                 │  │
│ │ Background: ○ White ○ Transparent                    │  │
│ │ Page Size: ○ Letter ○ A4 ○ Legal                     │  │
│ └──────────────────────────────────────────────────────┘  │
│                                                            │
│ Output:                                                    │
│ ○ Single PDF (all devices)                                │
│ ○ ZIP file (one file per device)                          │
│                                                            │
│                        [Cancel]  [Export]                 │
└────────────────────────────────────────────────────────────┘
```

### Export Progress Modal

```
Modal: Exporting... (During Export)
┌────────────────────────────────────────────────────────────┐
│ Exporting 5 Devices                    ✓                   │
├────────────────────────────────────────────────────────────┤
│                                                            │
│ Progress: 3 / 5 devices                                    │
│ ████████████░░░░░░░░░░░░░░░░░░  60%                       │
│                                                            │
│ Currently exporting: Switch-Main-B                         │
│ Status: Rendering preview...                              │
│                                                            │
│                                                            │
│ Processing Details:                                        │
│ ✓ Switch-Main-A     - Exported (156 KB)                   │
│ ✓ Switch-Main-B     - Processing...                       │
│ ⧖ AP-Office-01      - Queued                              │
│ ⧖ AP-Office-02      - Queued                              │
│ ⧖ Gateway-Primary   - Queued                              │
│                                                            │
│                                    [Cancel Export]         │
└────────────────────────────────────────────────────────────┘
```

---

## Multi-Device Interface

### Device Selection View

```
Devices Page with Multi-Select
┌────────────────────────────────────────────────────────────┐
│ My Devices [📥 Bulk Export] (2/5 selected)                 │
├────────────────────────────────────────────────────────────┤
│ ☐ All                                                      │
├────────────────────────────────────────────────────────────┤
│ ☑ Switch-Main-A      MS225-48FP      MS-1234...   Online ✓│
│ ☑ Switch-Main-B      MS225-48FP      MS-5678...   Online ✓│
│ ☐ AP-Office-01       MR32            MR-ABCD...   Online ✓│
│ ☐ AP-Office-02       MR32            MR-EFGH...   Offline⚠│
│ ☐ Gateway-Primary    MX64W           MX-1111...   Online ✓│
├────────────────────────────────────────────────────────────┤
│ 📥 [Bulk Export Selected] (enabled)                        │
└────────────────────────────────────────────────────────────┘
```

### Layout Preview in Modal

```
Layout Options Preview:

Grid Layout (2×3):
┌─────────────────────────────────────┐
│ ┌──────────┐ ┌──────────┐          │
│ │ Device 1 │ │ Device 2 │          │
│ └──────────┘ └──────────┘          │
│ ┌──────────┐ ┌──────────┐          │
│ │ Device 3 │ │ Device 4 │          │
│ └──────────┘ └──────────┘          │
│ ┌──────────┐ ┌──────────┐          │
│ │ Device 5 │ │[Empty]   │          │
│ └──────────┘ └──────────┘          │
└─────────────────────────────────────┘
Result: 1 page

Row Layout:
┌─────────────────────────────────────┐
│ ┌──────────────────────────────────┐│
│ │ Device 1                         ││
│ └──────────────────────────────────┘│
│ ┌──────────────────────────────────┐│
│ │ Device 2                         ││
│ └──────────────────────────────────┘│
│ ... (more pages)                    │
└─────────────────────────────────────┘
Result: 5 pages

Column Layout:
┌─────────────────────────────────────┐
│ ┌──────┐ ┌──────┐ ┌──────┐         │
│ │ Dev1 │ │ Dev2 │ │ Dev3 │         │
│ ├──────┤ ├──────┤ ├──────┤         │
│ │ Dev4 │ │ Dev5 │ │      │         │
│ └──────┘ └──────┘ └──────┘         │
└─────────────────────────────────────┘
Result: 1 page
```

---

## Error States

### No Template Match

```
Modal: Export Failed
┌────────────────────────────────────────────────────────────┐
│ Export Failed                                         [X]  │
├────────────────────────────────────────────────────────────┤
│                                                            │
│ ⚠ No suitable template found for this device               │
│                                                            │
│ Device: Switch-Advanced-X (Model: Custom-Model-123)       │
│                                                            │
│ Possible solutions:                                        │
│ 1. Create a new template for this device model             │
│ 2. Set a default template preference in Settings           │
│ 3. Manually select a template from available options:      │
│                                                            │
│    ○ Generic Device Sticker                               │
│    ○ Network Equipment Label                              │
│    ○ Cisco Meraki Device Label                            │
│                                                            │
│ [Learn More]  [Cancel]  [Try Again with Selected]         │
└────────────────────────────────────────────────────────────┘
```

### Partial Export Failure

```
Modal: Export Completed with Errors
┌────────────────────────────────────────────────────────────┐
│ Export Completed (with 1 error)                       [X]  │
├────────────────────────────────────────────────────────────┤
│                                                            │
│ ✓ 4 / 5 devices exported successfully                     │
│ ⚠ 1 device failed                                         │
│                                                            │
│ Successful:                                                │
│ ✓ Switch-Main-A - 156 KB PNG                              │
│ ✓ Switch-Main-B - 164 KB PNG                              │
│ ✓ AP-Office-01 - 142 KB PNG                               │
│ ✓ Gateway-Primary - 178 KB PNG                            │
│                                                            │
│ Failed:                                                    │
│ ✗ AP-Office-02 - No template available (offline device?)  │
│                                                            │
│ [Download Successful Exports as ZIP]  [Try Failed Again]  │
│                                       [Close]              │
└────────────────────────────────────────────────────────────┘
```

### Permission Denied

```
Modal: Export Error
┌────────────────────────────────────────────────────────────┐
│ Export Error                                          [X]  │
├────────────────────────────────────────────────────────────┤
│                                                            │
│ 🔒 Permission Denied                                      │
│                                                            │
│ You don't have permission to export this device:           │
│ Switch-Production-A                                        │
│                                                            │
│ This device may have been deleted or you may have lost     │
│ access to its parent connection.                           │
│                                                            │
│ Please refresh the device list and try again.              │
│                                                            │
│                                   [Refresh]  [Close]       │
└────────────────────────────────────────────────────────────┘
```

---

## Success States

### Export Complete

```
Modal: Export Successful
┌────────────────────────────────────────────────────────────┐
│ Export Successful ✓                                  [X]  │
├────────────────────────────────────────────────────────────┤
│                                                            │
│ ✓ Your sticker has been exported successfully              │
│                                                            │
│ Device: Switch-Main-Office                                │
│ File: sticker-ms-1234-abcd-5678.png (156 KB)              │
│ Format: PNG (300 DPI)                                      │
│ Generated: 2025-10-22 14:35:22                             │
│                                                            │
│ Your file should download automatically. If not:           │
│ [Manually Download Sticker]                               │
│                                                            │
│ Would you like to:                                         │
│ [Export Another Device]  [View All Exports]  [Close]      │
└────────────────────────────────────────────────────────────┘
```

### Batch Export Complete

```
Modal: Batch Export Successful
┌────────────────────────────────────────────────────────────┐
│ Batch Export Successful ✓                            [X]  │
├────────────────────────────────────────────────────────────┤
│                                                            │
│ ✓ 5 devices exported successfully                         │
│                                                            │
│ Files:                                                     │
│ ✓ Switch-Main-A.png     (156 KB)                          │
│ ✓ Switch-Main-B.png     (164 KB)                          │
│ ✓ AP-Office-01.png      (142 KB)                          │
│ ✓ AP-Office-02.png      (158 KB)                          │
│ ✓ Gateway-Primary.png   (178 KB)                          │
│                                                            │
│ Total: 798 KB (5 files)                                   │
│ Format: batch-export-2025-10-22-143522.zip                │
│                                                            │
│ Your file should download automatically.                   │
│                                                            │
│ [Re-download ZIP]  [View Export History]  [Close]         │
└────────────────────────────────────────────────────────────┘
```

---

## Interaction Flows

### Single Device Export Flow

```
START
  │
  ├─ User is on Devices page
  │
  ├─ User clicks [Export] action on a device
  │    │
  │    ├─ System fetches device data
  │    ├─ System finds matching template
  │    └─ Export modal opens
  │
  ├─ Modal shows:
  │    ├─ Device information
  │    ├─ Matched template (with confidence)
  │    ├─ Export settings (format, DPI, background)
  │    └─ Live preview
  │
  ├─ User reviews and customizes:
  │    ├─ Can change template via dropdown
  │    ├─ Can change export format
  │    └─ Preview updates in real-time
  │
  ├─ User clicks [Export]
  │    │
  │    ├─ Canvas is rendered at target resolution
  │    ├─ File is generated (PNG/SVG/PDF)
  │    ├─ Export logged to history
  │    └─ Download starts
  │
  ├─ Success modal shows:
  │    ├─ File size and name
  │    ├─ Export date/time
  │    └─ Options to export another device
  │
  └─ END
```

### Multi-Device Export Flow

```
START
  │
  ├─ User is on Devices page
  │
  ├─ User selects multiple devices:
  │    ├─ Clicks checkboxes for 3+ devices
  │    └─ [Bulk Export] button becomes enabled
  │
  ├─ User clicks [Bulk Export]
  │    │
  │    ├─ System fetches data for all selected devices
  │    ├─ System finds templates for each device
  │    └─ Batch export modal opens
  │
  ├─ Modal shows:
  │    ├─ List of selected devices with template matches
  │    ├─ Layout options (Grid, Rows, Columns)
  │    │   └─ Preview shows how many pages needed
  │    ├─ Export settings
  │    └─ Output format (single PDF or ZIP)
  │
  ├─ User customizes and clicks [Export]
  │    │
  │    ├─ Progress modal opens
  │    ├─ System processes each device sequentially:
  │    │    ├─ Merges device data with template
  │    │    ├─ Renders canvas
  │    │    ├─ Generates file
  │    │    └─ Updates progress (X/Y)
  │    │
  │    ├─ All exports complete
  │    ├─ Files combined into ZIP or single PDF
  │    └─ Download starts
  │
  ├─ Success modal shows:
  │    ├─ All devices exported successfully
  │    ├─ File list with sizes
  │    └─ Total file size
  │
  └─ END
```

### Template Matching Flow

```
Device arrives for export
  │
  ├─ System checks: Does template exist for this device model?
  │    │
  │    └─ YES → Use matched template (confidence: 1.0)
  │    └─ NO → Continue to next check
  │
  ├─ System checks: Does template exist for this device type?
  │    │
  │    └─ YES → Use matched template (confidence: 0.8)
  │    └─ NO → Continue to next check
  │
  ├─ System checks: Does user have a default template?
  │    │
  │    └─ YES → Use default template (confidence: 0.5)
  │    └─ NO → Continue to next check
  │
  ├─ System checks: Does a system default template exist?
  │    │
  │    └─ YES → Use system default (confidence: 0.3)
  │    └─ NO → Continue to next check
  │
  ├─ System uses: Any available template
  │    │
  │    └─ YES → Use first available (confidence: 0.1)
  │    └─ NO → ERROR - No template available
  │
  └─ Return matched template with confidence
```

---

## Accessibility Considerations

### Keyboard Navigation

- ✅ Tab through device list
- ✅ Space to select/deselect device
- ✅ Enter to open export modal
- ✅ Tab through modal controls
- ✅ Enter to export
- ✅ Escape to close modal

### Screen Reader Support

- ✅ "Export button, Single device: Switch-Main-A"
- ✅ "Selected devices: 2 of 5"
- ✅ "Matched template: Device Sticker - Switch (confidence: high)"
- ✅ "Export format: PNG, 300 DPI, White background"
- ✅ "Progress: 3 of 5 devices exported"

### Color & Contrast

- ✅ Status indicators (Online/Offline) have text labels, not just color
- ✅ Success/error messages use icons + text
- ✅ Progress bars have percentage text
- ✅ All text meets WCAG AA contrast requirements

---

## Mobile Considerations

### Responsive Behavior

**Mobile Devices (< 768px):**

```
Device List:
┌─────────────────────┐
│ My Devices      [+] │
├─────────────────────┤
│ Switch-Main-A   [···]
│ (MS225-48FP)       │
│ Serial: MS-1234... │
│                     │
│ Switch-Main-B   [···]
│ (MS225-48FP)       │
│ Serial: MS-5678... │
└─────────────────────┘

Action Menu (dropdown):
┌─────────────────────┐
│ 📥 Export as PNG   │
│ 📥 Export as SVG   │
│ 📥 Export as PDF   │
└─────────────────────┘
```

**Tablet (768px-1024px):**
- Table view with condensed columns
- Export action dropdown in row

**Desktop (>1024px):**
- Full table with all details visible
- Inline action buttons

---

**Document Version:** 1.0
**Author:** Claude
**Last Updated:** 2025-10-22
