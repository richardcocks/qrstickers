# Infinite Canvas Architecture

## Overview

The QRStickers template designer uses an **infinite canvas** pattern to allow users to design sticker templates with panning capabilities. This document explains the architecture, implementation details, and how to work with the system.

### What is an Infinite Canvas?

An infinite canvas is a design pattern where:
- The visible canvas size matches the viewport/container size
- Content can exist in a logical coordinate space beyond the physical canvas bounds
- Users pan around the logical space using viewport transforms
- Only the visible portion is rendered

This is the pattern used by professional design tools like Figma, Sketch, and Adobe XD.

### Why We Use This Approach

**Previous Approach (v1):**
- 4x enlarged physical canvas (2268 x 1133 pixels)
- Sticker boundary centered in enlarged canvas (850, 425)
- Viewport transform for panning

**Problems with v1:**
- Viewport math broke with enlarged canvas size
- Pan limits became inverted (minX > maxX)
- Container size detection was unreliable
- Complex coordinate calculations
- Viewport stuck at clamping boundaries

**Current Approach (v2 - Infinite Canvas):**
- Canvas size = visible container (800 x 600 pixels)
- Sticker boundary at origin (0, 0)
- Viewport transform for all panning
- Simple, predictable math

## Architecture

### Canvas Sizing

**File**: `frontend/src/designer/CanvasWrapper.ts` (lines 50-60)

```typescript
// Canvas size matches visible container
const containerEl = document.getElementById(config.containerId)?.parentElement;
const canvasWidth = containerEl?.clientWidth || 800;
const canvasHeight = containerEl?.clientHeight || 600;

// Place sticker boundary at origin
this.boundaryLeft = 0;
this.boundaryTop = 0;
this.canvasWidth = canvasWidth;
this.canvasHeight = canvasHeight;

// Create Fabric canvas matching container size
this.fabricCanvas = new Canvas(config.containerId, {
  width: canvasWidth,
  height: canvasHeight,
  backgroundColor: 'transparent',
  selection: true,
  preserveObjectStacking: true,
});
```

**Key Points:**
- Canvas dimensions are determined at initialization from the container
- Default fallback: 800 x 600 pixels
- No enlargement factor - canvas = viewport
- Boundary rectangle positioned at (0, 0) in logical coordinates

### Coordinate System

```
Logical Space (infinite):
┌─────────────────────────────────────────┐
│                                         │
│  ┌──────────┐                          │
│  │ Sticker  │ <- Boundary at (0,0)     │
│  │ Boundary │                           │
│  │ 567x283  │                           │
│  └──────────┘                           │
│                                         │
│  Objects can exist anywhere in          │
│  logical space (negative coords OK)     │
└─────────────────────────────────────────┘

Viewport (visible canvas - 800x600):
┌──────────────────────┐
│                      │
│   ┌──────────┐       │
│   │ Sticker  │       │ <- What user sees
│   │ Boundary │       │
│   └──────────┘       │
│                      │
└──────────────────────┘
```

**Coordinate Types:**
- **Logical coordinates**: Where objects actually exist (e.g., boundary at 0,0)
- **Viewport coordinates**: What's currently visible on screen
- **Screen coordinates**: Browser pixel positions

The viewport transform maps logical coordinates to viewport coordinates.

## Viewport Transform

### Fabric.js Transform Matrix

Fabric.js uses a 6-element array to represent 2D transforms:

```typescript
viewportTransform = [scaleX, skewX, skewY, scaleY, translateX, translateY]
//                      [0]    [1]    [2]    [3]      [4]       [5]

// For panning (no zoom/skew):
viewportTransform = [1, 0, 0, 1, panX, panY]
```

**Pan Values:**
- `vpt[4]` (panX): Horizontal pan offset
- `vpt[5]` (panY): Vertical pan offset

**Positive pan values** = viewport moves right/down (content appears to move left/up)
**Negative pan values** = viewport moves left/up (content appears to move right/down)

### Centering the Boundary

**File**: `frontend/src/designer/CanvasWrapper.ts:468-500`

To center the sticker boundary (at 0,0) in the viewport:

```typescript
resetView(): void {
  const vpt = this.fabricCanvas.viewportTransform;
  const rect = container?.getBoundingClientRect();
  const containerWidth = rect?.width || 800;
  const containerHeight = rect?.height || 600;

  // Center the boundary (at 0,0) in the viewport
  // Pan = (containerSize - stickerSize) / 2
  vpt[4] = (containerWidth - this.stickerWidthPx) / 2;
  vpt[5] = (containerHeight - this.stickerHeightPx) / 2;

  this.fabricCanvas.requestRenderAll();
}
```

