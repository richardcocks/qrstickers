/**
 * Designer page entry point
 * Integrates TypeScript Designer with ASP.NET Razor page
 */

import { Designer } from '../../designer/Designer';
import { mmToPx, pxToMm } from '../../utils/units';

console.log('✅ Designer module loaded (TypeScript implementation)');

// Expose utilities globally for Razor page compatibility
(window as any).mmToPx = mmToPx;
(window as any).pxToMm = pxToMm;
(window as any).Designer = Designer;

let designer: Designer;

// Auto-initialize on DOM ready
if (document.readyState === 'loading') {
  document.addEventListener('DOMContentLoaded', initDesigner);
} else {
  initDesigner();
}

function initDesigner(): void {
  console.log('🎨 Initializing Designer...');

  // Get config from window (passed from Razor page)
  const config = (window as any).designerConfig;
  if (!config) {
    console.error('❌ designerConfig not found on window');
    return;
  }

  const { templateData, editMode, systemTemplate } = config;

  // Initialize Designer instance
  designer = new Designer({
    containerId: 'designCanvas',
    widthMm: templateData.pageWidth,
    heightMm: templateData.pageHeight,
    marginTop: parseInt(document.getElementById('designCanvas')?.dataset.marginTop || '50'),
    marginLeft: parseInt(document.getElementById('designCanvas')?.dataset.marginLeft || '50'),
    marginBottom: parseInt(document.getElementById('designCanvas')?.dataset.marginBottom || '200'),
    marginRight: parseInt(document.getElementById('designCanvas')?.dataset.marginRight || '200'),
    onSelectionChange: handleSelectionChange,
    onElementsChange: handleElementsChange,
  });

  // Expose globally for debugging
  (window as any).designer = designer;

  // Load existing template if in edit mode
  if (editMode && templateData.templateJson) {
    try {
      designer.loadTemplate(templateData.templateJson);
      console.log('✅ Template loaded');
    } catch (error) {
      console.error('Failed to load template:', error);
    }
  }

  // Wire up UI controls
  wireUpToolbar();
  wireUpElementPalette();
  wireUpPropertyInspector();
  wireUpSaveButton(systemTemplate);
  wireUpExportButton();

  console.log('✅ Designer initialized successfully');

  // Reset viewport immediately to center and fit sticker boundary
  try {
    designer.resetView();
    updateZoomDisplay();
    console.log('✅ Viewport centered on sticker');
  } catch (error) {
    console.error('Failed to reset view:', error);
  }
}

function handleSelectionChange(element: any): void {
  // Update property inspector based on selected element
  const propertyInspector = document.getElementById('propertyInspector');
  if (!propertyInspector) return;

  // Hide all property panels
  document.querySelectorAll('.property-panel').forEach(panel => {
    (panel as HTMLElement).style.display = 'none';
  });

  if (!element) {
    document.getElementById('noSelection')!.style.display = 'block';
    return;
  }

  // Show type-specific properties
  const typeMap: Record<string, string> = {
    'qrcode': 'qrcodeProperties',
    'text': 'textProperties',
    'image': 'imageProperties',
    'rect': 'rectangleProperties',
    'line': 'lineProperties',
  };

  const panelId = typeMap[element.type];
  if (panelId) {
    document.getElementById(panelId)!.style.display = 'block';
  }

  // Always show common properties (position/size)
  document.getElementById('commonProperties')!.style.display = 'block';

  // Populate property values
  populatePropertyValues(element);
}

