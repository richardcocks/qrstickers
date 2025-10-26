/**
 * Designer - Main controller class
 * Coordinates canvas, elements, and user interactions
 */

import { CanvasWrapper } from './CanvasWrapper';
import type { CanvasConfig } from './CanvasWrapper';
import {
  BaseElement,
  QRElement,
  TextElement,
  ImageElement,
  RectElement,
} from './elements';
import type { ElementData } from './elements';

export type ElementType = 'qr' | 'text' | 'image' | 'rect';
export type ToolMode = 'select' | 'pan';

export interface DesignerConfig extends CanvasConfig {
  onSelectionChange?: (element: BaseElement | null) => void;
  onElementsChange?: () => void;
  onToolChange?: (tool: ToolMode) => void;
}

export class Designer {
  private canvas: CanvasWrapper;
  private elements: BaseElement[] = [];
  private selectedElement: BaseElement | null = null;
  private config: DesignerConfig;
  private toolMode: ToolMode = 'select';

  // Undo/redo history - track states with an index
  private history: string[] = [];
  private historyIndex = -1;
  private maxHistorySteps = 50;
  private isRestoring = false;

  // Clipboard for copy/paste
  private clipboard: ElementData | null = null;

  constructor(config: DesignerConfig) {
    this.config = config;
    this.canvas = new CanvasWrapper(config);
    this.initializeEventListeners();
    this.enableKeyboardShortcuts();
    // Save initial empty state so first undo has something to restore to
    this.saveState();
  }

  /**
   * Add a new element to the canvas
   */
  addElement(type: ElementType, position?: { x: number; y: number }): BaseElement {
    let element: BaseElement;

    const defaultX = position?.x ?? 10;
    const defaultY = position?.y ?? 10;

    switch (type) {
      case 'qr':
        element = new QRElement({ x: defaultX, y: defaultY, width: 25, height: 25 });
        break;
      case 'text':
        element = new TextElement({ x: defaultX, y: defaultY, width: 50, height: 10 });
        break;
      case 'image':
        element = new ImageElement({ x: defaultX, y: defaultY, width: 30, height: 30 });
        break;
      case 'rect':
        element = new RectElement({ x: defaultX, y: defaultY, width: 50, height: 30 });
        break;
    }

    this.elements.push(element);
    const fabricObj = element.getFabricObject(this.canvas.boundaryLeft, this.canvas.boundaryTop);
    this.canvas.add(fabricObj);
    this.canvas.setActiveObject(fabricObj);
    this.canvas.render();

    this.saveState();
    this.notifyElementsChange();

    return element;
  }

  /**
   * Remove the currently selected element
   */
  removeSelected(): void {
    if (!this.selectedElement) return;

    const index = this.elements.indexOf(this.selectedElement);
    if (index > -1) {
      const fabricObj = this.selectedElement.getFabricObject(
        this.canvas.boundaryLeft,
        this.canvas.boundaryTop
      );
      this.canvas.remove(fabricObj);
      this.elements.splice(index, 1);
      this.selectedElement = null;
      this.canvas.render();

      this.saveState();
      this.notifySelectionChange();
      this.notifyElementsChange();
    }
  }

  /**
   * Get the currently selected element
   */
  getSelectedElement(): BaseElement | null {
    return this.selectedElement;
  }

  /**
   * Get all elements
   */
  getElements(): BaseElement[] {
    return [...this.elements];
  }