**Math Explanation:**

With:
- Container: 800 x 600
- Sticker: 567 x 283

Centering calculation:
- panX = (800 - 567) / 2 = 116.5
- panY = (600 - 283) / 2 = 158.5

This positions the sticker boundary at the center of the visible viewport.

### Example Viewport States

**Centered (default):**
```
panX = 116, panY = 158
Sticker appears centered in viewport
```

**Panned Right:**
```
panX = 300, panY = 158
Viewport moved right, sticker appears on left side
```

**Panned Left:**
```
panX = -100, panY = 158
Viewport moved left, sticker appears on right side
```

## Pan Limits

### Why Limits Are Needed

Without limits, users could pan infinitely and lose track of the sticker boundary. Limits ensure:
- At least 10% of the boundary remains visible
- Users can't pan too far away
- Boundary is always reachable

### Limit Calculation

**File**: `frontend/src/designer/CanvasWrapper.ts:505-533`

```typescript
private getPanLimits() {
  const containerWidth = rect?.width || 800;
  const containerHeight = rect?.height || 600;

  const minVisibleAmount = 0.1; // 10%

  // With boundary at (0,0)
  // Maximum pan right: boundary left edge stays within right 10% of screen
  const maxX = containerWidth - this.stickerWidthPx * minVisibleAmount;

  // Maximum pan left: boundary right edge stays within left 10% of screen
  const minX = containerWidth * minVisibleAmount - this.stickerWidthPx;

  // Same logic for Y axis
  const maxY = containerHeight - this.stickerHeightPx * minVisibleAmount;
  const minY = containerHeight * minVisibleAmount - this.stickerHeightPx;

  return { minX, maxX, minY, maxY };
}
```

### Limit Examples

With container 800x600 and sticker 567x283:

```typescript
maxX = 800 - (567 * 0.1) = 743.3
minX = (800 * 0.1) - 567 = -487.0

maxY = 600 - (283 * 0.1) = 571.7
minY = (600 * 0.1) - 283 = -223.0
```

**Valid pan range:**
- X: -487 to 743 (minX < maxX ✓)
- Y: -223 to 572 (minY < maxY ✓)

### Clamping Pan Values

**File**: `frontend/src/designer/CanvasWrapper.ts:436-459`

```typescript
pan(deltaX: number, deltaY: number): void {
  const vpt = this.fabricCanvas.viewportTransform;

  const newPanX = vpt[4] + deltaX;
  const newPanY = vpt[5] + deltaY;

  const limits = this.getPanLimits();

  // Clamp to limits
  vpt[4] = Math.max(limits.minX, Math.min(limits.maxX, newPanX));
  vpt[5] = Math.max(limits.minY, Math.min(limits.maxY, newPanY));

  this.fabricCanvas.requestRenderAll();
}
```

This ensures pan values never exceed the calculated limits.

## Grid Rendering

### Dynamic Grid Overlay

The grid is rendered as a canvas overlay (not Fabric objects) that updates based on the viewport transform.

**File**: `frontend/src/designer/CanvasWrapper.ts:319-380`

```typescript
private renderGrid(): void {
  const ctx = this.fabricCanvas.getContext('2d');
  const vpt = this.fabricCanvas.viewportTransform;

  const zoom = vpt[0]; // Scale factor
  const panX = vpt[4];
  const panY = vpt[5];

  const canvasWidth = this.fabricCanvas.width || 800;
  const canvasHeight = this.fabricCanvas.height || 600;

  // Calculate visible area in canvas coordinates
  const visibleLeft = -panX / zoom;
  const visibleTop = -panY / zoom;
  const visibleWidth = canvasWidth / zoom;
  const visibleHeight = canvasHeight / zoom;

  // Grid spacing in pixels
  const gridSpacingPx = mmToPx(this.gridSpacingMm); // 2.5mm = ~14px

  // Draw grid dots aligned to grid
  for (let x = startX; x <= visibleLeft + visibleWidth; x += gridSpacingPx) {
    for (let y = startY; y <= visibleTop + visibleHeight; y += gridSpacingPx) {
      // Transform logical coords to screen coords
      const screenX = (x * zoom + panX) | 0;
      const screenY = (y * zoom + panY) | 0;

      ctx.fillRect(screenX - 0.5, screenY - 0.5, 1, 1);
    }
  }
}
```

