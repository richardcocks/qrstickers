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

export interface DesignerConfig extends CanvasConfig {
  onSelectionChange?: (element: BaseElement | null) => void;
  onElementsChange?: () => void;
}

export class Designer {
  private canvas: CanvasWrapper;
  private elements: BaseElement[] = [];
  private selectedElement: BaseElement | null = null;
  private config: DesignerConfig;

  // Undo/redo stacks
  private undoStack: string[] = [];
  private redoStack: string[] = [];
  private maxUndoSteps = 50;
  private isRestoring = false;

  constructor(config: DesignerConfig) {
    this.config = config;
    this.canvas = new CanvasWrapper(config);
    this.initializeEventListeners();
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
   * Clear all elements
   */
  clear(): void {
    this.elements = [];
    this.selectedElement = null;
    this.canvas.clear();
    this.undoStack = [];
    this.redoStack = [];
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
      objects: this.elements.map((el) => el.toJSON()),
    };
    return JSON.stringify(data, null, 2);
  }

  /**
   * Load template from JSON
   */
  loadTemplate(json: string): void {
    try {
      const data = JSON.parse(json);
      this.clear();

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
    if (this.undoStack.length === 0) return;

    const currentState = this.captureState();
    this.redoStack.push(currentState);

    const prevState = this.undoStack.pop();
    if (prevState) {
      this.restoreState(prevState);
    }
  }

  /**
   * Redo last undone action
   */
  redo(): void {
    if (this.redoStack.length === 0) return;

    const currentState = this.captureState();
    this.undoStack.push(currentState);

    const nextState = this.redoStack.pop();
    if (nextState) {
      this.restoreState(nextState);
    }
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
    this.undoStack.push(state);

    // Limit stack size
    if (this.undoStack.length > this.maxUndoSteps) {
      this.undoStack.shift();
    }

    // Clear redo stack on new action
    this.redoStack = [];
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
