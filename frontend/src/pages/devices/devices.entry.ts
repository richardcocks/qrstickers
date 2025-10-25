/**
 * Devices/Network page entry point
 * Loads when Network.cshtml is rendered
 * Provides export functionality for network devices
 */

import { mmToPx, pxToMm } from '../../utils/units';

console.log('✅ Devices module loaded (new implementation)');

// Expose utilities globally for backwards compatibility
(window as any).mmToPx = mmToPx;
(window as any).pxToMm = pxToMm;

export function initDevices(config?: any): void {
  console.log('📱 Devices initializing...', config);
  // TODO: Wire up device export functionality
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
