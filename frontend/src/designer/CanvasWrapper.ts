/**
 * Canvas Wrapper - Hides Fabric.js complexity
 * Simple, clean API for canvas operations
 */

import { Canvas, FabricObject, version as fabricVersion } from 'fabric';
import { mmToPx, pxToMm } from '../utils/units';

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
  private isPanningEnabled: boolean = false;
  private gridSpacingMm: number = 2.5;
  private lastPanX = 0;
  private lastPanY = 0;
  private isPanning = false;
  private currentZoom: number = 1;
  private debug: boolean = true; // Set to false to disable debug logging

  // Zoom limits
  private readonly MIN_ZOOM = 0.1;
  private readonly MAX_ZOOM = 5.0;

  public readonly widthMm: number;
  public readonly heightMm: number;
  public readonly boundaryLeft: number;
  public readonly boundaryTop: number;
  public readonly canvasWidth: number;
  public readonly canvasHeight: number;
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
    // Get container element to determine canvas dimensions
    const containerEl = document.getElementById(config.containerId)?.parentElement;
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
    });

    // Style selection
    this.fabricCanvas.selectionColor = 'rgba(25, 118, 210, 0.1)';
    this.fabricCanvas.selectionBorderColor = '#1976d2';
    this.fabricCanvas.selectionLineWidth = 2;

    // Set up grid and boundary rendering via after:render event
    this.setupGridRendering();

    // Enable mouse wheel zoom
    this.enableMouseWheelZoom();
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
    });
  }

  /**
   * Render grid dots on canvas overlay
   * Uses canvas context to draw grid dynamically based on viewport
   */
  private renderGrid(): void {
    if (!this.gridVisible) return;

    const ctx = this.fabricCanvas.getContext('2d');
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
    ctx.fillStyle = '#ddd';
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
    const ctx = this.fabricCanvas.getContext('2d');
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

    // Draw white background for sticker area (transformed to screen coords)
    const screenLeft = this.boundaryLeft * zoom + panX;
    const screenTop = this.boundaryTop * zoom + panY;
    const screenWidth = this.stickerWidthPx * zoom;
    const screenHeight = this.stickerHeightPx * zoom;

    ctx.fillStyle = 'white';
    ctx.fillRect(screenLeft, screenTop, screenWidth, screenHeight);

    // Draw boundary lines extending full canvas dimensions
    ctx.strokeStyle = '#cccccc';
    ctx.lineWidth = 2;
    ctx.setLineDash([10, 5]);

    // Calculate boundary edge positions in screen coordinates
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

    if (this.debug) console.log('[CanvasWrapper] panning enabled');

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

    // Add pan drag handlers
    (this.fabricCanvas as any).on('mouse:down', this.handlePanStart.bind(this));
    (this.fabricCanvas as any).on('mouse:move', this.handlePanMove.bind(this));
    (this.fabricCanvas as any).on('mouse:up', this.handlePanEnd.bind(this));
  }

  /**
   * Disable panning mode - re-enable selection
   */
  disablePanning(): void {
    if (!this.isPanningEnabled) return;
    this.isPanningEnabled = false;

    if (this.debug) console.log('[CanvasWrapper] panning disabled');

    // Remove pan drag handlers
    (this.fabricCanvas as any).off('mouse:down', this.handlePanStart.bind(this));
    (this.fabricCanvas as any).off('mouse:move', this.handlePanMove.bind(this));
    (this.fabricCanvas as any).off('mouse:up', this.handlePanEnd.bind(this));

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
    if (!vpt) {
      if (this.debug) console.log('[CanvasWrapper] pan: viewportTransform is null');
      return;
    }

    if (this.debug) console.log('[CanvasWrapper] pan input:', { deltaX, deltaY });

    const newPanX = vpt[4] + deltaX;
    const newPanY = vpt[5] + deltaY;

    if (this.debug) console.log('[CanvasWrapper] pan before limits:', { panX: newPanX, panY: newPanY });

    // Enforce pan limits (keep boundary partially visible)
    const limits = this.getPanLimits();

    vpt[4] = Math.max(limits.minX, Math.min(limits.maxX, newPanX));
    vpt[5] = Math.max(limits.minY, Math.min(limits.maxY, newPanY));

    if (this.debug) console.log('[CanvasWrapper] pan after clamping:', { panX: vpt[4], panY: vpt[5], limits });

    this.fabricCanvas.requestRenderAll();
  }

  /**
   * Reset view to center on sticker boundary at 1:1 zoom
   */
  resetView(): void {
    const vpt = this.fabricCanvas.viewportTransform;
    if (!vpt) {
      if (this.debug) console.log('[CanvasWrapper] resetView: viewportTransform is null');
      return;
    }

    // Get actual visible container size
    const container = this.fabricCanvas.getElement().parentElement;
    const rect = container?.getBoundingClientRect();
    const containerWidth = rect?.width || 800;
    const containerHeight = rect?.height || 600;

    if (this.debug) console.log('[CanvasWrapper] resetView:', {
      containerSize: { width: containerWidth, height: containerHeight },
      boundaryPos: { left: this.boundaryLeft, top: this.boundaryTop },
      stickerSize: { width: this.stickerWidthPx, height: this.stickerHeightPx },
      viewportTransformBefore: vpt.slice(),
      zoomBefore: this.currentZoom
    });

    // Reset zoom to 1:1
    vpt[0] = 1; // scaleX
    vpt[3] = 1; // scaleY
    this.currentZoom = 1;

    // Center the boundary (at 0,0) in the viewport
    // Pan = (containerSize - stickerSize) / 2
    vpt[4] = (containerWidth - this.stickerWidthPx) / 2;
    vpt[5] = (containerHeight - this.stickerHeightPx) / 2;

    if (this.debug) console.log('[CanvasWrapper] resetView calculated:', {
      viewportTransform: vpt.slice(),
      zoom: this.currentZoom,
      panX: vpt[4],
      panY: vpt[5]
    });

    this.fabricCanvas.requestRenderAll();
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

    if (this.debug) console.log('[CanvasWrapper] getPanLimits:', {
      containerSize: { width: containerWidth, height: containerHeight },
      zoom: this.currentZoom,
      stickerSize: { width: this.stickerWidthPx, height: this.stickerHeightPx },
      scaledSize: { width: scaledStickerWidth, height: scaledStickerHeight },
      limits: { minX, maxX, minY, maxY }
    });

    return { minX, maxX, minY, maxY };
  }

  /**
   * Pan drag handlers - private methods for mouse events
   */
  private handlePanStart(e: any): void {
    if (!this.isPanningEnabled) return;

    this.isPanning = true;
    this.lastPanX = e.pointer.x;
    this.lastPanY = e.pointer.y;

    if (this.debug) console.log('[CanvasWrapper] pan start:', { x: this.lastPanX, y: this.lastPanY });

    // Change cursor to grabbing
    const container = this.fabricCanvas.getElement().parentElement;
    if (container) {
      container.style.cursor = 'grabbing';
    }
  }

  private handlePanMove(e: any): void {
    if (!this.isPanning || !this.isPanningEnabled) return;

    const deltaX = e.pointer.x - this.lastPanX;
    const deltaY = e.pointer.y - this.lastPanY;

    if (this.debug) console.log('[CanvasWrapper] pan move:', { pointerX: e.pointer.x, pointerY: e.pointer.y, deltaX, deltaY });

    this.pan(deltaX, deltaY);

    this.lastPanX = e.pointer.x;
    this.lastPanY = e.pointer.y;
  }

  private handlePanEnd(): void {
    if (!this.isPanning) return;

    this.isPanning = false;

    if (this.debug) console.log('[CanvasWrapper] pan end');

    // Change cursor back to grab
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
    if (!vpt) {
      if (this.debug) console.log('[CanvasWrapper] setZoom: viewportTransform is null');
      return;
    }

    // Clamp scale to limits
    const newZoom = Math.max(this.MIN_ZOOM, Math.min(this.MAX_ZOOM, scale));
    const oldZoom = this.currentZoom;

    if (this.debug) console.log('[CanvasWrapper] setZoom input:', {
      requestedScale: scale,
      clampedScale: newZoom,
      oldZoom,
      centerX,
      centerY
    });

    // If no center point provided, use viewport center
    if (centerX === undefined || centerY === undefined) {
      const container = this.fabricCanvas.getElement().parentElement;
      const rect = container?.getBoundingClientRect();
      const containerWidth = rect?.width || 800;
      const containerHeight = rect?.height || 600;
      centerX = containerWidth / 2;
      centerY = containerHeight / 2;
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

    if (this.debug) console.log('[CanvasWrapper] setZoom result:', {
      zoom: newZoom,
      viewportTransform: vpt.slice(),
      panX: vpt[4],
      panY: vpt[5],
      limits
    });

    this.fabricCanvas.requestRenderAll();
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
    if (!vpt) {
      if (this.debug) console.log('[CanvasWrapper] zoomToFit: viewportTransform is null');
      return;
    }

    const container = this.fabricCanvas.getElement().parentElement;
    const rect = container?.getBoundingClientRect();
    const containerWidth = rect?.width || 800;
    const containerHeight = rect?.height || 600;

    // Calculate zoom to fit with some padding (90% of available space)
    const padding = 0.9;
    const zoomX = (containerWidth * padding) / this.stickerWidthPx;
    const zoomY = (containerHeight * padding) / this.stickerHeightPx;

    // Use the smaller zoom to ensure entire sticker fits
    const fitZoom = Math.min(zoomX, zoomY);

    if (this.debug) console.log('[CanvasWrapper] zoomToFit:', {
      containerSize: { width: containerWidth, height: containerHeight },
      stickerSize: { width: this.stickerWidthPx, height: this.stickerHeightPx },
      calculatedZoom: { x: zoomX, y: zoomY, final: fitZoom }
    });

    // Set zoom level
    vpt[0] = fitZoom; // scaleX
    vpt[3] = fitZoom; // scaleY
    this.currentZoom = fitZoom;

    // Center the boundary (at 0,0) in the viewport at the new zoom level
    // Pan = (containerSize - stickerSize * zoom) / 2
    vpt[4] = (containerWidth - this.stickerWidthPx * fitZoom) / 2;
    vpt[5] = (containerHeight - this.stickerHeightPx * fitZoom) / 2;

    if (this.debug) console.log('[CanvasWrapper] zoomToFit result:', {
      zoom: fitZoom,
      viewportTransform: vpt.slice(),
      panX: vpt[4],
      panY: vpt[5]
    });

    this.fabricCanvas.requestRenderAll();
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

    if (this.debug) console.log('[CanvasWrapper] mouse wheel zoom:', {
      deltaY: delta,
      zoomFactor,
      oldZoom: this.currentZoom,
      newZoom,
      mousePos: { x: mouseX, y: mouseY }
    });

    // Zoom centered on mouse cursor
    this.setZoom(newZoom, mouseX, mouseY);
  }
}
