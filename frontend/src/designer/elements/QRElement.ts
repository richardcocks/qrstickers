/**
 * QR Code Element
 */

import * as fabric from 'fabric';
import { BaseElement } from './BaseElement';
import type { ElementData } from './BaseElement';

export interface QRElementData extends ElementData {
  type: 'qrcode';
  eccLevel?: 'L' | 'M' | 'Q' | 'H';
  quietZone?: number;
}

export class QRElement extends BaseElement {
  public eccLevel: 'L' | 'M' | 'Q' | 'H';
  public quietZone: number;

  constructor(data: Partial<QRElementData> = {}) {
    super({ ...data, type: 'qrcode' });
    this.eccLevel = data.eccLevel ?? 'Q';
    this.quietZone = data.quietZone ?? 2;
    this.dataBinding = data.dataBinding ?? 'device.Serial';
  }

  createFabricObject(boundaryLeft: number, boundaryTop: number): any {
    const sizePx = this.mmToPx(this.width);
    const leftPx = this.mmToPx(this.x) + boundaryLeft;
    const topPx = this.mmToPx(this.y) + boundaryTop;

    // Create QR code visual placeholder
    const background = new fabric.Rect({
      width: sizePx,
      height: sizePx,
      fill: 'white',
      stroke: '#333',
      strokeWidth: 2,
    });

    // Create simple QR-like pattern
    const patternSize = sizePx / 5;
    const patterns = [
      // Top-left corner
      new fabric.Rect({
        left: patternSize * 0.3,
        top: patternSize * 0.3,
        width: patternSize * 0.8,
        height: patternSize * 0.8,
        fill: 'black',
      }),
      // Top-right corner
      new fabric.Rect({
        left: patternSize * 3.9,
        top: patternSize * 0.3,
        width: patternSize * 0.8,
        height: patternSize * 0.8,
        fill: 'black',
      }),
      // Bottom-left corner
      new fabric.Rect({
        left: patternSize * 0.3,
        top: patternSize * 3.9,
        width: patternSize * 0.8,
        height: patternSize * 0.8,
        fill: 'black',
      }),
      // Center pattern
      new fabric.Rect({
        left: patternSize * 2,
        top: patternSize * 2,
        width: patternSize,
        height: patternSize,
        fill: 'black',
      }),
      // QR label
      new fabric.Text('QR', {
        left: sizePx / 2,
        top: sizePx / 2,
        fontSize: sizePx / 4,
        fill: '#666',
        originX: 'center',
        originY: 'center',
        fontWeight: 'bold',
      }),
    ];

    const group = new fabric.Group([background, ...patterns], {
      left: leftPx,
      top: topPx,
      selectable: true,
    });

    // Store custom properties
    (group as any).dataSource = this.dataBinding;
    (group as any).eccLevel = this.eccLevel;
    (group as any).quietZone = this.quietZone;
    (group as any).customType = 'qrcode';

    return group;
  }

  updateFromFabricObject(boundaryLeft: number, boundaryTop: number): void {
    super.updateFromFabricObject(boundaryLeft, boundaryTop);
    if (this.fabricObject) {
      this.dataBinding = (this.fabricObject as any).dataSource;
      this.eccLevel = (this.fabricObject as any).eccLevel ?? 'Q';
      this.quietZone = (this.fabricObject as any).quietZone ?? 2;
    }
  }

  toJSON(): QRElementData {
    return {
      ...super.toJSON(),
      type: 'qrcode',
      eccLevel: this.eccLevel,
      quietZone: this.quietZone,
    };
  }

  static fromJSON(data: QRElementData): QRElement {
    return new QRElement(data);
  }
}
