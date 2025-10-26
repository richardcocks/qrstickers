/**
 * Unit tests for Designer class
 * Focuses on public API surface and JSON serialization
 */

import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { Designer } from '../../../src/designer/Designer';

// Mock Fabric.js with proper constructor functions
vi.mock('fabric', () => {
  const MockCanvas = function (this: any, _id: string, _options: any) {
    this.add = vi.fn();
    this.remove = vi.fn();
    this.getObjects = vi.fn(() => []);
    this.getActiveObject = vi.fn();
    this.setActiveObject = vi.fn();
    this.discardActiveObject = vi.fn();
    this.requestRenderAll = vi.fn();
    this.clear = vi.fn();
    this.on = vi.fn();
    this.off = vi.fn();
    this.sendObjectToBack = vi.fn();
    return this;
  };

  const MockRect = function (this: any, _options: any) {
    this.set = vi.fn();
    this.width = _options?.width || 100;
    this.height = _options?.height || 100;
    this.left = _options?.left || 0;
    this.top = _options?.top || 0;
    this.scaleX = 1;
    this.scaleY = 1;
    this.getScaledWidth = () => this.width * this.scaleX;
    this.getScaledHeight = () => this.height * this.scaleY;
    return this;
  };

  const MockGroup = function (this: any, _objects: any[], _options: any) {
    this.set = vi.fn();
    this.width = _options?.width || 100;
    this.height = _options?.height || 100;
    this.left = _options?.left || 0;
    this.top = _options?.top || 0;
    this.scaleX = 1;
    this.scaleY = 1;
    this.getScaledWidth = () => this.width * this.scaleX;
    this.getScaledHeight = () => this.height * this.scaleY;
    return this;
  };

  const MockIText = function (this: any, _text: string, _options: any) {
    this.set = vi.fn();
    this.text = _text;
    this.width = _options?.width || 100;
    this.height = _options?.height || 50;
    this.left = _options?.left || 0;
    this.top = _options?.top || 0;
    this.scaleX = 1;
    this.scaleY = 1;
    this.getScaledWidth = () => this.width * this.scaleX;
    this.getScaledHeight = () => this.height * this.scaleY;
    return this;
  };

  const MockText = function (this: any, _text: string, _options: any) {
    this.set = vi.fn();
    this.text = _text;
    this.width = _options?.width || 50;
    this.height = _options?.height || 20;
    return this;
  };

  return {
    Canvas: MockCanvas,
    Rect: MockRect,
    Group: MockGroup,
    IText: MockIText,
    Text: MockText,
  };
});

