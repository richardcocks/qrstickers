/**
 * Text Element
 */

import * as fabric from 'fabric';
import { BaseElement } from './BaseElement';
import type { ElementData } from './BaseElement';

export interface TextElementData extends ElementData {
  type: 'text';
  text?: string;
  fontFamily?: string;
  fontSize?: number;
  fontWeight?: string | number;
  fill?: string;
  maxLength?: number | null;
  overflow?: 'truncate' | 'wrap' | 'scale';
}

export class TextElement extends BaseElement {
  public text: string;
  public fontFamily: string;
  public fontSize: number;
  public fontWeight: string | number;
  public fill: string;
  public maxLength: number | null;
  public overflow: 'truncate' | 'wrap' | 'scale';

  constructor(data: Partial<TextElementData> = {}) {
    super({ ...data, type: 'text' });
    this.text = data.text ?? 'Text';
    this.fontFamily = data.fontFamily ?? 'Arial';
    this.fontSize = data.fontSize ?? 16;
    this.fontWeight = data.fontWeight ?? 'normal';
    this.fill = data.fill ?? '#000000';
    this.maxLength = data.maxLength ?? null;
    this.overflow = data.overflow ?? 'truncate';
  }

  createFabricObject(boundaryLeft: number, boundaryTop: number): any {
    const leftPx = this.mmToPx(this.x) + boundaryLeft;
    const topPx = this.mmToPx(this.y) + boundaryTop;

    const text = new fabric.IText(this.text, {
      left: leftPx,
      top: topPx,
      fontFamily: this.fontFamily,
      fontSize: this.fontSize,
      fill: this.fill,
      fontWeight: this.fontWeight,
    });

    // Store custom properties
    (text as any).dataSource = this.dataBinding;
    (text as any).maxLength = this.maxLength;
    (text as any).overflow = this.overflow;
    (text as any).customType = 'text';

    return text;
  }

  updateFromFabricObject(boundaryLeft: number, boundaryTop: number): void {
    super.updateFromFabricObject(boundaryLeft, boundaryTop);
    if (this.fabricObject) {
      this.text = this.fabricObject.text;
      this.fontFamily = this.fabricObject.fontFamily;
      this.fontSize = this.fabricObject.fontSize;
      this.fontWeight = this.fabricObject.fontWeight;
      this.fill = this.fabricObject.fill;
      this.dataBinding = (this.fabricObject as any).dataSource;
      this.maxLength = (this.fabricObject as any).maxLength;
      this.overflow = (this.fabricObject as any).overflow ?? 'truncate';
    }
  }

  toJSON(): TextElementData {
    return {
      ...super.toJSON(),
      type: 'text',
      text: this.text,
      fontFamily: this.fontFamily,
      fontSize: this.fontSize,
      fontWeight: this.fontWeight,
      fill: this.fill,
      maxLength: this.maxLength,
      overflow: this.overflow,
    };
  }

  static fromJSON(data: TextElementData): TextElement {
    return new TextElement(data);
  }
}
