/**
 * Device Export Module - Phase 5
 * Handles export of individual devices with real data from the Devices page
 * Integrates with export-preview.js for canvas rendering
 */

'use strict';

// State management
let deviceExportState = {
    currentDevice: null,
    currentExportData: null,
    currentTemplate: null,
    deviceExportModal: null,
    previewCanvas: null,
    currentExportFormat: 'png',
    currentExportOptions: { dpi: 300, background: 'white' }
};

/**
 * Initialize device export functionality on page load
 * Attaches click handlers to all export buttons
 */
function initDeviceExport() {
    console.log('[Device Export] Initializing device export module');

    // Find all export buttons in device table
    const exportButtons = document.querySelectorAll('[data-export-button]');
    exportButtons.forEach(btn => {
        btn.addEventListener('click', (e) => {
            e.preventDefault();
            const deviceId = parseInt(btn.getAttribute('data-device-id'));
            const connectionId = parseInt(btn.getAttribute('data-connection-id'));
            const deviceName = btn.getAttribute('data-device-name');
            openDeviceExportModal(deviceId, connectionId, deviceName);
        });
    });

    console.log(`[Device Export] Initialized ${exportButtons.length} export buttons`);
}

/**
 * Opens device export modal and fetches all necessary data
 */
async function openDeviceExportModal(deviceId, connectionId, deviceName) {
    console.log(`[Device Export] Opening export modal for device ${deviceId} (${deviceName})`);

    // Create modal if not exists
    if (!deviceExportState.deviceExportModal) {
        createDeviceExportModal();
    }

    // Show loading state
    const modal = deviceExportState.deviceExportModal;
    const contentArea = modal.querySelector('.modal-body');
    contentArea.innerHTML = '<div style="padding: 40px; text-align: center;">Loading device data...</div>';
    modal.style.display = 'block';

    try {
        // Fetch device export data from API
        console.log('[Device Export] Fetching export data from API');
        const response = await fetch(`/api/export/device/${deviceId}?connectionId=${connectionId}`, {
            method: 'GET',
            headers: {
                'Accept': 'application/json',
                'Content-Type': 'application/json'
            }
        });

        if (!response.ok) {
            if (response.status === 403) {
                throw new Error('You do not have permission to export this device');
            } else if (response.status === 404) {
                throw new Error('Device not found');
            } else {
                throw new Error(`API error: ${response.status}`);
            }
        }

        const result = await response.json();
        deviceExportState.currentExportData = result.data;
        deviceExportState.currentDevice = result.data.device;
        deviceExportState.currentTemplate = result.data.matchedTemplate;

        console.log('[Device Export] Device data loaded, rendering modal');

        // Render modal UI
        renderDeviceExportModalUI();

        // Generate initial preview
        await updateDeviceExportPreview();

    } catch (error) {
        console.error('[Device Export] Error opening modal:', error);
        contentArea.innerHTML = `<div style="padding: 40px; color: red; text-align: center;">
            <strong>Error:</strong> ${error.message}
            <br><br>
            <button onclick="deviceExportState.deviceExportModal.style.display = 'none';" class="btn-secondary">Close</button>
        </div>`;
    }
}

/**
 * Creates the export modal HTML structure
 */
function createDeviceExportModal() {
    const modal = document.createElement('div');
    modal.className = 'modal device-export-modal';
    modal.id = 'deviceExportModal';
    modal.innerHTML = `
        <div class="modal-content" style="max-width: 900px;">
            <div class="modal-header">
                <h2 id="modalTitle">Export Device Sticker</h2>
                <button class="modal-close-btn" onclick="closeDeviceExportModal()">✕</button>
            </div>
            <div class="modal-body device-export-body">
                <div class="export-controls">
                    <div id="deviceInfo" class="export-section"></div>
                    <div id="templateInfo" class="export-section"></div>
                    <div id="exportSettings" class="export-section"></div>
                </div>
                <div class="preview-section">
                    <h3>Preview</h3>
                    <div id="previewContainer" class="preview-container">
                        <canvas id="devicePreviewCanvas"></canvas>
                    </div>
                </div>
            </div>
            <div class="modal-footer">
                <button onclick="closeDeviceExportModal()" class="btn-secondary">Cancel</button>
                <button onclick="downloadDeviceExport()" class="btn-primary">📥 Export</button>
            </div>
        </div>
    `;

    document.body.appendChild(modal);
    deviceExportState.deviceExportModal = modal;

    // Attach close button handlers
    modal.onclick = (e) => {
        if (e.target === modal) {
            closeDeviceExportModal();
        }
    };
}

