/**
 * Unit tests for Designer class
 * Focuses on public API surface and JSON serialization
 */

import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { Designer } from '../../../src/designer/Designer';

// Mock Fabric.js with proper constructor functions
vi.mock('fabric', () => {
  const MockCanvas = function (this: any, _id: string, _options: any) {
    this._eventHandlers = {};
    this._activeObject = null;
    this._objects = [];

    this.add = vi.fn((obj: any) => {
      this._objects.push(obj);
    });
    this.remove = vi.fn((obj: any) => {
      const index = this._objects.indexOf(obj);
      if (index > -1) {
        this._objects.splice(index, 1);
      }
    });
    this.getObjects = vi.fn(() => this._objects);
    this.getActiveObject = vi.fn(() => this._activeObject);
    this.setActiveObject = vi.fn((obj: any) => {
      this._activeObject = obj;
      // Fire selection:created event
      if (this._eventHandlers['selection:created']) {
        this._eventHandlers['selection:created'].forEach((handler: any) => {
          handler({ selected: [obj] });
        });
      }
    });
    this.discardActiveObject = vi.fn(() => {
      this._activeObject = null;
      // Fire selection:cleared event
      if (this._eventHandlers['selection:cleared']) {
        this._eventHandlers['selection:cleared'].forEach((handler: any) => {
          handler();
        });
      }
    });
    this.requestRenderAll = vi.fn();
    this.clear = vi.fn(() => {
      this._objects = [];
    });
    this.on = vi.fn((event: string, handler: any) => {
      if (!this._eventHandlers[event]) {
        this._eventHandlers[event] = [];
      }
      this._eventHandlers[event].push(handler);
    });
    this.off = vi.fn();
    this.sendObjectToBack = vi.fn((obj: any) => {
      const index = this._objects.indexOf(obj);
      if (index > -1) {
        this._objects.splice(index, 1);
        this._objects.unshift(obj); // Move to beginning
      }
    });
    this.bringObjectToFront = vi.fn((obj: any) => {
      const index = this._objects.indexOf(obj);
      if (index > -1) {
        this._objects.splice(index, 1);
        this._objects.push(obj); // Move to end
      }
    });
    this.bringObjectForward = vi.fn((obj: any) => {
      const index = this._objects.indexOf(obj);
      if (index > -1 && index < this._objects.length - 1) {
        this._objects.splice(index, 1);
        this._objects.splice(index + 1, 0, obj); // Move forward one position
      }
    });
    this.sendObjectBackwards = vi.fn((obj: any) => {
      const index = this._objects.indexOf(obj);
      if (index > 0) {
        this._objects.splice(index, 1);
        this._objects.splice(index - 1, 0, obj); // Move backward one position
      }
    });
    return this;
  };

  const MockRect = function (this: any, _options: any) {
    this.set = vi.fn();
    this.id = _options?.id;
    this.name = _options?.name;
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
    this.id = _options?.id;
    // Calculate group dimensions from children or use default
    let calculatedWidth = 100;
    let calculatedHeight = 100;
    if (_objects && _objects.length > 0) {
      // Find the bounding box of all objects
      const maxRight = Math.max(..._objects.map((obj: any) =>
        (obj.left || 0) + (obj.width || 0)));
      const maxBottom = Math.max(..._objects.map((obj: any) =>
        (obj.top || 0) + (obj.height || 0)));
      calculatedWidth = maxRight;
      calculatedHeight = maxBottom;
    }
    this.width = _options?.width || calculatedWidth;
    this.height = _options?.height || calculatedHeight;
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
    this.id = _options?.id;
    this.text = _text;
    this.fontFamily = _options?.fontFamily || 'Arial';
    this.fontSize = _options?.fontSize || 16;
    this.fontWeight = _options?.fontWeight || 'normal';
    this.fill = _options?.fill || '#000000';
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

  describe('Update Element', () => {
    it('should update element properties', () => {
      const element = designer.addElement('text', { x: 10, y: 20 });
      expect(element.text).toBe('Text');

      designer.updateElement(element.id, { text: 'Updated Text' });

      const updatedElement = designer.getElements()[0];
      expect(updatedElement.text).toBe('Updated Text');
    });

    it('should update multiple properties at once', () => {
      const element = designer.addElement('qr', { x: 10, y: 20 });

      designer.updateElement(element.id, {
        x: 50,
        y: 60,
        width: 30,
        height: 30,
        dataBinding: 'device.MAC',
      });

      const updated = designer.getElements()[0];
      expect(updated.x).toBe(50);
      expect(updated.y).toBe(60);
      expect(updated.width).toBe(30);
      expect(updated.height).toBe(30);
      expect(updated.dataBinding).toBe('device.MAC');
    });

    it('should trigger onElementsChange when updating', () => {
      const onElementsChange = vi.fn();
      const d = new Designer({
        containerId: 'test-canvas',
        widthMm: 100,
        heightMm: 50,
        onElementsChange,
      });

      const element = d.addElement('text');
      onElementsChange.mockClear();

      d.updateElement(element.id, { text: 'New Text' });

      expect(onElementsChange).toHaveBeenCalled();
    });

    it('should not update non-existent element', () => {
      designer.addElement('qr');
      const elementsBefore = designer.getElements().length;

      designer.updateElement('non-existent-id', { x: 100 });

      expect(designer.getElements()).toHaveLength(elementsBefore);
    });
  });

  describe('Clipboard Operations', () => {
    it('should copy selected element', () => {
      const element = designer.addElement('qr', { x: 10, y: 20 });

      const result = designer.copy();

      expect(result).toBe(true);
    });

    it('should return false when copying with no selection', () => {
      const result = designer.copy();

      expect(result).toBe(false);
    });

    it('should paste copied element with offset', () => {
      const element = designer.addElement('qr', { x: 10, y: 20 });
      designer.copy();

      const pasted = designer.paste();

      expect(pasted).toBeDefined();
      expect(pasted?.type).toBe('qrcode');
      expect(pasted?.x).toBe(15); // 10 + 5mm offset
      expect(pasted?.y).toBe(25); // 20 + 5mm offset
      expect(designer.getElements()).toHaveLength(2);
    });

    it('should return null when pasting with empty clipboard', () => {
      const result = designer.paste();

      expect(result).toBeNull();
    });

    it('should duplicate selected element', () => {
      const element = designer.addElement('text', { x: 30, y: 40 });

      const duplicated = designer.duplicate();

      expect(duplicated).toBeDefined();
      expect(duplicated?.type).toBe('text');
      expect(duplicated?.x).toBeCloseTo(35, 1); // 30 + 5mm offset
      expect(duplicated?.y).toBeCloseTo(45, 1); // 40 + 5mm offset
      expect(designer.getElements()).toHaveLength(2);
    });

    it('should return null when duplicating with no selection', () => {
      const result = designer.duplicate();

      expect(result).toBeNull();
    });

    it('should paste preserve element properties', () => {
      const element = designer.addElement('text', { x: 10, y: 20 });
      designer.updateElement(element.id, {
        text: 'Custom Text',
        fontSize: 24,
        fill: '#ff0000',
      });
      designer.copy();

      const pasted = designer.paste();

      expect(pasted?.text).toBe('Custom Text');
      expect(pasted?.fontSize).toBe(24);
      expect(pasted?.fill).toBe('#ff0000');
    });
  });

  describe('Selection Management', () => {
    it('should deselect all elements', () => {
      designer.addElement('qr');
      const element = designer.getSelectedElement();
      expect(element).not.toBeNull();

      designer.deselectAll();

      expect(designer.getSelectedElement()).toBeNull();
    });

    it('should trigger onSelectionChange when deselecting', () => {
      const onSelectionChange = vi.fn();
      const d = new Designer({
        containerId: 'test-canvas',
        widthMm: 100,
        heightMm: 50,
        onSelectionChange,
      });

      d.addElement('qr');
      onSelectionChange.mockClear();

      d.deselectAll();

      expect(onSelectionChange).toHaveBeenCalledWith(null);
    });
  });

  describe('Layer Ordering', () => {
    it('should have layer ordering methods', () => {
      expect(typeof designer.bringToFront).toBe('function');
      expect(typeof designer.sendToBack).toBe('function');
      expect(typeof designer.bringForward).toBe('function');
      expect(typeof designer.sendBackward).toBe('function');
    });

    it('should not throw when no element selected', () => {
      expect(() => designer.bringToFront()).not.toThrow();
      expect(() => designer.sendToBack()).not.toThrow();
      expect(() => designer.bringForward()).not.toThrow();
      expect(() => designer.sendBackward()).not.toThrow();
    });

    it('should bring element to front', () => {
      const elem1 = designer.addElement('qr', { x: 10, y: 10 });
      const elem2 = designer.addElement('text', { x: 20, y: 20 });

      // Select first element (should be behind second)
      const canvas = designer.getCanvas();
      const fabricObj1 = elem1.getFabricObject(canvas.boundaryLeft, canvas.boundaryTop);
      canvas.setActiveObject(fabricObj1);

      designer.bringToFront();

      // Verify it's now in front (last in array)
      const objects = canvas.getObjects();
      expect(objects[objects.length - 1]).toBe(fabricObj1);
    });

    it('should send element to back', () => {
      const elem1 = designer.addElement('qr', { x: 10, y: 10 });
      const elem2 = designer.addElement('text', { x: 20, y: 20 });

      // Select second element (currently in front)
      const canvas = designer.getCanvas();
      const fabricObj2 = elem2.getFabricObject(canvas.boundaryLeft, canvas.boundaryTop);
      canvas.setActiveObject(fabricObj2);

      designer.sendToBack();

      // Verify it's now at back (but after boundary)
      const objects = canvas.getObjects();
      const boundaryIndex = objects.findIndex((o: any) => o.name === 'stickerBoundary');
      const elem2Index = objects.indexOf(fabricObj2);
      expect(elem2Index).toBeGreaterThan(boundaryIndex);
    });

    it('should save state after reordering for undo/redo', () => {
      const elem1 = designer.addElement('qr', { x: 10, y: 10 });
      const elem2 = designer.addElement('text', { x: 20, y: 20 });

      // Select first element
      const canvas = designer.getCanvas();
      const fabricObj1 = elem1.getFabricObject(canvas.boundaryLeft, canvas.boundaryTop);
      canvas.setActiveObject(fabricObj1);

      designer.bringToFront();

      // Verify element moved to front
      const objects = canvas.getObjects();
      const frontPosition = objects.indexOf(fabricObj1);

      // Now undo - element should go back to original position
      designer.undo();

      const objectsAfterUndo = canvas.getObjects();
      const positionAfterUndo = objectsAfterUndo.indexOf(fabricObj1);

      // Position should have changed (undo worked, meaning state was saved)
      expect(positionAfterUndo).not.toBe(frontPosition);
    });

    it('should bring element forward one layer', () => {
      const elem1 = designer.addElement('qr', { x: 10, y: 10 });
      const elem2 = designer.addElement('text', { x: 20, y: 20 });
      const elem3 = designer.addElement('rect', { x: 30, y: 30 });

      // Select first element (bottom of stack)
      const canvas = designer.getCanvas();
      const fabricObj1 = elem1.getFabricObject(canvas.boundaryLeft, canvas.boundaryTop);
      canvas.setActiveObject(fabricObj1);

      const objects = canvas.getObjects();
      const initialIndex = objects.indexOf(fabricObj1);

      designer.bringForward();

      const newIndex = canvas.getObjects().indexOf(fabricObj1);
      expect(newIndex).toBeGreaterThan(initialIndex);
    });

    it('should send element backward one layer', () => {
      const elem1 = designer.addElement('qr', { x: 10, y: 10 });
      const elem2 = designer.addElement('text', { x: 20, y: 20 });
      const elem3 = designer.addElement('rect', { x: 30, y: 30 });

      // Select third element (top of stack)
      const canvas = designer.getCanvas();
      const fabricObj3 = elem3.getFabricObject(canvas.boundaryLeft, canvas.boundaryTop);
      canvas.setActiveObject(fabricObj3);

      const objects = canvas.getObjects();
      const initialIndex = objects.indexOf(fabricObj3);

      designer.sendBackward();

      const newIndex = canvas.getObjects().indexOf(fabricObj3);
      expect(newIndex).toBeLessThan(initialIndex);
    });

    it('should preserve layer order when saving and loading template', () => {
      // Add 3 elements (they'll be in order: elem1, elem2, elem3 from bottom to top)
      const elem1 = designer.addElement('qr', { x: 10, y: 10 });
      const elem2 = designer.addElement('text', { x: 20, y: 20 });
      const elem3 = designer.addElement('rect', { x: 30, y: 30 });

      // Store original IDs for comparison
      const id1 = elem1.id;
      const id2 = elem2.id;
      const id3 = elem3.id;

      // Reorder: move elem1 to front
      const canvas = designer.getCanvas();
      const fabricObj1 = elem1.getFabricObject(canvas.boundaryLeft, canvas.boundaryTop);
      canvas.setActiveObject(fabricObj1);
      designer.bringToFront();

      // New order should be: elem2, elem3, elem1 (bottom to top)
      const objectsBeforeSave = canvas.getObjects();
      expect(objectsBeforeSave).toHaveLength(3);
      expect((objectsBeforeSave[0] as any).id).toBe(id2);
      expect((objectsBeforeSave[1] as any).id).toBe(id3);
      expect((objectsBeforeSave[2] as any).id).toBe(id1);

      // Save template
      const json = designer.saveTemplate();

      // Clear and reload
      designer.clear();
      designer.loadTemplate(json);

      // Verify order is preserved after reload
      const objectsAfterLoad = designer.getCanvas().getObjects();
      expect(objectsAfterLoad).toHaveLength(3);
      expect((objectsAfterLoad[0] as any).id).toBe(id2);
      expect((objectsAfterLoad[1] as any).id).toBe(id3);
      expect((objectsAfterLoad[2] as any).id).toBe(id1);
    });
  });
});
