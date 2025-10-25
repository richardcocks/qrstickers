/**
 * Unit tests for QRElement
 */

import { describe, it, expect, vi } from 'vitest';
import { QRElement } from '../../../../src/designer/elements/QRElement';

// Mock Fabric.js
vi.mock('fabric', () => ({
  Rect: vi.fn(() => ({
    set: vi.fn(),
  })),
  Group: vi.fn(() => ({
    set: vi.fn(),
  })),
  Text: vi.fn(() => ({
    set: vi.fn(),
  })),
}));

describe('QRElement', () => {
  describe('Constructor', () => {
    it('should create QRElement with default values', () => {
      const element = new QRElement({});

      expect(element.type).toBe('qrcode');
      expect(element.x).toBe(0);
      expect(element.y).toBe(0);
      expect(element.width).toBe(50); // BaseElement default
      expect(element.height).toBe(50); // BaseElement default
      expect(element.eccLevel).toBe('Q');
      expect(element.quietZone).toBe(2);
      expect(element.dataBinding).toBe('device.Serial');
    });

    it('should create QRElement with custom values', () => {
      const element = new QRElement({
        x: 10,
        y: 20,
        width: 30,
        height: 30,
        dataBinding: 'device.Serial',
        eccLevel: 'H',
        quietZone: 4,
      });

      expect(element.x).toBe(10);
      expect(element.y).toBe(20);
      expect(element.width).toBe(30);
      expect(element.height).toBe(30);
      expect(element.dataBinding).toBe('device.Serial');
      expect(element.eccLevel).toBe('H');
      expect(element.quietZone).toBe(4);
    });

    it('should generate unique ID', () => {
      const element1 = new QRElement({});
      const element2 = new QRElement({});

      expect(element1.id).toBeDefined();
      expect(element2.id).toBeDefined();
      expect(element1.id).not.toBe(element2.id);
    });

    it('should use provided ID if given', () => {
      const element = new QRElement({ id: 'custom-id' });
      expect(element.id).toBe('custom-id');
    });
  });

  describe('JSON Serialization', () => {
    it('should serialize to JSON', () => {
      const element = new QRElement({
        x: 10,
        y: 20,
        width: 25,
        height: 25,
        dataBinding: 'device.Serial',
        eccLevel: 'Q',
        quietZone: 2,
      });

      const json = element.toJSON();

      expect(json.type).toBe('qrcode');
      expect(json.x).toBe(10);
      expect(json.y).toBe(20);
      expect(json.width).toBe(25);
      expect(json.height).toBe(25);
      expect(json.dataBinding).toBe('device.Serial');
      expect(json.eccLevel).toBe('Q');
      expect(json.quietZone).toBe(2);
    });

    it('should deserialize from JSON', () => {
      const json = {
        id: 'test-id',
        type: 'qrcode' as const,
        x: 10,
        y: 20,
        width: 30,
        height: 30,
        dataBinding: 'device.MAC',
        eccLevel: 'H' as const,
        quietZone: 4,
      };

      const element = QRElement.fromJSON(json);

      expect(element.id).toBe('test-id');
      expect(element.type).toBe('qrcode');
      expect(element.x).toBe(10);
      expect(element.y).toBe(20);
      expect(element.width).toBe(30);
      expect(element.height).toBe(30);
      expect(element.dataBinding).toBe('device.MAC');
      expect(element.eccLevel).toBe('H');
      expect(element.quietZone).toBe(4);
    });

    it('should round-trip through JSON', () => {
      const original = new QRElement({
        x: 15,
        y: 25,
        width: 30,
        height: 30,
        dataBinding: 'device.Serial',
        eccLevel: 'M',
        quietZone: 3,
      });

      const json = original.toJSON();
      const restored = QRElement.fromJSON(json);

      expect(restored.x).toBe(original.x);
      expect(restored.y).toBe(original.y);
      expect(restored.width).toBe(original.width);
      expect(restored.height).toBe(original.height);
      expect(restored.dataBinding).toBe(original.dataBinding);
      expect(restored.eccLevel).toBe(original.eccLevel);
      expect(restored.quietZone).toBe(original.quietZone);
    });
  });

  describe('ECC Level Validation', () => {
    it('should accept valid ECC levels', () => {
      const levels: Array<'L' | 'M' | 'Q' | 'H'> = ['L', 'M', 'Q', 'H'];

      levels.forEach((level) => {
        const element = new QRElement({ eccLevel: level });
        expect(element.eccLevel).toBe(level);
      });
    });
  });

  describe('Data Binding', () => {
    it('should store data binding value', () => {
      const element = new QRElement({ dataBinding: 'device.Serial' });
      expect(element.dataBinding).toBe('device.Serial');
    });

    it('should have default data binding', () => {
      const element = new QRElement({});
      expect(element.dataBinding).toBe('device.Serial');
    });

    it('should update data binding', () => {
      const element = new QRElement({ dataBinding: 'device.Serial' });
      element.dataBinding = 'device.MAC';
      expect(element.dataBinding).toBe('device.MAC');
    });
  });
});