/**
 * Renders the modal content with device information and settings
 */
function renderDeviceExportModalUI() {
    const data = deviceExportState.currentExportData;
    const device = data.device;
    const template = data.matchedTemplate;
    const modal = deviceExportState.deviceExportModal;

    // Restore modal body structure (it was replaced by loading message)
    const modalBody = modal.querySelector('.modal-body');
    modalBody.innerHTML = `
        <div class="export-controls">
            <div id="deviceInfo" class="export-section"></div>
            <div id="templateInfo" class="export-section"></div>
            <div id="exportSettings" class="export-section"></div>
        </div>
        <div class="preview-section">
            <h3>Preview</h3>
            <div id="previewContainer" class="preview-container">
                <canvas id="devicePreviewCanvas"></canvas>
            </div>
        </div>
    `;

    // Device Information Section
    const deviceInfo = modal.querySelector('#deviceInfo');
    deviceInfo.innerHTML = `
        <h3>Device Information</h3>
        <div class="info-box">
            <p><strong>Name:</strong> ${device.name || 'Unnamed'}</p>
            <p><strong>Serial:</strong> <code>${device.serial || 'N/A'}</code></p>
            <p><strong>Model:</strong> ${device.model || 'N/A'}</p>
            <p><strong>IP Address:</strong> ${device.ipAddress || 'N/A'}</p>
            <p><strong>Status:</strong> ${device.status || 'Unknown'}</p>
        </div>
    `;

    // Template Information Section
    const templateInfo = modal.querySelector('#templateInfo');
    templateInfo.innerHTML = `
        <h3>Template</h3>
        <div class="info-box">
            <p><strong>Matched Template:</strong> ${template.name}</p>
            <p><strong>Match Reason:</strong> <span class="match-badge match-${template.matchReason}">${formatMatchReason(template.matchReason)}</span></p>
            <p><strong>Confidence:</strong> ${Math.round(template.confidence * 100)}%</p>
        </div>
    `;

    // Export Settings Section
    const exportSettings = modal.querySelector('#exportSettings');
    exportSettings.innerHTML = `
        <h3>Export Settings</h3>
        <div class="form-section">
            <label><strong>Format:</strong></label>
            <div class="radio-group">
                <label class="radio-label">
                    <input type="radio" name="export-format" value="png" checked onchange="onExportFormatChanged()">
                    PNG
                </label>
                <label class="radio-label">
                    <input type="radio" name="export-format" value="svg" onchange="onExportFormatChanged()">
                    SVG
                </label>
            </div>

            <div id="pngOptions" class="form-section">
                <label><strong>PNG DPI:</strong></label>
                <div class="radio-group">
                    <label class="radio-label">
                        <input type="radio" name="export-dpi" value="96" onchange="onExportDpiChanged()">
                        96 DPI (Web)
                    </label>
                    <label class="radio-label">
                        <input type="radio" name="export-dpi" value="150" onchange="onExportDpiChanged()">
                        150 DPI (Medium)
                    </label>
                    <label class="radio-label">
                        <input type="radio" name="export-dpi" value="300" checked onchange="onExportDpiChanged()">
                        300 DPI (Print)
                    </label>
                </div>

                <label><strong>Background:</strong></label>
                <div class="radio-group">
                    <label class="radio-label">
                        <input type="radio" name="export-background" value="white" checked onchange="onExportBackgroundChanged()">
                        White
                    </label>
                    <label class="radio-label">
                        <input type="radio" name="export-background" value="transparent" onchange="onExportBackgroundChanged()">
                        Transparent
                    </label>
                </div>
            </div>
        </div>
    `;
}

/**
 * Handler for format change
 */
function onExportFormatChanged() {
    const format = document.querySelector('input[name="export-format"]:checked')?.value || 'png';
    deviceExportState.currentExportFormat = format;

    // Show/hide PNG options
    const pngOptions = document.querySelector('#pngOptions');
    if (pngOptions) {
        pngOptions.style.display = format === 'png' ? 'block' : 'none';
    }

    updateDeviceExportPreview();
}

/**
 * Handler for DPI change
 */
function onExportDpiChanged() {
    const dpi = parseInt(document.querySelector('input[name="export-dpi"]:checked')?.value || '96');
    deviceExportState.currentExportOptions.dpi = dpi;
    updateDeviceExportPreview();
}

