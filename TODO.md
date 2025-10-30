# Frontend Integration TODO

## Critical for Production
- [x] **Export modal implementation** (PNG/SVG, DPI settings, preview, download) ✅ COMPLETED
  - Migrated export-preview.js to TypeScript (frontend/src/export/ExportPreview.ts)
  - Created export-shared bundle for device export compatibility
  - Added loading indicators and UX polish
  - Implemented template designer export with placeholder data
  - Updated Network.cshtml to use TypeScript bundle

## Designer Features
- [ ] Drag-and-drop from palette
- [ ] Rulers (horizontal and vertical tick marks with unit labels)
- [ ] Image handling in designer (palette display, click-to-add)
- [ ] Fix text data sources
- [ ] Hold right click to pan
- [ ] Reset grab-handles when zooming / resizing page
- [ ] Respect snap to grid on element resize

## Testing Required
- [ ] Test designer export with all formats and options (PNG 96/150/300 DPI, SVG, white/transparent backgrounds)
- [ ] Test device export workflow still works with new TypeScript bundle
- [ ] Test on staging environment
- [ ] Verify all export scenarios across different templates
- [ ] Performance testing (zoom/pan with large templates, memory usage)
- [ ] Browser compatibility (Chrome, Firefox, Safari, Edge)

## Completed
- [x] Delete legacy JavaScript files (`designer.js`, `fabric-extensions.js`, `export-preview.js`) ✅ DONE
