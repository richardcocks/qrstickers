/**
 * Devices/Network page entry point
 * Loads when Network.cshtml is rendered
 * Provides export functionality for network devices
 */

// Import Fabric.js extensions (needed for export preview)
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
import { mmToPx, pxToMm } from '../../utils/units';

console.log('✅ Devices module loaded');

// Register QRCode class with Fabric.js
registerQRCodeClass();

// Expose functions globally for backwards compatibility
// This allows existing device-export.js and export-preview.js to use these functions
(window as any).QRCode = QRCode;
(window as any).createQRCode = createQRCode;
(window as any).createQRCodePlaceholder = createQRCodePlaceholder;
(window as any).createBoundText = createBoundText;
(window as any).createTextObject = createTextObject;
(window as any).createImagePlaceholder = createImagePlaceholder;
(window as any).createRectangle = createRectangle;
(window as any).createLine = createLine;
(window as any).mmToPx = mmToPx;
(window as any).pxToMm = pxToMm;

console.log('🔧 Fabric.js extensions registered globally');

export function initDevices(config?: any): void {
  console.log('📱 Devices initializing...', config);
  console.log('📦 Available fabric extensions:', {
    QRCode: typeof QRCode,
    createQRCode: typeof createQRCode,
    mmToPx: typeof mmToPx,
  });
}

// Auto-initialize on DOM ready
if (document.readyState === 'loading') {
  document.addEventListener('DOMContentLoaded', () => {
    const config = (window as any).devicesConfig;
    initDevices(config);
  });
} else {
  const config = (window as any).devicesConfig;
  initDevices(config);
}
