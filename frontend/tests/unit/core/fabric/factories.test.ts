/**
 * Unit tests for Fabric.js factory functions
 */

import { describe, it, expect, beforeAll } from 'vitest';
import * as fabric from 'fabric';
import {
  createQRCode,
  createQRCodePlaceholder,
  createBoundText,
  createTextObject,
  createImagePlaceholder,
  createRectangle,
  createLine,
} from '../../../../src/core/fabric/factories';
import { registerQRCodeClass } from '../../../../src/core/fabric/QRCodeObject';

// Register QRCode class before tests
beforeAll(() => {
  registerQRCodeClass();
});

describe('factories', () => {
  describe('createQRCode', () => {
    it('should create QR code with default options', () => {
      const qrCode = createQRCode();

      expect(qrCode.type).toBe('qrcode');
      expect(qrCode.left).toBe(50);
      expect(qrCode.top).toBe(50);
      expect(qrCode.dataSource).toBe('device.Serial');
      expect(qrCode.eccLevel).toBe('Q');
    });

    it('should create QR code with custom options', () => {
      const qrCode = createQRCode({
        left: 100,
        top: 150,
        width: 200,
        dataSource: 'device.Name',
        eccLevel: 'H',
        quietZone: 4,
      });

      expect(qrCode.left).toBe(100);
      expect(qrCode.top).toBe(150);
      expect(qrCode.dataSource).toBe('device.Name');
      expect(qrCode.eccLevel).toBe('H');
      expect(qrCode.quietZone).toBe(4);
    });
  });

  describe('createQRCodePlaceholder', () => {
    it('should be an alias for createQRCode', () => {
      const qrCode = createQRCodePlaceholder({
        dataSource: 'device.MacAddress',
      });

      expect(qrCode.type).toBe('qrcode');
      expect(qrCode.dataSource).toBe('device.MacAddress');
    });
  });

  describe('createBoundText', () => {
    it('should create text with default options', () => {
      const text = createBoundText();

      expect(text.type).toBe('i-text');
      expect(text.text).toBe('Text');
      expect(text.left).toBe(50);
      expect(text.top).toBe(50);
      expect(text.fontFamily).toBe('Arial');
      expect(text.fontSize).toBe(16);
      expect(text.fill).toBe('#000000');
      expect(text.dataSource).toBe('');
      expect(text.maxLength).toBe(null);
      expect(text.overflow).toBe('truncate');
    });

    it('should create text with custom options', () => {
      const text = createBoundText({
        text: 'Device Name',
        left: 100,
        top: 200,
        fontFamily: 'Helvetica',
        fontSize: 24,
        fill: '#ff0000',
        fontWeight: 'bold',
        dataSource: 'device.Name',
        maxLength: 50,
        overflow: 'wrap',
      });

      expect(text.text).toBe('Device Name');
      expect(text.left).toBe(100);
      expect(text.top).toBe(200);
      expect(text.fontFamily).toBe('Helvetica');
      expect(text.fontSize).toBe(24);
      expect(text.fill).toBe('#ff0000');
      expect(text.fontWeight).toBe('bold');
      expect(text.dataSource).toBe('device.Name');
      expect(text.maxLength).toBe(50);
      expect(text.overflow).toBe('wrap');
    });

    it('should add custom properties to text object', () => {
      const text = createBoundText({
        dataSource: 'device.Model',
        maxLength: 30,
        overflow: 'scale',
      });

      expect(text.dataSource).toBe('device.Model');
      expect(text.maxLength).toBe(30);
      expect(text.overflow).toBe('scale');
    });
  });

  describe('createTextObject', () => {
    it('should be an alias for createBoundText', () => {
      const text = createTextObject({
        text: 'Test Text',
        dataSource: 'device.Serial',
      });

      expect(text.type).toBe('i-text');
      expect(text.text).toBe('Test Text');
      expect(text.dataSource).toBe('device.Serial');
    });
  });

  describe('createImagePlaceholder', () => {
    it('should create image placeholder with default options', () => {
      const image = createImagePlaceholder();

      expect(image.type).toBe('image');
      expect(image.left).toBe(50);
      expect(image.top).toBe(50);
      expect(image.dataSource).toBe('connection.CompanyLogoUrl');
      expect(image.src).toBe('');
      expect(image.aspectRatio).toBe('contain');
    });

    it('should create image placeholder with custom options', () => {
      const image = createImagePlaceholder({
        left: 100,
        top: 150,
        width: 200,
        height: 200,
        dataSource: 'custom.ImageUrl',
        src: '/images/logo.png',
        aspectRatio: 'cover',
      });

      expect(image.left).toBe(100);
      expect(image.top).toBe(150);
      expect(image.dataSource).toBe('custom.ImageUrl');
      expect(image.src).toBe('/images/logo.png');
      expect(image.aspectRatio).toBe('cover');
    });

    it('should create a group with rect and text', () => {
      const image = createImagePlaceholder();

      // Should contain rect + text = 2 objects
      expect(image.getObjects().length).toBe(2);
      expect(image.getObjects()[0].type).toBe('rect');
      expect(image.getObjects()[1].type).toBe('text');
    });

    it('should support custom image properties', () => {
      const image = createImagePlaceholder({
        customImageId: 42,
        customImageName: 'my-logo.png',
      });

      expect(image.customImageId).toBe(42);
      expect(image.customImageName).toBe('my-logo.png');
    });
  });

  describe('createRectangle', () => {
    it('should create rectangle with default options', () => {
      const rect = createRectangle();

      expect(rect.type).toBe('rect');
      expect(rect.left).toBe(50);
      expect(rect.top).toBe(50);
      expect(rect.width).toBe(100);
      expect(rect.height).toBe(50);
      expect(rect.fill).toBe('transparent');
      expect(rect.stroke).toBe('#333');
      expect(rect.strokeWidth).toBe(2);
    });

    it('should create rectangle with custom options', () => {
      const rect = createRectangle({
        left: 100,
        top: 200,
        width: 150,
        height: 75,
        fill: '#ff0000',
        stroke: '#0000ff',
        strokeWidth: 5,
      });

      expect(rect.left).toBe(100);
      expect(rect.top).toBe(200);
      expect(rect.width).toBe(150);
      expect(rect.height).toBe(75);
      expect(rect.fill).toBe('#ff0000');
      expect(rect.stroke).toBe('#0000ff');
      expect(rect.strokeWidth).toBe(5);
    });
  });

  describe('createLine', () => {
    it('should create line with default options', () => {
      const line = createLine();

      expect(line.type).toBe('line');
      expect(line.x1).toBe(50);
      expect(line.y1).toBe(50);
      expect(line.x2).toBe(150);
      expect(line.y2).toBe(50);
      expect(line.stroke).toBe('#333');
      expect(line.strokeWidth).toBe(2);
    });

    it('should create line with custom options', () => {
      const line = createLine({
        x1: 10,
        y1: 20,
        x2: 200,
        y2: 100,
        stroke: '#ff0000',
        strokeWidth: 3,
      });

      expect(line.x1).toBe(10);
      expect(line.y1).toBe(20);
      expect(line.x2).toBe(200);
      expect(line.y2).toBe(100);
      expect(line.stroke).toBe('#ff0000');
      expect(line.strokeWidth).toBe(3);
    });

    it('should create horizontal line by default', () => {
      const line = createLine();

      // Default line should be horizontal (same y coordinates)
      expect(line.y1).toBe(line.y2);
      expect(line.x1).not.toBe(line.x2);
    });
  });

  describe('type consistency', () => {
    it('should create objects with correct Fabric.js types', () => {
      const qrCode = createQRCode();
      const text = createBoundText();
      const image = createImagePlaceholder();
      const rect = createRectangle();
      const line = createLine();

      expect(qrCode).toBeInstanceOf(fabric.Group);
      expect(text).toBeInstanceOf(fabric.IText);
      expect(image).toBeInstanceOf(fabric.Group);
      expect(rect).toBeInstanceOf(fabric.Rect);
      expect(line).toBeInstanceOf(fabric.Line);
    });
  });
});
