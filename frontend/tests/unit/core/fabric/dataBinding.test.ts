/**
 * Unit tests for data binding utilities
 */

import { describe, it, expect, beforeEach } from 'vitest';
import * as fabric from 'fabric';
import {
  getDataBinding,
  updateDataBinding,
  hasDataBinding,
  clearDataBinding,
  getCanvasDataSources,
  validateDataSources,
} from '../../../../src/core/fabric/dataBinding';
import { createBoundText, createQRCode } from '../../../../src/core/fabric/factories';

describe('dataBinding', () => {
  describe('getDataBinding', () => {
    it('should return null for null object', () => {
      const binding = getDataBinding(null);
      expect(binding).toBeNull();
    });

    it('should extract data binding from text object', () => {
      const text = createBoundText({
        dataSource: 'device.Name',
      });

      const binding = getDataBinding(text);

      expect(binding).not.toBeNull();
      expect(binding?.type).toBe('i-text');
      expect(binding?.dataSource).toBe('device.Name');
      expect(binding?.hasBinding).toBe(true);
    });

    it('should extract data binding from QR code', () => {
      const qrCode = createQRCode({
        dataSource: 'device.Serial',
      });

      const binding = getDataBinding(qrCode);

      expect(binding).not.toBeNull();
      expect(binding?.type).toBe('qrcode');
      expect(binding?.dataSource).toBe('device.Serial');
      expect(binding?.hasBinding).toBe(true);
    });

    it('should return hasBinding: false for object without binding', () => {
      const rect = new fabric.Rect({
        width: 100,
        height: 100,
      });

      const binding = getDataBinding(rect);

      expect(binding).not.toBeNull();
      expect(binding?.hasBinding).toBe(false);
      expect(binding?.dataSource).toBeNull();
    });
  });

  describe('updateDataBinding', () => {
    it('should do nothing for null object', () => {
      // Should not throw
      expect(() => updateDataBinding(null, 'device.Name')).not.toThrow();
    });

    it('should update data source on object', () => {
      const text = createBoundText();

      updateDataBinding(text, 'device.Serial');

      expect(text.dataSource).toBe('device.Serial');
    });

    it('should update text content for text objects', () => {
      const text = createBoundText({
        text: 'Original Text',
      });

      updateDataBinding(text, 'device.Name');

      expect(text.text).toBe('{{device.Name}}');
    });

    it('should not change text for non-text objects', () => {
      const qrCode = createQRCode();

      updateDataBinding(qrCode, 'device.MacAddress');

      expect(qrCode.dataSource).toBe('device.MacAddress');
      // QR code shouldn't have text property modified
    });
  });

  describe('hasDataBinding', () => {
    it('should return false for null object', () => {
      expect(hasDataBinding(null)).toBe(false);
    });

    it('should return true for object with binding', () => {
      const text = createBoundText({
        dataSource: 'device.Name',
      });

      expect(hasDataBinding(text)).toBe(true);
    });

    it('should return false for object without binding', () => {
      const rect = new fabric.Rect({
        width: 100,
        height: 100,
      });

      expect(hasDataBinding(rect)).toBe(false);
    });

    it('should return false for object with empty dataSource', () => {
      const text = createBoundText({
        dataSource: '',
      });

      expect(hasDataBinding(text)).toBe(false);
    });
  });

  describe('clearDataBinding', () => {
    it('should do nothing for null object', () => {
      expect(() => clearDataBinding(null)).not.toThrow();
    });

    it('should clear data source from object', () => {
      const text = createBoundText({
        dataSource: 'device.Name',
      });

      clearDataBinding(text);

      expect(text.dataSource).toBeUndefined();
    });

    it('should clear text content for text objects', () => {
      const text = createBoundText({
        text: '{{device.Name}}',
        dataSource: 'device.Name',
      });

      clearDataBinding(text);

      expect(text.text).toBe('');
      expect(text.dataSource).toBeUndefined();
    });
  });

  describe('getCanvasDataSources', () => {
    let canvas: fabric.Canvas;

    beforeEach(() => {
      // Create a test canvas
      canvas = new fabric.Canvas(null as any);
    });

    it('should return empty array for empty canvas', () => {
      const sources = getCanvasDataSources(canvas);

      expect(sources).toEqual([]);
    });

    it('should extract data sources from canvas objects', () => {
      canvas.add(createBoundText({ dataSource: 'device.Name' }));
      canvas.add(createQRCode({ dataSource: 'device.Serial' }));
      canvas.add(createBoundText({ dataSource: 'device.Model' }));

      const sources = getCanvasDataSources(canvas);

      expect(sources).toHaveLength(3);
      expect(sources).toContain('device.Name');
      expect(sources).toContain('device.Serial');
      expect(sources).toContain('device.Model');
    });

    it('should return unique data sources', () => {
      canvas.add(createBoundText({ dataSource: 'device.Name' }));
      canvas.add(createBoundText({ dataSource: 'device.Name' }));
      canvas.add(createQRCode({ dataSource: 'device.Name' }));

      const sources = getCanvasDataSources(canvas);

      expect(sources).toEqual(['device.Name']);
    });

    it('should ignore objects without data binding', () => {
      canvas.add(createBoundText({ dataSource: 'device.Name' }));
      canvas.add(new fabric.Rect({ width: 100, height: 100 }));
      canvas.add(createQRCode({ dataSource: 'device.Serial' }));

      const sources = getCanvasDataSources(canvas);

      expect(sources).toHaveLength(2);
      expect(sources).toContain('device.Name');
      expect(sources).toContain('device.Serial');
    });

    it('should return sorted data sources', () => {
      canvas.add(createBoundText({ dataSource: 'device.ZZZ' }));
      canvas.add(createBoundText({ dataSource: 'device.AAA' }));
      canvas.add(createBoundText({ dataSource: 'device.MMM' }));

      const sources = getCanvasDataSources(canvas);

      expect(sources).toEqual(['device.AAA', 'device.MMM', 'device.ZZZ']);
    });
  });

  describe('validateDataSources', () => {
    let canvas: fabric.Canvas;

    beforeEach(() => {
      canvas = new fabric.Canvas(null as any);
    });

    it('should return empty array when all sources are available', () => {
      canvas.add(createBoundText({ dataSource: 'device.Name' }));
      canvas.add(createQRCode({ dataSource: 'device.Serial' }));

      const availableData = {
        device: {
          Name: 'Switch-01',
          Serial: 'ABC123',
        },
      };

      const missing = validateDataSources(canvas, availableData);

      expect(missing).toEqual([]);
    });

    it('should return missing data sources', () => {
      canvas.add(createBoundText({ dataSource: 'device.Name' }));
      canvas.add(createQRCode({ dataSource: 'device.Serial' }));
      canvas.add(createBoundText({ dataSource: 'device.Model' }));

      const availableData = {
        device: {
          Name: 'Switch-01',
          // Serial and Model missing
        },
      };

      const missing = validateDataSources(canvas, availableData);

      expect(missing).toHaveLength(2);
      expect(missing).toContain('device.Serial');
      expect(missing).toContain('device.Model');
    });

    it('should handle nested data paths', () => {
      canvas.add(createBoundText({ dataSource: 'device.network.Name' }));

      const availableData = {
        device: {
          network: {
            Name: 'Main Network',
          },
        },
      };

      const missing = validateDataSources(canvas, availableData);

      expect(missing).toEqual([]);
    });

    it('should detect missing nested paths', () => {
      canvas.add(createBoundText({ dataSource: 'device.network.Name' }));

      const availableData = {
        device: {
          // network object missing
        },
      };

      const missing = validateDataSources(canvas, availableData);

      expect(missing).toContain('device.network.Name');
    });
  });
});
