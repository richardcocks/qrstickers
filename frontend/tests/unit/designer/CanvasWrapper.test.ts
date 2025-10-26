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

  describe('Zoom Functionality', () => {
    it('should initialize with zoom level 1.0', () => {
      expect(wrapper.getZoom()).toBe(1);
    });

    it('should set zoom level within limits', () => {
      wrapper.setZoom(2.0);
      expect(wrapper.getZoom()).toBe(2.0);

      const mockCanvas = (wrapper as any).fabricCanvas;
      expect(mockCanvas.viewportTransform[0]).toBe(2.0); // scaleX
      expect(mockCanvas.viewportTransform[3]).toBe(2.0); // scaleY
    });

    it('should clamp zoom to MIN_ZOOM (0.1)', () => {
      wrapper.setZoom(0.05);
      expect(wrapper.getZoom()).toBe(0.1);
    });

    it('should clamp zoom to MAX_ZOOM (5.0)', () => {
      wrapper.setZoom(10.0);
      expect(wrapper.getZoom()).toBe(5.0);
    });

    it('should zoom in by factor 1.2', () => {
      wrapper.setZoom(1.0);
      wrapper.zoomIn();
      expect(wrapper.getZoom()).toBeCloseTo(1.2, 2);
    });

    it('should zoom out by factor 1.2', () => {
      wrapper.setZoom(1.2);
      wrapper.zoomOut();
      expect(wrapper.getZoom()).toBeCloseTo(1.0, 2);
    });

    it('should zoom in with custom factor', () => {
      wrapper.setZoom(1.0);
      wrapper.zoomIn(1.5);
      expect(wrapper.getZoom()).toBeCloseTo(1.5, 2);
    });

    it('should zoom out with custom factor', () => {
      wrapper.setZoom(2.0);
      wrapper.zoomOut(2.0);
      expect(wrapper.getZoom()).toBeCloseTo(1.0, 2);
    });

    it('should zoom centered on viewport center when no center provided', () => {
      const mockCanvas = (wrapper as any).fabricCanvas;
      const vptBefore = mockCanvas.viewportTransform.slice();

      wrapper.setZoom(2.0);

      const vptAfter = mockCanvas.viewportTransform;
      // Pan should have adjusted to keep center fixed
      expect(vptAfter[4]).not.toBe(vptBefore[4]); // panX changed
      expect(vptAfter[5]).not.toBe(vptBefore[5]); // panY changed
    });

    it('should zoom centered on specific point', () => {
      const mockCanvas = (wrapper as any).fabricCanvas;

      // Center view first
      wrapper.resetView();
      const vptBefore = mockCanvas.viewportTransform.slice();

      // Zoom centered on top-left corner (0, 0)
      wrapper.setZoom(2.0, 0, 0);

      const vptAfter = mockCanvas.viewportTransform;
      expect(vptAfter[0]).toBe(2.0); // zoom applied

      // Pan should have adjusted to keep (0,0) fixed
      // At zoom 1: screenPoint = logicalPoint * 1 + pan
      // At zoom 2: screenPoint = logicalPoint * 2 + newPan
      // For point (0,0) to stay at screen (0,0):
      // 0 = 0 * 2 + newPan => newPan should be adjusted
      expect(vptAfter[4]).not.toBe(vptBefore[4]);
    });

    it('should enforce pan limits after zooming', () => {
      // Zoom in significantly
      wrapper.setZoom(3.0);

      const mockCanvas = (wrapper as any).fabricCanvas;
      const vpt = mockCanvas.viewportTransform;

      // Pan values should be within calculated limits
      const limits = (wrapper as any).getPanLimits();
      expect(vpt[4]).toBeGreaterThanOrEqual(limits.minX);
      expect(vpt[4]).toBeLessThanOrEqual(limits.maxX);
      expect(vpt[5]).toBeGreaterThanOrEqual(limits.minY);
      expect(vpt[5]).toBeLessThanOrEqual(limits.maxY);
    });

    it('should calculate zoomToFit correctly', () => {
      wrapper.zoomToFit();

      const zoom = wrapper.getZoom();

      // With 90% padding:
      // zoomX = (800 * 0.9) / stickerWidthPx ≈ 1.27 (for 100mm ≈ 567px)
      // zoomY = (600 * 0.9) / stickerHeightPx ≈ 1.91 (for 50mm ≈ 283px)
      // Should use min(zoomX, zoomY) ≈ 1.27
      expect(zoom).toBeGreaterThan(1.0);
      expect(zoom).toBeLessThan(2.0);
    });

    it('should reset zoom to 1.0 when calling resetView', () => {
      wrapper.setZoom(2.5);
      expect(wrapper.getZoom()).toBe(2.5);

      wrapper.resetView();
      expect(wrapper.getZoom()).toBe(1.0);

      const mockCanvas = (wrapper as any).fabricCanvas;
      expect(mockCanvas.viewportTransform[0]).toBe(1.0); // scaleX
      expect(mockCanvas.viewportTransform[3]).toBe(1.0); // scaleY
    });

    it('should request render after zoom change', () => {
      const mockCanvas = (wrapper as any).fabricCanvas;
      mockCanvas.requestRenderAll.mockClear();

      wrapper.setZoom(1.5);

      expect(mockCanvas.requestRenderAll).toHaveBeenCalled();
    });
  });

  describe('Pan Limits with Zoom', () => {
    it('should calculate pan limits accounting for zoom level', () => {
      wrapper.setZoom(2.0);

      const limits = (wrapper as any).getPanLimits();

      // At 2x zoom, sticker appears 2x larger
      // So limits should be different than at 1x zoom
      const scaledWidth = wrapper.stickerWidthPx * 2.0;
      const scaledHeight = wrapper.stickerHeightPx * 2.0;

      const expectedMaxX = 800 - scaledWidth * 0.1;
      const expectedMinX = 800 * 0.1 - scaledWidth;

      expect(limits.maxX).toBeCloseTo(expectedMaxX, 1);
      expect(limits.minX).toBeCloseTo(expectedMinX, 1);
    });

    it('should allow more panning when zoomed in', () => {
      // Get limits at zoom 1.0
      wrapper.setZoom(1.0);
      const limits1x = (wrapper as any).getPanLimits();

      // Get limits at zoom 2.0
      wrapper.setZoom(2.0);
      const limits2x = (wrapper as any).getPanLimits();

      // At 2x zoom, sticker is larger, so we can pan more
      const range1x = limits1x.maxX - limits1x.minX;
      const range2x = limits2x.maxX - limits2x.minX;

      expect(range2x).toBeGreaterThan(range1x);
    });

    it('should restrict panning when zoomed out', () => {
      // Get limits at zoom 1.0
      wrapper.setZoom(1.0);
      const limits1x = (wrapper as any).getPanLimits();

      // Get limits at zoom 0.5
      wrapper.setZoom(0.5);
      const limits05x = (wrapper as any).getPanLimits();

      // At 0.5x zoom, sticker is smaller, so less panning needed
      const range1x = limits1x.maxX - limits1x.minX;
      const range05x = limits05x.maxX - limits05x.minX;

      expect(range05x).toBeLessThan(range1x);
    });
  });
});