  /**
   * Update a specific element's properties
   */
  updateElement(elementId: string, properties: Partial<ElementData>): void {
    const element = this.elements.find((el) => el.id === elementId);
    if (!element) return;

    // Save whether this element is currently selected BEFORE removing
    const wasSelected = this.selectedElement?.id === elementId;

    // Remove old fabric object
    const oldFabricObj = element.getFabricObject(this.canvas.boundaryLeft, this.canvas.boundaryTop);
    this.canvas.remove(oldFabricObj);

    // Update element properties
    Object.assign(element, properties);

    // Clear cached fabric object and create new one with updated properties
    (element as any).fabricObject = null;
    const newFabricObj = element.getFabricObject(this.canvas.boundaryLeft, this.canvas.boundaryTop);
    this.canvas.add(newFabricObj);

    // Maintain selection if this was the selected element
    if (wasSelected) {
      this.canvas.setActiveObject(newFabricObj);
    }

    this.canvas.render();

    this.saveState();
    this.notifyElementsChange();
  }

  /**
   * Clear all elements and reset undo/redo history
   */
  clear(): void {
    this.clearElements();
    this.history = [];
    this.historyIndex = -1;
  }

  /**
   * Clear all elements without affecting undo/redo history
   * (Private method used internally by loadTemplate)
   */
  private clearElements(): void {
    this.elements = [];
    this.selectedElement = null;
    this.canvas.clear();
    this.notifySelectionChange();
    this.notifyElementsChange();
  }

  /**
   * Save template to JSON
   */
  saveTemplate(): string {
    const data = {
      version: '1.0',
      pageSize: {
        width: this.canvas.widthMm,
        height: this.canvas.heightMm,
        unit: 'mm',
      },
      objects: this.getElementsInCanvasOrder().map((el) => el.toJSON()),
    };
    return JSON.stringify(data, null, 2);
  }

  /**
   * Load template from JSON
   */
  loadTemplate(json: string): void {
    try {
      const data = JSON.parse(json);
      this.clearElements();

      if (data.objects && Array.isArray(data.objects)) {
        data.objects.forEach((objData: ElementData) => {
          const element = this.createElementFromJSON(objData);
          if (element) {
            this.elements.push(element);
            const fabricObj = element.getFabricObject(
              this.canvas.boundaryLeft,
              this.canvas.boundaryTop
            );
            this.canvas.add(fabricObj);
          }
        });
      }

      this.canvas.render();
      this.saveState();
      this.notifyElementsChange();
    } catch (error) {
      console.error('Failed to load template:', error);
      throw error;
    }
  }

  /**
   * Undo last action
   */
  undo(): void {
    if (this.historyIndex > 0) {
      this.historyIndex--;
      const state = this.history[this.historyIndex];
      if (state) {
        this.restoreState(state);
      }
    }
  }

  /**
   * Redo last undone action
   */
  redo(): void {
    if (this.historyIndex < this.history.length - 1) {
      this.historyIndex++;
      const state = this.history[this.historyIndex];
      if (state) {
        this.restoreState(state);
      }
    }
  }

  /**
   * Copy selected element to clipboard
   */
  copy(): boolean {
    if (!this.selectedElement) return false;
    this.clipboard = this.selectedElement.toJSON();
    return true;
  }

  /**
   * Paste element from clipboard
   */
  paste(): BaseElement | null {
    if (!this.clipboard) return null;

    // Create offset position for pasted element
    const offsetX = 5; // 5mm offset
    const offsetY = 5;
    const pastedData = {
      ...this.clipboard,
      id: `${this.clipboard.type}-${Date.now()}`, // New unique ID
      x: this.clipboard.x + offsetX,
      y: this.clipboard.y + offsetY,
    };

    const element = this.createElementFromJSON(pastedData);
    if (!element) return null;

    this.elements.push(element);
    const fabricObj = element.getFabricObject(this.canvas.boundaryLeft, this.canvas.boundaryTop);
    this.canvas.add(fabricObj);
    this.canvas.setActiveObject(fabricObj);
    this.canvas.render();

    this.saveState();
    this.notifyElementsChange();

    return element;
  }

  /**
   * Duplicate the selected element
   */
  duplicate(): BaseElement | null {
    if (this.copy()) {
      return this.paste();
    }
    return null;
  }

  /**
   * Deselect all elements
   */
  deselectAll(): void {
    this.canvas.discardActiveObject();
    this.selectedElement = null;
    this.canvas.render();
    this.notifySelectionChange();
  }

