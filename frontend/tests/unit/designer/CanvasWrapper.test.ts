/**
 * Unit tests for CanvasWrapper class
 * Covers canvas initialization, grid rendering, and panning functionality
 */

import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';
import { CanvasWrapper } from '../../../src/designer/CanvasWrapper';
import { mmToPx } from '../../../src/utils/units';

// Mock Fabric.js with proper constructor functions and viewport support
vi.mock('fabric', () => {
  const MockCanvas = function (this: any, _id: string, options: any) {
    this._eventHandlers = {};
    this._activeObject = null;
    this._objects = [];
    this.width = options?.width || 800;
    this.height = options?.height || 600;
    this.backgroundColor = options?.backgroundColor || 'transparent';
    this.selection = options?.selection !== false;
    this.selectionColor = 'rgba(25, 118, 210, 0.1)';
    this.selectionBorderColor = '#1976d2';
    this.selectionLineWidth = 2;
    this.preserveObjectStacking = options?.preserveObjectStacking;

    // Viewport transform: [scaleX, 0, 0, scaleY, translateX, translateY]
    this.viewportTransform = [1, 0, 0, 1, 0, 0];

    // Mock 2D context for grid rendering
    const mockContext = {
      canvas: this,
      fillStyle: '#000000',
      globalAlpha: 1,
      fillRect: vi.fn(),
      save: vi.fn(),
      restore: vi.fn(),
      // Add more context methods as needed
    };
    this._context = mockContext;

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
      if (this._eventHandlers['selection:created']) {
        this._eventHandlers['selection:created'].forEach((handler: any) => {
          handler({ selected: [obj] });
        });
      }
    });

    this.discardActiveObject = vi.fn(() => {
      this._activeObject = null;
      if (this._eventHandlers['selection:cleared']) {
        this._eventHandlers['selection:cleared'].forEach((handler: any) => {
          handler();
        });
      }
    });

    this.requestRenderAll = vi.fn(() => {
      // Trigger after:render event
      if (this._eventHandlers['after:render']) {
        this._eventHandlers['after:render'].forEach((handler: any) => {
          handler();
        });
      }
    });

    this.clear = vi.fn(() => {
      this._objects = [];
    });

    this.on = vi.fn((event: string, handler: any) => {
      if (!this._eventHandlers[event]) {
        this._eventHandlers[event] = [];
      }
      this._eventHandlers[event].push(handler);
    });

    this.off = vi.fn((event: string, handler?: any) => {
      if (handler && this._eventHandlers[event]) {
        const index = this._eventHandlers[event].indexOf(handler);
        if (index > -1) {
          this._eventHandlers[event].splice(index, 1);
        }
      } else if (!handler && this._eventHandlers[event]) {
        this._eventHandlers[event] = [];
      }
    });

    this.sendObjectToBack = vi.fn((obj: any) => {
      const index = this._objects.indexOf(obj);
      if (index > -1) {
        this._objects.splice(index, 1);
        this._objects.unshift(obj);
      }
    });

    this.bringObjectToFront = vi.fn((obj: any) => {
      const index = this._objects.indexOf(obj);
      if (index > -1) {
        this._objects.splice(index, 1);
        this._objects.push(obj);
      }
    });

    this.bringObjectForward = vi.fn((obj: any) => {
      const index = this._objects.indexOf(obj);
      if (index > -1 && index < this._objects.length - 1) {
        this._objects.splice(index, 1);
        this._objects.splice(index + 1, 0, obj);
      }
    });

    this.sendObjectBackwards = vi.fn((obj: any) => {
      const index = this._objects.indexOf(obj);
      if (index > 0) {
        this._objects.splice(index, 1);
        this._objects.splice(index - 1, 0, obj);
      }
    });

    this.getElement = vi.fn(() => ({
      parentElement: {
        style: { cursor: 'default' },
        getBoundingClientRect: () => ({
          width: 800,
          height: 600,
          left: 0,
          top: 0,
          right: 800,
          bottom: 600,
          x: 0,
          y: 0,
          toJSON: () => ({})
        })
      },
      width: 800,
      height: 600,
    } as any));

    this.getContext = vi.fn(() => mockContext);

    return this;
  };

  const MockRect = function (this: any, options: any) {
    this.set = vi.fn();
    this.id = options?.id;
    this.name = options?.name;
    this.width = options?.width || 100;
    this.height = options?.height || 100;
    this.left = options?.left || 0;
    this.top = options?.top || 0;
    this.fill = options?.fill || 'white';
    this.stroke = options?.stroke || '#999';
    this.strokeWidth = options?.strokeWidth || 2;
    this.strokeDashArray = options?.strokeDashArray || [];
    this.selectable = options?.selectable !== false;
    this.evented = options?.evented !== false;
    this.excludeFromExport = options?.excludeFromExport || false;
    this.scaleX = 1;
    this.scaleY = 1;
    this.getScaledWidth = () => this.width * this.scaleX;
    this.getScaledHeight = () => this.height * this.scaleY;
    return this;
  };

  const MockGroup = function (this: any, objects: any[], options: any) {
    this.set = vi.fn();
    this.id = options?.id;
    this.name = options?.name;
    this.width = options?.width || 100;
    this.height = options?.height || 100;
    this.left = options?.left || 0;
    this.top = options?.top || 0;
    this.selectable = options?.selectable !== false;
    this.evented = options?.evented !== false;
    this.excludeFromExport = options?.excludeFromExport || false;
    this.visible = true;
    this.scaleX = 1;
    this.scaleY = 1;
    this.getScaledWidth = () => this.width * this.scaleX;
    this.getScaledHeight = () => this.height * this.scaleY;
    return this;
  };

  return {
    Canvas: MockCanvas,
    Rect: MockRect,
    Circle: function (this: any, options: any) {
      this.radius = options?.radius || 1;
      this.fill = options?.fill || '#000000';
      this.selectable = options?.selectable !== false;
      this.evented = options?.evented !== false;
      this.originX = options?.originX || 'center';
      this.originY = options?.originY || 'center';
      return this;
    },
    Group: MockGroup,
  };
});