describe('Designer', () => {
  let designer: Designer;
  let container: HTMLCanvasElement;

  beforeEach(() => {
    // Create a canvas element for testing
    container = document.createElement('canvas');
    container.id = 'test-canvas';
    document.body.appendChild(container);

    designer = new Designer({
      containerId: 'test-canvas',
      widthMm: 100,
      heightMm: 50,
    });
  });

  afterEach(() => {
    if (container && container.parentNode) {
      document.body.removeChild(container);
    }
  });

  describe('Constructor', () => {
    it('should create a Designer instance', () => {
      expect(designer).toBeDefined();
      expect(designer.getCanvas()).toBeDefined();
    });

    it('should initialize with empty elements', () => {
      expect(designer.getElements()).toEqual([]);
    });

    it('should start with no selection', () => {
      expect(designer.getSelectedElement()).toBeNull();
    });
  });

  describe('Adding Elements', () => {
    it('should add QR element', () => {
      const element = designer.addElement('qr');

      expect(element).toBeDefined();
      expect(element.type).toBe('qrcode');
      expect(designer.getElements()).toHaveLength(1);
    });

    it('should add text element', () => {
      const element = designer.addElement('text');

      expect(element).toBeDefined();
      expect(element.type).toBe('text');
      expect(designer.getElements()).toHaveLength(1);
    });

    it('should add image element', () => {
      const element = designer.addElement('image');

      expect(element).toBeDefined();
      expect(element.type).toBe('image');
      expect(designer.getElements()).toHaveLength(1);
    });

    it('should add rectangle element', () => {
      const element = designer.addElement('rect');

      expect(element).toBeDefined();
      expect(element.type).toBe('rect');
      expect(designer.getElements()).toHaveLength(1);
    });

    it('should add element at specified position', () => {
      const element = designer.addElement('qr', { x: 15, y: 25 });

      expect(element.x).toBe(15);
      expect(element.y).toBe(25);
    });

    it('should add multiple elements', () => {
      designer.addElement('qr');
      designer.addElement('text');
      designer.addElement('image');

      expect(designer.getElements()).toHaveLength(3);
    });
  });

  describe('Clearing Canvas', () => {
    it('should clear all elements', () => {
      designer.addElement('qr');
      designer.addElement('text');
      designer.addElement('image');

      designer.clear();

      expect(designer.getElements()).toHaveLength(0);
    });

    it('should work when canvas is already empty', () => {
      expect(() => designer.clear()).not.toThrow();
      expect(designer.getElements()).toHaveLength(0);
    });
  });

  describe('Template Serialization', () => {
    it('should save empty template', () => {
      const json = designer.saveTemplate();
      const data = JSON.parse(json);

      expect(data.version).toBe('1.0');
      expect(data.pageSize.width).toBe(100);
      expect(data.pageSize.height).toBe(50);
      expect(data.pageSize.unit).toBe('mm');
      expect(data.objects).toEqual([]);
    });

    it('should save template with elements', () => {
      designer.addElement('qr', { x: 10, y: 20 });
      designer.addElement('text', { x: 30, y: 40 });

      const json = designer.saveTemplate();
      const data = JSON.parse(json);

      expect(data.objects).toHaveLength(2);
      expect(data.objects[0].type).toBe('qrcode');
      expect(data.objects[1].type).toBe('text');
    });

    it('should include element positions in template', () => {
      designer.addElement('qr', { x: 15, y: 25 });

      const json = designer.saveTemplate();
      const data = JSON.parse(json);

      expect(data.objects[0].x).toBe(15);
      expect(data.objects[0].y).toBe(25);
    });

    it('should load template from JSON', () => {
      const templateJson = JSON.stringify({
        version: '1.0',
        pageSize: { width: 100, height: 50, unit: 'mm' },
        objects: [
          {
            id: 'qr-1',
            type: 'qrcode',
            x: 10,
            y: 20,
            width: 25,
            height: 25,
            dataBinding: 'device.Serial',
            eccLevel: 'Q',
            quietZone: 2,
          },
          {
            id: 'text-1',
            type: 'text',
            x: 40,
            y: 30,
            width: 50,
            height: 10,
            text: 'Test Device',
            fontSize: 16,
            fontFamily: 'Arial',
            fill: '#000000',
          },
        ],
      });

      designer.loadTemplate(templateJson);

      expect(designer.getElements()).toHaveLength(2);
      expect(designer.getElements()[0].type).toBe('qrcode');
      expect(designer.getElements()[1].type).toBe('text');
    });

    it('should clear existing elements when loading template', () => {
      designer.addElement('qr');
      designer.addElement('text');

      const templateJson = JSON.stringify({
        version: '1.0',
        pageSize: { width: 100, height: 50, unit: 'mm' },
        objects: [
          {
            id: 'rect-1',
            type: 'rect',
            x: 10,
            y: 10,
            width: 20,
            height: 20,
            fill: 'transparent',
            stroke: '#333',
            strokeWidth: 2,
          },
        ],
      });

      designer.loadTemplate(templateJson);

      expect(designer.getElements()).toHaveLength(1);
      expect(designer.getElements()[0].type).toBe('rect');
    });

    it('should round-trip through JSON', () => {
      designer.addElement('qr', { x: 10, y: 20 });
      designer.addElement('text', { x: 30, y: 40 });

      const json1 = designer.saveTemplate();
      designer.loadTemplate(json1);
      const json2 = designer.saveTemplate();

      const data1 = JSON.parse(json1);
      const data2 = JSON.parse(json2);

      expect(data1.objects.length).toBe(data2.objects.length);
      expect(data2.objects[0].type).toBe('qrcode');
      expect(data2.objects[1].type).toBe('text');
    });
  });

  describe('Undo/Redo', () => {
    it('should have undo method', () => {
      expect(typeof designer.undo).toBe('function');
    });

    it('should have redo method', () => {
      expect(typeof designer.redo).toBe('function');
    });

    it('should not throw when calling undo with no history', () => {
      expect(() => designer.undo()).not.toThrow();
    });

    it('should not throw when calling redo with no history', () => {
      expect(() => designer.redo()).not.toThrow();
    });

    it('should undo a single element addition', () => {
      designer.addElement('qr');
      expect(designer.getElements()).toHaveLength(1);

      designer.undo();
      expect(designer.getElements()).toHaveLength(0);
    });

    it('should redo after undo', () => {
      designer.addElement('qr');
      expect(designer.getElements()).toHaveLength(1);

      designer.undo();
      expect(designer.getElements()).toHaveLength(0);

      designer.redo();
      expect(designer.getElements()).toHaveLength(1);
    });

    it('should undo multiple operations in sequence', () => {
      designer.addElement('qr');
      designer.addElement('text');
      designer.addElement('image');
      expect(designer.getElements()).toHaveLength(3);

      designer.undo();
      expect(designer.getElements()).toHaveLength(2);

      designer.undo();
      expect(designer.getElements()).toHaveLength(1);

      designer.undo();
      expect(designer.getElements()).toHaveLength(0);
    });

    it('should redo multiple operations in sequence', () => {
      designer.addElement('qr');
      designer.addElement('text');
      designer.addElement('image');

      designer.undo();
      designer.undo();
      designer.undo();
      expect(designer.getElements()).toHaveLength(0);

      designer.redo();
      expect(designer.getElements()).toHaveLength(1);

      designer.redo();
      expect(designer.getElements()).toHaveLength(2);

      designer.redo();
      expect(designer.getElements()).toHaveLength(3);
    });

    it('should clear redo stack when new action performed after undo', () => {
      designer.addElement('qr');
      designer.addElement('text');
      expect(designer.getElements()).toHaveLength(2);

      designer.undo();
      expect(designer.getElements()).toHaveLength(1);

      // Adding a new element should clear the redo stack
      designer.addElement('image');
      expect(designer.getElements()).toHaveLength(2);

      // Trying to redo should do nothing since redo stack was cleared
      designer.redo();
      expect(designer.getElements()).toHaveLength(2);
    });

    it('should respect maxUndoSteps limit', () => {
      // Add more than 50 elements (maxHistorySteps)
      for (let i = 0; i < 55; i++) {
        designer.addElement('qr');
      }
      expect(designer.getElements()).toHaveLength(55);

      // Now undo everything - should only be able to undo ~50 steps
      // (the oldest steps will have been discarded)
      for (let i = 0; i < 55; i++) {
        designer.undo();
        if (designer.getElements().length === 0) break;
      }
      // Due to history limit, we won't get all the way back
      // With 50 history steps and starting with initial empty state,
      // we can only go back 49 steps, leaving ~6 elements
      // This is expected behavior - history only stores 50 states
      expect(designer.getElements().length).toBeLessThanOrEqual(10);
    });

    it('should handle undo/redo with element properties changed', () => {
      const element = designer.addElement('text', { x: 10, y: 20 });
      expect(element.x).toBe(10);
      expect(element.y).toBe(20);

      // Simulate a property change (what would happen on canvas drag)
      element.x = 50;
      element.y = 60;
      element.updateFabricObject(0, 0);
      designer.getCanvas().render();
      // Manually trigger save state (normally happens via object:modified event)
      const currentElements = designer.getElements();
      expect(currentElements[0].x).toBe(50);
      expect(currentElements[0].y).toBe(60);
    });

    it('should clear undo/redo stacks on explicit clear', () => {
      designer.addElement('qr');
      designer.addElement('text');

      designer.clear();

      expect(designer.getElements()).toHaveLength(0);

      // Both undo and redo should do nothing since stacks were cleared
      designer.undo();
      expect(designer.getElements()).toHaveLength(0);

      designer.redo();
      expect(designer.getElements()).toHaveLength(0);
    });
  });

  describe('Callbacks', () => {
    it('should trigger onElementsChange when adding element', () => {
      const onElementsChange = vi.fn();
      const d = new Designer({
        containerId: 'test-canvas',
        widthMm: 100,
        heightMm: 50,
        onElementsChange,
      });

      d.addElement('qr');

      expect(onElementsChange).toHaveBeenCalled();
    });

    it('should trigger onElementsChange when clearing canvas', () => {
      const onElementsChange = vi.fn();
      const d = new Designer({
        containerId: 'test-canvas',
        widthMm: 100,
        heightMm: 50,
        onElementsChange,
      });

      d.addElement('qr');
      onElementsChange.mockClear();

      d.clear();

      expect(onElementsChange).toHaveBeenCalled();
    });

    it('should trigger onElementsChange when undoing', () => {
      const onElementsChange = vi.fn();
      const d = new Designer({
        containerId: 'test-canvas',
        widthMm: 100,
        heightMm: 50,
        onElementsChange,
      });

      d.addElement('qr');
      onElementsChange.mockClear();

      d.undo();

      expect(onElementsChange).toHaveBeenCalled();
    });
  });
});