**Key Points:**
- Grid renders after Fabric.js via `after:render` event
- Only draws dots in visible area (performance optimization)
- Transforms logical grid positions to screen coordinates
- Grid spacing: 2.5mm (~14 pixels at 144 DPI)

## Working with the System

### Adding Objects

Objects are positioned in **logical coordinates** relative to the boundary at (0, 0):

```typescript
// Add QR code at 10mm from boundary left, 5mm from top
const qr = new QRElement({
  id: 'qr-1',
  x: 10,  // mm from boundary left
  y: 5,   // mm from boundary top
  width: 20,
  height: 20,
  dataSource: 'device.Serial'
});

// Canvas wrapper handles conversion to pixels
designer.addElement(qr);
```

**Coordinate Conversion:**
- Input: millimeters relative to boundary
- Stored: pixels relative to boundary (at 0,0)
- Displayed: transformed by viewport

### Exporting

**File**: `frontend/src/designer/CanvasWrapper.ts:206-229`

Export converts from canvas coordinates back to millimeters:

```typescript
toJSON(): any {
  const objects = this.getObjects()
    .filter((obj: any) => !obj.excludeFromExport)
    .map((obj: any) => ({
      type: obj.type,
      id: obj.id,
      // Convert from pixels relative to boundary
      left: pxToMm((obj.left ?? 0) - this.boundaryLeft),
      top: pxToMm((obj.top ?? 0) - this.boundaryTop),
      width: pxToMm(obj.getScaledWidth()),
      height: pxToMm(obj.getScaledHeight()),
      // ... other properties
    }));

  return {
    version: '1.0',
    pageSize: {
      width: this.widthMm,
      height: this.heightMm,
      unit: 'mm',
    },
    objects
  };
}
```

Since `boundaryLeft` and `boundaryTop` are now 0, the subtraction has no effect, but the code remains for consistency.

### Testing Considerations

**Test Mocks:**

Mock canvas must provide:
- `viewportTransform` array
- `width` and `height` properties
- `getElement()` returning element with `parentElement.getBoundingClientRect()`

```typescript
// Mock setup
this.viewportTransform = [1, 0, 0, 1, 0, 0];
this.width = 800;
this.height = 600;

this.getElement = vi.fn(() => ({
  parentElement: {
    getBoundingClientRect: () => ({
      width: 800,
      height: 600,
      // ... other DOMRect properties
    })
  }
}));
```

**Test Expectations:**

```typescript
// Canvas matches container size (not enlarged)
expect(wrapper.canvasWidth).toBe(800);
expect(wrapper.canvasHeight).toBe(600);

// Boundary at origin
expect(wrapper.boundaryLeft).toBe(0);
expect(wrapper.boundaryTop).toBe(0);

// Pan limits are not inverted
const limits = wrapper.getPanLimits();
expect(limits.minX).toBeLessThan(limits.maxX);
expect(limits.minY).toBeLessThan(limits.maxY);
```

## Migration Notes

### What Changed

**From v1 (4x Enlarged Canvas):**
```typescript
// OLD
const enlargementFactor = 4;
const canvasWidth = stickerWidthPx * enlargementFactor;
this.boundaryLeft = (canvasWidth - stickerWidthPx) / 2; // 850
```

**To v2 (Infinite Canvas):**
```typescript
// NEW
const canvasWidth = containerEl?.clientWidth || 800;
this.boundaryLeft = 0; // Always at origin
```

### Why the Rewrite

The original 4x enlargement approach caused fundamental issues:

1. **Viewport math complexity**: Centering required complex calculations
2. **Inverted pan limits**: `minX > maxX` blocked panning
3. **Container size detection**: `getBoundingClientRect()` returned enlarged canvas size
4. **Clamping failures**: Viewport would stick at boundaries

The infinite canvas pattern:
- Simplifies all math significantly
- Fixes pan limits naturally
- Matches professional design tool patterns
- More maintainable and extensible

### Migration Checklist

If updating from v1 to v2:

- [ ] Update canvas initialization expectations (800x600, not 2268x1133)
- [ ] Update boundary position expectations (0,0 not 850,425)
- [ ] Verify pan limits are not inverted
- [ ] Check object positioning still works correctly
- [ ] Test export/import preserves coordinates
- [ ] Update any hardcoded coordinate values

## Debug Logging

Enable detailed logging by setting `debug = true` in CanvasWrapper:

```typescript
private debug: boolean = true;
```

This logs:
- `resetView()`: Container size, boundary position, viewport transform
- `getPanLimits()`: Calculated limits for each axis
- `pan()`: Delta values, before/after clamping
- `enablePanning()`/`disablePanning()`: Mode changes
- Pan handlers: Mouse positions, delta calculations

