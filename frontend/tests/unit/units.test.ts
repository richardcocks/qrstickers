import { describe, it, expect } from 'vitest';
import { mmToPx, pxToMm } from '../../src/utils/units';

describe('units', () => {
  describe('mmToPx', () => {
    it('should convert millimeters to pixels', () => {
      expect(mmToPx(10)).toBeCloseTo(56.69, 2);
      expect(mmToPx(0)).toBe(0);
    });
  });

  describe('pxToMm', () => {
    it('should convert pixels to millimeters', () => {
      expect(pxToMm(56.69)).toBeCloseTo(10, 1);
      expect(pxToMm(0)).toBe(0);
    });

    it('should be inverse of mmToPx', () => {
      const mm = 25.4;
      expect(pxToMm(mmToPx(mm))).toBeCloseTo(mm, 2);
    });
  });
});
