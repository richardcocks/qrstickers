/**
 * Rectangle Element
 */

import * as fabric from 'fabric';
import { BaseElement } from './BaseElement';
import type { ElementData } from './BaseElement';

export interface RectElementData extends ElementData {
  type: 'rect';
  fill?: string;
  stroke?: string;
  strokeWidth?: number;
}

export class RectElement extends BaseElement {
  public fill: string;
  public stroke: string;
  public strokeWidth: number;

  constructor(data: Partial<RectElementData> = {}) {
    super({ ...data, type: 'rect' });
    this.fill = data.fill ?? 'transparent';
    this.stroke = data.stroke ?? '#333';
    this.strokeWidth = data.strokeWidth ?? 2;
  }

  createFabricObject(boundaryLeft: number, boundaryTop: number): any {
    // Use constant base size to prevent size compounding bug
    const BASE_WIDTH_MM = 50;
    const BASE_HEIGHT_MM = 30;
    const baseWidthPx = this.mmToPx(BASE_WIDTH_MM);
    const baseHeightPx = this.mmToPx(BASE_HEIGHT_MM);

    // Calculate scale to achieve desired size
    const scaleX = this.width / BASE_WIDTH_MM;
    const scaleY = this.height / BASE_HEIGHT_MM;

    const leftPx = this.mmToPx(this.x) + boundaryLeft;
    const topPx = this.mmToPx(this.y) + boundaryTop;

    const rect = new fabric.Rect({
      left: leftPx,
      top: topPx,
      width: baseWidthPx,
      height: baseHeightPx,
      scaleX: scaleX,
      scaleY: scaleY,
      fill: this.fill,
      stroke: this.stroke,
      strokeWidth: this.strokeWidth,
    });

    (rect as any).customType = 'rect';
    // Store base size to prevent compounding when recreating
    (rect as any).baseWidth = baseWidthPx;
    (rect as any).baseHeight = baseHeightPx;

    return rect;
  }

  updateFromFabricObject(boundaryLeft: number, boundaryTop: number): void {
    super.updateFromFabricObject(boundaryLeft, boundaryTop);
    if (this.fabricObject) {
      this.fill = this.fabricObject.fill;
      this.stroke = this.fabricObject.stroke;
      this.strokeWidth = this.fabricObject.strokeWidth;
    }
  }

  toJSON(): RectElementData {
    return {
      ...super.toJSON(),
      type: 'rect',
      fill: this.fill,
      stroke: this.stroke,
      strokeWidth: this.strokeWidth,
    };
  }

  static fromJSON(data: RectElementData): RectElement {
    return new RectElement(data);
  }
}
