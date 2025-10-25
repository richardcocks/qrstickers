/**
 * Image Element (placeholder for logos/images)
 */

import * as fabric from 'fabric';
import { BaseElement } from './BaseElement';
import type { ElementData } from './BaseElement';

export interface ImageElementData extends ElementData {
  type: 'image';
  src?: string;
  aspectRatio?: 'contain' | 'cover' | 'stretch';
  customImageId?: number;
  customImageName?: string;
}

export class ImageElement extends BaseElement {
  public src: string;
  public aspectRatio: 'contain' | 'cover' | 'stretch';
  public customImageId?: number;
  public customImageName?: string;

  constructor(data: Partial<ImageElementData> = {}) {
    super({ ...data, type: 'image' });
    this.src = data.src ?? '';
    this.aspectRatio = data.aspectRatio ?? 'contain';
    this.customImageId = data.customImageId;
    this.customImageName = data.customImageName;
    this.dataBinding = data.dataBinding ?? 'connection.CompanyLogoUrl';
  }

  createFabricObject(boundaryLeft: number, boundaryTop: number): any {
    const widthPx = this.mmToPx(this.width);
    const heightPx = this.mmToPx(this.height);
    const leftPx = this.mmToPx(this.x) + boundaryLeft;
    const topPx = this.mmToPx(this.y) + boundaryTop;

    const rect = new fabric.Rect({
      left: 0,
      top: 0,
      width: widthPx,
      height: heightPx,
      fill: '#f0f0f0',
      stroke: '#999',
      strokeWidth: 2,
      strokeDashArray: [5, 5],
    });

    const text = new fabric.Text('IMAGE', {
      left: widthPx / 2,
      top: heightPx / 2,
      fontSize: 14,
      fill: '#999',
      originX: 'center',
      originY: 'center',
    });

    const group = new fabric.Group([rect, text], {
      left: leftPx,
      top: topPx,
      selectable: true,
    });

    // Store custom properties
    (group as any).dataSource = this.dataBinding;
    (group as any).src = this.src;
    (group as any).aspectRatio = this.aspectRatio;
    (group as any).customImageId = this.customImageId;
    (group as any).customImageName = this.customImageName;
    (group as any).customType = 'image';

    return group;
  }

  updateFromFabricObject(boundaryLeft: number, boundaryTop: number): void {
    super.updateFromFabricObject(boundaryLeft, boundaryTop);
    if (this.fabricObject) {
      this.dataBinding = (this.fabricObject as any).dataSource;
      this.src = (this.fabricObject as any).src ?? '';
      this.aspectRatio = (this.fabricObject as any).aspectRatio ?? 'contain';
      this.customImageId = (this.fabricObject as any).customImageId;
      this.customImageName = (this.fabricObject as any).customImageName;
    }
  }

  toJSON(): ImageElementData {
    return {
      ...super.toJSON(),
      type: 'image',
      src: this.src,
      aspectRatio: this.aspectRatio,
      customImageId: this.customImageId,
      customImageName: this.customImageName,
    };
  }

  static fromJSON(data: ImageElementData): ImageElement {
    return new ImageElement(data);
  }
}
