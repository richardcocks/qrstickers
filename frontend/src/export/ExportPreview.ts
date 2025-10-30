/**
 * QR Sticker Export & Preview Engine (TypeScript)
 * Client-side placeholder generation and export functionality
 * Migrated from export-preview.js to TypeScript with Fabric.js v6
 */

import * as fabric from 'fabric';
import type {
  ExportOptions,
  PlaceholderMap,
  TemplateJson,
  TemplateObject,
  UploadedImage,
  FabricCanvasType
} from './types';

// Constants for unit conversion (144 DPI for screen display)
const MM_TO_PX_RATIO = 5.669291339;

/**
 * Realistic placeholder values for all data binding types
 */
export const PLACEHOLDER_VALUES: PlaceholderMap = {
  'device.serial': 'MS-1234-ABCD-5678',      // Cisco Meraki format
  'device.name': 'Example Switch',            // Descriptive name
  'device.mac': '00:1A:2B:3C:4D:5E',        // Valid MAC format
  'device.model': 'MS225-48FP',              // Actual Meraki model
  'device.ipaddress': '192.168.1.10',        // Valid IP
  'device.tags': 'production, datacenter',   // Comma-separated
  'connection.name': 'Main Office',          // Business location
  'connection.displayname': 'HQ Network',    // User-friendly name
  'connection.companylogourl': 'https://example.com/logo.png',  // Logo URL
  'network.name': 'Production Network',       // Network name
  'global.supporturl': 'support.example.com',  // Support website
  'global.supportphone': '+1-555-0100',       // Support phone
  // QR code data URIs (real scannable QR codes for preview)
  'device.qrcode': 'data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAa0AAAGtAQAAAACNLRaaAAABp0lEQVR4nO3bu3HDMAyAYehSpNQIHiWj2aNlFI3g0oXP9BEEXxLzrMDkR5E7hPxUCTRM0hJ+EzeBwWAwGAwGg8H+CbtKiSVc5WzD70v8e6+DNgKD+WXNTH3GKQ6+6b/uMemmwWCOmb7nr4WJjjxkTayvDRhsEpYSiRWgCQw2KZOXNAiDTcgOXXVM4mxl/TQYzC8rYYv5KexXdg0YzDfrorLUYh8DBvPLLmIvvcgaLrEviazrS0YVAIP5Ys223SON75L0QBjMO9MKSF310iZlJgw2B7O9jrJtpzOt3y4jsRxgMN/M+pJwKzM3yU1KCGGTOGIJDOab2cwcmy3m2m9rEoYVAIO5YrZT17ciurLbM5oEBnPMtAJy99G99Kst8/npMJhr1hwSagUcriQ1TQoM5ph10V2oqxsfw8KBwXyx0Xn34cSwBgzml+1vIeUb+3lD76vPABjMCxu1IrqYl6QGDDYFs6+OzXm3fLAdDYO5Zrv1Ww+/v/ebFRjMAbOXO99s3t2u6zaqYTDHrERZzJu7ofl+0qcbHzCYA/bDgMFgMBgMBoPBwp9nTwEx5Yt/LG0pAAAAAElFTkSuQmCC',
  'network.qrcode': 'data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAYgAAAGIAQAAAABzOEqLAAACEklEQVR4nO2XQY7EMAgE8/9PZ7WhG3CkHOYAp8pqMzamfGk1kOv+9bkgICAgICCCuOK5r+tZ31ePxEt7iBWiiRmUsv6DT17LgFggpFspltoqpAyIVSLUeqdISIhtIraibC45CmKTEKejekWr+a6JECOE+/3339fMADFA+NHZsTszIFYIi1S2Kt2egCcBiBWipbv1VP+Rqi9HQQwSClXuy0O6qjsKYo7QqxupucjfkRBrhAbiyNG5RgIvU0OIcSKaSjsx3NuORwOIDaJ1evcWt5lmNIgVotUuuyn9VcThQYhJQieaAELNs9dkw4FYIKyXnVT3eFYOS0GsEA1KtayoIhoNIDYIy1P9RpFqNK9yCDFJHHlStHq/zJUnENPE9cI1L1dxKwxigbBkoVvFVOFyNmjXQgwSWeaq0qXJTFhHiAXC41goqL1I1bo0HsQ40QqdTt19jlZ0eBBimtC/PSRfae8AxAbhZJW401bHfRALRDSb+kDxSf52L0GMExbIyqn5xCVxY99ADBPpm2YmpanaSVyIFaI3/hBKYqbN0lUQC4Ts9Ly9OJduPxAbhB83e+kWOYe4EBuEvxK1sYnUdcpUTXOISUJLa+VANRu1HIglwvo98ZzMVOgcSdtB7BDK1hAg7XJaPiocxAJhKHO9an6D2CBcyaq0WcqgZDeIHULHoVO/4TlKU0HsED89EBAQEBA3xP0HUHCq6tODNOAAAAAASUVORK5CYII=',
  'organization.qrcode': 'data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAYgAAAGIAQAAAABzOEqLAAACGUlEQVR4nO2XUY/DMAiD+///dE4NtiGtbtKkg3txpi1p4NuLhaHX+nZdJkyYMGHCRBBXrHV/9k087Yd73999Z2KEkDRXwfVzZJiYIEI/baV8GEPIxCSxdyXej3E28S/ETkMRFeU+KGiihyjVE0GoGThyTUwRZQL45fNhZjDx10RZUU1oPtT1zDDRThwCKSO7Dv/JxAixs6ja9jUd1YPS+Ex0E+ouO07NIFy5KhVlopNAGaHfq4yoLZiqoIlGIgsGpoYAbI0eZ2KIQFp4XLLUkO2IFyaaiVCLgFrLaXn0OBP9BOyL6kG+UljvTmSij2AuJrMIRyUFXQYyE/0EkigczUwxmt55a6KLYFWtp1jo+vXOxATBACtrHZoy8eFwJpqI8LbS6zkCVI3jL0wMEDK10nUUQmE9HM5EK1GHsZQydUOZmRgilIxR+FAx9mMuMNFJ5MyVyZiQ5XapqYkRQgKFmHpnxE4TNNFPSCxVlDxPs4GWiXaCd4zW1sN2c3YcE60ERcMZJVW+5EzMENIqE4rXVQc0MUBIIJ5YVUiVviaGiCOWQV4+31hM9BK4wjWxHAtKzzHRTnCVeTgtTk4HxEQ7wZKBx11FUUZeE5mJRgJHllGpIcwELwVNtBLs8bI0qKk+BElNjBKlscRDuh79zsQkEZJRRR5SPhMzhDhOyVBMLGrLxAhBjehqqZ0Ki4CJfuKrZcKECRMmlon1A+oPsuKNVshAAAAAAElFTkSuQmCC'
};

