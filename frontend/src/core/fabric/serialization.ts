/**
 * Serialization utilities for converting Fabric.js canvas to template JSON
 * Handles conversion between canvas objects and portable template format
 */

import * as fabric from 'fabric';
import { pxToMm } from '../../utils/units';
import type {
  TemplateJson,
  SerializedObject,
  FabricObjectWithBinding,
} from './types';

/**
 * Generate a unique ID for template objects
 * Uses crypto.randomUUID() if available, falls back to timestamp-based ID
 */
export function generateId(): string {
  // Use crypto.randomUUID() if available (modern browsers)
  if (typeof crypto !== 'undefined' && crypto.randomUUID) {
    return `obj-${crypto.randomUUID()}`;
  }

  // Fallback to timestamp-based ID
  return `obj-${Date.now()}-${Math.random().toString(36).substring(2, 11)}`;
}

/**
 * Serialize a Fabric.js canvas to template JSON format
 *
 * @param canvas - The Fabric.js canvas to serialize
 * @param pageWidth - Page width in millimeters
 * @param pageHeight - Page height in millimeters
 * @param boundaryOffset - Optional offset of sticker boundary (defaults to 0,0)
 * @returns Template JSON object
 */
export function canvasToTemplateJson(
  canvas: fabric.Canvas,
  pageWidth: number,
  pageHeight: number,
  boundaryOffset: { left: number; top: number } = { left: 0, top: 0 }
): TemplateJson {
  const offsetX = boundaryOffset.left;
  const offsetY = boundaryOffset.top;

  // Filter out boundary and other non-exportable objects
  const objects = canvas
    .getObjects()
    .filter((obj) => {
      const extObj = obj as FabricObjectWithBinding;
      return !extObj.excludeFromExport && extObj.name !== 'stickerBoundary';
    })
    .map((obj): SerializedObject => {
      const extObj = obj as FabricObjectWithBinding;

      // Adjust positions to be relative to sticker boundary (0,0)
      const baseObj: any = {
        type: obj.type,
        id: extObj.id || generateId(),
        left: pxToMm((obj.left ?? 0) - offsetX),
        top: pxToMm((obj.top ?? 0) - offsetY),
        width: pxToMm(obj.getScaledWidth()),
        height: pxToMm(obj.getScaledHeight()),
        scaleX: obj.scaleX ?? 1,
        scaleY: obj.scaleY ?? 1,
        angle: obj.angle ?? 0,
      };

      // Add type-specific properties
      if (obj.type === 'qrcode') {
        baseObj.properties = {
          dataSource: (obj as any).dataSource || extObj.dataSource || 'device.Serial',
          eccLevel: (obj as any).eccLevel || (obj as any).get?.('eccLevel') || 'Q',
          quietZone: (obj as any).quietZone || (obj as any).get?.('quietZone') || 2,
        };
      } else if (obj.type === 'i-text' || obj.type === 'text') {
        const textObj = obj as fabric.IText;
        baseObj.text = textObj.text;
        baseObj.fontFamily = textObj.fontFamily;
        baseObj.fontSize = textObj.fontSize;
        baseObj.fontWeight = textObj.fontWeight;
        baseObj.fill = textObj.fill;
        baseObj.properties = {
          dataSource: (textObj as any).dataSource || extObj.dataSource || '',
          maxLength: (textObj as any).maxLength || (textObj as any).get?.('maxLength') || null,
          overflow: (textObj as any).overflow || (textObj as any).get?.('overflow') || 'truncate',
        };
      } else if (obj.type === 'image' || (obj.type === 'group' && (obj as any).get?.('type') === 'image')) {
        const imgObj = obj as any;
        baseObj.src = imgObj.src || imgObj.get?.('src') || '';
        baseObj.properties = {
          dataSource: imgObj.dataSource || extObj.dataSource || '',
          aspectRatio: imgObj.aspectRatio || imgObj.get?.('aspectRatio') || 'contain',
          placeholder: true,
          customImageId: imgObj.customImageId || imgObj.get?.('customImageId'),
          customImageName: imgObj.customImageName || imgObj.get?.('customImageName'),
        };
      } else if (obj.type === 'rect') {
        const rectObj = obj as fabric.Rect;
        baseObj.fill = rectObj.fill;
        baseObj.stroke = rectObj.stroke;
        baseObj.strokeWidth = rectObj.strokeWidth;
      } else if (obj.type === 'line') {
        const lineObj = obj as fabric.Line;
        baseObj.stroke = lineObj.stroke;
        baseObj.strokeWidth = lineObj.strokeWidth;
      }

      return baseObj;
    });

  return {
    version: '1.0',
    fabricVersion: fabric.version,
    pageSize: {
      width: pageWidth,
      height: pageHeight,
      unit: 'mm',
    },
    objects,
  };
}