  /**
   * Bring selected element to front
   */
  bringToFront(): void {
    if (!this.selectedElement) return;

    const fabricObj = this.selectedElement.getFabricObject(
      this.canvas.boundaryLeft,
      this.canvas.boundaryTop
    );
    this.canvas.bringToFront(fabricObj);

    this.saveState();
  }

  /**
   * Send selected element to back
   */
  sendToBack(): void {
    if (!this.selectedElement) return;

    const fabricObj = this.selectedElement.getFabricObject(
      this.canvas.boundaryLeft,
      this.canvas.boundaryTop
    );
    this.canvas.sendToBack(fabricObj);

    this.saveState();
  }

  /**
   * Bring selected element forward one layer
   */
  bringForward(): void {
    if (!this.selectedElement) return;

    const fabricObj = this.selectedElement.getFabricObject(
      this.canvas.boundaryLeft,
      this.canvas.boundaryTop
    );
    this.canvas.bringForward(fabricObj);

    this.saveState();
  }

  /**
   * Send selected element backward one layer
   */
  sendBackward(): void {
    if (!this.selectedElement) return;

    const fabricObj = this.selectedElement.getFabricObject(
      this.canvas.boundaryLeft,
      this.canvas.boundaryTop
    );
    this.canvas.sendBackward(fabricObj);

    this.saveState();
  }

  /**
   * Toggle grid visibility
   * @returns true if grid is now visible, false if hidden
   */
  toggleGrid(): boolean {
    if (this.canvas.isGridVisible()) {
      this.canvas.hideGrid();
      return false;
    } else {
      this.canvas.showGrid();
      return true;
    }
  }

  /**
   * Set the current tool mode (select or pan)
   */
  setTool(tool: ToolMode): void {
    if (this.toolMode === tool) return;

    this.toolMode = tool;

    if (tool === 'pan') {
      this.canvas.enablePanning();
    } else {
      this.canvas.disablePanning();
    }

    // Notify tool change
    if (this.config.onToolChange) {
      this.config.onToolChange(tool);
    }
  }

  /**
   * Get the current tool mode
   */
  getTool(): ToolMode {
    return this.toolMode;
  }

  /**
   * Reset view to center on sticker boundary
   */
  resetView(): void {
    this.canvas.resetView();
  }

  /**
   * Get elements sorted by their canvas z-index (layer order)
   */
  private getElementsInCanvasOrder(): BaseElement[] {
    const canvasObjects = this.canvas.getObjects();
    const elementsInOrder: BaseElement[] = [];

    for (const fabricObj of canvasObjects) {
      const element = this.findElementByFabricObject(fabricObj);
      if (element) {
        elementsInOrder.push(element);
      }
    }

    return elementsInOrder;
  }

  /**
   * Enable keyboard shortcuts
   */
  private enableKeyboardShortcuts(): void {
    document.addEventListener('keydown', (e: KeyboardEvent) => {
      // Don't trigger shortcuts when typing in input fields
      const target = e.target as HTMLElement;
      if (target.tagName === 'INPUT' || target.tagName === 'TEXTAREA') {
        return;
      }

      const isMac = navigator.platform.toUpperCase().indexOf('MAC') >= 0;
      const ctrlOrCmd = isMac ? e.metaKey : e.ctrlKey;

      // Delete
      if (e.key === 'Delete' || e.key === 'Backspace') {
        e.preventDefault();
        this.removeSelected();
      }
      // Undo: Ctrl/Cmd+Z
      else if (ctrlOrCmd && e.key === 'z' && !e.shiftKey) {
        e.preventDefault();
        this.undo();
      }
      // Redo: Ctrl/Cmd+Y or Ctrl/Cmd+Shift+Z
      else if (ctrlOrCmd && (e.key === 'y' || (e.key === 'z' && e.shiftKey))) {
        e.preventDefault();
        this.redo();
      }
      // Copy: Ctrl/Cmd+C
      else if (ctrlOrCmd && e.key === 'c') {
        e.preventDefault();
        this.copy();
      }
      // Paste: Ctrl/Cmd+V
      else if (ctrlOrCmd && e.key === 'v') {
        e.preventDefault();
        this.paste();
      }
      // Duplicate: Ctrl/Cmd+D
      else if (ctrlOrCmd && e.key === 'd') {
        e.preventDefault();
        this.duplicate();
      }
      // Escape: Deselect
      else if (e.key === 'Escape') {
        e.preventDefault();
        this.deselectAll();
      }
      // Toggle tool: 'h' key for Hand/Pan tool
      else if (e.key === 'h' || e.key === 'H') {
        e.preventDefault();
        const newTool = this.toolMode === 'select' ? 'pan' : 'select';
        this.setTool(newTool);
      }
    });
  }

