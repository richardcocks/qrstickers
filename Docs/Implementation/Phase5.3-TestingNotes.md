# Phase 5.3: Multi-Device Export - Testing Notes

**Date:** 2025-10-22
**Status:** Ready for Testing
**Estimated Testing Time:** 15-20 minutes

---

## Prerequisites

Before testing, ensure:
- ✅ Phase 5 MVP is working (single device export functional)
- ✅ At least 10 devices available in a network
- ✅ At least one template exists (user or system default)
- ✅ Browser console open (F12) to monitor for errors

---

## Quick Start Test (3 minutes)

**Goal:** Verify basic functionality works

1. Navigate to Network page with devices
2. Check 2-3 device checkboxes
3. Verify bulk button shows correct count: "📦 Export Selected (3)"
4. Click bulk button → Modal opens
5. Verify device list shows all selected devices
6. Keep default settings (PNG, 300 DPI, White)
7. Click "Start Export"
8. Watch progress bar animate
9. ZIP file downloads
10. Extract and verify 3 PNG files inside

**Expected Result:** ZIP file downloads with correct number of stickers

---

## Core Functionality Tests

### Test 1: Checkbox Selection ✅
- [ ] Click individual checkboxes → Count updates
- [ ] Click "Select All" checkbox → All devices selected
- [ ] Uncheck "Select All" → All devices deselected
- [ ] Bulk button disabled when no selection
- [ ] Bulk button enabled when 1+ selected
- [ ] Selection summary shows correct count

### Test 2: Modal Display ✅
- [ ] Modal opens when clicking bulk button
- [ ] Title shows "Bulk Export: N Devices"
- [ ] Device list shows all selected devices
- [ ] Each device shows: name, serial, template, match badge
- [ ] Match badges color-coded (green=model, blue=type, etc.)
- [ ] Export options visible (format, DPI, background)
- [ ] "Cancel" and "Start Export" buttons visible

### Test 3: PNG Export (Zip) ✅
- [ ] Select 5 devices
- [ ] Choose "Individual PNG Files (ZIP)"
- [ ] Choose 300 DPI
- [ ] Choose White background
- [ ] Click "Start Export"
- [ ] Progress bar animates 0% → 100%
- [ ] Device names update in progress text
- [ ] ZIP file downloads
- [ ] Extract ZIP → Contains 5 PNG files
- [ ] Filenames format: `sticker-{serial}-300dpi.png`
- [ ] Open PNG files → Correct DPI and content

### Test 4: SVG Export (Zip) ✅
- [ ] Select 3 devices
- [ ] Choose "Individual SVG Files (ZIP)"
- [ ] Click "Start Export"
- [ ] ZIP file downloads
- [ ] Extract ZIP → Contains 3 SVG files
- [ ] Filenames format: `sticker-{serial}.svg`
- [ ] Open SVG files → Editable vectors

### Test 5: Different DPI Options ✅
- [ ] Export 2 devices at 96 DPI
- [ ] Extract and check file sizes (smaller than 300 DPI)
- [ ] Export 2 devices at 150 DPI
- [ ] Export 2 devices at 300 DPI
- [ ] File sizes increase: 96 DPI < 150 DPI < 300 DPI

### Test 6: Transparent Background ✅
- [ ] Select 2 devices
- [ ] Choose PNG, 300 DPI, Transparent
- [ ] Export and extract ZIP
- [ ] Open PNG in image editor
- [ ] Verify transparency (checkerboard background visible)

### Test 7: Progress Indicator ✅
- [ ] Select 10 devices
- [ ] Start export
- [ ] Progress bar updates smoothly
- [ ] Percentage shown: 10%, 20%, ..., 100%
- [ ] Device name updates for each device
- [ ] Text shows "Processing device X of 10: {name}"
- [ ] No UI freezing during export

### Test 8: Large Batch (25+ Devices) ⚡
- [ ] Select 25 devices
- [ ] Start export
- [ ] Export completes in < 30 seconds
- [ ] All 25 files in ZIP
- [ ] No browser memory errors
- [ ] Console shows no errors

### Test 9: Mixed Templates ✅
- [ ] Select devices that match different templates
- [ ] Verify device list shows different template names
- [ ] Export all
- [ ] Extract ZIP
- [ ] Verify each sticker uses correct template

