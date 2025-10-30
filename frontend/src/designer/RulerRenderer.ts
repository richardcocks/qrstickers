/**
 * Ruler Renderer - Canvas-based ruler system for designer
 * Renders horizontal and vertical rulers with tick marks and labels
 * Supports zoom/pan viewport transforms and cursor position indicator
 */

import { mmToPx, pxToMm } from '../utils/units';

export class RulerRenderer {
  private visible: boolean = true;
  private cursorPosition: { x: number; y: number } | null = null;

  // Ruler configuration
  private readonly MAJOR_TICK_MM = 10;
  private readonly MINOR_TICK_MM = 1;
  private readonly RULER_THICKNESS_PX = 30;
  private readonly MAJOR_TICK_HEIGHT_PX = 8;
  private readonly MINOR_TICK_HEIGHT_PX = 4;

  // Colors
  private readonly RULER_BG_COLOR = '#f5f5f5';
  private readonly RULER_BG_ALPHA = 0.95;
  private readonly TICK_COLOR = '#666';
  private readonly LABEL_COLOR = '#333';
  private readonly HIGHLIGHT_COLOR = '#1976d2';
  private readonly FONT_SIZE = 10;
  private readonly FONT_FAMILY = 'Arial, sans-serif';

  /**
   * Main render method - called from CanvasWrapper's after:render event
   */
  public render(
    ctx: CanvasRenderingContext2D,
    vpt: number[],
    canvasWidth: number,
    canvasHeight: number
  ): void {
    if (!this.visible) return;

    ctx.save();

    // Render rulers
    this.renderHorizontalRuler(ctx, vpt, canvasWidth, canvasHeight);
    this.renderVerticalRuler(ctx, vpt, canvasWidth, canvasHeight);

    // Render cursor position indicator
    if (this.cursorPosition) {
      this.renderCursorIndicator(ctx, vpt, canvasWidth, canvasHeight);
    }

    ctx.restore();
  }

  /**
   * Render horizontal ruler at top edge
   */
  private renderHorizontalRuler(
    ctx: CanvasRenderingContext2D,
    vpt: number[],
    canvasWidth: number,
    _canvasHeight: number
  ): void {
    const zoom = vpt[0];
    const panX = vpt[4];

    // Draw ruler background bar
    ctx.fillStyle = this.RULER_BG_COLOR;
    ctx.globalAlpha = this.RULER_BG_ALPHA;
    ctx.fillRect(0, 0, canvasWidth, this.RULER_THICKNESS_PX);
    ctx.globalAlpha = 1;

    // Calculate visible range in MM
    const visibleStartPx = (-panX) / zoom;
    const visibleEndPx = visibleStartPx + canvasWidth / zoom;
    const visibleStartMm = pxToMm(visibleStartPx);
    const visibleEndMm = pxToMm(visibleEndPx);

    // Round to nearest minor tick
    const startTickMm = Math.floor(visibleStartMm / this.MINOR_TICK_MM) * this.MINOR_TICK_MM;

    // Draw tick marks and labels
    for (let tickMm = startTickMm; tickMm <= visibleEndMm; tickMm += this.MINOR_TICK_MM) {
      const isMajor = Math.abs(tickMm % this.MAJOR_TICK_MM) < 0.001; // Floating point tolerance
      const tickPx = mmToPx(tickMm);
      const screenX = tickPx * zoom + panX;

      // Only draw ticks within visible range (with small buffer)
      if (screenX >= -5 && screenX <= canvasWidth + 5) {
        this.drawHorizontalTick(ctx, screenX, isMajor);

        // Draw label for major ticks
        if (isMajor && tickMm >= 0) {
          this.drawHorizontalLabel(ctx, screenX, tickMm);
        }
      }
    }
  }

  /**
   * Render vertical ruler at left edge
   */
  private renderVerticalRuler(
    ctx: CanvasRenderingContext2D,
    vpt: number[],
    _canvasWidth: number,
    canvasHeight: number
  ): void {
    const zoom = vpt[0];
    const panY = vpt[5];

    // Draw ruler background bar
    ctx.fillStyle = this.RULER_BG_COLOR;
    ctx.globalAlpha = this.RULER_BG_ALPHA;
    ctx.fillRect(0, 0, this.RULER_THICKNESS_PX, canvasHeight);
    ctx.globalAlpha = 1;

    // Calculate visible range in MM
    const visibleStartPx = (-panY) / zoom;
    const visibleEndPx = visibleStartPx + canvasHeight / zoom;
    const visibleStartMm = pxToMm(visibleStartPx);
    const visibleEndMm = pxToMm(visibleEndPx);

    // Round to nearest minor tick
    const startTickMm = Math.floor(visibleStartMm / this.MINOR_TICK_MM) * this.MINOR_TICK_MM;

    // Draw tick marks and labels
    for (let tickMm = startTickMm; tickMm <= visibleEndMm; tickMm += this.MINOR_TICK_MM) {
      const isMajor = Math.abs(tickMm % this.MAJOR_TICK_MM) < 0.001; // Floating point tolerance
      const tickPx = mmToPx(tickMm);
      const screenY = tickPx * zoom + panY;

      // Only draw ticks within visible range (with small buffer)
      if (screenY >= -5 && screenY <= canvasHeight + 5) {
        this.drawVerticalTick(ctx, screenY, isMajor);

        // Draw label for major ticks
        if (isMajor && tickMm >= 0) {
          this.drawVerticalLabel(ctx, screenY, tickMm);
        }
      }
    }
  }

