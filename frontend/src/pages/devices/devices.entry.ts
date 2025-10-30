/**
 * Devices/Network page entry point
 * Loads when Network.cshtml is rendered
 * Provides export functionality for network devices
 */

import { mmToPx, pxToMm } from '../../utils/units';

// Expose utilities globally for backwards compatibility
(window as any).mmToPx = mmToPx;
(window as any).pxToMm = pxToMm;

export function initDevices(): void {
  // TODO: Wire up device export functionality
  // Config will be available from window.devicesConfig when needed
}

// Auto-initialize on DOM ready
if (document.readyState === 'loading') {
  document.addEventListener('DOMContentLoaded', () => {
    initDevices();
  });
} else {
  initDevices();
}