function populatePropertyValues(element: any): void {
  // Common properties
  (document.getElementById('posX') as HTMLInputElement).value = element.x.toFixed(1);
  (document.getElementById('posY') as HTMLInputElement).value = element.y.toFixed(1);
  (document.getElementById('sizeWidth') as HTMLInputElement).value = element.width.toFixed(1);
  (document.getElementById('sizeHeight') as HTMLInputElement).value = element.height.toFixed(1);
  (document.getElementById('rotation') as HTMLInputElement).value = (element.angle || 0).toString();

  // Type-specific properties
  if (element.type === 'qrcode') {
    (document.getElementById('qrDataSource') as HTMLSelectElement).value = element.dataBinding || '';
    (document.getElementById('qrSize') as HTMLInputElement).value = element.width.toFixed(1);
  } else if (element.type === 'text') {
    (document.getElementById('textContent') as HTMLInputElement).value = element.text || '';
    (document.getElementById('textDataSource') as HTMLSelectElement).value = element.dataBinding || '';
    (document.getElementById('textFontFamily') as HTMLSelectElement).value = element.fontFamily || 'Arial';
    (document.getElementById('textFontSize') as HTMLInputElement).value = (element.fontSize || 16).toString();
    (document.getElementById('textFontWeight') as HTMLSelectElement).value = element.fontWeight || 'normal';
    (document.getElementById('textColor') as HTMLInputElement).value = element.fill || '#000000';
  } else if (element.type === 'rect') {
    (document.getElementById('rectFill') as HTMLInputElement).value = element.fill || '#ffffff';
    (document.getElementById('rectStroke') as HTMLInputElement).value = element.stroke || '#000000';
    (document.getElementById('rectStrokeWidth') as HTMLInputElement).value = (element.strokeWidth || 2).toString();
  } else if (element.type === 'line') {
    (document.getElementById('lineStroke') as HTMLInputElement).value = element.stroke || '#000000';
    (document.getElementById('lineStrokeWidth') as HTMLInputElement).value = (element.strokeWidth || 2).toString();
  }
}

function handleElementsChange(): void {
  // Mark as having unsaved changes
  (window as any).hasUnsavedChanges = true;

  // Update save status indicator
  const saveStatus = document.getElementById('saveStatus');
  if (saveStatus) {
    saveStatus.textContent = '●';
    saveStatus.style.color = '#ff9800';
  }
}

function wireUpToolbar(): void {
  // Zoom controls
  document.getElementById('btnZoomIn')?.addEventListener('click', () => {
    designer.getCanvas().zoomIn();
    updateZoomDisplay();
  });

  document.getElementById('btnZoomOut')?.addEventListener('click', () => {
    designer.getCanvas().zoomOut();
    updateZoomDisplay();
  });

  document.getElementById('btnZoomReset')?.addEventListener('click', () => {
    designer.resetView();
    updateZoomDisplay();
  });

  // Tool mode switching
  document.getElementById('btnSelectTool')?.addEventListener('click', () => {
    designer.setTool('select');
    updateToolButtons('select');
  });

  document.getElementById('btnPanTool')?.addEventListener('click', () => {
    designer.setTool('pan');
    updateToolButtons('pan');
  });

  // Grid controls
  document.getElementById('chkShowGrid')?.addEventListener('change', () => {
    designer.toggleGrid();
  });

  document.getElementById('chkSnapToGrid')?.addEventListener('change', () => {
    designer.toggleSnapToGrid();
  });

  // Page size controls
  document.getElementById('pageWidth')?.addEventListener('change', (e) => {
    alert('Canvas resizing is not yet implemented. Please create a new template with the desired size.');
    // Reset to current value
    const config = (window as any).designerConfig;
    (e.target as HTMLInputElement).value = config.templateData.pageWidth;
  });

  document.getElementById('pageHeight')?.addEventListener('change', (e) => {
    alert('Canvas resizing is not yet implemented. Please create a new template with the desired size.');
    // Reset to current value
    const config = (window as any).designerConfig;
    (e.target as HTMLInputElement).value = config.templateData.pageHeight;
  });

  // Fullscreen toggle
  document.getElementById('btnFullscreen')?.addEventListener('click', () => {
    const container = document.querySelector('.designer-container');
    if (container) {
      if (!document.fullscreenElement) {
        container.requestFullscreen().catch((err) => {
          console.error('Error attempting to enable fullscreen:', err);
        });
      } else {
        document.exitFullscreen();
      }
    }
  });

  // Update fullscreen button text on state change
  document.addEventListener('fullscreenchange', () => {
    const btn = document.getElementById('btnFullscreen');
    if (btn) {
      btn.textContent = document.fullscreenElement ? '⛶ Exit Fullscreen' : '⛶ Full-Screen';
    }
    // Defer resize until browser completes layout update
    requestAnimationFrame(() => {
      designer.resize();
      updateZoomDisplay();
    });
  });

  // Handle window resize events (including fullscreen transitions)
  window.addEventListener('resize', () => {
    // Defer resize to avoid multiple rapid calls during resize
    requestAnimationFrame(() => {
      designer.resize();
      updateZoomDisplay();
    });
  });
}