/**
 * Handler for background change
 */
function onExportBackgroundChanged() {
    const background = document.querySelector('input[name="export-background"]:checked')?.value || 'white';
    deviceExportState.currentExportOptions.background = background;
    updateDeviceExportPreview();
}

/**
 * Updates the preview canvas with current export settings
 */
async function updateDeviceExportPreview() {
    console.log('[Device Export] Updating preview');

    const previewContainer = document.querySelector('#previewContainer');
    if (!previewContainer) return;

    try {
        const data = deviceExportState.currentExportData;
        const template = data.matchedTemplate;
        const templateJson = JSON.parse(template.templateJson || '{}');

        // Create device data map
        const deviceDataMap = createDeviceDataMap(data);

        // Clone and merge template with real device data
        const mergedTemplate = JSON.parse(JSON.stringify(templateJson));
        replacePlaceholdersInTemplate(mergedTemplate, deviceDataMap);

        // Get canvas element
        let canvas = document.querySelector('#devicePreviewCanvas');
        if (!canvas) {
            canvas = document.createElement('canvas');
            canvas.id = 'devicePreviewCanvas';
            previewContainer.appendChild(canvas);
        }

        // Create preview canvas
        deviceExportState.previewCanvas = createAndRenderPreviewCanvas(
            canvas,
            mergedTemplate,
            template.pageWidth,
            template.pageHeight,
            false // Don't scale for preview, use actual size
        );

        // Update checkerboard if transparent
        if (deviceExportState.currentExportOptions.background === 'transparent') {
            previewContainer.classList.add('transparent-bg');
        } else {
            previewContainer.classList.remove('transparent-bg');
        }

        console.log('[Device Export] Preview updated');
    } catch (error) {
        console.error('[Device Export] Error updating preview:', error);
    }
}

/**
 * Creates device data map from export context
 */
function createDeviceDataMap(exportData) {
    const device = exportData.device;
    const tags = device.tags || [];
    const tagsStr = tags.join(', ');

    return {
        device: {
            id: device.id,
            serial: device.serial || '',
            name: device.name || 'Unnamed Device',
            mac: device.mac || '',
            model: device.model || '',
            ipAddress: device.ipAddress || '',
            ipaddress: device.ipAddress || '', // Lowercase alias
            type: device.type || 'unknown',
            status: device.status || 'unknown',
            firmware: device.firmware || '',
            tags: tags,
            tags_str: tagsStr,
            networkId: device.networkId,
            connectionId: device.connectionId
        },
        network: exportData.network ? {
            id: exportData.network.id,
            name: exportData.network.name || '',
            organizationId: exportData.network.organizationId
        } : null,
        connection: {
            id: exportData.connection.id,
            displayName: exportData.connection.displayName || '',
            type: exportData.connection.type || '',
            companyLogoUrl: exportData.connection.companyLogoUrl || ''
        },
        global: exportData.globalVariables || {}
    };
}

/**
 * Replaces all {{binding}} placeholders with actual device data
 * Handles both text replacement and properties.dataSource lookups
 */
function replacePlaceholdersInTemplate(templateObj, dataMap) {
    if (!Array.isArray(templateObj.objects)) {
        return;
    }

    templateObj.objects.forEach(obj => {
        // Replace text content with placeholder patterns
        if ((obj.type === 'text' || obj.type === 'i-text') && obj.text) {
            obj.text = replacePlaceholders(obj.text, dataMap);
        }

        // Replace dataSource bindings for QR codes and other objects
        if (obj.properties && obj.properties.dataSource) {
            const dataSource = obj.properties.dataSource;
            const value = resolveDataSource(dataSource, dataMap);
            if (value !== null) {
                obj.properties.data = value;
            }
        }

        // Handle Fabric.js groups (which may have nested objects)
        if (obj.objects && Array.isArray(obj.objects)) {
            replacePlaceholdersInTemplate({ objects: obj.objects }, dataMap);
        }
    });
}

/**
 * Resolves a dataSource binding (e.g., "device.serial") to its actual value
 */
function resolveDataSource(dataSource, dataMap) {
    if (!dataSource) return null;

    // Parse "entity.field" format (case-insensitive)
    const parts = dataSource.split('.');
    if (parts.length !== 2) return null;

    const [entity, field] = parts;
    const entityLower = entity.toLowerCase();
    const fieldLower = field.toLowerCase();

    // Try exact match first, then lowercase match
    let value = dataMap[entity]?.[field];
    if (value === undefined) {
        value = dataMap[entityLower]?.[fieldLower];
    }

    return value !== undefined ? value : null;
}