  /**
   * Draw horizontal tick mark
   */
  private drawHorizontalTick(ctx: CanvasRenderingContext2D, x: number, isMajor: boolean): void {
    const height = isMajor ? this.MAJOR_TICK_HEIGHT_PX : this.MINOR_TICK_HEIGHT_PX;
    ctx.strokeStyle = this.TICK_COLOR;
    ctx.lineWidth = 1;

    ctx.beginPath();
    ctx.moveTo(x, this.RULER_THICKNESS_PX - height);
    ctx.lineTo(x, this.RULER_THICKNESS_PX);
    ctx.stroke();
  }

  /**
   * Draw vertical tick mark
   */
  private drawVerticalTick(ctx: CanvasRenderingContext2D, y: number, isMajor: boolean): void {
    const width = isMajor ? this.MAJOR_TICK_HEIGHT_PX : this.MINOR_TICK_HEIGHT_PX;
    ctx.strokeStyle = this.TICK_COLOR;
    ctx.lineWidth = 1;

    ctx.beginPath();
    ctx.moveTo(this.RULER_THICKNESS_PX - width, y);
    ctx.lineTo(this.RULER_THICKNESS_PX, y);
    ctx.stroke();
  }

  /**
   * Draw horizontal ruler label (MM value)
   */
  private drawHorizontalLabel(ctx: CanvasRenderingContext2D, x: number, mm: number): void {
    const label = Math.round(mm).toString();

    ctx.fillStyle = this.LABEL_COLOR;
    ctx.font = `${this.FONT_SIZE}px ${this.FONT_FAMILY}`;
    ctx.textAlign = 'center';
    ctx.textBaseline = 'top';

    // Draw text slightly above the tick mark
    ctx.fillText(label, x, 2);
  }

  /**
   * Draw vertical ruler label (MM value)
   */
  private drawVerticalLabel(ctx: CanvasRenderingContext2D, y: number, mm: number): void {
    const label = Math.round(mm).toString();

    ctx.save();
    ctx.fillStyle = this.LABEL_COLOR;
    ctx.font = `${this.FONT_SIZE}px ${this.FONT_FAMILY}`;
    ctx.textAlign = 'center';
    ctx.textBaseline = 'middle';

    // Rotate text 90 degrees for vertical ruler
    ctx.translate(this.FONT_SIZE, y);
    ctx.rotate(-Math.PI / 2);
    ctx.fillText(label, 0, 0);
    ctx.restore();
  }

  /**
   * Render cursor position indicator (crosshair lines on rulers)
   */
  private renderCursorIndicator(
    ctx: CanvasRenderingContext2D,
    vpt: number[],
    canvasWidth: number,
    canvasHeight: number
  ): void {
    if (!this.cursorPosition) return;

    const zoom = vpt[0];
    const panX = vpt[4];
    const panY = vpt[5];

    // Convert cursor position (canvas coordinates) to screen coordinates
    const screenX = this.cursorPosition.x * zoom + panX;
    const screenY = this.cursorPosition.y * zoom + panY;

    ctx.strokeStyle = this.HIGHLIGHT_COLOR;
    ctx.lineWidth = 1;
    ctx.globalAlpha = 0.8;

    // Draw vertical indicator line on horizontal ruler
    if (screenX >= 0 && screenX <= canvasWidth) {
      ctx.beginPath();
      ctx.moveTo(screenX, 0);
      ctx.lineTo(screenX, this.RULER_THICKNESS_PX);
      ctx.stroke();
    }

    // Draw horizontal indicator line on vertical ruler
    if (screenY >= 0 && screenY <= canvasHeight) {
      ctx.beginPath();
      ctx.moveTo(0, screenY);
      ctx.lineTo(this.RULER_THICKNESS_PX, screenY);
      ctx.stroke();
    }

    ctx.globalAlpha = 1;
  }

  /**
   * Update cursor position for indicator rendering
   * @param x - Canvas X coordinate (not screen coordinate)
   * @param y - Canvas Y coordinate (not screen coordinate)
   */
  public updateCursorPosition(x: number | null, y: number | null): void {
    if (x === null || y === null) {
      this.cursorPosition = null;
    } else {
      this.cursorPosition = { x, y };
    }
  }

  /**
   * Set ruler visibility
   */
  public setVisible(visible: boolean): void {
    this.visible = visible;
  }

  /**
   * Get current visibility state
   */
  public isVisible(): boolean {
    return this.visible;
  }

  /**
   * Toggle ruler visibility
   */
  public toggle(): void {
    this.visible = !this.visible;
  }
}