**Example output:**
```
[CanvasWrapper] resetView: { containerSize: { width: 800, height: 600 }, ... }
[CanvasWrapper] resetView calculated: { panX: 116.5, panY: 158.5 }
[CanvasWrapper] panning enabled
[CanvasWrapper] pan start: { x: 320, y: 397 }
[CanvasWrapper] pan move: { deltaX: 10, deltaY: 5 }
[CanvasWrapper] getPanLimits: { minX: -487, maxX: 743, minY: -223, maxY: 572 }
[CanvasWrapper] pan after clamping: { panX: 126.5, panY: 163.5 }
```

## Performance Considerations

### Grid Rendering Optimization

- Only visible grid dots are drawn (not the entire infinite grid)
- Grid updates via `requestAnimationFrame` through Fabric's render cycle
- Integer pixel positions (`| 0`) for performance

### Viewport Updates

- Pan updates modify the transform array in-place (no allocations)
- Single `requestRenderAll()` per pan event
- Fabric.js handles dirty region rendering

### Memory

- Canvas size matches viewport (800x600 vs 2268x1133)
- ~70% reduction in canvas pixel buffer size
- Lower memory footprint overall

## Zoom Functionality

### Overview

The infinite canvas supports zooming in and out while maintaining the center point fixed. Zoom functionality integrates seamlessly with panning and grid rendering.

### Zoom Implementation

**File**: `frontend/src/designer/CanvasWrapper.ts`

**Zoom State**:
```typescript
private currentZoom: number = 1;
private readonly MIN_ZOOM = 0.1;  // 10% zoom
private readonly MAX_ZOOM = 5.0;  // 500% zoom
```

**Viewport Transform with Zoom**:
```typescript
viewportTransform = [scaleX, skewX, skewY, scaleY, panX, panY]
//                     [0]    [1]    [2]    [3]    [4]    [5]

// With zoom 2.0 (200%):
viewportTransform = [2.0, 0, 0, 2.0, panX, panY]
```

### Zoom Methods

**setZoom(scale, centerX?, centerY?)**:
- Sets zoom level (clamped to MIN_ZOOM - MAX_ZOOM)
- Optionally zooms centered on a specific screen point
- Defaults to zooming on viewport center if no point provided
- Adjusts pan values to keep the center point fixed

**Math for Zoom-to-Point** (lines 622-632):
```typescript
// Keep screen point fixed while zooming
const zoomRatio = newZoom / oldZoom;
const deltaX = (centerX - vpt[4]) * (1 - zoomRatio);
const deltaY = (centerY - vpt[5]) * (1 - zoomRatio);

vpt[0] = newZoom; // scaleX
vpt[3] = newZoom; // scaleY
vpt[4] += deltaX; // adjust panX to keep point fixed
vpt[5] += deltaY; // adjust panY to keep point fixed
```

**getZoom()**: Returns current zoom level

**zoomIn(factor = 1.2)**: Zooms in by multiplying current zoom by factor

**zoomOut(factor = 1.2)**: Zooms out by dividing current zoom by factor

**zoomToFit()**: Calculates optimal zoom to fit sticker boundary in viewport with 90% padding

### Mouse Wheel Zoom

Zoom is triggered by scrolling the mouse wheel over the canvas. The zoom is centered on the cursor position, creating an intuitive zooming experience.

**File**: `frontend/src/designer/CanvasWrapper.ts:719-744`

```typescript
private handleMouseWheel(e: WheelEvent): void {
  e.preventDefault();

  // Get mouse position relative to canvas
  const rect = canvasElement.getBoundingClientRect();
  const mouseX = e.clientX - rect.left;
  const mouseY = e.clientY - rect.top;

  // Calculate zoom factor (negative deltaY = zoom in)
  const zoomFactor = e.deltaY > 0 ? 0.9 : 1.1;
  const newZoom = this.currentZoom * zoomFactor;

  // Zoom centered on cursor
  this.setZoom(newZoom, mouseX, mouseY);
}
```

### Pan Limits with Zoom

Pan limits are dynamically calculated based on the current zoom level to ensure the boundary remains partially visible.

**File**: `frontend/src/designer/CanvasWrapper.ts:514-548`

```typescript
// Account for zoom: sticker appears larger/smaller at different zoom levels
const scaledStickerWidth = this.stickerWidthPx * this.currentZoom;
const scaledStickerHeight = this.stickerHeightPx * this.currentZoom;

// Calculate limits based on scaled dimensions
const maxX = containerWidth - scaledStickerWidth * minVisibleAmount;
const minX = containerWidth * minVisibleAmount - scaledStickerWidth;
```