### Test 10: Single Device Selection ✅
- [ ] Select only 1 device
- [ ] Bulk button shows "Export Selected (1)"
- [ ] Export works correctly
- [ ] ZIP contains 1 file

---

## Edge Cases & Error Handling

### Test 11: Cancel Mid-Export ✅
- [ ] Select 10 devices
- [ ] Start export
- [ ] Click "Cancel" when progress at 50%
- [ ] Export stops
- [ ] Partial ZIP may download (or no download)
- [ ] Modal closes
- [ ] No console errors

### Test 12: Network Error Handling ⚠️
**Setup:** Disconnect network after export starts
- [ ] Select 5 devices
- [ ] Start export
- [ ] Disconnect network mid-export
- [ ] Some devices fail, others succeed
- [ ] Completion summary shows failed devices
- [ ] Failed device names and errors listed
- [ ] ZIP downloads with successful exports

### Test 13: Missing Template ⚠️
**Setup:** Use device with no matching template (edge case)
- [ ] Device should use fallback template
- [ ] Export completes successfully
- [ ] Badge shows "⚠ Fallback"

### Test 14: Very Long Device Names 📝
- [ ] Select device with 50+ character name/serial
- [ ] Filename truncated appropriately
- [ ] No filesystem errors
- [ ] ZIP extracts correctly

### Test 15: Special Characters in Serial 📝
**Setup:** Device serial contains spaces, slashes, etc.
- [ ] Filename sanitizes special chars to hyphens
- [ ] Example: "MS/1234-ABCD 5678" → "sticker-ms-1234-abcd-5678-300dpi.png"
- [ ] File downloads and opens correctly

---

## What to Verify

### ZIP File Contents
```
device-stickers-5-devices-1729600000000.zip
  ├── sticker-ms-1234-abcd-5678-300dpi.png
  ├── sticker-mr-2345-bcde-6789-300dpi.png
  ├── sticker-mx-3456-cdef-7890-300dpi.png
  ├── sticker-mv-4567-defg-8901-300dpi.png
  └── sticker-mt-5678-efgh-9012-300dpi.png
```

### File Naming Convention
- **PNG:** `sticker-{serial}-{dpi}dpi.png`
- **SVG:** `sticker-{serial}.svg`
- Serial: Lowercase, special chars → hyphens

### Sticker Content Verification
Open a few stickers and verify:
- [ ] Device serial number appears (not {{device.serial}})
- [ ] Device name appears (not {{device.name}})
- [ ] Device model appears (not {{device.model}})
- [ ] Template design renders correctly
- [ ] No placeholder text visible
- [ ] QR code placeholder present (real QR in Phase 5.5)

### Browser Console
Check for:
- ✅ `[Bulk Export] Opening bulk export modal`
- ✅ `[Bulk Export] Fetching data for N devices`
- ✅ `[Bulk Export] Exported X/N: filename.png`
- ✅ `[Bulk Export] Generating ZIP file...`
- ❌ No red error messages
- ❌ No "Uncaught" exceptions

---

## Performance Benchmarks

| Devices | Expected Time | Acceptable |
|---------|---------------|------------|
| 5       | ~2 seconds    | < 5s       |
| 10      | ~4 seconds    | < 8s       |
| 25      | ~10 seconds   | < 20s      |
| 50      | ~20 seconds   | < 35s      |

**If slower than "Acceptable":**
- Check browser DevTools → Performance tab
- Look for memory leaks
- Verify canvas disposal happening

---

## Known Issues to Watch For

### Issue: Modal Doesn't Open
**Symptom:** Click bulk button, nothing happens
**Debug:**
```javascript
// Check in console:
getSelectedDevices()  // Should return array with devices
```
**Fix:** Verify checkboxes have correct data attributes

### Issue: ZIP is Empty
**Symptom:** ZIP downloads but contains no files
**Debug:**
```javascript
// Check console for errors during export
// Look for: "Failed to export device X"
```
**Fix:** Likely template JSON is invalid or device data missing

### Issue: Progress Bar Doesn't Update
**Symptom:** Progress stays at 0%
**Debug:** Check console for JavaScript errors
**Fix:** Ensure `updateProgress()` function is being called

