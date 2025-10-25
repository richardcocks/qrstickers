/**
 * Custom QR Code object for Fabric.js
 * Renders as a placeholder group with QR code pattern
 */

import * as fabric from 'fabric';
import type { FabricQRCode, QRCodeProperties } from './types';

/**
 * Options for QRCode constructor
 */
export interface QRCodeConstructorOptions extends fabric.IGroupOptions {
  /** Data source for QR code content */
  dataSource?: string;
  /** Error correction level */
  eccLevel?: 'L' | 'M' | 'Q' | 'H';
  /** Quiet zone size in modules */
  quietZone?: number;
  /** Width of QR code */
  width?: number;
  /** Height of QR code */
  height?: number;
}

/**
 * Custom QRCode class extending fabric.Group
 * Creates a visual placeholder for QR codes in the designer
 */
export class QRCode extends fabric.Group implements FabricQRCode {
  declare type: 'qrcode';
  declare dataSource: string;
  declare eccLevel: 'L' | 'M' | 'Q' | 'H';
  declare quietZone: number;

  /**
   * Create a new QRCode object
   */
  constructor(options: QRCodeConstructorOptions = {}) {
    // Default size
    const size = options.width || 100;

    // Create QR code visual placeholder
    const background = new fabric.Rect({
      width: size,
      height: size,
      fill: 'white',
      stroke: '#333',
      strokeWidth: 2,
    });

    // Create simple QR-like pattern
    const patternSize = size / 5;
    const patterns = [
      // Top-left corner pattern
      new fabric.Rect({
        left: patternSize * 0.3,
        top: patternSize * 0.3,
        width: patternSize * 0.8,
        height: patternSize * 0.8,
        fill: 'black',
      }),
      // Top-right corner pattern
      new fabric.Rect({
        left: patternSize * 3.9,
        top: patternSize * 0.3,
        width: patternSize * 0.8,
        height: patternSize * 0.8,
        fill: 'black',
      }),
      // Bottom-left corner pattern
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
      // QR label text
      new fabric.Text('QR', {
        left: size / 2,
        top: size / 2,
        fontSize: size / 4,
        fill: '#666',
        originX: 'center',
        originY: 'center',
        fontWeight: 'bold',
      }),
    ];

    const objects = [background, ...patterns];

    // Call parent constructor
    super(objects, options);

    // Set custom properties with defaults
    this.dataSource = options.dataSource || 'device.Serial';
    this.eccLevel = options.eccLevel || 'Q';
    this.quietZone = options.quietZone || 2;

    // Set type AFTER calling super and setting properties
    // Use defineProperty to make it non-writable so Fabric.js doesn't overwrite it
    Object.defineProperty(this, 'type', {
      value: 'qrcode',
      writable: false,
      enumerable: true,
      configurable: true,
    });
  }

  /**
   * Serialize object to JSON
   */
  toObject(propertiesToInclude: string[] = []): any {
    return {
      ...super.toObject(propertiesToInclude),
      type: 'qrcode',
      dataSource: this.dataSource,
      eccLevel: this.eccLevel,
      quietZone: this.quietZone,
    };
  }

  /**
   * Create QRCode from serialized object
   */
  static fromObject(object: any, callback?: (obj: QRCode) => void): QRCode {
    const qrCode = new QRCode({
      ...object,
      dataSource: object.dataSource,
      eccLevel: object.eccLevel,
      quietZone: object.quietZone,
    });

    if (callback) {
      callback(qrCode);
    }

    return qrCode;
  }
}

/**
 * Register QRCode class with Fabric.js
 * This allows Fabric.js to deserialize QRCode objects from JSON
 */
export function registerQRCodeClass(): void {
  // Try to register class for deserialization if classRegistry exists
  // Fabric.js v6.x structure varies by build
  if (fabric.util && (fabric.util as any).classRegistry) {
    (fabric.util as any).classRegistry.setClass(QRCode, 'qrcode');
  }

  // Make QRCode available globally for backwards compatibility
  // This allows window.createQRCode() and fabric.QRCode to work
  if (typeof window !== 'undefined') {
    (window as any).FabricQRCode = QRCode;
  }
}

/**
 * Get QRCode properties for serialization
 */
export function getQRCodeProperties(qrCode: QRCode): QRCodeProperties {
  return {
    dataSource: qrCode.dataSource,
    eccLevel: qrCode.eccLevel,
    quietZone: qrCode.quietZone,
  };
}
