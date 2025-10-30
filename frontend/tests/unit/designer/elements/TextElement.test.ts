/**
 * Unit tests for TextElement
 */

import { describe, it, expect, vi } from 'vitest';
import { TextElement } from '../../../../src/designer/elements/TextElement';

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

    it('should display placeholder value when dataBinding is set', () => {
      const element = new TextElement({
        text: 'Static Text',
        dataBinding: 'device.serial',
      });

      const fabricObject = element.createFabricObject(0, 0);

      // Should display placeholder value, not static text
      expect(fabricObject.text).toBe('MS-1234-ABCD-5678');
      expect(fabricObject.text).not.toBe('Static Text');
    });

    it('should display static text when dataBinding is not set', () => {
      const element = new TextElement({
        text: 'My Static Text',
      });

      const fabricObject = element.createFabricObject(0, 0);

      // Should display static text
      expect(fabricObject.text).toBe('My Static Text');
    });

    it('should make fabric object read-only when dataBinding is set', () => {
      const element = new TextElement({
        dataBinding: 'device.name',
      });

      const fabricObject = element.createFabricObject(0, 0);

      // Should be read-only (not editable)
      expect(fabricObject.editable).toBe(false);
    });

    it('should make fabric object editable when dataBinding is not set', () => {
      const element = new TextElement({
        text: 'Editable Text',
      });

      const fabricObject = element.createFabricObject(0, 0);

      // Should be editable
      expect(fabricObject.editable).toBe(true);
    });

    it('should use fallback format for unknown data bindings', () => {
      const element = new TextElement({
        dataBinding: 'custom.unknownField',
      });

      const fabricObject = element.createFabricObject(0, 0);

      // Should use fallback format: [binding]
      expect(fabricObject.text).toBe('[custom.unknownField]');
    });

    it('should preserve static text when updateFromFabricObject is called with dataBinding', () => {
      const element = new TextElement({
        text: 'Original Static Text',
        dataBinding: 'device.serial',
      });

      // Create fabric object (will show placeholder)
      const fabricObject = element.createFabricObject(0, 0);
      (element as any).fabricObject = fabricObject;

      // Simulate canvas update
      element.updateFromFabricObject(0, 0);

      // Static text should be preserved, not overwritten with placeholder
      expect(element.text).toBe('Original Static Text');
      expect(element.dataBinding).toBe('device.serial');
    });

    it('should update static text when updateFromFabricObject is called without dataBinding', () => {
      const element = new TextElement({
        text: 'Original Text',
      });

      // Create fabric object
      const fabricObject = element.createFabricObject(0, 0);

      // User edits text on canvas
      fabricObject.text = 'Edited Text';
      (element as any).fabricObject = fabricObject;

      // Update element from canvas
      element.updateFromFabricObject(0, 0);

      // Text should be updated from fabric object
      expect(element.text).toBe('Edited Text');
    });

    it('should store dataBinding in fabric object custom properties', () => {
      const element = new TextElement({
        dataBinding: 'device.model',
      });

      const fabricObject = element.createFabricObject(0, 0);

      // Should store dataBinding as dataSource
      expect((fabricObject as any).dataSource).toBe('device.model');
    });
  });
});
