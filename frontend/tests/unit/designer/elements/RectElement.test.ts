/**
 * Unit tests for RectElement
 */

import { describe, it, expect, vi } from 'vitest';
import { RectElement } from '../../../../src/designer/elements/RectElement';

// Mock Fabric.js
vi.mock('fabric', () => ({
  Rect: vi.fn(() => ({
    set: vi.fn(),
  })),
}));

describe('RectElement', () => {
  describe('Constructor', () => {
    it('should create RectElement with default values', () => {
      const element = new RectElement({});

      expect(element.type).toBe('rect');
      expect(element.x).toBe(0);
      expect(element.y).toBe(0);
      expect(element.width).toBe(50); // BaseElement default
      expect(element.height).toBe(50); // BaseElement default
      expect(element.fill).toBe('transparent');
      expect(element.stroke).toBe('#333');
      expect(element.strokeWidth).toBe(2);
    });

    it('should create RectElement with custom values', () => {
      const element = new RectElement({
        x: 10,
        y: 20,
        width: 50,
        height: 40,
        fill: '#FF0000',
        stroke: '#000000',
        strokeWidth: 2,
      });

      expect(element.x).toBe(10);
      expect(element.y).toBe(20);
      expect(element.width).toBe(50);
      expect(element.height).toBe(40);
      expect(element.fill).toBe('#FF0000');
      expect(element.stroke).toBe('#000000');
      expect(element.strokeWidth).toBe(2);
    });

    it('should generate unique ID', () => {
      const element1 = new RectElement({});
      const element2 = new RectElement({});

      expect(element1.id).toBeDefined();
      expect(element2.id).toBeDefined();
      expect(element1.id).not.toBe(element2.id);
    });
  });

  describe('JSON Serialization', () => {
    it('should serialize to JSON', () => {
      const element = new RectElement({
        x: 15,
        y: 25,
        width: 60,
        height: 45,
        fill: '#00FF00',
        stroke: '#0000FF',
        strokeWidth: 3,
      });

      const json = element.toJSON();

      expect(json.type).toBe('rect');
      expect(json.x).toBe(15);
      expect(json.y).toBe(25);
      expect(json.width).toBe(60);
      expect(json.height).toBe(45);
      expect(json.fill).toBe('#00FF00');
      expect(json.stroke).toBe('#0000FF');
      expect(json.strokeWidth).toBe(3);
    });

    it('should deserialize from JSON', () => {
      const json = {
        id: 'rect-1',
        type: 'rect',
        x: 20,
        y: 30,
        width: 70,
        height: 50,
        fill: '#FFFF00',
        stroke: '#FF00FF',
        strokeWidth: 4,
      };

      const element = RectElement.fromJSON(json);

      expect(element.id).toBe('rect-1');
      expect(element.x).toBe(20);
      expect(element.y).toBe(30);
      expect(element.width).toBe(70);
      expect(element.height).toBe(50);
      expect(element.fill).toBe('#FFFF00');
      expect(element.stroke).toBe('#FF00FF');
      expect(element.strokeWidth).toBe(4);
    });

    it('should round-trip through JSON', () => {
      const original = new RectElement({
        x: 10,
        y: 15,
        width: 40,
        height: 30,
        fill: '#123456',
        stroke: '#654321',
        strokeWidth: 2,
      });

      const json = original.toJSON();
      const restored = RectElement.fromJSON(json);

      expect(restored.x).toBe(original.x);
      expect(restored.y).toBe(original.y);
      expect(restored.width).toBe(original.width);
      expect(restored.height).toBe(original.height);
      expect(restored.fill).toBe(original.fill);
      expect(restored.stroke).toBe(original.stroke);
      expect(restored.strokeWidth).toBe(original.strokeWidth);
    });
  });

  describe('Style Properties', () => {
    it('should update fill color', () => {
      const element = new RectElement({ fill: '#FFFFFF' });
      element.fill = '#000000';
      expect(element.fill).toBe('#000000');
    });

    it('should update stroke color', () => {
      const element = new RectElement({ stroke: '#FFFFFF' });
      element.stroke = '#000000';
      expect(element.stroke).toBe('#000000');
    });

    it('should update stroke width', () => {
      const element = new RectElement({ strokeWidth: 1 });
      element.strokeWidth = 5;
      expect(element.strokeWidth).toBe(5);
    });
  });

  describe('Dimensions', () => {
    it('should update width', () => {
      const element = new RectElement({ width: 30 });
      element.width = 60;
      expect(element.width).toBe(60);
    });

    it('should update height', () => {
      const element = new RectElement({ height: 20 });
      element.height = 40;
      expect(element.height).toBe(40);
    });
  });
});