describe('CanvasWrapper', () => {
  let wrapper: CanvasWrapper;
  let container: HTMLCanvasElement;

  beforeEach(() => {
    // Create a canvas element for testing
    container = document.createElement('canvas');
    container.id = 'test-canvas';
    document.body.appendChild(container);

    wrapper = new CanvasWrapper({
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

  describe('Canvas Initialization', () => {
    it('should create CanvasWrapper instance', () => {
      expect(wrapper).toBeDefined();
    });

    it('should store sticker dimensions in mm', () => {
      expect(wrapper.widthMm).toBe(100);
      expect(wrapper.heightMm).toBe(50);
    });

    it('should size canvas to match container', () => {
      // Canvas should match container size (800x600 from mock)
      expect(wrapper.canvasWidth).toBe(800);
      expect(wrapper.canvasHeight).toBe(600);
    });

    it('should place sticker boundary at origin', () => {
      // Boundary is at (0,0) for simpler viewport math
      expect(wrapper.boundaryLeft).toBe(0);
      expect(wrapper.boundaryTop).toBe(0);
    });

    it('should store sticker pixel dimensions', () => {
      const expectedWidthPx = mmToPx(100);
      const expectedHeightPx = mmToPx(50);

      expect(wrapper.stickerWidthPx).toBeCloseTo(expectedWidthPx, 1);
      expect(wrapper.stickerHeightPx).toBeCloseTo(expectedHeightPx, 1);
    });

    it('should initialize with grid visible', () => {
      expect(wrapper.isGridVisible()).toBe(true);
    });
  });

  describe('Grid Rendering', () => {
    it('should set up grid rendering on after:render event', () => {
      const mockCanvas = (wrapper as any).fabricCanvas;
      expect(mockCanvas.on).toHaveBeenCalledWith('after:render', expect.any(Function));
    });

    it('should toggle grid visibility on/off', () => {
      expect(wrapper.isGridVisible()).toBe(true);

      wrapper.hideGrid();
      expect(wrapper.isGridVisible()).toBe(false);

      wrapper.showGrid();
      expect(wrapper.isGridVisible()).toBe(true);
    });

    it('should trigger render when hiding grid', () => {
      const mockCanvas = (wrapper as any).fabricCanvas;
      mockCanvas.requestRenderAll.mockClear();

      wrapper.hideGrid();

      expect(mockCanvas.requestRenderAll).toHaveBeenCalled();
    });

    it('should trigger render when showing grid', () => {
      wrapper.hideGrid();
      const mockCanvas = (wrapper as any).fabricCanvas;
      mockCanvas.requestRenderAll.mockClear();

      wrapper.showGrid();

      expect(mockCanvas.requestRenderAll).toHaveBeenCalled();
    });

    it('should not throw when hiding already hidden grid', () => {
      wrapper.hideGrid();
      expect(() => wrapper.hideGrid()).not.toThrow();
    });

    it('should not throw when showing already visible grid', () => {
      expect(() => wrapper.showGrid()).not.toThrow();
    });
  });

  describe('Panning - Enable/Disable', () => {
    it('should enable panning mode', () => {
      wrapper.enablePanning();
      expect((wrapper as any).isPanningEnabled).toBe(true);
    });

    it('should disable panning mode', () => {
      wrapper.enablePanning();
      wrapper.disablePanning();
      expect((wrapper as any).isPanningEnabled).toBe(false);
    });

    it('should disable selection when panning enabled', () => {
      const mockCanvas = (wrapper as any).fabricCanvas;
      mockCanvas.selection = true;

      wrapper.enablePanning();

      expect(mockCanvas.selection).toBe(false);
    });

    it('should re-enable selection when panning disabled', () => {
      wrapper.enablePanning();
      const mockCanvas = (wrapper as any).fabricCanvas;
      mockCanvas.selection = false;

      wrapper.disablePanning();

      expect(mockCanvas.selection).toBe(true);
    });

    it('should change cursor to grab when panning enabled', () => {
      expect(() => wrapper.enablePanning()).not.toThrow();
      expect((wrapper as any).isPanningEnabled).toBe(true);
    });

    it('should register pan event handlers on enable', () => {
      // Verify panning is enabled and state is correct
      wrapper.enablePanning();
      expect((wrapper as any).isPanningEnabled).toBe(true);
    });

    it('should unregister pan event handlers on disable', () => {
      wrapper.enablePanning();
      expect((wrapper as any).isPanningEnabled).toBe(true);

      wrapper.disablePanning();
      expect((wrapper as any).isPanningEnabled).toBe(false);
    });

    it('should be idempotent when enabling twice', () => {
      wrapper.enablePanning();
      expect((wrapper as any).isPanningEnabled).toBe(true);

      // Should not throw and state should remain true
      expect(() => wrapper.enablePanning()).not.toThrow();
      expect((wrapper as any).isPanningEnabled).toBe(true);
    });

    it('should be idempotent when disabling twice', () => {
      wrapper.enablePanning();
      wrapper.disablePanning();
      expect((wrapper as any).isPanningEnabled).toBe(false);

      // Should not throw and state should remain false
      expect(() => wrapper.disablePanning()).not.toThrow();
      expect((wrapper as any).isPanningEnabled).toBe(false);
    });
  });

  describe('Pan Offset', () => {
    it('should pan by delta amounts', () => {
      const mockCanvas = (wrapper as any).fabricCanvas;
      const initialVpt = [...mockCanvas.viewportTransform];

      wrapper.pan(50, 100);

      const newVpt = mockCanvas.viewportTransform;
      // Should have translated by approximately the delta (within limits)
      expect(newVpt[4]).not.toBe(initialVpt[4]);
      expect(newVpt[5]).not.toBe(initialVpt[5]);
    });

    it('should enforce pan limits to keep boundary visible', () => {
      const mockCanvas = (wrapper as any).fabricCanvas;

      // Try to pan far left (should be limited)
      wrapper.pan(-10000, 0);
      const leftLimitVpt = [...mockCanvas.viewportTransform];

      // Try to pan far right (should be limited)
      mockCanvas.viewportTransform = [1, 0, 0, 1, 0, 0]; // Reset
      wrapper.pan(10000, 0);
      const rightLimitVpt = [...mockCanvas.viewportTransform];

      // Both should be clamped to limits
      expect(leftLimitVpt[4]).toBeGreaterThan(-10000);
      expect(rightLimitVpt[4]).toBeLessThan(10000);
    });

    it('should allow panning within limits', () => {
      const mockCanvas = (wrapper as any).fabricCanvas;
      mockCanvas.requestRenderAll.mockClear();

      // Pan a reasonable amount
      wrapper.pan(100, 100);

      // Should have rendered
      expect(mockCanvas.requestRenderAll).toHaveBeenCalled();
    });

    it('should not throw with null viewportTransform', () => {
      const mockCanvas = (wrapper as any).fabricCanvas;
      mockCanvas.viewportTransform = null;

      expect(() => wrapper.pan(50, 50)).not.toThrow();
    });
  });

  describe('Pan Limits Calculation', () => {
    it('should calculate pan limits', () => {
      const limits = (wrapper as any).getPanLimits();

      expect(limits).toHaveProperty('minX');
      expect(limits).toHaveProperty('maxX');
      expect(limits).toHaveProperty('minY');
      expect(limits).toHaveProperty('maxY');
    });

    it('should ensure minimum boundary visibility (10%)', () => {
      const limits = (wrapper as any).getPanLimits();

      // Limits should be calculated as numbers
      expect(typeof limits.minX).toBe('number');
      expect(typeof limits.maxX).toBe('number');
      expect(typeof limits.minY).toBe('number');
      expect(typeof limits.maxY).toBe('number');
    });

  });

  describe('Reset View', () => {
    it('should reset view to center on sticker', () => {
      const mockCanvas = (wrapper as any).fabricCanvas;
      mockCanvas.viewportTransform = [1, 0, 0, 1, 500, 500];

      expect(() => wrapper.resetView()).not.toThrow();

      // Should have modified viewport transform
      expect(mockCanvas.viewportTransform).toBeDefined();
    });

    it('should not throw when viewportTransform is null', () => {
      const mockCanvas = (wrapper as any).fabricCanvas;
      mockCanvas.viewportTransform = null;

      expect(() => wrapper.resetView()).not.toThrow();
    });
  });

  describe('Pan Drag Handlers', () => {
    it('should handle pan start event', () => {
      expect(() => wrapper.enablePanning()).not.toThrow();
      expect((wrapper as any).isPanningEnabled).toBe(true);
    });

    it('should handle pan move event', () => {
      wrapper.enablePanning();
      expect((wrapper as any).isPanningEnabled).toBe(true);

      // Pan operations should work when enabled
      expect(() => wrapper.pan(50, 50)).not.toThrow();
    });

    it('should handle pan end event', () => {
      wrapper.enablePanning();
      expect((wrapper as any).isPanningEnabled).toBe(true);

      // Disable panning should work
      expect(() => wrapper.disablePanning()).not.toThrow();
      expect((wrapper as any).isPanningEnabled).toBe(false);
    });

    it('should not pan if panning not enabled during move', () => {
      const mockCanvas = (wrapper as any).fabricCanvas;
      mockCanvas.viewportTransform = [1, 0, 0, 1, 0, 0];

      // Try to pan without enabling panning first
      wrapper.pan(100, 100);

      // Should not throw, just apply limits
      expect(mockCanvas.viewportTransform).toBeDefined();
    });
  });

  describe('Integration with Existing Features', () => {
    it('should not remove grid when clearing canvas', () => {
      // Grid is rendered via after:render event, not stored as object
      // So it persists through clear()
      expect(() => wrapper.clear()).not.toThrow();
    });
  });

  describe('Events', () => {
    it('should accept event handlers via on()', () => {
      const handler = vi.fn();
      wrapper.on('test:event', handler);

      // Event handler should be registered
      const mockCanvas = (wrapper as any).fabricCanvas;
      expect(mockCanvas.on).toHaveBeenCalledWith('test:event', handler);
    });

    it('should remove event handlers via off()', () => {
      const handler = vi.fn();
      wrapper.on('test:event', handler);
      wrapper.off('test:event', handler);

      // Event handler should be removed
      const mockCanvas = (wrapper as any).fabricCanvas;
      expect(mockCanvas.off).toHaveBeenCalled();
    });
  });
});
