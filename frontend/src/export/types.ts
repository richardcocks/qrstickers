/**
 * Type definitions for QR Sticker Export functionality
 */

import { Canvas as FabricCanvas } from 'fabric';

export interface ExportOptions {
  dpi: 96 | 150 | 300;
  background: 'white' | 'transparent';
  format: 'png' | 'svg';
}

export interface TemplateData {
  pageWidth: number;  // in mm
  pageHeight: number;  // in mm
  templateJson: string;  // JSON-serialized template
}

export interface UploadedImage {
  id: number;
  name: string;
  dataUri: string;  // base64 data URI
  widthPx: number;
  heightPx: number;
}

export interface PlaceholderMap {
  [key: string]: string;
}

export interface TemplateJson {
  version?: string;
  fabricVersion?: string;
  pageSize?: {
    width: number;
    height: number;
    unit: string;
  };
  objects: TemplateObject[];
}

export interface TemplateObject {
  type: string;
  id?: string;
  left: number;
  top: number;
  width?: number;
  height?: number;
  scaleX?: number;
  scaleY?: number;
  angle?: number;
  dataBinding?: string;  // Data binding for text elements (e.g., "device.serial", "network.name")

  // QR Code specific
  properties?: {
    dataSource?: string;
    eccLevel?: string;
    quietZone?: number;
    data?: string;  // Actual QR code data URI (for export)
    maxLength?: number;
    overflow?: string;
    aspectRatio?: string;
    placeholder?: boolean;
    customImageId?: number;
    customImageName?: string;
  };

  // Text specific
  text?: string;
  fontFamily?: string;
  fontSize?: number;
  fontWeight?: string;
  fill?: string;

  // Image specific
  src?: string;

  // Shape specific
  stroke?: string;
  strokeWidth?: number;

  // Preview data (for placeholder rendering)
  previewData?: string;
}

export interface DeviceData {
  id: number;
  serial: string;
  name: string;
  mac?: string;
  model: string;
  productType: string;
  type?: string;
  status?: string;
  firmware?: string;
  tags: string[];
  tags_str: string;
  networkId: number;
  connectionId: number;
  qrcode: string;  // data URI
}

export interface NetworkData {
  id: number;
  name: string;
  organizationId: number;
  qrcode: string;  // data URI
}

export interface OrganizationData {
  id: number;
  organizationid: string;
  name: string;
  url: string;
  qrcode: string;  // data URI
}

export interface ConnectionData {
  id: number;
  displayName: string;
  type: string;
  companyLogoUrl?: string;
}

export interface DeviceDataMap {
  device: Partial<DeviceData>;
  network: Partial<NetworkData>;
  organization: Partial<OrganizationData>;
  connection: Partial<ConnectionData>;
  global: { [key: string]: string };
  [key: string]: any;  // For custom images like "customimage.image_42"
}

export type FabricCanvasType = FabricCanvas;