function updateZoomDisplay(): void {
  const zoom = designer.getCanvas().getZoom();
  const zoomPercent = Math.round(zoom * 100);
  const zoomEl = document.getElementById('zoomLevel');
  if (zoomEl) {
    zoomEl.textContent = `${zoomPercent}%`;
  }
}

function updateToolButtons(tool: 'select' | 'pan'): void {
  const selectBtn = document.getElementById('btnSelectTool');
  const panBtn = document.getElementById('btnPanTool');

  if (tool === 'select') {
    selectBtn?.classList.add('active');
    panBtn?.classList.remove('active');
  } else {
    selectBtn?.classList.remove('active');
    panBtn?.classList.add('active');
  }
}

function wireUpElementPalette(): void {
  document.querySelectorAll('.palette-item').forEach(item => {
    item.addEventListener('click', () => {
      const elementType = item.getAttribute('data-element-type');
      if (elementType) {
        addElement(elementType);
      }
    });
  });
}

function addElement(type: string): void {
  const typeMap: Record<string, string> = {
    'qrcode': 'qr',
    'rectangle': 'rect',
  };

  const designerType = typeMap[type] || type;
  designer.addElement(designerType as any);
}

function wireUpPropertyInspector(): void {
  // Common property change handlers
  document.getElementById('posX')?.addEventListener('input', (e) => {
    updateSelectedElement({ x: parseFloat((e.target as HTMLInputElement).value) });
  });

  document.getElementById('posY')?.addEventListener('input', (e) => {
    updateSelectedElement({ y: parseFloat((e.target as HTMLInputElement).value) });
  });

  document.getElementById('sizeWidth')?.addEventListener('input', (e) => {
    updateSelectedElement({ width: parseFloat((e.target as HTMLInputElement).value) });
  });

  document.getElementById('sizeHeight')?.addEventListener('input', (e) => {
    updateSelectedElement({ height: parseFloat((e.target as HTMLInputElement).value) });
  });

  // Text properties
  document.getElementById('textContent')?.addEventListener('input', (e) => {
    updateSelectedElement({ text: (e.target as HTMLInputElement).value });
  });

  document.getElementById('textFontFamily')?.addEventListener('change', (e) => {
    updateSelectedElement({ fontFamily: (e.target as HTMLSelectElement).value });
  });

  document.getElementById('textFontSize')?.addEventListener('input', (e) => {
    updateSelectedElement({ fontSize: parseInt((e.target as HTMLInputElement).value) });
  });

  // QR code properties
  document.getElementById('qrDataSource')?.addEventListener('change', (e) => {
    updateSelectedElement({ dataBinding: (e.target as HTMLSelectElement).value });
  });

  // Layer ordering buttons
  document.getElementById('btnBringToFront')?.addEventListener('click', () => designer.bringToFront());
  document.getElementById('btnSendToBack')?.addEventListener('click', () => designer.sendToBack());
  document.getElementById('btnBringForward')?.addEventListener('click', () => designer.bringForward());
  document.getElementById('btnSendBackward')?.addEventListener('click', () => designer.sendBackward());
}

function updateSelectedElement(updates: any): void {
  const element = designer.getSelectedElement();
  if (element) {
    designer.updateElement(element.id, updates);
  }
}

function wireUpSaveButton(systemTemplate: boolean): void {
  document.getElementById('btnSave')?.addEventListener('click', () => {
    if (systemTemplate) {
      alert('Cannot modify system templates. Please clone the template first.');
      return;
    }

    saveTemplate();
  });
}

function saveTemplate(): void {
  const json = designer.saveTemplate();

  // Update hidden form field
  (document.getElementById('templateJson') as HTMLInputElement).value = json;

  // Update page size fields
  const config = (window as any).designerConfig;
  (document.getElementById('templatePageWidth') as HTMLInputElement).value = config.templateData.pageWidth;
  (document.getElementById('templatePageHeight') as HTMLInputElement).value = config.templateData.pageHeight;

  // Submit form
  (document.getElementById('saveForm') as HTMLFormElement).submit();
}

function wireUpExportButton(): void {
  document.getElementById('btnExport')?.addEventListener('click', () => {
    alert('Export functionality coming soon!');
    // TODO: Implement export modal
  });
}
