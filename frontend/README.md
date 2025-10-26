# Frontend - QRStickers Designer

Clean TypeScript implementation of the sticker designer using Vite + Fabric.js.

## Quick Start

### Development & Testing

Start the Vite dev server to test the Designer:

```bash
npm install
npm run dev
```

Open http://localhost:5173 in your browser to access the interactive test page.

### Production Build

Build optimized bundles for ASP.NET integration:

```bash
npm run build
```

Outputs to `../src/wwwroot/dist/` with:
- `assets/designer-[hash].js` - Designer module
- `assets/devices-[hash].js` - Devices module
- `manifest.json` - Build manifest for cache-busting

### Run Tests

```bash
npm test
```

## Project Structure

```
frontend/
├── src/
│   ├── designer/              # Core Designer implementation
│   │   ├── Designer.ts        # Main controller class
│   │   ├── CanvasWrapper.ts   # Fabric.js wrapper
│   │   └── elements/          # Element classes
│   │       ├── BaseElement.ts
│   │       ├── QRElement.ts
│   │       ├── TextElement.ts
│   │       ├── ImageElement.ts
│   │       └── RectElement.ts
│   ├── pages/                 # Entry points for ASP.NET pages
│   │   ├── designer/
│   │   │   └── designer.entry.ts
│   │   └── devices/
│   │       └── devices.entry.ts
│   └── utils/                 # Shared utilities
│
├── tests/                     # Unit tests (Vitest)
│   └── unit/
│       ├── designer/
│       │   ├── Designer.test.ts
│       │   └── elements/
│       └── utils/
│
├── index.html                 # Dev server test page
├── vite.config.ts            # Vite configuration
├── vitest.config.ts          # Test configuration
└── tsconfig.json             # TypeScript configuration
```

## Designer API

### Basic Usage

```typescript
import { Designer } from './src/designer/Designer';

const designer = new Designer({
  containerId: 'myCanvas',
  widthMm: 100,
  heightMm: 50,
  onSelectionChange: (element) => console.log('Selected:', element),
  onElementsChange: () => console.log('Elements changed'),
});

// Add elements
designer.addElement('qr', { x: 10, y: 10 });
designer.addElement('text', { x: 40, y: 10 });

// Save/load templates
const json = designer.saveTemplate();
designer.loadTemplate(json);

// Undo/redo (fully functional with proper history management)
designer.undo();    // Move back one step in history
designer.redo();    // Move forward one step in history

// Get state
const elements = designer.getElements();
const selected = designer.getSelectedElement();
```

### Element Types

- `qr` - QR code placeholder
- `text` - Text label
- `image` - Image placeholder
- `rect` - Rectangle shape

## Architecture Decisions

### Wrapper Pattern
All Fabric.js complexity is hidden behind clean wrapper classes:
- `CanvasWrapper` - Wraps Fabric.Canvas
- `BaseElement` - Abstract base for all elements
- Uses `any` for internal Fabric objects to avoid type conflicts

### Element Hierarchy
```
BaseElement (abstract)
├── QRElement
├── TextElement
├── ImageElement
└── RectElement
```

All elements:
- Store dimensions in millimeters
- Handle MM ↔ PX conversion internally
- Manage Fabric.js object lifecycle
- Serialize to/from JSON

### State Management
- **Undo/redo**: Index-based history system with JSON snapshots (50-step limit)
  - History array stores all states
  - Single index pointer tracks current position
  - Undo/redo by moving index backward/forward
  - New actions after undo trim future history (standard behavior)
- Observer pattern for selection/element changes
- Clean separation of concerns (Canvas, Elements, Designer)

## Development Notes

### Hot Module Replacement
Changes to TypeScript files auto-reload in the browser when using `npm run dev`.

### TypeScript Configuration
- Target: ES2020
- Module: ESNext
- Strict mode enabled
- `verbatimModuleSyntax` - Requires `import type` for type-only imports

### Testing
- Framework: Vitest
- Comprehensive test suite including:
  - 32 Designer tests (constructor, adding elements, clearing, serialization, undo/redo, callbacks)
  - 5 element type tests (QR, Text, Image, Rect)
  - 3 unit tests (DPI conversion)
  - All tests passing with mocked Fabric.js for fast execution
- Focus on public API contracts and behavior-driven testing

## Integration with ASP.NET

The built bundles (`npm run build`) are consumed by ASP.NET Razor pages:

```html
<script type="module" src="~/dist/assets/designer-[hash].js"></script>
```

Use `manifest.json` to resolve hash-based filenames at runtime.
