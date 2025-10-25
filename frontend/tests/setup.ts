import { expect, beforeAll } from 'vitest';

// Setup code that runs before all tests
console.log('Test environment initialized');

// Mock canvas for Fabric.js tests
// happy-dom has incomplete canvas implementation, so we need to enhance it
beforeAll(() => {
  if (typeof HTMLCanvasElement !== 'undefined') {
    const originalGetContext = HTMLCanvasElement.prototype.getContext;

    HTMLCanvasElement.prototype.getContext = function(this: HTMLCanvasElement, contextType: string, ...args: any[]) {
      if (contextType === '2d') {
        // Return a comprehensive mock 2D context
        const mockContext = {
          canvas: this,
          // Styles
          fillStyle: '#000000',
          strokeStyle: '#000000',
          globalAlpha: 1,
          globalCompositeOperation: 'source-over',
          // Line styles
          lineWidth: 1,
          lineCap: 'butt',
          lineJoin: 'miter',
          miterLimit: 10,
          lineDashOffset: 0,
          // Shadows
          shadowBlur: 0,
          shadowColor: 'rgba(0,0,0,0)',
          shadowOffsetX: 0,
          shadowOffsetY: 0,
          // Text
          font: '10px sans-serif',
          textAlign: 'start',
          textBaseline: 'alphabetic',
          direction: 'inherit',
          // Transformations
          scale: () => mockContext,
          rotate: () => mockContext,
          translate: () => mockContext,
          transform: () => mockContext,
          setTransform: () => mockContext,
          resetTransform: () => mockContext,
          getTransform: () => ({ a: 1, b: 0, c: 0, d: 1, e: 0, f: 0 }),
          // Drawing rectangles
          clearRect: () => {},
          fillRect: () => {},
          strokeRect: () => {},
          // Drawing paths
          beginPath: () => {},
          closePath: () => {},
          moveTo: () => {},
          lineTo: () => {},
          bezierCurveTo: () => {},
          quadraticCurveTo: () => {},
          arc: () => {},
          arcTo: () => {},
          ellipse: () => {},
          rect: () => {},
          // Drawing
          fill: () => {},
          stroke: () => {},
          clip: () => {},
          isPointInPath: () => false,
          isPointInStroke: () => false,
          // Text
          fillText: () => {},
          strokeText: () => {},
          measureText: (text: string) => ({
            width: text.length * 8,
            actualBoundingBoxLeft: 0,
            actualBoundingBoxRight: text.length * 8,
            actualBoundingBoxAscent: 8,
            actualBoundingBoxDescent: 2,
            fontBoundingBoxAscent: 10,
            fontBoundingBoxDescent: 2,
            alphabeticBaseline: 0,
            hangingBaseline: 0,
            ideographicBaseline: 0,
          }),
          // Images
          drawImage: () => {},
          createImageData: () => ({ data: new Uint8ClampedArray(), width: 0, height: 0, colorSpace: 'srgb' }),
          getImageData: () => ({ data: new Uint8ClampedArray(), width: 0, height: 0, colorSpace: 'srgb' }),
          putImageData: () => {},
          // State
          save: () => {},
          restore: () => {},
          // Gradients and patterns
          createLinearGradient: () => ({
            addColorStop: () => {},
          }),
          createRadialGradient: () => ({
            addColorStop: () => {},
          }),
          createConicGradient: () => ({
            addColorStop: () => {},
          }),
          createPattern: () => null,
          // Line dashes
          setLineDash: () => {},
          getLineDash: () => [],
          // Pixel manipulation
          getContextAttributes: () => ({ alpha: true, desynchronized: false }),
        };

        return mockContext;
      }

      // For other context types, try original implementation
      return originalGetContext?.call(this, contextType, ...args) || null;
    };
  }
});

// You can add custom matchers or global test utilities here