### Issue: Files Have Wrong DPI
**Symptom:** 300 DPI selected but files are 96 DPI
**Debug:** Open PNG in image editor, check DPI metadata
**Fix:** Verify `createAndRenderPreviewCanvas()` receives correct options

### Issue: Transparent Background Not Working
**Symptom:** PNG has white background despite "Transparent" selected
**Debug:** Check PNG alpha channel in image editor
**Fix:** Verify template has no background rectangle

### Issue: Browser Freezes
**Symptom:** UI unresponsive during export
**Debug:** Check browser task manager (Shift+Esc in Chrome)
**Fix:** Reduce batch size or add setTimeout delays

---

## Browser Compatibility

**Tested On:**
- [ ] Chrome/Edge (primary)
- [ ] Firefox
- [ ] Safari (if available)

**Requirements:**
- Canvas API ✅
- Fetch API ✅
- Blob API ✅
- JSZip library ✅
- Download attribute ✅

**Minimum Browser Versions:**
- Chrome 90+
- Firefox 88+
- Edge 90+
- Safari 14+

---

## Debug Console Commands

**Get selected devices:**
```javascript
getSelectedDevices()
```

**Check bulk export state:**
```javascript
bulkExportState
```

**Manually trigger export (bypass UI):**
```javascript
startBulkExport()
```

**Check if JSZip loaded:**
```javascript
typeof JSZip  // Should return "function"
```

**Clear selection:**
```javascript
document.querySelectorAll('.device-checkbox').forEach(cb => cb.checked = false);
onDeviceSelectionChanged();
```

---

## Success Criteria

Phase 5.3 is **ready for production** if:

- ✅ All core functionality tests pass (Tests 1-10)
- ✅ Edge cases handled gracefully (Tests 11-15)
- ✅ No console errors during normal operation
- ✅ ZIP files download correctly with all selected devices
- ✅ Performance acceptable (50 devices < 35 seconds)
- ✅ Sticker content shows real device data (no placeholders)
- ✅ Cancel functionality works
- ✅ Error handling doesn't crash export

**Minor Issues (Acceptable):**
- ⚠️ Very large exports (100+ devices) may be slow
- ⚠️ Browser memory warning with 100+ devices
- ⚠️ Filename truncation for extremely long serials

**Blocker Issues (Must Fix Before Production):**
- ❌ Export fails for any device count
- ❌ ZIP downloads but is corrupt/empty
- ❌ Browser crashes or freezes
- ❌ Placeholders not replaced with real data
- ❌ JavaScript errors in console

---

## Reporting Issues

When reporting bugs, include:
1. **Browser:** Chrome 120, Firefox 115, etc.
2. **Device Count:** How many devices selected
3. **Export Settings:** Format, DPI, background
4. **Console Errors:** Copy full error message
5. **Steps to Reproduce:** Exact sequence
6. **Screenshot:** If UI issue

**Example Bug Report:**
```
Browser: Chrome 120.0.6099.71
Devices: 10 selected
Settings: PNG, 300 DPI, Transparent
Error: "Uncaught TypeError: Cannot read property 'name' of undefined"
Steps:
1. Select 10 devices
2. Open bulk export modal
3. Click Start Export
4. Error occurs at device 5/10

Console shows: [full error stack trace]
```

---

## Next Steps After Testing

If all tests pass:
- ✅ Mark Phase 5.3 as **COMPLETE**
- ✅ Create `PHASE5.3_COMPLETION_NOTES.md` documenting any bugs found/fixed
- ✅ Update `README.md` with bulk export feature
- ✅ Update `PHASE5_MVP_IMPLEMENTATION.md` to reference Phase 5.3
- ✅ Consider Phase 5.4 (Grid Layout) or Phase 5.5 (PDF Export)

If issues found:
- 🐛 Document bugs in testing notes
- 🔧 Fix critical issues
- 🔁 Re-test after fixes

---

**Testing Completed:** __________ (Date)
**Tested By:** __________
**Status:** ☐ Pass  ☐ Fail  ☐ Pass with Minor Issues
**Notes:**
_________________________________________________________________________
_________________________________________________________________________
_________________________________________________________________________

---

**Document Version:** 1.0
**Last Updated:** 2025-10-22
