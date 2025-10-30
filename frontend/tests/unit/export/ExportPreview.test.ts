/**
 * Unit tests for ExportPreview.ts pure functions
 * Tests data transformation logic without canvas/DOM dependencies
 */

import { describe, it, expect } from 'vitest';
import {
  extractDataBindings,
  generatePlaceholderMap,
  createPreviewTemplate,
  PLACEHOLDER_VALUES,
} from '../../../src/export/ExportPreview';
import type { TemplateJson, UploadedImage, PlaceholderMap } from '../../../src/export/types';

describe('ExportPreview - Pure Functions', () => {
  describe('extractDataBindings', () => {
    it('should extract QR code data sources', () => {
      const template: TemplateJson = {
        version: '1.0',
        fabricVersion: '6.7.1',
        pageSize: { width: 100, height: 50, unit: 'mm' },
        objects: [
          {
            type: 'qrcode',
            id: 'qr1',
            left: 10,
            top: 10,
            width: 20,
            height: 20,
            properties: {
              dataSource: 'Device.Serial', // Mixed case
            },
          },
          {
            type: 'qrcode',
            id: 'qr2',
            left: 40,
            top: 10,
            width: 20,
            height: 20,
            properties: {
              dataSource: 'Network.Name',
            },
          },
        ],
      };

      const bindings = extractDataBindings(template);

      expect(bindings).toContain('device.serial');
      expect(bindings).toContain('network.name');
      expect(bindings).toHaveLength(2);
    });

    it('should extract {{...}} text patterns', () => {
      const template: TemplateJson = {
        version: '1.0',
        fabricVersion: '6.7.1',
        pageSize: { width: 100, height: 50, unit: 'mm' },
        objects: [
          {
            type: 'text',
            id: 'text1',
            left: 10,
            top: 10,
            width: 30,
            height: 10,
            text: 'Serial: {{device.serial}}',
          },
          {
            type: 'text',
            id: 'text2',
            left: 10,
            top: 25,
            width: 30,
            height: 10,
            text: 'MAC: {{Device.MAC}}', // Mixed case
          },
        ],
      };

      const bindings = extractDataBindings(template);

      expect(bindings).toContain('device.serial');
      expect(bindings).toContain('device.mac');
      expect(bindings).toHaveLength(2);
    });

    it('should extract image data sources', () => {
      const template: TemplateJson = {
        version: '1.0',
        fabricVersion: '6.7.1',
        pageSize: { width: 100, height: 50, unit: 'mm' },
        objects: [
          {
            type: 'image',
            id: 'img1',
            left: 10,
            top: 10,
            width: 20,
            height: 20,
            properties: {
              dataSource: 'CustomImage.image_123',
            },
          },
        ],
      };

      const bindings = extractDataBindings(template);

      expect(bindings).toContain('customimage.image_123');
      expect(bindings).toHaveLength(1);
    });

    it('should return unique bindings (deduplicate)', () => {
      const template: TemplateJson = {
        version: '1.0',
        fabricVersion: '6.7.1',
        pageSize: { width: 100, height: 50, unit: 'mm' },
        objects: [
          {
            type: 'qrcode',
            id: 'qr1',
            left: 10,
            top: 10,
            width: 20,
            height: 20,
            properties: {
              dataSource: 'device.serial',
            },
          },
          {
            type: 'text',
            id: 'text1',
            left: 10,
            top: 35,
            width: 30,
            height: 10,
            text: 'Serial: {{device.serial}}', // Duplicate
          },
          {
            type: 'text',
            id: 'text2',
            left: 10,
            top: 45,
            width: 30,
            height: 10,
            text: 'Also: {{Device.Serial}}', // Mixed case duplicate
          },
        ],
      };

      const bindings = extractDataBindings(template);

      expect(bindings).toEqual(['device.serial']);
      expect(bindings).toHaveLength(1);
    });

    it('should normalize bindings to lowercase', () => {
      const template: TemplateJson = {
        version: '1.0',
        fabricVersion: '6.7.1',
        pageSize: { width: 100, height: 50, unit: 'mm' },
        objects: [
          {
            type: 'qrcode',
            id: 'qr1',
            left: 10,
            top: 10,
            width: 20,
            height: 20,
            properties: {
              dataSource: 'DEVICE.SERIAL',
            },
          },
          {
            type: 'text',
            id: 'text1',
            left: 10,
            top: 35,
            width: 30,
            height: 10,
            text: 'Name: {{NETWORK.NAME}}',
          },
        ],
      };

      const bindings = extractDataBindings(template);

      expect(bindings).toContain('device.serial');
      expect(bindings).toContain('network.name');
      expect(bindings).not.toContain('DEVICE.SERIAL');
      expect(bindings).not.toContain('NETWORK.NAME');
    });

    it('should handle templates with no objects', () => {
      const template: TemplateJson = {
        version: '1.0',
        fabricVersion: '6.7.1',
        pageSize: { width: 100, height: 50, unit: 'mm' },
        objects: [],
      };

      const bindings = extractDataBindings(template);

      expect(bindings).toEqual([]);
    });

    it('should handle null/undefined template', () => {
      expect(extractDataBindings(null as any)).toEqual([]);
      expect(extractDataBindings(undefined as any)).toEqual([]);
      expect(extractDataBindings({} as any)).toEqual([]);
    });

    it('should extract multiple patterns from single text field', () => {
      const template: TemplateJson = {
        version: '1.0',
        fabricVersion: '6.7.1',
        pageSize: { width: 100, height: 50, unit: 'mm' },
        objects: [
          {
            type: 'text',
            id: 'text1',
            left: 10,
            top: 10,
            width: 50,
            height: 10,
            text: '{{device.name}} - Serial: {{device.serial}}, MAC: {{device.mac}}',
          },
        ],
      };

      const bindings = extractDataBindings(template);

      expect(bindings).toContain('device.name');
      expect(bindings).toContain('device.serial');
      expect(bindings).toContain('device.mac');
      expect(bindings).toHaveLength(3);
    });
  });

  describe('generatePlaceholderMap', () => {
    it('should use PLACEHOLDER_VALUES for known bindings', () => {
      const template: TemplateJson = {
        version: '1.0',
        fabricVersion: '6.7.1',
        pageSize: { width: 100, height: 50, unit: 'mm' },
        objects: [
          {
            type: 'qrcode',
            id: 'qr1',
            left: 10,
            top: 10,
            width: 20,
            height: 20,
            properties: {
              dataSource: 'device.serial',
            },
          },
          {
            type: 'text',
            id: 'text1',
            left: 10,
            top: 35,
            width: 30,
            height: 10,
            text: 'Name: {{device.name}}',
          },
        ],
      };

      const placeholders = generatePlaceholderMap(template);

      expect(placeholders['device.serial']).toBe(PLACEHOLDER_VALUES['device.serial']);
      expect(placeholders['device.name']).toBe(PLACEHOLDER_VALUES['device.name']);
      expect(placeholders['device.serial']).toBe('MS-1234-ABCD-5678');
      expect(placeholders['device.name']).toBe('Example Switch');
    });

    it('should generate generic placeholders for unknown bindings', () => {
      const template: TemplateJson = {
        version: '1.0',
        fabricVersion: '6.7.1',
        pageSize: { width: 100, height: 50, unit: 'mm' },
        objects: [
          {
            type: 'text',
            id: 'text1',
            left: 10,
            top: 10,
            width: 30,
            height: 10,
            text: 'Custom: {{custom.field}}',
          },
          {
            type: 'qrcode',
            id: 'qr1',
            left: 10,
            top: 25,
            width: 20,
            height: 20,
            properties: {
              dataSource: 'unknown.binding',
            },
          },
        ],
      };

      const placeholders = generatePlaceholderMap(template);

      expect(placeholders['custom.field']).toBe('[custom.field]');
      expect(placeholders['unknown.binding']).toBe('[unknown.binding]');
    });

    it('should include custom images from uploadedImages array', () => {
      const template: TemplateJson = {
        version: '1.0',
        fabricVersion: '6.7.1',
        pageSize: { width: 100, height: 50, unit: 'mm' },
        objects: [
          {
            type: 'image',
            id: 'img1',
            left: 10,
            top: 10,
            width: 20,
            height: 20,
            properties: {
              dataSource: 'customimage.image_123',
            },
          },
        ],
      };

      const uploadedImages: UploadedImage[] = [
        {
          id: 123,
          filename: 'logo.png',
          dataUri: 'data:image/png;base64,iVBORw0KGgo...',
          uploadedAt: new Date(),
        },
      ];

      const placeholders = generatePlaceholderMap(template, uploadedImages);

      expect(placeholders['customimage.image_123']).toBe('data:image/png;base64,iVBORw0KGgo...');
    });

    it('should handle empty bindings array', () => {
      const template: TemplateJson = {
        version: '1.0',
        fabricVersion: '6.7.1',
        pageSize: { width: 100, height: 50, unit: 'mm' },
        objects: [],
      };

      const placeholders = generatePlaceholderMap(template);

      expect(placeholders).toEqual({});
    });

    it('should handle uploadedImages = undefined', () => {
      const template: TemplateJson = {
        version: '1.0',
        fabricVersion: '6.7.1',
        pageSize: { width: 100, height: 50, unit: 'mm' },
        objects: [
          {
            type: 'qrcode',
            id: 'qr1',
            left: 10,
            top: 10,
            width: 20,
            height: 20,
            properties: {
              dataSource: 'device.serial',
            },
          },
        ],
      };

      const placeholders = generatePlaceholderMap(template);

      expect(placeholders['device.serial']).toBe(PLACEHOLDER_VALUES['device.serial']);
      expect(Object.keys(placeholders)).toHaveLength(1);
    });

    it('should normalize binding keys to lowercase', () => {
      const template: TemplateJson = {
        version: '1.0',
        fabricVersion: '6.7.1',
        pageSize: { width: 100, height: 50, unit: 'mm' },
        objects: [
          {
            type: 'qrcode',
            id: 'qr1',
            left: 10,
            top: 10,
            width: 20,
            height: 20,
            properties: {
              dataSource: 'DEVICE.SERIAL',
            },
          },
        ],
      };

      const placeholders = generatePlaceholderMap(template);

      expect(placeholders['device.serial']).toBeDefined();
      expect(placeholders['DEVICE.SERIAL']).toBeUndefined();
    });

    it('should map all standard bindings', () => {
      const template: TemplateJson = {
        version: '1.0',
        fabricVersion: '6.7.1',
        pageSize: { width: 100, height: 50, unit: 'mm' },
        objects: [
          { type: 'qrcode', id: 'qr1', left: 0, top: 0, width: 10, height: 10, properties: { dataSource: 'device.serial' } },
          { type: 'qrcode', id: 'qr2', left: 0, top: 0, width: 10, height: 10, properties: { dataSource: 'device.name' } },
          { type: 'qrcode', id: 'qr3', left: 0, top: 0, width: 10, height: 10, properties: { dataSource: 'device.mac' } },
          { type: 'qrcode', id: 'qr4', left: 0, top: 0, width: 10, height: 10, properties: { dataSource: 'network.name' } },
          { type: 'qrcode', id: 'qr5', left: 0, top: 0, width: 10, height: 10, properties: { dataSource: 'connection.name' } },
        ],
      };

      const placeholders = generatePlaceholderMap(template);

      expect(placeholders['device.serial']).toBe('MS-1234-ABCD-5678');
      expect(placeholders['device.name']).toBe('Example Switch');
      expect(placeholders['device.mac']).toBe('00:1A:2B:3C:4D:5E');
      expect(placeholders['network.name']).toBe('Production Network');
      expect(placeholders['connection.name']).toBe('Main Office');
    });
  });

  describe('createPreviewTemplate', () => {
    it('should replace QR code data sources with previewData', () => {
      const template: TemplateJson = {
        version: '1.0',
        fabricVersion: '6.7.1',
        pageSize: { width: 100, height: 50, unit: 'mm' },
        objects: [
          {
            type: 'qrcode',
            id: 'qr1',
            left: 10,
            top: 10,
            width: 20,
            height: 20,
            properties: {
              dataSource: 'device.serial',
            },
          },
        ],
      };

      const placeholders: PlaceholderMap = {
        'device.serial': 'MS-TEST-1234',
      };

      const previewTemplate = createPreviewTemplate(template, placeholders);

      expect(previewTemplate.objects[0].previewData).toBe('MS-TEST-1234');
    });

    it('should replace root-level text dataBinding', () => {
      const template: TemplateJson = {
        version: '1.0',
        fabricVersion: '6.7.1',
        pageSize: { width: 100, height: 50, unit: 'mm' },
        objects: [
          {
            type: 'text',
            id: 'text1',
            left: 10,
            top: 10,
            width: 30,
            height: 10,
            text: '',
            dataBinding: 'device.name',
          },
        ],
      };

      const placeholders: PlaceholderMap = {
        'device.name': 'Test Switch',
      };

      const previewTemplate = createPreviewTemplate(template, placeholders);

      expect(previewTemplate.objects[0].text).toBe('Test Switch');
    });

    it('should replace {{...}} patterns in text fields', () => {
      const template: TemplateJson = {
        version: '1.0',
        fabricVersion: '6.7.1',
        pageSize: { width: 100, height: 50, unit: 'mm' },
        objects: [
          {
            type: 'text',
            id: 'text1',
            left: 10,
            top: 10,
            width: 50,
            height: 10,
            text: 'Serial: {{device.serial}}, Name: {{device.name}}',
          },
        ],
      };

      const placeholders: PlaceholderMap = {
        'device.serial': 'MS-TEST-5678',
        'device.name': 'Test Device',
      };

      const previewTemplate = createPreviewTemplate(template, placeholders);

      expect(previewTemplate.objects[0].text).toBe('Serial: MS-TEST-5678, Name: Test Device');
    });

    it('should preserve template structure (pageSize, version)', () => {
      const template: TemplateJson = {
        version: '1.0',
        fabricVersion: '6.7.1',
        pageSize: { width: 100, height: 50, unit: 'mm' },
        objects: [
          {
            type: 'text',
            id: 'text1',
            left: 10,
            top: 10,
            width: 30,
            height: 10,
            text: 'Test',
          },
        ],
      };

      const placeholders: PlaceholderMap = {};

      const previewTemplate = createPreviewTemplate(template, placeholders);

      expect(previewTemplate.version).toBe('1.0');
      expect(previewTemplate.fabricVersion).toBe('6.7.1');
      expect(previewTemplate.pageSize).toEqual({ width: 100, height: 50, unit: 'mm' });
    });

    it('should perform deep clone (not mutate original)', () => {
      const template: TemplateJson = {
        version: '1.0',
        fabricVersion: '6.7.1',
        pageSize: { width: 100, height: 50, unit: 'mm' },
        objects: [
          {
            type: 'text',
            id: 'text1',
            left: 10,
            top: 10,
            width: 30,
            height: 10,
            text: 'Original: {{device.name}}',
          },
        ],
      };

      const originalText = template.objects[0].text;

      const placeholders: PlaceholderMap = {
        'device.name': 'Changed Device',
      };

      const previewTemplate = createPreviewTemplate(template, placeholders);

      // Original should be unchanged
      expect(template.objects[0].text).toBe(originalText);
      expect(template.objects[0].text).toBe('Original: {{device.name}}');

      // Preview should have replaced text
      expect(previewTemplate.objects[0].text).toBe('Original: Changed Device');

      // Verify they're different objects
      expect(previewTemplate).not.toBe(template);
      expect(previewTemplate.objects[0]).not.toBe(template.objects[0]);
    });

    it('should handle null/empty objects array', () => {
      const template: TemplateJson = {
        version: '1.0',
        fabricVersion: '6.7.1',
        pageSize: { width: 100, height: 50, unit: 'mm' },
        objects: [],
      };

      const placeholders: PlaceholderMap = {};

      const previewTemplate = createPreviewTemplate(template, placeholders);

      expect(previewTemplate.objects).toEqual([]);
    });

    it('should handle custom image bindings', () => {
      const template: TemplateJson = {
        version: '1.0',
        fabricVersion: '6.7.1',
        pageSize: { width: 100, height: 50, unit: 'mm' },
        objects: [
          {
            type: 'image',
            id: 'img1',
            left: 10,
            top: 10,
            width: 20,
            height: 20,
            properties: {
              dataSource: 'customimage.image_456',
            },
          },
        ],
      };

      const placeholders: PlaceholderMap = {
        'customimage.image_456': 'data:image/png;base64,customdata...',
      };

      const previewTemplate = createPreviewTemplate(template, placeholders);

      expect(previewTemplate.objects[0].previewData).toBe('data:image/png;base64,customdata...');
      expect(previewTemplate.objects[0].properties?.data).toBe('data:image/png;base64,customdata...');
    });

    it('should fallback to PLACEHOLDER_VALUES for unknown bindings', () => {
      const template: TemplateJson = {
        version: '1.0',
        fabricVersion: '6.7.1',
        pageSize: { width: 100, height: 50, unit: 'mm' },
        objects: [
          {
            type: 'qrcode',
            id: 'qr1',
            left: 10,
            top: 10,
            width: 20,
            height: 20,
            properties: {
              dataSource: 'device.serial',
            },
          },
        ],
      };

      // Empty placeholders map - should fallback to PLACEHOLDER_VALUES
      const placeholders: PlaceholderMap = {};

      const previewTemplate = createPreviewTemplate(template, placeholders);

      expect(previewTemplate.objects[0].previewData).toBe(PLACEHOLDER_VALUES['device.serial']);
      expect(previewTemplate.objects[0].previewData).toBe('MS-1234-ABCD-5678');
    });
  });
});
