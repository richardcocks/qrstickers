/**
 * Factory functions for creating Fabric.js objects
 * Provides convenient helpers for creating designer elements
 */

import * as fabric from 'fabric';
import { QRCode } from './QRCodeObject';
import type {
  QRCodeOptions,
  BoundTextOptions,
  ImagePlaceholderOptions,
  RectangleOptions,
  LineOptions,
  FabricBoundText,
  FabricImagePlaceholder,
} from './types';

/**
 * Create a QR code object
 */
export function createQRCode(options: QRCodeOptions = {}): QRCode {
  return new QRCode({
    left: options.left ?? 50,
    top: options.top ?? 50,
    width: options.width ?? 100,
    height: options.height ?? 100,
    dataSource: options.dataSource ?? 'device.Serial',
    eccLevel: options.eccLevel ?? 'Q',
    quietZone: options.quietZone ?? 2,
  });
}

/**
 * Alias for createQRCode - used by export-preview.js
 * Maintains backwards compatibility with existing code
 */
export function createQRCodePlaceholder(options: QRCodeOptions = {}): QRCode {
  return createQRCode(options);
}

/**
 * Create a text object with data binding
 */
export function createBoundText(options: BoundTextOptions = {}): FabricBoundText {
  const text = new fabric.IText(options.text ?? 'Text', {
    left: options.left ?? 50,
    top: options.top ?? 50,
    fontFamily: options.fontFamily ?? 'Arial',
    fontSize: options.fontSize ?? 16,
    fill: options.fill ?? '#000000',
    fontWeight: options.fontWeight ?? 'normal',
  }) as FabricBoundText;

  // Add custom data binding properties
  text.dataSource = options.dataSource ?? '';
  text.maxLength = options.maxLength ?? null;
  text.overflow = options.overflow ?? 'truncate';

  return text;
}

/**
 * Alias for createBoundText - used by export-preview.js
 * Maintains backwards compatibility with existing code
 */
export function createTextObject(options: BoundTextOptions = {}): FabricBoundText {
  return createBoundText(options);
}

/**
 * Create an image placeholder
 */
export function createImagePlaceholder(options: ImagePlaceholderOptions = {}): FabricImagePlaceholder {
  const left = options.left ?? 50;
  const top = options.top ?? 50;
  const width = options.width ?? 100;
  const height = options.height ?? 100;

  const rect = new fabric.Rect({
    left: 0,
    top: 0,
    width,
    height,
    fill: '#f0f0f0',
    stroke: '#999',
    strokeWidth: 2,
    strokeDashArray: [5, 5],
  });

  const text = new fabric.Text('IMAGE', {
    left: width / 2,
    top: height / 2,
    fontSize: 14,
    fill: '#999',
    originX: 'center',
    originY: 'center',
  });

  const group = new fabric.Group([rect, text], {
    left,
    top,
    selectable: true,
  }) as FabricImagePlaceholder;

  // Add custom properties
  group.dataSource = options.dataSource ?? 'connection.CompanyLogoUrl';
  group.src = options.src ?? '';
  group.aspectRatio = options.aspectRatio ?? 'contain';

  // Optional custom image properties
  if (options.customImageId) {
    group.customImageId = options.customImageId;
  }
  if (options.customImageName) {
    group.customImageName = options.customImageName;
  }

  // Set type using defineProperty to prevent Fabric.js from overwriting it
  Object.defineProperty(group, 'type', {
    value: 'image',
    writable: false,
    enumerable: true,
    configurable: true,
  });

  return group;
}

/**
 * Create a rectangle
 */
export function createRectangle(options: RectangleOptions = {}): fabric.Rect {
  return new fabric.Rect({
    left: options.left ?? 50,
    top: options.top ?? 50,
    width: options.width ?? 100,
    height: options.height ?? 50,
    fill: options.fill ?? 'transparent',
    stroke: options.stroke ?? '#333',
    strokeWidth: options.strokeWidth ?? 2,
  });
}

/**
 * Create a line
 */
export function createLine(options: LineOptions = {}): fabric.Line {
  return new fabric.Line(
    [
      options.x1 ?? 50,
      options.y1 ?? 50,
      options.x2 ?? 150,
      options.y2 ?? 50,
    ],
    {
      stroke: options.stroke ?? '#333',
      strokeWidth: options.strokeWidth ?? 2,
    }
  );
}
