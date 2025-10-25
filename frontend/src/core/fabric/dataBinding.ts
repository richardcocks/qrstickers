/**
 * Data binding utilities for Fabric.js objects
 * Handles binding template objects to data sources
 */

import * as fabric from 'fabric';
import type { DataBindingInfo, FabricObjectWithBinding } from './types';

/**
 * Extract data binding information from a Fabric.js object
 *
 * @param fabricObject - The Fabric.js object to inspect
 * @returns Data binding information or null if object is null
 */
export function getDataBinding(fabricObject: fabric.Object | null): DataBindingInfo | null {
  if (!fabricObject) {
    return null;
  }

  const type = fabricObject.type || 'unknown';

  // Try to get dataSource from direct property or via get() method
  const obj = fabricObject as FabricObjectWithBinding;
  const dataSource = obj.dataSource ?? (fabricObject as any).get?.('dataSource') ?? null;

  return {
    type,
    dataSource,
    hasBinding: !!dataSource,
  };
}

/**
 * Update data binding on a Fabric.js object
 * For text objects, updates the text content to show the binding
 *
 * @param fabricObject - The Fabric.js object to update
 * @param dataSource - The new data source path (e.g., "device.Serial")
 */
export function updateDataBinding(fabricObject: fabric.Object | null, dataSource: string): void {
  if (!fabricObject) {
    return;
  }

  const obj = fabricObject as FabricObjectWithBinding;

  // Set dataSource property
  obj.dataSource = dataSource;

  // Update text content to show binding if it's a text object
  if (fabricObject.type === 'i-text' || fabricObject.type === 'text') {
    const textObj = fabricObject as fabric.IText;
    if (dataSource) {
      textObj.set('text', `{{${dataSource}}}`);
    }
  }
}

/**
 * Check if an object has a data binding
 *
 * @param fabricObject - The Fabric.js object to check
 * @returns True if the object has a non-empty data source
 */
export function hasDataBinding(fabricObject: fabric.Object | null): boolean {
  if (!fabricObject) {
    return false;
  }

  const binding = getDataBinding(fabricObject);
  return binding?.hasBinding ?? false;
}

/**
 * Clear data binding from an object
 *
 * @param fabricObject - The Fabric.js object to clear binding from
 */
export function clearDataBinding(fabricObject: fabric.Object | null): void {
  if (!fabricObject) {
    return;
  }

  const obj = fabricObject as FabricObjectWithBinding;
  obj.dataSource = undefined;

  // Clear text placeholder if it's a text object
  if (fabricObject.type === 'i-text' || fabricObject.type === 'text') {
    const textObj = fabricObject as fabric.IText;
    textObj.set('text', '');
  }
}

/**
 * Get all data sources used in a canvas
 * Useful for validating template data requirements
 *
 * @param canvas - The Fabric.js canvas to scan
 * @returns Array of unique data source paths
 */
export function getCanvasDataSources(canvas: fabric.Canvas): string[] {
  const dataSources = new Set<string>();

  canvas.getObjects().forEach((obj) => {
    const binding = getDataBinding(obj);
    if (binding?.dataSource) {
      dataSources.add(binding.dataSource);
    }
  });

  return Array.from(dataSources).sort();
}

/**
 * Validate that all required data sources are available
 *
 * @param canvas - The Fabric.js canvas to validate
 * @param availableData - Object containing available data paths
 * @returns Array of missing data source paths
 */
export function validateDataSources(canvas: fabric.Canvas, availableData: Record<string, any>): string[] {
  const requiredSources = getCanvasDataSources(canvas);
  const missing: string[] = [];

  requiredSources.forEach((source) => {
    // Simple path validation - check if path exists in data object
    const parts = source.split('.');
    let current: any = availableData;

    for (const part of parts) {
      if (current && typeof current === 'object' && part in current) {
        current = current[part];
      } else {
        missing.push(source);
        break;
      }
    }
  });

  return missing;
}