/**
 * Replaces {{variable.field}} patterns with actual values
 * Case-insensitive matching ({{device.Name}} matches device.name in data map)
 */
function replacePlaceholders(text, dataMap) {
    return text.replace(/\{\{(\w+)\.(\w+)\}\}/g, (match, entity, field) => {
        // Convert to lowercase for case-insensitive lookup
        const entityLower = entity.toLowerCase();
        const fieldLower = field.toLowerCase();

        // Try exact match first, then lowercase match
        let value = dataMap[entity]?.[field];
        if (value === undefined) {
            value = dataMap[entityLower]?.[fieldLower];
        }

        return value !== undefined ? String(value) : match;
    });
}

/**
 * Downloads the device sticker with current settings
 */
async function downloadDeviceExport() {
    console.log('[Device Export] Starting export');

    try {
        const data = deviceExportState.currentExportData;
        const template = data.matchedTemplate;
        const templateJson = JSON.parse(template.templateJson || '{}');
        const device = data.device;

        // Create device data map
        const deviceDataMap = createDeviceDataMap(data);

        // Clone template
        const exportTemplate = JSON.parse(JSON.stringify(templateJson));
        replacePlaceholdersInTemplate(exportTemplate, deviceDataMap);

        // Get export options
        const format = deviceExportState.currentExportFormat;
        const options = deviceExportState.currentExportOptions;

        // Create temporary canvas for export
        const tempCanvas = document.createElement('canvas');

        // Use createAndRenderPreviewCanvas to render at full resolution
        const exportCanvas = createAndRenderPreviewCanvas(
            tempCanvas,
            exportTemplate,
            template.pageWidth,
            template.pageHeight,
            true, // Export at full resolution
            options
        );

        // Export based on format
        if (format === 'png') {
            exportPNG(exportCanvas, template.pageWidth, template.pageHeight, device.serial || 'device');
        } else if (format === 'svg') {
            exportSVG(exportCanvas, template.pageWidth, template.pageHeight, device.serial || 'device');
        }

        // Log export to history (async, non-blocking)
        logExportToHistory(device.id, template.id, format, options);

        // Show success message
        showNotification(`✓ Exported ${device.name} as ${format.toUpperCase()}`, 'success');

    } catch (error) {
        console.error('[Device Export] Export error:', error);
        showNotification('Export failed: ' + error.message, 'error');
    }
}

/**
 * Logs export to server history (Phase 5.1)
 */
function logExportToHistory(deviceId, templateId, format, options) {
    // This is a POST endpoint we'll need to create for Phase 5.2
    // For now, we'll just log to console
    console.log('[Device Export] Logging export to history:', { deviceId, templateId, format, options });

    // TODO: POST /api/export/history with export details
}

/**
 * Closes the device export modal
 */
function closeDeviceExportModal() {
    console.log('[Device Export] Closing export modal');

    if (deviceExportState.previewCanvas) {
        deviceExportState.previewCanvas.dispose?.();
    }

    if (deviceExportState.deviceExportModal) {
        deviceExportState.deviceExportModal.style.display = 'none';
    }

    deviceExportState.currentExportData = null;
    deviceExportState.currentDevice = null;
}

/**
 * Format match reason for display
 */
function formatMatchReason(reason) {
    const reasons = {
        'model_match': '✓ Model Match',
        'type_match': '✓ Type Match',
        'user_default': '⚙ User Default',
        'system_default': '⚙ System Default',
        'fallback': '⚠ Fallback'
    };
    return reasons[reason] || reason;
}

/**
 * Show notification message
 */
function showNotification(message, type = 'info') {
    const notification = document.createElement('div');
    notification.className = `notification notification-${type}`;
    notification.textContent = message;
    notification.style.cssText = `
        position: fixed;
        top: 20px;
        right: 20px;
        padding: 15px 20px;
        background: ${type === 'success' ? '#4CAF50' : '#f44336'};
        color: white;
        border-radius: 4px;
        z-index: 10000;
        animation: slideIn 0.3s ease-in-out;
    `;

    document.body.appendChild(notification);

    setTimeout(() => {
        notification.remove();
    }, 3000);
}

// Initialize on DOM ready
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', initDeviceExport);
} else {
    initDeviceExport();
}
