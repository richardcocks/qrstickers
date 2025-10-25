/**
 * Unit conversion utilities
 * Converts between millimeters and pixels for canvas rendering
 */

// 144 DPI conversion (same as your current fabric-extensions.js)
const MM_TO_PX_RATIO = 5.669291339;

/**
 * Convert millimeters to pixels
 */
export function mmToPx(mm: number): number {
  return mm * MM_TO_PX_RATIO;
}

/**
 * Convert pixels to millimeters
 */
export function pxToMm(px: number): number {
  return px / MM_TO_PX_RATIO;
}