/**
 * Extract all data binding patterns from template JSON
 * Returns an array of unique bindings found in the template
 */
export function extractDataBindings(templateJson: TemplateJson): string[] {
  const bindings = new Set<string>();

  if (!templateJson || !templateJson.objects) {
    return Array.from(bindings);
  }

  templateJson.objects.forEach(obj => {
    // QR code data source
    if (obj.properties && obj.properties.dataSource) {
      const binding = obj.properties.dataSource.toLowerCase();
      bindings.add(binding);
    }

    // Text content (extract {{...}} patterns)
    if (obj.text) {
      const pattern = /\{\{([^}]+)\}\}/g;
      let match: RegExpExecArray | null;
      while ((match = pattern.exec(obj.text)) !== null) {
        const binding = match[1].toLowerCase();
        bindings.add(binding);
      }
    }

    // Image data source
    if (obj.type === 'image' && obj.properties && obj.properties.dataSource) {
      const binding = obj.properties.dataSource.toLowerCase();
      bindings.add(binding);
    }
  });

  return Array.from(bindings);
}

/**
 * Generate a generic placeholder for unknown data sources
 */
function generateGenericPlaceholder(binding: string): string {
  const parts = binding.split('.');
  if (parts.length === 2) {
    const [category, field] = parts;
    return `[${category}.${field}]`;
  }
  return `[${binding}]`;
}

/**
 * Generate placeholder map for all detected bindings
 */
export function generatePlaceholderMap(
  templateJson: TemplateJson,
  uploadedImages: UploadedImage[] = []
): PlaceholderMap {
  const bindings = extractDataBindings(templateJson);
  const placeholders: PlaceholderMap = {};

  bindings.forEach(binding => {
    // Direct lookup in PLACEHOLDER_VALUES
    if (PLACEHOLDER_VALUES[binding]) {
      placeholders[binding] = PLACEHOLDER_VALUES[binding];
    } else {
      // Generate a generic placeholder for unknown bindings
      placeholders[binding] = generateGenericPlaceholder(binding);
    }
  });

  // Add custom images from uploadedImages array
  if (uploadedImages && uploadedImages.length > 0) {
    uploadedImages.forEach(image => {
      const bindingKey = `customimage.image_${image.id}`;
      placeholders[bindingKey] = image.dataUri;
    });
  }

  return placeholders;
}

