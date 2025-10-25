/**
 * Designer page entry point
 * Loads Fabric.js extensions and initializes the designer
 */

// Import Fabric.js extensions
import { QRCode, registerQRCodeClass } from '../../core/fabric/QRCodeObject';
import {
  createQRCode,
  createQRCodePlaceholder,
  createBoundText,
  createTextObject,
  createImagePlaceholder,
  createRectangle,
  createLine,
} from '../../core/fabric/factories';
import {
  getDataBinding,
  updateDataBinding,
  hasDataBinding,
  clearDataBinding,
  getCanvasDataSources,
  validateDataSources,
} from '../../core/fabric/dataBinding';
import {
  generateId,
  canvasToTemplateJson,
  getBoundaryOffset,
  getCanvasBoundingBox,
  validateTemplateJson,
} from '../../core/fabric/serialization';
import { mmToPx, pxToMm } from '../../utils/units';

console.log('✅ Designer module loaded');

// Register QRCode class with Fabric.js
registerQRCodeClass();

// Expose functions globally for backwards compatibility
// This allows existing designer.js to use these functions
(window as any).QRCode = QRCode;
(window as any).createQRCode = createQRCode;
(window as any).createQRCodePlaceholder = createQRCodePlaceholder;
(window as any).createBoundText = createBoundText;
(window as any).createTextObject = createTextObject;
(window as any).createImagePlaceholder = createImagePlaceholder;
(window as any).createRectangle = createRectangle;
(window as any).createLine = createLine;
(window as any).getDataBinding = getDataBinding;
(window as any).updateDataBinding = updateDataBinding;
(window as any).hasDataBinding = hasDataBinding;
(window as any).clearDataBinding = clearDataBinding;
(window as any).getCanvasDataSources = getCanvasDataSources;
(window as any).validateDataSources = validateDataSources;
(window as any).generateId = generateId;
(window as any).canvasToTemplateJson = canvasToTemplateJson;
(window as any).getBoundaryOffset = getBoundaryOffset;
(window as any).getCanvasBoundingBox = getCanvasBoundingBox;
(window as any).validateTemplateJson = validateTemplateJson;
(window as any).mmToPx = mmToPx;
(window as any).pxToMm = pxToMm;

console.log('🔧 Fabric.js extensions registered globally');

export function initDesigner(config?: any): void {
  console.log('🎨 Designer initializing...', config);
  console.log('📦 Available fabric extensions:', {
    QRCode: typeof QRCode,
    createQRCode: typeof createQRCode,
    canvasToTemplateJson: typeof canvasToTemplateJson,
    mmToPx: typeof mmToPx,
  });
}

// Auto-initialize on DOM ready
if (document.readyState === 'loading') {
  document.addEventListener('DOMContentLoaded', () => {
    const config = (window as any).designerConfig;
    initDesigner(config);
  });
} else {
  const config = (window as any).designerConfig;
  initDesigner(config);
}
