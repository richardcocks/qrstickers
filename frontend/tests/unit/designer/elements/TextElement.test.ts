/**
 * Unit tests for TextElement
 */

import { describe, it, expect, vi } from 'vitest';
import { TextElement } from '../../../../src/designer/elements/TextElement';

// Mock Fabric.js
vi.mock('fabric', () => ({
  IText: vi.fn(() => ({
    set: vi.fn(),
  })),
}));

describe('TextElement', () => {
  describe('Constructor', () => {
    it('should create TextElement with default values', () => {
      const element = new TextElement({});

      expect(element.type).toBe('text');
      expect(element.x).toBe(0);
      expect(element.y).toBe(0);
      expect(element.width).toBe(50); // BaseElement default
      expect(element.height).toBe(50); // BaseElement default
      expect(element.text).toBe('Text');
      expect(element.fontSize).toBe(16);
      expect(element.fontFamily).toBe('Arial');
      expect(element.fill).toBe('#000000');
    });

    it('should create TextElement with custom values', () => {
      const element = new TextElement({
        x: 10,
        y: 20,
        width: 60,
        height: 15,
        text: 'Custom Text',
        fontSize: 16,
        fontFamily: 'Helvetica',
        fill: '#FF0000',
        dataBinding: 'device.Name',
      });

      expect(element.x).toBe(10);
      expect(element.y).toBe(20);
      expect(element.width).toBe(60);
      expect(element.height).toBe(15);
      expect(element.text).toBe('Custom Text');
      expect(element.fontSize).toBe(16);
      expect(element.fontFamily).toBe('Helvetica');
      expect(element.fill).toBe('#FF0000');
      expect(element.dataBinding).toBe('device.Name');
    });

    it('should generate unique ID', () => {
      const element1 = new TextElement({});
      const element2 = new TextElement({});

      expect(element1.id).toBeDefined();
      expect(element2.id).toBeDefined();
      expect(element1.id).not.toBe(element2.id);
    });
  });

  describe('JSON Serialization', () => {
    it('should serialize to JSON', () => {
      const element = new TextElement({
        x: 10,
        y: 20,
        width: 60,
        height: 15,
        text: 'Test Text',
        fontSize: 14,
        fontFamily: 'Helvetica',
        fill: '#333333',
        dataBinding: 'device.Name',
      });

      const json = element.toJSON();

      expect(json.type).toBe('text');
      expect(json.x).toBe(10);
      expect(json.y).toBe(20);
      expect(json.width).toBe(60);
      expect(json.height).toBe(15);
      expect(json.text).toBe('Test Text');
      expect(json.fontSize).toBe(14);
      expect(json.fontFamily).toBe('Helvetica');
      expect(json.fill).toBe('#333333');
      expect(json.dataBinding).toBe('device.Name');
    });

    it('should deserialize from JSON', () => {
      const json = {
        id: 'text-1',
        type: 'text',
        x: 15,
        y: 25,
        width: 70,
        height: 20,
        text: 'Restored Text',
        fontSize: 18,
        fontFamily: 'Times New Roman',
        fill: '#0000FF',
        dataBinding: 'device.Serial',
      };

      const element = TextElement.fromJSON(json);

      expect(element.id).toBe('text-1');
      expect(element.x).toBe(15);
      expect(element.y).toBe(25);
      expect(element.text).toBe('Restored Text');
      expect(element.fontSize).toBe(18);
      expect(element.fontFamily).toBe('Times New Roman');
      expect(element.fill).toBe('#0000FF');
      expect(element.dataBinding).toBe('device.Serial');
    });

    it('should round-trip through JSON', () => {
      const original = new TextElement({
        text: 'Round Trip',
        fontSize: 16,
        fontFamily: 'Courier',
        fill: '#FF00FF',
      });

      const json = original.toJSON();
      const restored = TextElement.fromJSON(json);

      expect(restored.text).toBe(original.text);
      expect(restored.fontSize).toBe(original.fontSize);
      expect(restored.fontFamily).toBe(original.fontFamily);
      expect(restored.fill).toBe(original.fill);
    });
  });

  describe('Text Properties', () => {
    it('should update text content', () => {
      const element = new TextElement({ text: 'Initial' });
      element.text = 'Updated';
      expect(element.text).toBe('Updated');
    });

    it('should update font size', () => {
      const element = new TextElement({ fontSize: 12 });
      element.fontSize = 24;
      expect(element.fontSize).toBe(24);
    });

    it('should update font family', () => {
      const element = new TextElement({ fontFamily: 'Arial' });
      element.fontFamily = 'Verdana';
      expect(element.fontFamily).toBe('Verdana');
    });

    it('should update fill color', () => {
      const element = new TextElement({ fill: '#000000' });
      element.fill = '#FFFFFF';
      expect(element.fill).toBe('#FFFFFF');
    });
  });

  describe('Data Binding', () => {
    it('should support data binding', () => {
      const element = new TextElement({ dataBinding: 'device.Name' });
      expect(element.dataBinding).toBe('device.Name');
    });

    it('should allow undefined data binding', () => {
      const element = new TextElement({});
      expect(element.dataBinding).toBeUndefined();
    });
  });
});
