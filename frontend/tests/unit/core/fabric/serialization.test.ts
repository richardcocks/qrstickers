/**
 * Unit tests for serialization utilities
 */

import { describe, it, expect, beforeEach } from 'vitest';
import * as fabric from 'fabric';
import {
  generateId,
  canvasToTemplateJson,
  getBoundaryOffset,
  getCanvasBoundingBox,
  validateTemplateJson,
} from '../../../../src/core/fabric/serialization';
import { createBoundText, createQRCode, createRectangle } from '../../../../src/core/fabric/factories';
import { mmToPx } from '../../../../src/utils/units';

describe('serialization', () => {
  describe('generateId', () => {
    it('should generate unique IDs', () => {
      const id1 = generateId();
      const id2 = generateId();

      expect(id1).not.toBe(id2);
    });

    it('should generate IDs with obj- prefix', () => {
      const id = generateId();

      expect(id).toMatch(/^obj-/);
    });

    it('should generate valid ID strings', () => {
      const id = generateId();

      expect(typeof id).toBe('string');
      expect(id.length).toBeGreaterThan(4);
    });
  });

  describe('canvasToTemplateJson', () => {
    let canvas: fabric.Canvas;

    beforeEach(() => {
      canvas = new fabric.Canvas(null as any);
    });

    it('should serialize empty canvas', () => {
      const json = canvasToTemplateJson(canvas, 100, 50);

      expect(json.version).toBe('1.0');
      expect(json.fabricVersion).toBe(fabric.version);
      expect(json.pageSize).toEqual({
        width: 100,
        height: 50,
        unit: 'mm',
      });
      expect(json.objects).toEqual([]);
    });

    it('should serialize canvas with QR code', () => {
      const qrCode = createQRCode({
        left: 50,
        top: 50,
        width: 100,
        dataSource: 'device.Serial',
        eccLevel: 'H',
      });

      canvas.add(qrCode);

      const json = canvasToTemplateJson(canvas, 100, 50);

      expect(json.objects).toHaveLength(1);
      expect(json.objects[0].type).toBe('qrcode');
      expect(json.objects[0].properties).toEqual({
        dataSource: 'device.Serial',
        eccLevel: 'H',
        quietZone: 2,
      });
    });

    it('should serialize canvas with text object', () => {
      const text = createBoundText({
        text: 'Device Name',
        left: 100,
        top: 200,
        fontSize: 24,
        fontFamily: 'Arial',
        fill: '#000000',
        dataSource: 'device.Name',
      });

      canvas.add(text);

      const json = canvasToTemplateJson(canvas, 100, 50);

      expect(json.objects).toHaveLength(1);
      const obj = json.objects[0];
      expect(obj.type).toBe('i-text');
      expect((obj as any).text).toBe('Device Name');
      expect((obj as any).fontFamily).toBe('Arial');
      expect((obj as any).fontSize).toBe(24);
      expect(obj.properties).toMatchObject({
        dataSource: 'device.Name',
      });
    });

    it('should serialize canvas with rectangle', () => {
      const rect = createRectangle({
        left: 50,
        top: 50,
        width: 100,
        height: 50,
        fill: '#ff0000',
        stroke: '#0000ff',
        strokeWidth: 3,
      });

      canvas.add(rect);

      const json = canvasToTemplateJson(canvas, 100, 50);

      expect(json.objects).toHaveLength(1);
      expect(json.objects[0].type).toBe('rect');
      expect((json.objects[0] as any).fill).toBe('#ff0000');
      expect((json.objects[0] as any).stroke).toBe('#0000ff');
      expect((json.objects[0] as any).strokeWidth).toBe(3);
    });

    it('should filter out objects marked as excludeFromExport', () => {
      const text1 = createBoundText({ text: 'Visible' });
      const text2 = createBoundText({ text: 'Hidden' });
      (text2 as any).excludeFromExport = true;

      canvas.add(text1, text2);

      const json = canvasToTemplateJson(canvas, 100, 50);

      expect(json.objects).toHaveLength(1);
      expect((json.objects[0] as any).text).toBe('Visible');
    });

    it('should filter out sticker boundary', () => {
      const boundary = createRectangle();
      (boundary as any).name = 'stickerBoundary';
      const text = createBoundText({ text: 'Content' });

      canvas.add(boundary, text);

      const json = canvasToTemplateJson(canvas, 100, 50);

      expect(json.objects).toHaveLength(1);
      expect((json.objects[0] as any).text).toBe('Content');
    });

    it('should adjust positions relative to boundary offset', () => {
      const text = createBoundText({
        left: 150,
        top: 200,
      });

      canvas.add(text);

      const boundaryOffset = { left: 100, top: 150 };
      const json = canvasToTemplateJson(canvas, 100, 50, boundaryOffset);

      // Positions should be relative to boundary (150-100, 200-150) = (50, 50) px
      // Then converted to mm
      expect(json.objects[0].left).toBeCloseTo(8.82, 1); // 50px ≈ 8.82mm
      expect(json.objects[0].top).toBeCloseTo(8.82, 1);
    });

    it('should convert pixel measurements to millimeters', () => {
      const qrCode = createQRCode({
        left: mmToPx(10), // 10mm in pixels
        top: mmToPx(20), // 20mm in pixels
        width: mmToPx(25), // 25mm in pixels
      });

      canvas.add(qrCode);

      const json = canvasToTemplateJson(canvas, 100, 50);

      expect(json.objects[0].left).toBeCloseTo(10, 0); // 0 decimal places
      expect(json.objects[0].top).toBeCloseTo(20, 0);
      // QRCode width includes internal stroke widths, so allow ~0.5mm tolerance
      expect(json.objects[0].width).toBeCloseTo(25, 0);
    });

    it('should generate unique IDs for objects without IDs', () => {
      const text1 = createBoundText({ text: 'Text 1' });
      const text2 = createBoundText({ text: 'Text 2' });

      canvas.add(text1, text2);

      const json = canvasToTemplateJson(canvas, 100, 50);

      expect(json.objects[0].id).toBeTruthy();
      expect(json.objects[1].id).toBeTruthy();
      expect(json.objects[0].id).not.toBe(json.objects[1].id);
    });
  });

  describe('getBoundaryOffset', () => {
    let canvas: fabric.Canvas;

    beforeEach(() => {
      canvas = new fabric.Canvas(null as any);
    });

    it('should return {0,0} for canvas without boundary', () => {
      const offset = getBoundaryOffset(canvas);

      expect(offset).toEqual({ left: 0, top: 0 });
    });

    it('should find boundary offset', () => {
      const boundary = createRectangle({
        left: 100,
        top: 150,
      });
      (boundary as any).name = 'stickerBoundary';

      canvas.add(boundary);

      const offset = getBoundaryOffset(canvas);

      expect(offset).toEqual({ left: 100, top: 150 });
    });

    it('should ignore objects not named stickerBoundary', () => {
      const rect1 = createRectangle({ left: 50, top: 50 });
      const rect2 = createRectangle({ left: 100, top: 100 });

      canvas.add(rect1, rect2);

      const offset = getBoundaryOffset(canvas);

      expect(offset).toEqual({ left: 0, top: 0 });
    });
  });

  describe('getCanvasBoundingBox', () => {
    let canvas: fabric.Canvas;

    beforeEach(() => {
      canvas = new fabric.Canvas(null as any);
    });

    it('should return zero bounding box for empty canvas', () => {
      const bbox = getCanvasBoundingBox(canvas);

      expect(bbox).toEqual({ left: 0, top: 0, width: 0, height: 0 });
    });

    it('should calculate bounding box for single object', () => {
      const rect = createRectangle({
        left: 50,
        top: 100,
        width: 200,
        height: 150,
      });

      canvas.add(rect);

      const bbox = getCanvasBoundingBox(canvas);

      expect(bbox.left).toBe(50);
      expect(bbox.top).toBe(100);
      // Width/height include stroke (2px default)
      expect(bbox.width).toBe(202);
      expect(bbox.height).toBe(152);
    });

    it('should calculate bounding box for multiple objects', () => {
      const rect1 = createRectangle({
        left: 50,
        top: 50,
        width: 100,
        height: 100,
      });
      const rect2 = createRectangle({
        left: 200,
        top: 200,
        width: 100,
        height: 100,
      });

      canvas.add(rect1, rect2);

      const bbox = getCanvasBoundingBox(canvas);

      expect(bbox.left).toBe(50);
      expect(bbox.top).toBe(50);
      // rect1: 50 to 152 (100+2 stroke), rect2: 200 to 302 (100+2 stroke)
      expect(bbox.width).toBe(252); // 302 - 50
      expect(bbox.height).toBe(252); // 302 - 50
    });

    it('should exclude objects marked as excludeFromExport', () => {
      const rect1 = createRectangle({ left: 50, top: 50, width: 100, height: 100 });
      const rect2 = createRectangle({ left: 500, top: 500, width: 100, height: 100 });
      (rect2 as any).excludeFromExport = true;

      canvas.add(rect1, rect2);

      const bbox = getCanvasBoundingBox(canvas);

      // Should only consider rect1 (with 2px stroke)
      expect(bbox.left).toBe(50);
      expect(bbox.top).toBe(50);
      expect(bbox.width).toBe(102); // 100 + 2px stroke
      expect(bbox.height).toBe(102); // 100 + 2px stroke
    });
  });

  describe('validateTemplateJson', () => {
    it('should validate valid template JSON', () => {
      const json = {
        version: '1.0',
        fabricVersion: '6.0.0',
        pageSize: {
          width: 100,
          height: 50,
          unit: 'mm',
        },
        objects: [
          {
            type: 'qrcode',
            id: 'obj-1',
            left: 10,
            top: 20,
            width: 25,
            height: 25,
            scaleX: 1,
            scaleY: 1,
            angle: 0,
          },
        ],
      };

      const errors = validateTemplateJson(json);

      expect(errors).toEqual([]);
    });

    it('should detect missing version', () => {
      const json = {
        pageSize: { width: 100, height: 50, unit: 'mm' },
        objects: [],
      };

      const errors = validateTemplateJson(json);

      expect(errors).toContain('Missing version field');
    });

    it('should detect missing pageSize', () => {
      const json = {
        version: '1.0',
        objects: [],
      };

      const errors = validateTemplateJson(json);

      expect(errors).toContain('Missing pageSize field');
    });

    it('should detect invalid pageSize.width', () => {
      const json = {
        version: '1.0',
        pageSize: { width: -10, height: 50, unit: 'mm' },
        objects: [],
      };

      const errors = validateTemplateJson(json);

      expect(errors).toContain('Invalid pageSize.width');
    });

    it('should detect invalid pageSize.unit', () => {
      const json = {
        version: '1.0',
        pageSize: { width: 100, height: 50, unit: 'invalid' },
        objects: [],
      };

      const errors = validateTemplateJson(json);

      expect(errors).toContain('Invalid pageSize.unit (must be mm, px, or in)');
    });

    it('should detect non-array objects field', () => {
      const json = {
        version: '1.0',
        pageSize: { width: 100, height: 50, unit: 'mm' },
        objects: 'not an array',
      };

      const errors = validateTemplateJson(json);

      expect(errors).toContain('objects must be an array');
    });

    it('should detect objects missing required fields', () => {
      const json = {
        version: '1.0',
        pageSize: { width: 100, height: 50, unit: 'mm' },
        objects: [
          {
            // Missing type, id, left, top
          },
        ],
      };

      const errors = validateTemplateJson(json);

      expect(errors).toContain('Object 0: Missing type field');
      expect(errors).toContain('Object 0: Missing id field');
      expect(errors).toContain('Object 0: Invalid left position');
      expect(errors).toContain('Object 0: Invalid top position');
    });

    it('should return multiple errors for invalid template', () => {
      const json = {
        // Missing version
        pageSize: { width: -10, height: 50, unit: 'invalid' },
        objects: 'not an array',
      };

      const errors = validateTemplateJson(json);

      expect(errors.length).toBeGreaterThan(1);
    });

    it('should handle null/undefined input', () => {
      const errors1 = validateTemplateJson(null);
      const errors2 = validateTemplateJson(undefined);

      expect(errors1).toContain('Template JSON is null or undefined');
      expect(errors2).toContain('Template JSON is null or undefined');
    });
  });
});
