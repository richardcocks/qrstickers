/**
 * Canvas Wrapper - Hides Fabric.js complexity
 * Simple, clean API for canvas operations
 */

import { Canvas, FabricObject, version as fabricVersion } from 'fabric';
import { mmToPx, pxToMm } from '../utils/units';
import { RulerRenderer } from './RulerRenderer';

export interface CanvasConfig {
  containerId: string;
  widthMm: number;
  heightMm: number;
  marginTop?: number;
  marginLeft?: number;
  marginBottom?: number;
  marginRight?: number;
}

export class CanvasWrapper {
  private fabricCanvas: Canvas;
  private gridVisible: boolean = true;
  private snapToGridEnabled: boolean = true;  // Match checkbox default state
  private isPanningEnabled: boolean = false;
  private gridSpacingMm: number = 2.5;
  private lastPanX = 0;
  private lastPanY = 0;
  private isPanning = false;
  private currentZoom: number = 1;
  private savedActiveObject: FabricObject | undefined = undefined; // Store active object during view changes
  private isRightClickPanning = false; // Track if right-click pan is active
  private rightClickStartX = 0; // Track initial mouse position to detect drag vs click
  private rightClickStartY = 0;
  private rulerRenderer: RulerRenderer; // Ruler rendering system

  // Zoom limits
  private readonly MIN_ZOOM = 0.1;
  private readonly MAX_ZOOM = 5.0;

  public readonly widthMm: number;
  public readonly heightMm: number;
  public readonly boundaryLeft: number;
  public readonly boundaryTop: number;
  public canvasWidth: number; // Mutable to support dynamic resize
  public canvasHeight: number; // Mutable to support dynamic resize
  public readonly stickerWidthPx: number;
  public readonly stickerHeightPx: number;

