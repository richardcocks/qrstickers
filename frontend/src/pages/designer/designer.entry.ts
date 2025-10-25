/**
 * Designer page entry point
 * New clean implementation using wrapper classes
 */

import { Designer } from '../../designer/Designer';
import { mmToPx, pxToMm } from '../../utils/units';

console.log('✅ Designer module loaded (new implementation)');

// Expose utilities globally for backwards compatibility
(window as any).mmToPx = mmToPx;
(window as any).pxToMm = pxToMm;

// Expose Designer class globally
(window as any).Designer = Designer;

export function initDesigner(config?: any): void {
  console.log('🎨 Designer initializing (new clean architecture)...', config);

  // TODO: Initialize the designer when we integrate with the page
  // For now, just log that we're ready
  console.log('📦 Designer class available:', typeof Designer);
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
