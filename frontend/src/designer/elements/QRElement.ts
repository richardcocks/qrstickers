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
    // Use constant base size to prevent size compounding bug
    const BASE_SIZE_MM = 25;
    const baseSizePx = this.mmToPx(BASE_SIZE_MM);

    // Calculate scale to achieve desired size
    const scaleX = this.width / BASE_SIZE_MM;
    const scaleY = this.height / BASE_SIZE_MM;

    const leftPx = this.mmToPx(this.x) + boundaryLeft;
    const topPx = this.mmToPx(this.y) + boundaryTop;

    // Create QR code visual placeholder at base size
    const background = new fabric.Rect({
      width: baseSizePx,
      height: baseSizePx,
      fill: 'white',
      stroke: '#333',
      strokeWidth: 2,
    });

    // Create simple QR-like pattern at base size
    const patternSize = baseSizePx / 5;
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
        left: baseSizePx / 2,
        top: baseSizePx / 2,
        fontSize: baseSizePx / 4,
        fill: '#666',
        originX: 'center',
        originY: 'center',
        fontWeight: 'bold',
      }),
    ];

    const group = new fabric.Group([background, ...patterns], {
      left: leftPx,
      top: topPx,
      scaleX: scaleX,
      scaleY: scaleY,
      selectable: true,
    });

    // Store custom properties
    (group as any).dataSource = this.dataBinding;
    (group as any).eccLevel = this.eccLevel;
    (group as any).quietZone = this.quietZone;
    (group as any).customType = 'qrcode';
    // Store base size to prevent compounding when recreating
    (group as any).baseWidth = baseSizePx;
    (group as any).baseHeight = baseSizePx;

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