  constructor(config: CanvasConfig) {
    this.widthMm = config.widthMm;
    this.heightMm = config.heightMm;

    // Calculate dimensions
    const stickerWidthPx = mmToPx(config.widthMm);
    const stickerHeightPx = mmToPx(config.heightMm);

    this.stickerWidthPx = stickerWidthPx;
    this.stickerHeightPx = stickerHeightPx;

    // For infinite canvas, canvas size matches visible container
    // Get .designer-canvas-container (not .canvas-wrapper) for accurate dimensions
    const canvasEl = document.getElementById(config.containerId);
    const canvasWrapper = canvasEl?.parentElement; // .canvas-wrapper
    const containerEl = canvasWrapper?.parentElement; // .designer-canvas-container
    const canvasWidth = containerEl?.clientWidth || 800;
    const canvasHeight = containerEl?.clientHeight || 600;

    // Place sticker boundary at origin for simpler viewport math
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
      fireRightClick: true,  // Enable right-click events for right-click pan feature
    });

    // Style selection
    this.fabricCanvas.selectionColor = 'rgba(25, 118, 210, 0.1)';
    this.fabricCanvas.selectionBorderColor = '#1976d2';
    this.fabricCanvas.selectionLineWidth = 2;

    // Initialize ruler rendering system
    this.rulerRenderer = new RulerRenderer();

    // Set up grid and boundary rendering via after:render event
    this.setupGridRendering();

    // Enable snap to grid on object movement
    this.setupSnapToGrid();

    // Enable mouse wheel zoom
    this.enableMouseWheelZoom();

    // Set up global mouse event handlers for right-click panning
    this.setupRightClickPan();

    // Set up ruler cursor position tracking
    this.setupRulerCursorTracking();
  }

  /**
   * Add a Fabric object to the canvas
   */
  add(fabricObject: FabricObject): void {
    this.fabricCanvas.add(fabricObject);
  }

  /**
   * Remove a Fabric object from the canvas
   */
  remove(fabricObject: FabricObject): void {
    this.fabricCanvas.remove(fabricObject);
  }

  /**
   * Get all objects on canvas
   */
  getObjects(): FabricObject[] {
    return this.fabricCanvas.getObjects();
  }

  /**
   * Get currently selected object
   */
  getActiveObject(): FabricObject | undefined {
    return this.fabricCanvas.getActiveObject();
  }

  /**
   * Set active object
   */
  setActiveObject(obj: FabricObject): void {
    this.fabricCanvas.setActiveObject(obj);
    this.fabricCanvas.requestRenderAll();
  }

  /**
   * Deselect all objects
   */
  discardActiveObject(): void {
    this.fabricCanvas.discardActiveObject();
    this.fabricCanvas.requestRenderAll();
  }

  /**
   * Bring object to front (top layer)
   */
  bringToFront(fabricObject: FabricObject): void {
    this.fabricCanvas.bringObjectToFront(fabricObject);
    this.fabricCanvas.requestRenderAll();
  }

  /**
   * Send object to back (bottom layer)
   */
  sendToBack(fabricObject: FabricObject): void {
    this.fabricCanvas.sendObjectToBack(fabricObject);
    this.fabricCanvas.requestRenderAll();
  }

  /**
   * Bring object forward one layer
   */
  bringForward(fabricObject: FabricObject): void {
    this.fabricCanvas.bringObjectForward(fabricObject);
    this.fabricCanvas.requestRenderAll();
  }

  /**
   * Send object backward one layer
   */
  sendBackward(fabricObject: FabricObject): void {
    this.fabricCanvas.sendObjectBackwards(fabricObject);
    this.fabricCanvas.requestRenderAll();
  }

  /**
   * Clear all objects
   */
  clear(): void {
    this.fabricCanvas.clear();
  }

  /**
   * Render the canvas
   */
  render(): void {
    this.fabricCanvas.requestRenderAll();
  }

  /**
   * Export canvas to JSON (excluding boundary and non-exportable objects)
   */
  toJSON(): any {
    const objects = this.getObjects()
      .filter((obj: any) => !obj.excludeFromExport)
      .map((obj: any) => ({
        type: obj.type,
        id: obj.id || this.generateId(),
        left: pxToMm((obj.left ?? 0) - this.boundaryLeft),
        top: pxToMm((obj.top ?? 0) - this.boundaryTop),
        width: pxToMm(obj.getScaledWidth()),
        height: pxToMm(obj.getScaledHeight()),
        ...this.extractObjectProperties(obj),
      }));

    return {
      version: '1.0',
      fabricVersion: fabricVersion,
      pageSize: {
        width: this.widthMm,
        height: this.heightMm,
        unit: 'mm',
      },
      objects,
    };
  }

  /**
   * Load template from JSON
   */
  fromJSON(_json: any): void {
    this.clear();
    // Will be implemented when we have element classes
  }

  /**
   * Register event handler
   */
  on(eventName: string, handler: (e: unknown) => void): void {
    (this.fabricCanvas as any).on(eventName, handler);
  }

  /**
   * Unregister event handler
   */
  off(eventName: string, handler?: (e: unknown) => void): void {
    (this.fabricCanvas as any).off(eventName, handler);
  }

  /**
   * Check if grid is visible
   */
  isGridVisible(): boolean {
    return this.gridVisible;
  }

  /**
   * Show grid dots
   */
  showGrid(): void {
    this.gridVisible = true;
    this.fabricCanvas.requestRenderAll();
  }

  /**
   * Hide grid dots
   */
  hideGrid(): void {
    this.gridVisible = false;
    this.fabricCanvas.requestRenderAll();
  }

  /**
   * Enable snap to grid
   */
  enableSnapToGrid(): void {
    this.snapToGridEnabled = true;
  }

  /**
   * Disable snap to grid
   */
  disableSnapToGrid(): void {
    this.snapToGridEnabled = false;
  }

  /**
   * Check if snap to grid is enabled
   */
  isSnapToGridEnabled(): boolean {
    return this.snapToGridEnabled;
  }

  // Private helpers

  private generateId(): string {
    if (typeof crypto !== 'undefined' && crypto.randomUUID) {
      return `obj-${crypto.randomUUID()}`;
    }
    return `obj-${Date.now()}-${Math.random().toString(36).substring(2, 11)}`;
  }

  private extractObjectProperties(obj: any): any {
    const props: any = {
      scaleX: obj.scaleX ?? 1,
      scaleY: obj.scaleY ?? 1,
      angle: obj.angle ?? 0,
    };

    // Extract type-specific properties
    if (obj.type === 'qrcode' || obj.customType === 'qrcode') {
      props.properties = {
        dataSource: obj.dataSource ?? 'device.Serial',
        eccLevel: obj.eccLevel ?? 'Q',
        quietZone: obj.quietZone ?? 2,
      };
    } else if (obj.type === 'i-text' || obj.type === 'text') {
      props.text = obj.text;
      props.fontFamily = obj.fontFamily;
      props.fontSize = obj.fontSize;
      props.fontWeight = obj.fontWeight;
      props.fill = obj.fill;
      props.properties = {
        dataSource: obj.dataSource ?? '',
        maxLength: obj.maxLength ?? null,
        overflow: obj.overflow ?? 'truncate',
      };
    }

    return props;
  }

  /**
   * Set up grid and boundary rendering via canvas overlay
   * Both are drawn on canvas context after Fabric.js renders
   */
  private setupGridRendering(): void {
    (this.fabricCanvas as any).on('after:render', () => {
      this.renderBoundary();
      this.renderGrid();
      this.renderRulers();
    });
  }

  /**
   * Render rulers on canvas overlay
   */
  private renderRulers(): void {
    const ctx = this.fabricCanvas.getContext();
    if (!ctx) return;

    const vpt = this.fabricCanvas.viewportTransform;
    if (!vpt) return;

    const canvasWidth = this.fabricCanvas.width || 800;
    const canvasHeight = this.fabricCanvas.height || 600;

    this.rulerRenderer.render(ctx, vpt, canvasWidth, canvasHeight);
  }

  /**
   * Set up snap to grid behavior
   * Snaps object positions to grid during movement
   */
  private setupSnapToGrid(): void {
    (this.fabricCanvas as any).on('object:moving', (e: any) => {
      if (!this.snapToGridEnabled) return;

      const obj = e.target;
      if (!obj) return;

      // Grid spacing in pixels
      const gridSpacingPx = mmToPx(this.gridSpacingMm);

      // Snap object position to nearest grid point
      obj.set({
        left: Math.round(obj.left / gridSpacingPx) * gridSpacingPx,
        top: Math.round(obj.top / gridSpacingPx) * gridSpacingPx,
      });

      // Update coordinates to maintain alignment
      obj.setCoords();
    });
  }

  /**
   * Render grid dots on canvas overlay
   * Uses canvas context to draw grid dynamically based on viewport
   */
  private renderGrid(): void {
    if (!this.gridVisible) return;

    const ctx = this.fabricCanvas.getContext();
    if (!ctx) return;

    // Get viewport transform (pan and zoom)
    const vpt = this.fabricCanvas.viewportTransform;
    if (!vpt) return;

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
    const gridSpacingPx = mmToPx(this.gridSpacingMm);

    // Calculate starting position (aligned to grid)
    const startX = Math.floor(visibleLeft / gridSpacingPx) * gridSpacingPx;
    const startY = Math.floor(visibleTop / gridSpacingPx) * gridSpacingPx;

    // Draw grid dots
    ctx.fillStyle = '#bbb';
    ctx.globalAlpha = 0.8;

    for (let x = startX; x <= visibleLeft + visibleWidth; x += gridSpacingPx) {
      for (let y = startY; y <= visibleTop + visibleHeight; y += gridSpacingPx) {
        // Transform canvas coordinates to screen coordinates for drawing
        const screenX = (x * zoom + panX) | 0;
        const screenY = (y * zoom + panY) | 0;

        // Only draw if within visible bounds (with some buffer)
        if (
          screenX >= -5 &&
          screenX <= canvasWidth + 5 &&
          screenY >= -5 &&
          screenY <= canvasHeight + 5
        ) {
          ctx.fillRect(screenX - 0.5, screenY - 0.5, 1, 1);
        }
      }
    }

    ctx.globalAlpha = 1;
  }

  /**
   * Render sticker boundary overlay (canvas overlay, non-interactive)
   * Drawn on canvas context after Fabric.js renders, automatically respects zoom/pan
   */
  private renderBoundary(): void {
    const ctx = this.fabricCanvas.getContext();
    if (!ctx) return;

    // Get viewport transform (pan and zoom)
    const vpt = this.fabricCanvas.viewportTransform;
    if (!vpt) return;

    const zoom = vpt[0]; // Scale factor
    const panX = vpt[4]; // Pan X offset
    const panY = vpt[5]; // Pan Y offset

    const canvasWidth = this.fabricCanvas.width || 800;
    const canvasHeight = this.fabricCanvas.height || 600;

    ctx.save();

    // Calculate boundary positions in screen coordinates (transformed by viewport)
    const screenLeft = this.boundaryLeft * zoom + panX;
    const screenTop = this.boundaryTop * zoom + panY;
    const screenWidth = this.stickerWidthPx * zoom;
    const screenHeight = this.stickerHeightPx * zoom;

    // Draw boundary lines extending full canvas dimensions
    ctx.strokeStyle = '#cccccc';
    ctx.lineWidth = 2;
    ctx.setLineDash([10, 5]);

    // Calculate boundary edge positions
    const leftEdge = screenLeft;
    const rightEdge = screenLeft + screenWidth;
    const topEdge = screenTop;
    const bottomEdge = screenTop + screenHeight;

    // Vertical line at left edge (full canvas height)
    ctx.beginPath();
    ctx.moveTo(leftEdge, 0);
    ctx.lineTo(leftEdge, canvasHeight);
    ctx.stroke();

    // Vertical line at right edge (full canvas height)
    ctx.beginPath();
    ctx.moveTo(rightEdge, 0);
    ctx.lineTo(rightEdge, canvasHeight);
    ctx.stroke();

    // Horizontal line at top edge (full canvas width)
    ctx.beginPath();
    ctx.moveTo(0, topEdge);
    ctx.lineTo(canvasWidth, topEdge);
    ctx.stroke();

    // Horizontal line at bottom edge (full canvas width)
    ctx.beginPath();
    ctx.moveTo(0, bottomEdge);
    ctx.lineTo(canvasWidth, bottomEdge);
    ctx.stroke();

    ctx.restore();
  }

  /**
   * Enable panning mode - disable selection, enable pan on drag
   */
  enablePanning(): void {
    if (this.isPanningEnabled) return;
    this.isPanningEnabled = true;

    // Disable selection while panning
    this.fabricCanvas.selection = false;
    this.fabricCanvas.getObjects().forEach((obj) => {
      (obj as any).selectable = false;
    });

    // Change cursor to grab
    const container = this.fabricCanvas.getElement().parentElement;
    if (container) {
      container.style.cursor = 'grab';
    }

    // Note: Mouse handlers are already registered globally in setupRightClickPan
  }

  /**
   * Disable panning mode - re-enable selection
   */
  disablePanning(): void {
    if (!this.isPanningEnabled) return;
    this.isPanningEnabled = false;

    // Note: Mouse handlers remain registered globally for right-click panning

    // Re-enable selection
    this.fabricCanvas.selection = true;
    this.fabricCanvas.getObjects().forEach((obj) => {
      (obj as any).selectable = true;
    });

    // Restore default cursor
    const container = this.fabricCanvas.getElement().parentElement;
    if (container) {
      container.style.cursor = 'default';
    }
  }

  /**
   * Pan by offset amount with limit enforcement
   */
  pan(deltaX: number, deltaY: number): void {
    const vpt = this.fabricCanvas.viewportTransform;
    if (!vpt) return;

    const newPanX = vpt[4] + deltaX;
    const newPanY = vpt[5] + deltaY;

    // Enforce pan limits (keep boundary partially visible)
    const limits = this.getPanLimits();

    vpt[4] = Math.max(limits.minX, Math.min(limits.maxX, newPanX));
    vpt[5] = Math.max(limits.minY, Math.min(limits.maxY, newPanY));

    // Note: Don't deselect here during continuous panning - handled in handlePanStart/End

    this.fabricCanvas.requestRenderAll();
  }

  /**
   * Reset view to center on sticker boundary at 1:1 zoom
   */
  resetView(): void {
    const vpt = this.fabricCanvas.viewportTransform;
    if (!vpt) return;

    // Save active object before deselecting
    const activeObject = this.fabricCanvas.getActiveObject();

    // Get actual visible container size
    const container = this.fabricCanvas.getElement().parentElement;
    const rect = container?.getBoundingClientRect();
    const containerWidth = rect?.width || 800;
    const containerHeight = rect?.height || 600;

    // Deselect to fix grab handle alignment during view change
    if (activeObject) {
      this.discardActiveObject();
    }

    // Reset zoom to 1:1
    vpt[0] = 1; // scaleX
    vpt[3] = 1; // scaleY
    this.currentZoom = 1;

    // Center the boundary (at 0,0) in the viewport
    // Pan = (containerSize - stickerSize) / 2
    vpt[4] = (containerWidth - this.stickerWidthPx) / 2;
    vpt[5] = (containerHeight - this.stickerHeightPx) / 2;

    this.fabricCanvas.requestRenderAll();

    // Re-select the object for seamless experience
    if (activeObject) {
      this.setActiveObject(activeObject);
    }
  }

  /**
   * Resize canvas to match current container dimensions
   * Call this when container size changes (e.g., fullscreen, window resize)
   */
  resize(): void {
    // Get .designer-canvas-container (not .canvas-wrapper) for accurate dimensions
    const canvasElement = this.fabricCanvas.getElement();
    const canvasWrapper = canvasElement.parentElement; // .canvas-wrapper
    const container = canvasWrapper?.parentElement; // .designer-canvas-container

    if (!container) {
      console.error('[CanvasWrapper] resize: .designer-canvas-container not found');
      return;
    }

    const rect = container.getBoundingClientRect();
    const newWidth = rect.width;
    const newHeight = rect.height;

    // Skip if dimensions haven't changed
    if (newWidth === this.canvasWidth && newHeight === this.canvasHeight) {
      return;
    }

    // Update Fabric canvas dimensions
    this.fabricCanvas.setDimensions({
      width: newWidth,
      height: newHeight
    });

    // Update stored dimensions
    this.canvasWidth = newWidth;
    this.canvasHeight = newHeight;

    // Recalculate viewport to maintain proper positioning
    this.resetView();
  }

  /**
   * Calculate pan limits to keep sticker boundary partially visible
   * Accounts for current zoom level
   */
  private getPanLimits(): { minX: number; maxX: number; minY: number; maxY: number } {
    // Get actual visible container size
    const container = this.fabricCanvas.getElement().parentElement;
    const rect = container?.getBoundingClientRect();
    const containerWidth = rect?.width || 800;
    const containerHeight = rect?.height || 600;

    // Minimum visible amount of boundary (10% must be visible)
    const minVisibleAmount = 0.1;

    // Account for zoom: sticker appears larger/smaller at different zoom levels
    const scaledStickerWidth = this.stickerWidthPx * this.currentZoom;
    const scaledStickerHeight = this.stickerHeightPx * this.currentZoom;

    // With boundary at (0,0), calculate limits to keep it partially visible
    // Maximum pan right: boundary left edge stays within right side of screen
    const maxX = containerWidth - scaledStickerWidth * minVisibleAmount;

    // Maximum pan left: boundary right edge stays within left side of screen
    const minX = containerWidth * minVisibleAmount - scaledStickerWidth;

    // Same logic for Y axis
    const maxY = containerHeight - scaledStickerHeight * minVisibleAmount;
    const minY = containerHeight * minVisibleAmount - scaledStickerHeight;

    return { minX, maxX, minY, maxY };
  }

  /**
   * Pan drag handlers - private methods for mouse events
   */
  private handlePanStart(e: any): void {
    // Check for right-click (button === 2)
    const isRightClick = e.e && e.e.button === 2;

    if (isRightClick) {
      // Enable temporary right-click panning regardless of tool mode
      this.isRightClickPanning = true;
      this.isPanning = true;
      this.lastPanX = e.pointer.x;
      this.lastPanY = e.pointer.y;
      this.rightClickStartX = e.pointer.x;
      this.rightClickStartY = e.pointer.y;

      // Save and deselect active object
      this.savedActiveObject = this.fabricCanvas.getActiveObject();
      if (this.savedActiveObject) {
        this.discardActiveObject();
      }

      // Change cursor to grabbing
      const container = this.fabricCanvas.getElement().parentElement;
      if (container) {
        container.style.cursor = 'grabbing';
      }
      return;
    }

    // Regular left-click pan (only when panning mode is enabled)
    if (!this.isPanningEnabled) return;

    this.isPanning = true;
    this.lastPanX = e.pointer.x;
    this.lastPanY = e.pointer.y;

    // Save and deselect active object to fix grab handle alignment during pan
    this.savedActiveObject = this.fabricCanvas.getActiveObject();
    if (this.savedActiveObject) {
      this.discardActiveObject();
    }

    // Change cursor to grabbing
    const container = this.fabricCanvas.getElement().parentElement;
    if (container) {
      container.style.cursor = 'grabbing';
    }
  }

  private handlePanMove(e: any): void {
    // Allow panning for right-click or regular pan mode
    if (!this.isPanning || (!this.isPanningEnabled && !this.isRightClickPanning)) return;

    const deltaX = e.pointer.x - this.lastPanX;
    const deltaY = e.pointer.y - this.lastPanY;

    this.pan(deltaX, deltaY);

    this.lastPanX = e.pointer.x;
    this.lastPanY = e.pointer.y;
  }

  private handlePanEnd(): void {
    if (!this.isPanning) return;

    this.isPanning = false;

    // Handle right-click pan cleanup
    if (this.isRightClickPanning) {
      this.isRightClickPanning = false;

      // Re-select the previously active object for seamless experience
      if (this.savedActiveObject) {
        this.setActiveObject(this.savedActiveObject);
        this.savedActiveObject = undefined;
      }

      // Restore cursor to default (not grab, since we're not in pan mode)
      const container = this.fabricCanvas.getElement().parentElement;
      if (container) {
        container.style.cursor = 'default';
      }
      return;
    }

    // Regular pan mode cleanup
    // Re-select the previously active object for seamless experience
    if (this.savedActiveObject) {
      this.setActiveObject(this.savedActiveObject);
      this.savedActiveObject = undefined;
    }

    // Change cursor back to grab (we're in pan mode)
    const container = this.fabricCanvas.getElement().parentElement;
    if (container) {
      container.style.cursor = 'grab';
    }
  }

  /**
   * Set zoom level, optionally centered on a specific screen point
   * @param scale - New zoom level (clamped to MIN_ZOOM - MAX_ZOOM)
   * @param centerX - Optional screen X coordinate to zoom towards (defaults to viewport center)
   * @param centerY - Optional screen Y coordinate to zoom towards (defaults to viewport center)
   */
  setZoom(scale: number, centerX?: number, centerY?: number): void {
    const vpt = this.fabricCanvas.viewportTransform;
    if (!vpt) return;

    // Save active object before deselecting
    const activeObject = this.fabricCanvas.getActiveObject();

    // Clamp scale to limits
    const newZoom = Math.max(this.MIN_ZOOM, Math.min(this.MAX_ZOOM, scale));
    const oldZoom = this.currentZoom;

    // If no center point provided, use viewport center
    if (centerX === undefined || centerY === undefined) {
      const container = this.fabricCanvas.getElement().parentElement;
      const rect = container?.getBoundingClientRect();
      const containerWidth = rect?.width || 800;
      const containerHeight = rect?.height || 600;
      centerX = containerWidth / 2;
      centerY = containerHeight / 2;
    }

    // Deselect to fix grab handle alignment during zoom
    if (activeObject) {
      this.discardActiveObject();
    }

    // Calculate pan adjustment to keep center point fixed
    // newPan = oldPan + (centerPoint - oldPan) * (1 - newZoom/oldZoom)
    const zoomRatio = newZoom / oldZoom;
    const deltaX = (centerX - vpt[4]) * (1 - zoomRatio);
    const deltaY = (centerY - vpt[5]) * (1 - zoomRatio);

    // Update viewport transform
    vpt[0] = newZoom; // scaleX
    vpt[3] = newZoom; // scaleY
    vpt[4] += deltaX; // panX
    vpt[5] += deltaY; // panY

    // Enforce pan limits at new zoom level
    const limits = this.getPanLimits();
    vpt[4] = Math.max(limits.minX, Math.min(limits.maxX, vpt[4]));
    vpt[5] = Math.max(limits.minY, Math.min(limits.maxY, vpt[5]));

    this.currentZoom = newZoom;

    this.fabricCanvas.requestRenderAll();

    // Re-select the object for seamless experience
    if (activeObject) {
      this.setActiveObject(activeObject);
    }
  }

  /**
   * Get current zoom level
   */
  getZoom(): number {
    return this.currentZoom;
  }

  /**
   * Zoom in by a factor (default 1.2x)
   */
  zoomIn(factor: number = 1.2): void {
    this.setZoom(this.currentZoom * factor);
  }

  /**
   * Zoom out by a factor (default 1.2x)
   */
  zoomOut(factor: number = 1.2): void {
    this.setZoom(this.currentZoom / factor);
  }

  /**
   * Zoom to fit the sticker boundary in the viewport
   */
  zoomToFit(): void {
    const vpt = this.fabricCanvas.viewportTransform;
    if (!vpt) return;

    // Save active object before deselecting
    const activeObject = this.fabricCanvas.getActiveObject();

    const container = this.fabricCanvas.getElement().parentElement;
    const rect = container?.getBoundingClientRect();
    const containerWidth = rect?.width || 800;
    const containerHeight = rect?.height || 600;

    // Deselect to fix grab handle alignment during zoom
    if (activeObject) {
      this.discardActiveObject();
    }

    // Calculate zoom to fit with some padding (90% of available space)
    const padding = 0.9;
    const zoomX = (containerWidth * padding) / this.stickerWidthPx;
    const zoomY = (containerHeight * padding) / this.stickerHeightPx;

    // Use the smaller zoom to ensure entire sticker fits
    const fitZoom = Math.min(zoomX, zoomY);

    // Set zoom level
    vpt[0] = fitZoom; // scaleX
    vpt[3] = fitZoom; // scaleY
    this.currentZoom = fitZoom;

    // Center the boundary (at 0,0) in the viewport at the new zoom level
    // Pan = (containerSize - stickerSize * zoom) / 2
    vpt[4] = (containerWidth - this.stickerWidthPx * fitZoom) / 2;
    vpt[5] = (containerHeight - this.stickerHeightPx * fitZoom) / 2;

    this.fabricCanvas.requestRenderAll();

    // Re-select the object for seamless experience
    if (activeObject) {
      this.setActiveObject(activeObject);
    }
  }

  /**
   * Enable mouse wheel zoom (centered on cursor position)
   */
  private enableMouseWheelZoom(): void {
    const canvasElement = this.fabricCanvas.getElement();
    const container = canvasElement?.parentElement;
    if (!container || typeof container.addEventListener !== 'function') return;

    container.addEventListener('wheel', this.handleMouseWheel.bind(this), { passive: false });
  }

  /**
   * Set up right-click pan functionality
   * Allows users to hold right-click and drag to pan regardless of tool mode
   */
  private setupRightClickPan(): void {
    const canvasElement = this.fabricCanvas.getElement();
    const container = canvasElement?.parentElement;
    if (!container || typeof container.addEventListener !== 'function') return;

    // Register global mouse event handlers for right-click panning
    // These work independently of the pan mode toggle
    (this.fabricCanvas as any).on('mouse:down', this.handlePanStart.bind(this));
    (this.fabricCanvas as any).on('mouse:move', this.handlePanMove.bind(this));
    (this.fabricCanvas as any).on('mouse:up', this.handlePanEnd.bind(this));

    // Prevent context menu during drag, allow it for simple right-click
    container.addEventListener('contextmenu', this.handleContextMenu.bind(this));
  }

  /**
   * Handle context menu event - prevent if user dragged, allow if just clicked
   */
  private handleContextMenu(e: MouseEvent): void {
    // If we have valid right-click start coordinates, check if user dragged
    if (this.rightClickStartX !== 0 || this.rightClickStartY !== 0) {
      // Get mouse position relative to canvas
      const canvasElement = this.fabricCanvas.getElement();
      const rect = canvasElement.getBoundingClientRect();
      const mouseX = e.clientX - rect.left;
      const mouseY = e.clientY - rect.top;

      const distanceX = Math.abs(mouseX - this.rightClickStartX);
      const distanceY = Math.abs(mouseY - this.rightClickStartY);
      const distance = Math.sqrt(distanceX * distanceX + distanceY * distanceY);

      // If user dragged more than 5 pixels, prevent context menu
      if (distance > 5) {
        e.preventDefault();
        e.stopPropagation();
      }

      // Reset coordinates after checking
      this.rightClickStartX = 0;
      this.rightClickStartY = 0;
    }
  }

  /**
   * Handle mouse wheel events for zooming
   */
  private handleMouseWheel(e: WheelEvent): void {
    e.preventDefault();
    e.stopPropagation();

    // Get mouse position relative to canvas
    const canvasElement = this.fabricCanvas.getElement();
    const rect = canvasElement.getBoundingClientRect();
    const mouseX = e.clientX - rect.left;
    const mouseY = e.clientY - rect.top;

    // Calculate zoom delta (negative deltaY means zoom in)
    const delta = e.deltaY;
    const zoomFactor = delta > 0 ? 0.9 : 1.1; // Zoom out or in

    const newZoom = this.currentZoom * zoomFactor;

    // Zoom centered on mouse cursor
    this.setZoom(newZoom, mouseX, mouseY);
  }

  /**
   * Set up ruler cursor position tracking
   */
  private setupRulerCursorTracking(): void {
    (this.fabricCanvas as any).on('mouse:move', (e: any) => {
      if (!e.pointer) {
        this.rulerRenderer.updateCursorPosition(null, null);
        return;
      }

      // Get viewport transform
      const vpt = this.fabricCanvas.viewportTransform;
      if (!vpt) return;

      const zoom = vpt[0];
      const panX = vpt[4];
      const panY = vpt[5];

      // Convert screen coordinates to canvas coordinates
      const canvasX = (e.pointer.x - panX) / zoom;
      const canvasY = (e.pointer.y - panY) / zoom;

      this.rulerRenderer.updateCursorPosition(canvasX, canvasY);
    });

    // Clear cursor position when mouse leaves canvas
    const canvasElement = this.fabricCanvas.getElement();
    const container = canvasElement?.parentElement;
    if (container) {
      container.addEventListener('mouseleave', () => {
        this.rulerRenderer.updateCursorPosition(null, null);
        this.fabricCanvas.requestRenderAll();
      });
    }
  }

  /**
   * Show rulers
   */
  showRulers(): void {
    this.rulerRenderer.setVisible(true);
    this.fabricCanvas.requestRenderAll();
  }

  /**
   * Hide rulers
   */
  hideRulers(): void {
    this.rulerRenderer.setVisible(false);
    this.fabricCanvas.requestRenderAll();
  }

  /**
   * Toggle ruler visibility
   */
  toggleRulers(): void {
    this.rulerRenderer.toggle();
    this.fabricCanvas.requestRenderAll();
  }

  /**
   * Check if rulers are visible
   */
  isRulersVisible(): boolean {
    return this.rulerRenderer.isVisible();
  }

  /**
   * Destroy the canvas and clean up resources
   * Should be called before removing the canvas element from the DOM
   */
  destroy(): void {
    this.fabricCanvas.dispose();
  }
}
