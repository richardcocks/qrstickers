/**
 * Export Shared Entry Point
 * Exposes export functions globally for use by legacy JavaScript (device-export.js)
 * This bundle is loaded by both Designer.cshtml and Network.cshtml
 */

import * as ExportPreview from '../../export/ExportPreview';
import type { PlaceholderMap } from '../../export/types';

// Export all functions and constants to window object for legacy JavaScript
declare global {
  interface Window {
    // Constants
    PLACEHOLDER_VALUES: PlaceholderMap;
    MM_TO_PX_RATIO: number;

    // Functions
    extractDataBindings: typeof ExportPreview.extractDataBindings;
    generatePlaceholderMap: typeof ExportPreview.generatePlaceholderMap;
    createPreviewTemplate: typeof ExportPreview.createPreviewTemplate;
    createAndRenderPreviewCanvas: typeof ExportPreview.createAndRenderPreviewCanvas;
    exportPNG: typeof ExportPreview.exportPNG;
    exportSVG: typeof ExportPreview.exportSVG;
    exportPNGForDevice: typeof ExportPreview.exportPNGForDevice;
    exportSVGForDevice: typeof ExportPreview.exportSVGForDevice;

    // Helper functions for backward compatibility
    mmToPx: (mm: number) => number;
    pxToMm: (px: number) => number;
  }
}

// Constants
const MM_TO_PX_RATIO = 5.669291339; // 144 DPI

// Export to window object
window.PLACEHOLDER_VALUES = ExportPreview.PLACEHOLDER_VALUES;
window.MM_TO_PX_RATIO = MM_TO_PX_RATIO;

// Core export functions
window.extractDataBindings = ExportPreview.extractDataBindings;
window.generatePlaceholderMap = ExportPreview.generatePlaceholderMap;
window.createPreviewTemplate = ExportPreview.createPreviewTemplate;
window.createAndRenderPreviewCanvas = ExportPreview.createAndRenderPreviewCanvas;
window.exportPNG = ExportPreview.exportPNG;
window.exportSVG = ExportPreview.exportSVG;
window.exportPNGForDevice = ExportPreview.exportPNGForDevice;
window.exportSVGForDevice = ExportPreview.exportSVGForDevice;

// Helper functions (for backward compatibility with fabric-extensions.js)
window.mmToPx = (mm: number) => mm * MM_TO_PX_RATIO;
window.pxToMm = (px: number) => px / MM_TO_PX_RATIO;