/**
 * Replace {{variable}} patterns in text with placeholder values
 */
function replacePlaceholders(text: string, placeholders: PlaceholderMap): string {
  let result = text;
  const pattern = /\{\{([^}]+)\}\}/g;

  result = result.replace(pattern, (match, binding) => {
    const normalizedBinding = binding.toLowerCase().trim();
    return placeholders[normalizedBinding] || match;
  });

  return result;
}

/**
 * Replace data bindings in template JSON with placeholder values
 */
export function createPreviewTemplate(
  templateJson: TemplateJson,
  placeholders: PlaceholderMap
): TemplateJson {
  // Deep clone the template to avoid mutating the original
  const previewTemplate: TemplateJson = JSON.parse(JSON.stringify(templateJson));

  if (!previewTemplate.objects) {
    return previewTemplate;
  }

  previewTemplate.objects = previewTemplate.objects.map(obj => {
    const objCopy: TemplateObject = JSON.parse(JSON.stringify(obj));

    // Replace QR code data source
    if (objCopy.properties && objCopy.properties.dataSource) {
      const binding = objCopy.properties.dataSource.toLowerCase();
      // For preview, we'll store the placeholder value in previewData
      objCopy.previewData = placeholders[binding] || PLACEHOLDER_VALUES[binding] || binding;

      // Also populate properties.data for custom images
      if (binding.startsWith('customimage.') && placeholders[binding]) {
        objCopy.properties.data = placeholders[binding];
      }
    }

    // Replace text elements with dataBinding (root-level)
    if (obj.type === 'text' && objCopy.dataBinding) {
      const binding = objCopy.dataBinding.toLowerCase();
      objCopy.text = placeholders[binding] || PLACEHOLDER_VALUES[binding] || `[${objCopy.dataBinding}]`;
    }

    // Replace text content ({{...}} patterns)
    if (objCopy.text) {
      objCopy.text = replacePlaceholders(objCopy.text, placeholders);
    }

    return objCopy;
  });

  return previewTemplate;
}

/**
 * Create QR code placeholder (Fabric.js Group)
 */
function createQRCodePlaceholder(options: {
  left: number;
  top: number;
  width: number;
  height: number;
  dataSource?: string;
  data?: string;
}): fabric.Group {
  const size = options.width || 100;

  // Create QR code visual placeholder
  const background = new fabric.Rect({
    width: size,
    height: size,
    fill: 'white',
    stroke: '#333',
    strokeWidth: 2,
  });

  // Create simple QR-like pattern
  const patternSize = size / 5;
  const patterns = [
    // Top-left corner
    new fabric.Rect({
      left: patternSize * 0.3,
      top: patternSize * 0.3,
      width: patternSize * 0.8,
      height: patternSize * 0.8,
      fill: 'black',
    }),
    // Top-right corner
    new fabric.Rect({
      left: patternSize * 3.9,
      top: patternSize * 0.3,
      width: patternSize * 0.8,
      height: patternSize * 0.8,
      fill: 'black',
    }),
    // Bottom-left corner
    new fabric.Rect({
      left: patternSize * 0.3,
      top: patternSize * 3.9,
      width: patternSize * 0.8,
      height: patternSize * 0.8,
      fill: 'black',
    }),
    // Center pattern
    new fabric.Rect({
      left: patternSize * 2,
      top: patternSize * 2,
      width: patternSize,
      height: patternSize,
      fill: 'black',
    }),
    // QR label
    new fabric.Text('QR', {
      left: size / 2,
      top: size / 2,
      fontSize: size / 4,
      fill: '#666',
      originX: 'center',
      originY: 'center',
      fontWeight: 'bold',
    }),
  ];

  const group = new fabric.Group([background, ...patterns], {
    left: options.left,
    top: options.top,
    selectable: false,
  });

  // Store custom properties
  (group as any).customType = 'qrcode';
  (group as any).dataSource = options.dataSource;

  return group;
}

/**
 * Create text object
 */