/**
 * Get boundary offset from canvas
 * Searches for an object named 'stickerBoundary' and returns its position
 *
 * @param canvas - The Fabric.js canvas to search
 * @returns Boundary offset or {left: 0, top: 0} if not found
 */
export function getBoundaryOffset(canvas: fabric.Canvas): { left: number; top: number } {
  const boundary = canvas.getObjects().find((obj) => {
    const extObj = obj as FabricObjectWithBinding;
    return extObj.name === 'stickerBoundary';
  });

  if (boundary) {
    return {
      left: boundary.left ?? 0,
      top: boundary.top ?? 0,
    };
  }

  return { left: 0, top: 0 };
}

/**
 * Calculate bounding box for all objects on canvas
 * Useful for determining required page size
 *
 * @param canvas - The Fabric.js canvas
 * @returns Bounding box {left, top, width, height} in pixels
 */
export function getCanvasBoundingBox(canvas: fabric.Canvas): {
  left: number;
  top: number;
  width: number;
  height: number;
} {
  const objects = canvas.getObjects().filter((obj) => {
    const extObj = obj as FabricObjectWithBinding;
    return !extObj.excludeFromExport && extObj.name !== 'stickerBoundary';
  });

  if (objects.length === 0) {
    return { left: 0, top: 0, width: 0, height: 0 };
  }

  let minX = Infinity;
  let minY = Infinity;
  let maxX = -Infinity;
  let maxY = -Infinity;

  objects.forEach((obj) => {
    const left = obj.left ?? 0;
    const top = obj.top ?? 0;
    const width = obj.getScaledWidth();
    const height = obj.getScaledHeight();

    minX = Math.min(minX, left);
    minY = Math.min(minY, top);
    maxX = Math.max(maxX, left + width);
    maxY = Math.max(maxY, top + height);
  });

  return {
    left: minX,
    top: minY,
    width: maxX - minX,
    height: maxY - minY,
  };
}

/**
 * Validate template JSON structure
 * Checks that the template has required fields and valid values
 *
 * @param json - The template JSON to validate
 * @returns Array of validation error messages (empty if valid)
 */
export function validateTemplateJson(json: any): string[] {
  const errors: string[] = [];

  if (!json) {
    errors.push('Template JSON is null or undefined');
    return errors;
  }

  if (!json.version) {
    errors.push('Missing version field');
  }

  if (!json.pageSize) {
    errors.push('Missing pageSize field');
  } else {
    if (typeof json.pageSize.width !== 'number' || json.pageSize.width <= 0) {
      errors.push('Invalid pageSize.width');
    }
    if (typeof json.pageSize.height !== 'number' || json.pageSize.height <= 0) {
      errors.push('Invalid pageSize.height');
    }
    if (!['mm', 'px', 'in'].includes(json.pageSize.unit)) {
      errors.push('Invalid pageSize.unit (must be mm, px, or in)');
    }
  }

  if (!Array.isArray(json.objects)) {
    errors.push('objects must be an array');
  } else {
    json.objects.forEach((obj: any, index: number) => {
      if (!obj.type) {
        errors.push(`Object ${index}: Missing type field`);
      }
      if (!obj.id) {
        errors.push(`Object ${index}: Missing id field`);
      }
      if (typeof obj.left !== 'number') {
        errors.push(`Object ${index}: Invalid left position`);
      }
      if (typeof obj.top !== 'number') {
        errors.push(`Object ${index}: Invalid top position`);
      }
    });
  }

  return errors;
}