**Example Pan Limits at Different Zooms**:

With container 800x600 and sticker 567x283:

```typescript
// At zoom 1.0:
minX = -487, maxX = 743 (range: 1230)

// At zoom 2.0:
minX = -1054, maxX = 687 (range: 1741) - more panning allowed

// At zoom 0.5:
minX = -204, maxX = 772 (range: 976) - less panning needed
```

### Reset View with Zoom

The `resetView()` method resets both zoom to 1:1 and centers the boundary:

```typescript
resetView(): void {
  vpt[0] = 1; // reset scaleX
  vpt[3] = 1; // reset scaleY
  this.currentZoom = 1;

  // Center boundary at 1:1 zoom
  vpt[4] = (containerWidth - stickerWidthPx) / 2;
  vpt[5] = (containerHeight - stickerHeightPx) / 2;
}
```

### Grid Rendering with Zoom

The grid rendering already accounts for zoom (no changes needed):

**File**: `frontend/src/designer/CanvasWrapper.ts:338-380`

```typescript
const zoom = vpt[0]; // Get current zoom from transform

// Calculate visible area accounting for zoom
const visibleLeft = -panX / zoom;
const visibleTop = -panY / zoom;
const visibleWidth = canvasWidth / zoom;
const visibleHeight = canvasHeight / zoom;

// Transform grid dots to screen coordinates
const screenX = (x * zoom + panX) | 0;
const screenY = (y * zoom + panY) | 0;
```

This ensures grid dots remain aligned to the logical grid at all zoom levels.

### UI Controls

**File**: `frontend/index.html:256-265`

```html
<h2>Zoom</h2>
<div class="toolbar">
    <div class="button-group">
        <button onclick="zoomIn()">➕ In</button>
        <button onclick="zoomOut()">➖ Out</button>
    </div>
    <button onclick="zoomToFit()">⊡ Fit</button>
    <button onclick="zoomReset()">100%</button>
    <div id="zoomDisplay">100%</div>
</div>
```

**JavaScript Handlers**:
- `zoomIn()`: Zoom in by 1.2x
- `zoomOut()`: Zoom out by 1.2x
- `zoomToFit()`: Fit sticker to viewport
- `zoomReset()`: Reset to 1:1 zoom centered
- `updateZoomDisplay()`: Updates zoom percentage display

### Testing

**File**: `frontend/tests/unit/designer/CanvasWrapper.test.ts`

Comprehensive zoom tests include:
- Zoom level clamping (MIN_ZOOM, MAX_ZOOM)
- Zoom in/out with default and custom factors
- Zoom centered on viewport center vs specific point
- Pan limit enforcement at different zoom levels
- zoomToFit() calculation
- resetView() zoom reset
- Pan limit ranges at different zoom levels

**Running tests**:
```bash
cd frontend && npm run test
```

### Debug Logging

Enable zoom logging by setting `debug = true` in CanvasWrapper:

**Example output**:
```
[CanvasWrapper] setZoom input: { requestedScale: 2, clampedScale: 2, oldZoom: 1, centerX: 400, centerY: 300 }
[CanvasWrapper] setZoom result: { zoom: 2, panX: -116.5, panY: -141.5, limits: {...} }
[CanvasWrapper] getPanLimits: { zoom: 2, scaledSize: { width: 1134, height: 567 }, limits: {...} }
```

## Future Enhancements

Potential improvements to the infinite canvas:

1. ~~**Zoom support**~~: ✅ Implemented with mouse wheel and UI controls
2. **Minimap**: Show overview of full logical space
3. **Snap to grid**: Align objects to grid lines
4. **Rulers**: Display measurement guides
5. **Multiple artboards**: Support multiple sticker templates in one canvas
6. **Performance viewport**: Cull objects outside visible area
7. **Keyboard shortcuts**: Ctrl+Plus/Minus for zoom, Ctrl+0 for reset
8. **Touch gestures**: Pinch-to-zoom on touch devices

## References

- [Fabric.js Viewport Documentation](http://fabricjs.com/fabric-intro-part-5)
- [2D Transform Matrix](https://developer.mozilla.org/en-US/docs/Web/API/Canvas_API/Tutorial/Transformations)
- [Infinite Canvas Pattern](https://www.figma.com/blog/how-figmas-multiplayer-technology-works/)
- [Zoom and Pan Interactions](https://www.nan.fyi/svg-paths)