function createTextObject(options: {
  left: number;
  top: number;
  text: string;
  dataSource?: string;
  fontFamily?: string;
  fontSize?: number;
  fontWeight?: string;
  fill?: string;
}): fabric.IText {
  // Use placeholder value if dataSource is provided, otherwise use literal text
  let displayText = options.text || 'Text';
  if (options.dataSource) {
    const normalizedBinding = options.dataSource.toLowerCase();
    displayText = PLACEHOLDER_VALUES[normalizedBinding] || `[${options.dataSource}]`;
  }

  const text = new fabric.IText(displayText, {
    left: options.left || 50,
    top: options.top || 50,
    fontFamily: options.fontFamily || 'Arial',
    fontSize: options.fontSize || 16,
    fill: options.fill || '#000000',
    fontWeight: options.fontWeight || 'normal',
    selectable: false,
  });

  // Add custom data binding property
  (text as any).dataSource = options.dataSource || '';

  return text;
}

/**
 * Create image placeholder
 */
function createImagePlaceholder(options: {
  left: number;
  top: number;
  width: number;
  height: number;
  dataSource?: string;
  src?: string;
}): fabric.Group {
  const rect = new fabric.Rect({
    left: 0,
    top: 0,
    width: options.width || 100,
    height: options.height || 100,
    fill: '#f0f0f0',
    stroke: '#999',
    strokeWidth: 2,
    strokeDashArray: [5, 5],
  });

  const text = new fabric.Text('IMAGE', {
    left: (options.width || 100) / 2,
    top: (options.height || 100) / 2,
    fontSize: 14,
    fill: '#999',
    originX: 'center',
    originY: 'center',
  });

  const group = new fabric.Group([rect, text], {
    left: options.left || 50,
    top: options.top || 50,
    selectable: false,
  });

  // Add custom properties
  (group as any).customType = 'image';
  (group as any).dataSource = options.dataSource;
  (group as any).src = options.src;

  return group;
}

/**
 * Create and render a preview canvas with template and device data
 * Used by both template preview and device export
 */
