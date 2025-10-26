/**
 * Canvas Wrapper - Hides Fabric.js complexity
 * Simple, clean API for canvas operations
 */

import { Canvas, FabricObject, Rect, version as fabricVersion } from 'fabric';
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
  private boundaryRect: Rect;

  public readonly widthMm: number;
  public readonly heightMm: number;
  public readonly boundaryLeft: number;
  public readonly boundaryTop: number;

  constructor(config: CanvasConfig) {
    this.widthMm = config.widthMm;
    this.heightMm = config.heightMm;

    // Calculate dimensions
    const stickerWidthPx = mmToPx(config.widthMm);
    const stickerHeightPx = mmToPx(config.heightMm);
    const marginTop = config.marginTop ?? 100;
    const marginLeft = config.marginLeft ?? 100;
    const marginBottom = config.marginBottom ?? 500;
    const marginRight = config.marginRight ?? 500;

    const canvasWidth = stickerWidthPx + marginLeft + marginRight;
    const canvasHeight = stickerHeightPx + marginTop + marginBottom;

    this.boundaryLeft = marginLeft;
    this.boundaryTop = marginTop;

    // Create Fabric canvas
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

    // Create boundary rectangle
    this.boundaryRect = new Rect({
      left: marginLeft,
      top: marginTop,
      width: stickerWidthPx,
      height: stickerHeightPx,
      fill: 'white',
      stroke: '#999',
      strokeWidth: 2,
      strokeDashArray: [10, 5],
      selectable: false,
      evented: false,
      excludeFromExport: true,
      name: 'stickerBoundary',
    });

    this.fabricCanvas.add(this.boundaryRect);
    this.fabricCanvas.sendObjectToBack(this.boundaryRect);
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
   * Get all objects on canvas (excluding boundary)
   */
  getObjects(): FabricObject[] {
    return this.fabricCanvas.getObjects().filter((obj) => (obj as any).name !== 'stickerBoundary');
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
    // Ensure boundary stays at back
    this.fabricCanvas.sendObjectToBack(this.boundaryRect);
    this.fabricCanvas.requestRenderAll();
  }

  /**
   * Send object to back (bottom layer, but above boundary)
   */
  sendToBack(fabricObject: FabricObject): void {
    this.fabricCanvas.sendObjectToBack(fabricObject);
    // Ensure boundary stays at back
    this.fabricCanvas.sendObjectToBack(this.boundaryRect);
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
    // Ensure object doesn't go behind boundary
    const boundaryIndex = this.fabricCanvas.getObjects().indexOf(this.boundaryRect);
    const objectIndex = this.fabricCanvas.getObjects().indexOf(fabricObject);
    if (objectIndex <= boundaryIndex) {
      this.fabricCanvas.bringObjectForward(fabricObject);
    }
    this.fabricCanvas.requestRenderAll();
  }

  /**
   * Clear all objects (except boundary)
   */
  clear(): void {
    const boundary = this.boundaryRect;
    this.fabricCanvas.clear();
    this.fabricCanvas.add(boundary);
    this.fabricCanvas.sendObjectToBack(boundary);
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
}
