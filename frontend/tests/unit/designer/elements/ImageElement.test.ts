/**
 * Unit tests for ImageElement
 */

import { describe, it, expect, vi } from 'vitest';
import { ImageElement } from '../../../../src/designer/elements/ImageElement';

// Mock Fabric.js
vi.mock('fabric', () => ({
  Rect: vi.fn(() => ({
    set: vi.fn(),
  })),
  Group: vi.fn(() => ({
    set: vi.fn(),
  })),
  Text: vi.fn(() => ({
    set: vi.fn(),
  })),
}));

describe('ImageElement', () => {
  describe('Constructor', () => {
    it('should create ImageElement with default values', () => {
      const element = new ImageElement({});

      expect(element.type).toBe('image');
      expect(element.x).toBe(0);
      expect(element.y).toBe(0);
      expect(element.width).toBe(50); // BaseElement default
      expect(element.height).toBe(50); // BaseElement default
      expect(element.src).toBe('');
      expect(element.aspectRatio).toBe('contain');
    });

    it('should create ImageElement with custom values', () => {
      const element = new ImageElement({
        x: 10,
        y: 20,
        width: 50,
        height: 50,
        src: 'https://example.com/image.png',
        dataBinding: 'device.Logo',
        aspectRatio: 'cover',
      });

      expect(element.x).toBe(10);
      expect(element.y).toBe(20);
      expect(element.width).toBe(50);
      expect(element.height).toBe(50);
      expect(element.src).toBe('https://example.com/image.png');
      expect(element.dataBinding).toBe('device.Logo');
      expect(element.aspectRatio).toBe('cover');
    });

    it('should generate unique ID', () => {
      const element1 = new ImageElement({});
      const element2 = new ImageElement({});

      expect(element1.id).toBeDefined();
      expect(element2.id).toBeDefined();
      expect(element1.id).not.toBe(element2.id);
    });
  });

  describe('JSON Serialization', () => {
    it('should serialize to JSON', () => {
      const element = new ImageElement({
        x: 15,
        y: 25,
        width: 60,
        height: 60,
        src: 'https://example.com/logo.png',
        dataBinding: 'device.CustomImage',
        aspectRatio: 'cover',
      });

      const json = element.toJSON();

      expect(json.type).toBe('image');
      expect(json.x).toBe(15);
      expect(json.y).toBe(25);
      expect(json.width).toBe(60);
      expect(json.height).toBe(60);
      expect(json.src).toBe('https://example.com/logo.png');
      expect(json.dataBinding).toBe('device.CustomImage');
      expect(json.aspectRatio).toBe('cover');
    });

    it('should deserialize from JSON', () => {
      const json = {
        id: 'image-1',
        type: 'image' as const,
        x: 20,
        y: 30,
        width: 70,
        height: 70,
        src: 'https://example.com/restored.png',
        dataBinding: 'device.Image',
        aspectRatio: 'stretch' as const,
      };

      const element = ImageElement.fromJSON(json);

      expect(element.id).toBe('image-1');
      expect(element.x).toBe(20);
      expect(element.y).toBe(30);
      expect(element.width).toBe(70);
      expect(element.height).toBe(70);
      expect(element.src).toBe('https://example.com/restored.png');
      expect(element.dataBinding).toBe('device.Image');
      expect(element.aspectRatio).toBe('stretch');
    });

    it('should round-trip through JSON', () => {
      const original = new ImageElement({
        x: 10,
        y: 15,
        width: 50,
        height: 50,
        src: 'https://example.com/test.png',
        aspectRatio: 'cover',
      });

      const json = original.toJSON();
      const restored = ImageElement.fromJSON(json);

      expect(restored.x).toBe(original.x);
      expect(restored.y).toBe(original.y);
      expect(restored.width).toBe(original.width);
      expect(restored.height).toBe(original.height);
      expect(restored.src).toBe(original.src);
      expect(restored.aspectRatio).toBe(original.aspectRatio);
    });

    it('should handle empty src', () => {
      const element = new ImageElement({});
      const json = element.toJSON();

      expect(json.src).toBe('');

      const restored = ImageElement.fromJSON(json);
      expect(restored.src).toBe('');
    });
  });

  describe('Image Source', () => {
    it('should store image src', () => {
      const element = new ImageElement({
        src: 'https://example.com/image.png',
      });
      expect(element.src).toBe('https://example.com/image.png');
    });

    it('should update image src', () => {
      const element = new ImageElement({
        src: 'https://example.com/old.png',
      });
      element.src = 'https://example.com/new.png';
      expect(element.src).toBe('https://example.com/new.png');
    });

    it('should have empty src by default', () => {
      const element = new ImageElement({});
      expect(element.src).toBe('');
    });
  });

  describe('Data Binding', () => {
    it('should support data binding', () => {
      const element = new ImageElement({ dataBinding: 'device.Logo' });
      expect(element.dataBinding).toBe('device.Logo');
    });

    it('should have default data binding', () => {
      const element = new ImageElement({});
      expect(element.dataBinding).toBe('connection.CompanyLogoUrl');
    });
  });
});