export async function createAndRenderPreviewCanvas(
  canvasElement: HTMLCanvasElement,
  templateJson: TemplateJson,
  pageWidthMm: number = 100,
  pageHeightMm: number = 50,
  forExport: boolean = false,
  exportOptions: Partial<ExportOptions> = { dpi: 96, background: 'white' }
): Promise<FabricCanvasType> {
  // Calculate canvas dimensions
  const canvasWidth = pageWidthMm * MM_TO_PX_RATIO;
  const canvasHeight = pageHeightMm * MM_TO_PX_RATIO;

  // For export, apply DPI multiplier
  const multiplier = forExport ? (exportOptions.dpi || 96) / 96 : 1;
  const exportWidth = canvasWidth * multiplier;
  const exportHeight = canvasHeight * multiplier;

  // Set canvas element dimensions
  canvasElement.width = forExport ? exportWidth : canvasWidth;
  canvasElement.height = forExport ? exportHeight : canvasHeight;
  canvasElement.style.width = forExport ? exportWidth + 'px' : canvasWidth + 'px';
  canvasElement.style.height = forExport ? exportHeight + 'px' : canvasHeight + 'px';

  // Create Fabric.js canvas
  const canvas = new fabric.Canvas(canvasElement, {
    width: forExport ? exportWidth : canvasWidth,
    height: forExport ? exportHeight : canvasHeight,
    backgroundColor: exportOptions.background === 'transparent' ? 'rgba(0,0,0,0)' : '#ffffff',
    selection: false,
    renderOnAddRemove: true,
  });

  // Load template objects onto canvas
  const qrLoadPromises: Promise<void>[] = [];

  if (templateJson && templateJson.objects) {
    for (const obj of templateJson.objects) {
      try {
        // Convert coordinates from millimeters to pixels, then scale for DPI
        const scaledObj = JSON.parse(JSON.stringify(obj));

        // Step 1: Convert positional/dimensional properties from mm to px
        // Handle both x/y and left/top field naming conventions (designer uses x/y)
        const xMm = scaledObj.x ?? scaledObj.left ?? 0;
        const yMm = scaledObj.y ?? scaledObj.top ?? 0;
        const widthMm = scaledObj.width ?? 50;
        const heightMm = scaledObj.height ?? 50;

        scaledObj.left = xMm * MM_TO_PX_RATIO;
        scaledObj.top = yMm * MM_TO_PX_RATIO;
        scaledObj.width = widthMm * MM_TO_PX_RATIO;
        scaledObj.height = heightMm * MM_TO_PX_RATIO;

        // Convert fontSize from points to canvas pixels (144 DPI / 72 pt/inch = 2.0)
        // This matches the conversion done in TextElement.ts
        scaledObj.fontSize = (scaledObj.fontSize || 16) * 2.0;

        // Step 2: Apply DPI multiplier if exporting at higher resolution
        if (multiplier !== 1) {
          scaledObj.left *= multiplier;
          scaledObj.top *= multiplier;
          scaledObj.width *= multiplier;
          scaledObj.height *= multiplier;
          scaledObj.fontSize *= multiplier;  // Now scaling the already-converted canvas pixels
          scaledObj.strokeWidth = (scaledObj.strokeWidth || 1) * multiplier;
        }

        let fabricObject: fabric.FabricObject | fabric.Group | fabric.IText | null = null;

        switch (obj.type) {
          case 'qrcode':
            fabricObject = createQRCodePlaceholder({
              left: scaledObj.left,
              top: scaledObj.top,
              width: scaledObj.width || 50,
              height: scaledObj.height || 50,
              dataSource: obj.properties?.dataSource || '',
              data: obj.properties?.data || '',
            });
            break;

          case 'text':
          case 'i-text':
            fabricObject = createTextObject({
              left: scaledObj.left,
              top: scaledObj.top,
              text: scaledObj.text || '',
              dataSource: obj.dataBinding || obj.properties?.dataSource || '',
              fontFamily: scaledObj.fontFamily || 'Arial',
              fontSize: scaledObj.fontSize || 16,
              fontWeight: scaledObj.fontWeight || 'normal',
              fill: scaledObj.fill || '#000000',
            });
            break;

          case 'image':
            fabricObject = createImagePlaceholder({
              left: scaledObj.left,
              top: scaledObj.top,
              width: scaledObj.width || 50,
              height: scaledObj.height || 50,
              dataSource: obj.properties?.dataSource || '',
              src: obj.src || '',
            });
            break;

          case 'rect':
            fabricObject = new fabric.Rect({
              left: scaledObj.left,
              top: scaledObj.top,
              width: scaledObj.width || 50,
              height: scaledObj.height || 50,
              fill: scaledObj.fill || 'transparent',
              stroke: scaledObj.stroke || '#000000',
              strokeWidth: scaledObj.strokeWidth || 1,
              selectable: false,
            });
            break;

          case 'line':
            fabricObject = new fabric.Line([
              scaledObj.left,
              scaledObj.top,
              scaledObj.left + (scaledObj.width || 50),
              scaledObj.top,
            ], {
              stroke: scaledObj.stroke || '#000000',
              strokeWidth: scaledObj.strokeWidth || 1,
              selectable: false,
            });
            break;
        }

        if (fabricObject) {
          if (scaledObj.angle) {
            fabricObject.set('angle', scaledObj.angle);
          }

          let shouldAddToCanvas = true;

          // Replace QR code placeholder with real image if we have QR data
          if (obj.type === 'qrcode' && obj.properties?.data) {
            const qrDataUri = obj.properties.data;

            const qrLoadPromise = new Promise<void>((resolve, reject) => {
              fabric.FabricImage.fromURL(qrDataUri, {
                crossOrigin: 'anonymous',
              }).then((img) => {
                if (!img || !img.width) {
                  console.error('[Export Preview] QR image loaded but has no dimensions');
                  reject(new Error('QR image has no dimensions'));
                  return;
                }

                img.set({
                  left: fabricObject!.left,
                  top: fabricObject!.top,
                  scaleX: (fabricObject as fabric.Group).width! / img.width,
                  scaleY: (fabricObject as fabric.Group).height! / img.height,
                  angle: fabricObject!.angle || 0,
                  originX: 'center',
                  originY: 'center',
                  selectable: false,
                });

                canvas.add(img);
                canvas.renderAll();
                resolve();
              }).catch((error) => {
                console.error('[Export Preview] Error loading QR image:', error);
                reject(error);
              });
            });

            qrLoadPromises.push(qrLoadPromise);
            shouldAddToCanvas = false;  // Don't add placeholder
          }

          // Replace image placeholder with real image if we have custom image data
          if (obj.type === 'image' && obj.properties?.data) {
            const imageDataUri = obj.properties.data;

            const imageLoadPromise = new Promise<void>((resolve, reject) => {
              fabric.FabricImage.fromURL(imageDataUri, {
                crossOrigin: 'anonymous',
              }).then((img) => {
                if (!img || !img.width) {
                  console.error('[Export Preview] Custom image loaded but has no dimensions');
                  reject(new Error('Custom image has no dimensions'));
                  return;
                }

                img.set({
                  left: fabricObject!.left,
                  top: fabricObject!.top,
                  scaleX: (fabricObject as fabric.Group).width! / img.width,
                  scaleY: (fabricObject as fabric.Group).height! / img.height,
                  angle: fabricObject!.angle || 0,
                  originX: 'center',
                  originY: 'center',
                  selectable: false,
                });

                canvas.add(img);
                canvas.renderAll();
                resolve();
              }).catch((error) => {
                console.error('[Export Preview] Error loading custom image:', error);
                reject(error);
              });
            });

            qrLoadPromises.push(imageLoadPromise);
            shouldAddToCanvas = false;  // Don't add placeholder
          }

          if (shouldAddToCanvas) {
            canvas.add(fabricObject);
          }
        }
      } catch (error) {
        console.error(`[Export Preview] Error loading object:`, error);
      }
    }
  }

  // Wait for all QR/custom images to load
  if (qrLoadPromises.length > 0) {
    await Promise.all(qrLoadPromises);
  }

  canvas.renderAll();

  return canvas;
}