  /**
   * Get canvas wrapper (for advanced usage)
   */
  getCanvas(): CanvasWrapper {
    return this.canvas;
  }

  // Private methods

  private initializeEventListeners(): void {
    // Selection change
    this.canvas.on('selection:created', (e: any) => this.handleSelectionChange(e));
    this.canvas.on('selection:updated', (e: any) => this.handleSelectionChange(e));
    this.canvas.on('selection:cleared', () => this.handleSelectionClear());

    // Object modification
    this.canvas.on('object:modified', () => this.handleObjectModified());
  }

  private handleSelectionChange(e: any): void {
    const fabricObj = e.selected?.[0] || this.canvas.getActiveObject();
    if (fabricObj) {
      const element = this.findElementByFabricObject(fabricObj);
      if (element) {
        element.updateFromFabricObject(this.canvas.boundaryLeft, this.canvas.boundaryTop);
        this.selectedElement = element;
        this.notifySelectionChange();
      }
    }
  }

  private handleSelectionClear(): void {
    this.selectedElement = null;
    this.notifySelectionChange();
  }

  private handleObjectModified(): void {
    if (this.selectedElement) {
      this.selectedElement.updateFromFabricObject(this.canvas.boundaryLeft, this.canvas.boundaryTop);
      this.saveState();
      this.notifyElementsChange();
    }
  }

  private findElementByFabricObject(fabricObj: any): BaseElement | null {
    const id = fabricObj.id;
    return this.elements.find((el) => el.id === id) || null;
  }

  private createElementFromJSON(data: ElementData): BaseElement | null {
    switch (data.type) {
      case 'qrcode':
        return QRElement.fromJSON(data as any);
      case 'text':
        return TextElement.fromJSON(data as any);
      case 'image':
        return ImageElement.fromJSON(data as any);
      case 'rect':
        return RectElement.fromJSON(data as any);
      default:
        console.warn('Unknown element type:', data.type);
        return null;
    }
  }

  private saveState(): void {
    if (this.isRestoring) return;

    const state = this.captureState();

    // If we're in the middle of the history (after an undo), remove everything after current index
    if (this.historyIndex < this.history.length - 1) {
      this.history = this.history.slice(0, this.historyIndex + 1);
    }

    // Add new state
    this.history.push(state);
    this.historyIndex = this.history.length - 1;

    // Limit history size
    if (this.history.length > this.maxHistorySteps) {
      this.history.shift();
      this.historyIndex--;
    }
  }

  private captureState(): string {
    return this.saveTemplate();
  }

  private restoreState(state: string): void {
    this.isRestoring = true;
    try {
      this.loadTemplate(state);
    } finally {
      this.isRestoring = false;
    }
  }

  private notifySelectionChange(): void {
    if (this.config.onSelectionChange) {
      this.config.onSelectionChange(this.selectedElement);
    }
  }

  private notifyElementsChange(): void {
    if (this.config.onElementsChange) {
      this.config.onElementsChange();
    }
  }
}
