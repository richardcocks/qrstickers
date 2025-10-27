/**
 * Base Element - Abstract base class for all design elements
 */

export interface ElementData {
  id: string;
  type: string;
  x: number; // in MM
  y: number; // in MM
  width: number; // in MM
  height: number; // in MM
  dataBinding?: string;
  [key: string]: any;
}

export abstract class BaseElement {
  public id: string;
  public type: string;
  public x: number; // Position in MM
  public y: number;
  public width: number; // Size in MM
  public height: number;
  public dataBinding?: string;

  protected fabricObject: any; // Internal Fabric.js object (hidden from outside)

  constructor(data: Partial<ElementData>) {
    this.id = data.id || this.generateId();
    this.type = data.type || 'unknown';
    this.x = data.x ?? 0;
    this.y = data.y ?? 0;
    this.width = data.width ?? 50;
    this.height = data.height ?? 50;
    this.dataBinding = data.dataBinding;
  }

  /**
   * Create the Fabric.js object representation
   * Each subclass implements this to create its specific Fabric object
   */
  abstract createFabricObject(boundaryLeft: number, boundaryTop: number): any;

  /**
   * Get the internal Fabric.js object
   * Returns the cached object or creates it if needed
   */
  getFabricObject(boundaryLeft: number, boundaryTop: number): any {
    if (!this.fabricObject) {
      this.fabricObject = this.createFabricObject(boundaryLeft, boundaryTop);
      this.fabricObject.id = this.id;
      this.fabricObject.customType = this.type; // Store our custom type
    }
    return this.fabricObject;
  }

  /**
   * Update element properties from Fabric object
   * Called when user modifies object on canvas
   */
  updateFromFabricObject(boundaryLeft: number, boundaryTop: number): void {
    if (!this.fabricObject) return;

    const obj = this.fabricObject;
    this.x = this.pxToMm((obj.left ?? 0) - boundaryLeft);
    this.y = this.pxToMm((obj.top ?? 0) - boundaryTop);

    // Use stored base dimensions to prevent size compounding
    // Fabric.js auto-calculates Group dimensions which differ from intended base
    const baseWidth = obj.baseWidth ?? obj.width;
    const baseHeight = obj.baseHeight ?? obj.height;
    this.width = this.pxToMm(baseWidth * (obj.scaleX ?? 1));
    this.height = this.pxToMm(baseHeight * (obj.scaleY ?? 1));
  }

  /**
   * Update Fabric object from element properties
   * Called when properties are changed programmatically
   */
  updateFabricObject(boundaryLeft: number, boundaryTop: number): void {
    if (!this.fabricObject) return;

    // Use stored base dimensions to prevent size compounding
    // Fabric.js auto-calculates Group dimensions which differ slightly from intended base
    const baseWidth = this.fabricObject.baseWidth ?? this.fabricObject.width;
    const baseHeight = this.fabricObject.baseHeight ?? this.fabricObject.height;

    this.fabricObject.set({
      left: this.mmToPx(this.x) + boundaryLeft,
      top: this.mmToPx(this.y) + boundaryTop,
      scaleX: this.mmToPx(this.width) / baseWidth,
      scaleY: this.mmToPx(this.height) / baseHeight,
    });
  }

  /**
   * Serialize to JSON
   */
  toJSON(): ElementData {
    return {
      id: this.id,
      type: this.type,
      x: this.x,
      y: this.y,
      width: this.width,
      height: this.height,
      dataBinding: this.dataBinding,
    };
  }

  /**
   * Create element from JSON
   * Subclasses should override and call super.fromJSON()
   */
  static fromJSON(_data: ElementData): BaseElement {
    throw new Error('fromJSON must be implemented by subclass');
  }

  // Utility methods

  protected generateId(): string {
    if (typeof crypto !== 'undefined' && crypto.randomUUID) {
      return `elem-${crypto.randomUUID()}`;
    }
    return `elem-${Date.now()}-${Math.random().toString(36).substring(2, 11)}`;
  }

  protected mmToPx(mm: number): number {
    return mm * 5.669291339; // 144 DPI
  }

  protected pxToMm(px: number): number {
    return px / 5.669291339;
  }
}