/**
 * Trigger file download
 */
function downloadFile(dataUrl: string, fileName: string, mimeType: string): void {
  try {
    const link = document.createElement('a');
    link.href = dataUrl;
    link.download = fileName;
    link.type = mimeType;

    // Ensure link is added to DOM (required in some browsers)
    document.body.appendChild(link);

    // Trigger download
    link.click();

    // Clean up
    document.body.removeChild(link);
  } catch (error) {
    console.error('Download error:', error);
    alert('Error downloading file: ' + (error as Error).message);
  }
}

/**
 * Export canvas as PNG with specified DPI
 */
export function exportPNG(
  canvas: FabricCanvasType,
  dpi: number = 96,
  _backgroundColor: 'white' | 'transparent' = 'white', // Prefix with _ to indicate intentionally unused (canvas already has background set)
  fileName: string = 'template-preview.png'
): void {
  try {
    // Calculate multiplier based on DPI
    const multiplier = dpi / 96;

    // Export options (backgroundColor already set on canvas during creation)
    const dataUrl = canvas.toDataURL({
      format: 'png',
      multiplier: multiplier,
      enableRetinaScaling: false,
    });

    // Download file
    downloadFile(dataUrl, fileName, 'image/png');
  } catch (error) {
    console.error('PNG export error:', error);
    alert('Error exporting PNG: ' + (error as Error).message);
  }
}

/**
 * Export canvas as SVG
 */
export function exportSVG(
  canvas: FabricCanvasType,
  fileName: string = 'template-preview.svg'
): void {
  try {
    // Export as SVG string
    const svgString = canvas.toSVG();

    // Create blob from SVG string
    const blob = new Blob([svgString], { type: 'image/svg+xml' });
    const url = URL.createObjectURL(blob);

    // Download file
    downloadFile(url, fileName, 'image/svg+xml');

    // Clean up object URL
    URL.revokeObjectURL(url);
  } catch (error) {
    console.error('SVG export error:', error);
    alert('Error exporting SVG: ' + (error as Error).message);
  }
}

/**
 * Export canvas as PNG with device context (for device export)
 */
export function exportPNGForDevice(
  canvas: FabricCanvasType,
  deviceIdentifier: string = 'device',
  dpi: number = 96,
  background: 'white' | 'transparent' = 'white'
): void {
  const fileName = `sticker-${deviceIdentifier.replace(/\s+/g, '-').toLowerCase()}-${dpi}dpi.png`;
  exportPNG(canvas, dpi, background, fileName);
}

/**
 * Export canvas as SVG with device context (for device export)
 */
export function exportSVGForDevice(
  canvas: FabricCanvasType,
  deviceIdentifier: string = 'device'
): void {
  const fileName = `sticker-${deviceIdentifier.replace(/\s+/g, '-').toLowerCase()}.svg`;
  exportSVG(canvas, fileName);
}
