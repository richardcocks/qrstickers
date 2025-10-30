/**
 * Designer page entry point
 * Integrates TypeScript Designer with ASP.NET Razor page
 */

import { Designer } from '../../designer/Designer';
import { mmToPx, pxToMm } from '../../utils/units';
import * as ExportPreview from '../../export/ExportPreview';
import type { TemplateJson, FabricCanvasType } from '../../export/types';

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
    onZoomChanged: updateZoomDisplay,
  });

  // Expose globally for debugging
  (window as any).designer = designer;

  // Load existing template if in edit mode
  if (editMode && templateData.templateJson) {
    try {
      designer.loadTemplate(templateData.templateJson);
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

  // Reset viewport immediately to center and fit sticker boundary
  try {
    designer.resetView();
    updateZoomDisplay();
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
    const hasDataBinding = !!element.dataBinding;

    // Show/hide text content input based on data binding
    const textContentGroup = document.getElementById('textContentGroup');
    if (textContentGroup) {
      textContentGroup.style.display = hasDataBinding ? 'none' : 'block';
    }

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

  document.getElementById('chkShowRulers')?.addEventListener('change', () => {
    designer.toggleRulers();
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

function updateZoomDisplay(zoom?: number): void {
  // Use provided zoom or fetch current zoom from canvas
  const currentZoom = zoom ?? designer.getCanvas().getZoom();
  const zoomPercent = Math.round(currentZoom * 100);
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

  // Text data binding
  document.getElementById('textDataSource')?.addEventListener('change', (e) => {
    updateSelectedElement({ dataBinding: (e.target as HTMLSelectElement).value });

    // Re-populate property panel to update text content visibility
    const element = designer.getSelectedElement();
    if (element) {
      populatePropertyValues(element);
    }
  });

  // Text font weight
  document.getElementById('textFontWeight')?.addEventListener('change', (e) => {
    updateSelectedElement({ fontWeight: (e.target as HTMLSelectElement).value });
  });

  // Text color
  document.getElementById('textColor')?.addEventListener('input', (e) => {
    updateSelectedElement({ fill: (e.target as HTMLInputElement).value });
  });

  // Rectangle fill color
  document.getElementById('rectFill')?.addEventListener('input', (e) => {
    updateSelectedElement({ fill: (e.target as HTMLInputElement).value });
  });

  // Rectangle stroke color
  document.getElementById('rectStroke')?.addEventListener('input', (e) => {
    updateSelectedElement({ stroke: (e.target as HTMLInputElement).value });
  });

  // Rectangle stroke width
  document.getElementById('rectStrokeWidth')?.addEventListener('input', (e) => {
    updateSelectedElement({ strokeWidth: parseFloat((e.target as HTMLInputElement).value) });
  });

  // Line stroke color
  document.getElementById('lineStroke')?.addEventListener('input', (e) => {
    updateSelectedElement({ stroke: (e.target as HTMLInputElement).value });
  });

  // Line stroke width
  document.getElementById('lineStrokeWidth')?.addEventListener('input', (e) => {
    updateSelectedElement({ strokeWidth: parseFloat((e.target as HTMLInputElement).value) });
  });

  // Rotation (common property)
  document.getElementById('rotation')?.addEventListener('input', (e) => {
    updateSelectedElement({ angle: parseFloat((e.target as HTMLInputElement).value) });
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

// Export state
let isExportModalOpen: boolean = false;
let previewCanvasInstance: FabricCanvasType | null = null;

function wireUpExportButton(): void {
  document.getElementById('btnExport')?.addEventListener('click', () => {
    openExportModal();
  });

  // Wire up modal controls (on load)
  wireUpExportModalControls();
}

async function openExportModal(): Promise<void> {
  const modal = document.getElementById('exportModal');
  const overlay = document.getElementById('exportModalOverlay');

  if (!modal || !overlay) {
    console.error('Export modal elements not found');
    return;
  }

  isExportModalOpen = true;

  // Show modal (use flex to maintain centering from CSS)
  modal.style.display = 'flex';
  overlay.style.display = 'flex';

  // Show loading indicator
  showExportLoading('Generating preview...');

  try {
    // Get current template JSON from designer
    const templateJson = designer.saveTemplate();
    const config = (window as any).designerConfig;

    // Generate placeholder map
    const parsedTemplate: TemplateJson = JSON.parse(templateJson);
    const placeholders = ExportPreview.generatePlaceholderMap(
      parsedTemplate,
      config.uploadedImages || []
    );

    // Create preview template with placeholders
    const previewTemplate = ExportPreview.createPreviewTemplate(parsedTemplate, placeholders);

    // Generate preview
    await updateExportPreview(
      previewTemplate,
      config.templateData.pageWidth,
      config.templateData.pageHeight
    );

    hideExportLoading();
  } catch (error) {
    console.error('Error generating export preview:', error);
    alert('Error generating preview: ' + (error as Error).message);
    hideExportLoading();
    closeExportModal();
  }
}

async function updateExportPreview(
  templateJson: TemplateJson,
  pageWidthMm: number,
  pageHeightMm: number
): Promise<void> {
  const canvas = document.getElementById('previewCanvas') as HTMLCanvasElement;
  if (!canvas) {
    console.error('Preview canvas element not found');
    return;
  }

  // Dispose previous canvas instance to avoid "already initialized" error
  if (previewCanvasInstance) {
    previewCanvasInstance.dispose();
    previewCanvasInstance = null;
  }

  // Get selected export options
  const format = (document.querySelector('input[name="exportFormat"]:checked') as HTMLInputElement)?.value || 'png';
  const background = (document.querySelector('input[name="pngBackground"]:checked') as HTMLInputElement)?.value || 'white';

  // Update PNG options visibility
  const pngOptions = document.getElementById('pngOptions');
  if (pngOptions) {
    pngOptions.style.display = format === 'png' ? 'block' : 'none';
  }

  try {
    // Create and render preview canvas (always at 96 DPI for preview)
    previewCanvasInstance = await ExportPreview.createAndRenderPreviewCanvas(
      canvas,
      templateJson,
      pageWidthMm,
      pageHeightMm,
      false, // Preview mode (not export)
      { dpi: 96, background: background as 'white' | 'transparent', format: format as 'png' | 'svg' }
    );

    // Update preview container background
    const previewContainer = document.getElementById('previewContainer');
    if (previewContainer && background === 'transparent') {
      previewContainer.classList.add('transparent-bg');
    } else if (previewContainer) {
      previewContainer.classList.remove('transparent-bg');
    }
  } catch (error) {
    console.error('Error rendering preview:', error);
    throw error;
  }
}

function wireUpExportModalControls(): void {
  // Format change
  document.querySelectorAll('input[name="exportFormat"]').forEach(radio => {
    radio.addEventListener('change', async () => {
      if (!isExportModalOpen) return;

      showExportLoading('Updating preview...');
      try {
        const templateJson = designer.saveTemplate();
        const config = (window as any).designerConfig;
        const parsedTemplate: TemplateJson = JSON.parse(templateJson);
        const placeholders = ExportPreview.generatePlaceholderMap(
          parsedTemplate,
          config.uploadedImages || []
        );
        const previewTemplate = ExportPreview.createPreviewTemplate(parsedTemplate, placeholders);

        await updateExportPreview(
          previewTemplate,
          config.templateData.pageWidth,
          config.templateData.pageHeight
        );
        hideExportLoading();
      } catch (error) {
        console.error('Error updating preview:', error);
        hideExportLoading();
      }
    });
  });

  // DPI change
  document.querySelectorAll('input[name="pngDpi"]').forEach(radio => {
    radio.addEventListener('change', () => {
      // DPI only affects export, not preview - no need to regenerate
    });
  });

  // Background change
  document.querySelectorAll('input[name="pngBackground"]').forEach(radio => {
    radio.addEventListener('change', async () => {
      if (!isExportModalOpen) return;

      showExportLoading('Updating preview...');
      try {
        const templateJson = designer.saveTemplate();
        const config = (window as any).designerConfig;
        const parsedTemplate: TemplateJson = JSON.parse(templateJson);
        const placeholders = ExportPreview.generatePlaceholderMap(
          parsedTemplate,
          config.uploadedImages || []
        );
        const previewTemplate = ExportPreview.createPreviewTemplate(parsedTemplate, placeholders);

        await updateExportPreview(
          previewTemplate,
          config.templateData.pageWidth,
          config.templateData.pageHeight
        );
        hideExportLoading();
      } catch (error) {
        console.error('Error updating preview:', error);
        hideExportLoading();
      }
    });
  });

  // Download button
  document.getElementById('btnDownload')?.addEventListener('click', async () => {
    await downloadExport();
  });

  // Cancel button
  document.getElementById('btnCancelExport')?.addEventListener('click', () => {
    closeExportModal();
  });

  // Close button
  document.getElementById('btnCloseModal')?.addEventListener('click', () => {
    closeExportModal();
  });

  // Close on overlay click
  document.getElementById('exportModalOverlay')?.addEventListener('click', () => {
    closeExportModal();
  });
}

async function downloadExport(): Promise<void> {
  showExportLoading('Generating export...');

  try {
    const templateJson = designer.saveTemplate();
    const config = (window as any).designerConfig;
    const format = (document.querySelector('input[name="exportFormat"]:checked') as HTMLInputElement)?.value || 'png';
    const dpi = parseInt((document.querySelector('input[name="pngDpi"]:checked') as HTMLInputElement)?.value || '96');
    const background = (document.querySelector('input[name="pngBackground"]:checked') as HTMLInputElement)?.value || 'white';

    // Parse template
    const parsedTemplate: TemplateJson = JSON.parse(templateJson);
    const placeholders = ExportPreview.generatePlaceholderMap(
      parsedTemplate,
      config.uploadedImages || []
    );
    const exportTemplate = ExportPreview.createPreviewTemplate(parsedTemplate, placeholders);

    // Create temporary canvas for export at full resolution
    const tempCanvas = document.createElement('canvas');

    // Render at export resolution
    const exportCanvasInstance = await ExportPreview.createAndRenderPreviewCanvas(
      tempCanvas,
      exportTemplate,
      config.templateData.pageWidth,
      config.templateData.pageHeight,
      true, // Export mode (full resolution)
      { dpi: dpi as 96 | 150 | 300, background: background as 'white' | 'transparent', format: format as 'png' | 'svg' }
    );

    // Export file
    if (format === 'png') {
      ExportPreview.exportPNG(exportCanvasInstance, dpi, background as 'white' | 'transparent', 'template-preview.png');
    } else {
      ExportPreview.exportSVG(exportCanvasInstance, 'template-preview.svg');
    }

    hideExportLoading();
    closeExportModal();
  } catch (error) {
    console.error('Error exporting:', error);
    alert('Error exporting: ' + (error as Error).message);
    hideExportLoading();
  }
}

function closeExportModal(): void {
  const modal = document.getElementById('exportModal');
  const overlay = document.getElementById('exportModalOverlay');

  if (modal) modal.style.display = 'none';
  if (overlay) overlay.style.display = 'none';

  // Dispose preview canvas instance
  if (previewCanvasInstance) {
    previewCanvasInstance.dispose();
    previewCanvasInstance = null;
  }

  isExportModalOpen = false;
}

function showExportLoading(message: string): void {
  const loadingEl = document.getElementById('exportLoading');
  const loadingMessage = document.getElementById('exportLoadingMessage');
  const downloadBtn = document.getElementById('btnDownload') as HTMLButtonElement;

  if (loadingEl) {
    loadingEl.style.display = 'flex';
  }
  if (loadingMessage) {
    loadingMessage.textContent = message;
  }
  if (downloadBtn) {
    downloadBtn.disabled = true;
  }
}

function hideExportLoading(): void {
  const loadingEl = document.getElementById('exportLoading');
  const downloadBtn = document.getElementById('btnDownload') as HTMLButtonElement;

  if (loadingEl) {
    loadingEl.style.display = 'none';
  }
  if (downloadBtn) {
    downloadBtn.disabled = false;
  }
}
