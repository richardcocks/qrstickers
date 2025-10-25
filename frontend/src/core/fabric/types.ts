/**
 * TypeScript type definitions for QRStickers template designer
 * Fabric.js extensions and custom objects
 */

import * as fabric from 'fabric';

// ============================================================================
// Template Object Properties
// ============================================================================

/**
 * QR Code specific properties
 */
export interface QRCodeProperties {
  /** Data source path (e.g., "device.Serial") */
  dataSource: string;
  /** Error correction level: L, M, Q, H */
  eccLevel: 'L' | 'M' | 'Q' | 'H';
  /** Quiet zone size in modules */
  quietZone: number;
}

/**
 * Text object specific properties
 */
export interface TextProperties {
  /** Data source path (e.g., "device.Name") */
  dataSource: string;
  /** Maximum character length */
  maxLength: number | null;
  /** Overflow behavior */
  overflow: 'truncate' | 'wrap' | 'scale';
}

/**
 * Image object specific properties
 */
export interface ImageProperties {
  /** Data source path (e.g., "connection.CompanyLogoUrl") */
  dataSource: string;
  /** How to fit image in bounds */
  aspectRatio: 'contain' | 'cover' | 'stretch';
  /** Whether this is a placeholder */
  placeholder?: boolean;
  /** Custom uploaded image ID */
  customImageId?: number;
  /** Custom uploaded image name */
  customImageName?: string;
}

// ============================================================================
// Fabric.js Object Extensions
// ============================================================================

/**
 * Extended Fabric.js object with custom properties
 */
export interface FabricObjectWithBinding extends fabric.Object {
  /** Unique identifier for this object */
  id?: string;
  /** Data source binding path */
  dataSource?: string;
  /** Whether to exclude from export */
  excludeFromExport?: boolean;
  /** Custom name identifier */
  name?: string;
}

/**
 * Custom QRCode Fabric.js object (extends Group)
 */
export interface FabricQRCode extends fabric.Group, FabricObjectWithBinding {
  type: 'qrcode';
  dataSource: string;
  eccLevel: 'L' | 'M' | 'Q' | 'H';
  quietZone: number;
}

/**
 * Text object with data binding
 */
export interface FabricBoundText extends fabric.IText, FabricObjectWithBinding {
  dataSource?: string;
  maxLength?: number | null;
  overflow?: 'truncate' | 'wrap' | 'scale';
}

/**
 * Image placeholder group
 */
export interface FabricImagePlaceholder extends fabric.Group, FabricObjectWithBinding {
  type: 'image';
  dataSource?: string;
  src?: string;
  aspectRatio?: 'contain' | 'cover' | 'stretch';
  customImageId?: number;
  customImageName?: string;
}

// ============================================================================
// Factory Function Options
// ============================================================================

/**
 * Options for creating a QR code object
 */
export interface QRCodeOptions {
  left?: number;
  top?: number;
  width?: number;
  height?: number;
  dataSource?: string;
  eccLevel?: 'L' | 'M' | 'Q' | 'H';
  quietZone?: number;
}

/**
 * Options for creating bound text
 */
export interface BoundTextOptions {
  left?: number;
  top?: number;
  text?: string;
  fontFamily?: string;
  fontSize?: number;
  fill?: string;
  fontWeight?: string | number;
  dataSource?: string;
  maxLength?: number | null;
  overflow?: 'truncate' | 'wrap' | 'scale';
}

/**
 * Options for creating image placeholder
 */
export interface ImagePlaceholderOptions {
  left?: number;
  top?: number;
  width?: number;
  height?: number;
  dataSource?: string;
  src?: string;
  aspectRatio?: 'contain' | 'cover' | 'stretch';
  customImageId?: number;
  customImageName?: string;
}

/**
 * Options for creating rectangle
 */
export interface RectangleOptions {
  left?: number;
  top?: number;
  width?: number;
  height?: number;
  fill?: string;
  stroke?: string;
  strokeWidth?: number;
}

/**
 * Options for creating line
 */
export interface LineOptions {
  x1?: number;
  y1?: number;
  x2?: number;
  y2?: number;
  stroke?: string;
  strokeWidth?: number;
}

// ============================================================================
// Data Binding Types
// ============================================================================

/**
 * Data binding information for an object
 */
export interface DataBindingInfo {
  /** Object type */
  type: string;
  /** Data source path if bound */
  dataSource: string | null;
  /** Whether object has data binding */
  hasBinding: boolean;
}

// ============================================================================
// Template JSON Format
// ============================================================================

/**
 * Page size specification
 */
export interface PageSize {
  width: number;
  height: number;
  unit: 'mm' | 'px' | 'in';
}

/**
 * Serialized template object (base properties)
 */
export interface SerializedTemplateObject {
  type: string;
  id: string;
  left: number;
  top: number;
  width: number;
  height: number;
  scaleX: number;
  scaleY: number;
  angle: number;
  properties?: QRCodeProperties | TextProperties | ImageProperties;
}

/**
 * Serialized QR code object
 */
export interface SerializedQRCode extends SerializedTemplateObject {
  type: 'qrcode';
  properties: QRCodeProperties;
}

/**
 * Serialized text object
 */
export interface SerializedText extends SerializedTemplateObject {
  type: 'i-text' | 'text';
  text: string;
  fontFamily: string;
  fontSize: number;
  fontWeight: string | number;
  fill: string;
  properties: TextProperties;
}

/**
 * Serialized image object
 */
export interface SerializedImage extends SerializedTemplateObject {
  type: 'image' | 'group';
  src: string;
  properties: ImageProperties;
}

/**
 * Serialized rectangle object
 */
export interface SerializedRect extends SerializedTemplateObject {
  type: 'rect';
  fill: string;
  stroke: string;
  strokeWidth: number;
}

/**
 * Serialized line object
 */
export interface SerializedLine extends SerializedTemplateObject {
  type: 'line';
  stroke: string;
  strokeWidth: number;
}

/**
 * Union type for all serialized objects
 */
export type SerializedObject =
  | SerializedQRCode
  | SerializedText
  | SerializedImage
  | SerializedRect
  | SerializedLine;

/**
 * Complete template JSON format
 */
export interface TemplateJson {
  version: string;
  fabricVersion: string;
  pageSize: PageSize;
  objects: SerializedObject[];
}

// ============================================================================
// Type Guards
// ============================================================================

/**
 * Check if object is a QR code
 */
export function isQRCode(obj: fabric.Object): obj is FabricQRCode {
  return obj.type === 'qrcode';
}

/**
 * Check if object is bound text
 */
export function isBoundText(obj: fabric.Object): obj is FabricBoundText {
  return obj.type === 'i-text' || obj.type === 'text';
}

/**
 * Check if object is image placeholder
 */
export function isImagePlaceholder(obj: fabric.Object): obj is FabricImagePlaceholder {
  return obj.type === 'image' || (obj.type === 'group' && (obj as any).get?.('type') === 'image');
}

/**
 * Check if object has data binding
 */
export function hasDataBinding(obj: fabric.Object): obj is FabricObjectWithBinding {
  const binding = (obj as FabricObjectWithBinding).dataSource || (obj as any).get?.('dataSource');
  return !!binding;
}
