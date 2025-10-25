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
    const widthPx = this.mmToPx(this.width);
    const heightPx = this.mmToPx(this.height);
    const leftPx = this.mmToPx(this.x) + boundaryLeft;
    const topPx = this.mmToPx(this.y) + boundaryTop;

    const rect = new fabric.Rect({
      left: leftPx,
      top: topPx,
      width: widthPx,
      height: heightPx,
      fill: this.fill,
      stroke: this.stroke,
      strokeWidth: this.strokeWidth,
    });

    (rect as any).customType = 'rect';

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
