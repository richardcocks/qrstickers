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
    originalMatchedTemplate: null, // Original matched template (for switching back)
    alternateTemplates: null, // List of alternate template options
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
}

/**
 * Opens device export modal and fetches all necessary data
 */
async function openDeviceExportModal(deviceId, connectionId, deviceName) {
    // Create modal if not exists
    if (!deviceExportState.deviceExportModal) {
        createDeviceExportModal();
    }

    const modal = deviceExportState.deviceExportModal;

    try {
        // Fetch device export data from API (while modal still hidden)
        // includeAlternates=true to get template selection options
        const response = await fetch(`/api/export/device/${deviceId}?connectionId=${connectionId}&includeAlternates=true`, {
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
        deviceExportState.originalMatchedTemplate = result.data.matchedTemplate; // Store original for switching back
        deviceExportState.alternateTemplates = result.data.alternateTemplates || null;

        // Render modal UI
        renderDeviceExportModalUI();

        // Generate initial preview
        await updateDeviceExportPreview();

        // Show modal after content is fully rendered (prevents left-side flash)
        modal.style.display = 'flex';

    } catch (error) {
        console.error('[Device Export] Error opening modal:', error);
        const contentArea = modal.querySelector('.modal-body');
        contentArea.innerHTML = `<div style="padding: 40px; color: red; text-align: center;">
            <strong>Error:</strong> ${error.message}
            <br><br>
            <button onclick="closeDeviceExportModal()" class="btn-secondary">Close</button>
        </div>`;
        // Show modal with error message
        modal.style.display = 'flex';
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
        <div class="modal-content" style="max-width: 1000px;">
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

    // Device Information Section (use textContent for XSS prevention)
    const deviceInfo = modal.querySelector('#deviceInfo');
    deviceInfo.innerHTML = `
        <h3>Device Information</h3>
        <div class="info-box">
            <p><strong>Name:</strong> <span data-field="device-name"></span></p>
            <p><strong>Serial:</strong> <code data-field="device-serial"></code></p>
            <p><strong>Model:</strong> <span data-field="device-model"></span></p>
            <p><strong>Product Type:</strong> <span data-field="device-type"></span></p>
        </div>
    `;
    // Populate with textContent (safe, like Razor's @Model.Property)
    deviceInfo.querySelector('[data-field="device-name"]').textContent = device.name || 'Unnamed';
    deviceInfo.querySelector('[data-field="device-serial"]').textContent = device.serial || 'N/A';
    deviceInfo.querySelector('[data-field="device-model"]').textContent = device.model || 'N/A';
    deviceInfo.querySelector('[data-field="device-type"]').textContent = device.productType || 'Unknown';

    // Template Selection Section
    const templateInfo = modal.querySelector('#templateInfo');
    const hasAlternates = deviceExportState.alternateTemplates && deviceExportState.alternateTemplates.length > 0;

    templateInfo.innerHTML = `
        <h3>Template</h3>
        <div class="form-section">
            <label><strong>Select Template:</strong></label>
            <select id="templateSelector" class="form-select template-selector" onchange="onTemplateChanged()">
                ${renderTemplateOptions()}
            </select>
            <small id="templateCompatibilityNote" class="form-text text-muted"></small>
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
                <p style="font-size: 11px; color: #666; margin-top: 8px; font-style: italic;">
                    Note: Preview is shown at 96 DPI. Selected DPI applies to exported file.
                </p>

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
 * Renders template selector dropdown options with grouping
 * Groups: Currently Matched -> Recommended -> Compatible -> Not Recommended
 */
function renderTemplateOptions() {
    const matchedTemplate = deviceExportState.currentTemplate;
    const alternates = deviceExportState.alternateTemplates;

    // Helper function to escape HTML (XSS prevention)
    const escapeHtml = (text) => {
        const div = document.createElement('div');
        div.textContent = text;
        return div.innerHTML;
    };

    let optionsHtml = '';

    // Currently matched template (always first, selected by default)
    optionsHtml += `<option value="${matchedTemplate.id}" selected data-category="matched">
        ${escapeHtml(matchedTemplate.name)} (Currently Matched)
    </option>`;

    // If no alternates, return early
    if (!alternates || alternates.length === 0) {
        return optionsHtml;
    }

    // Group alternates by category
    const recommended = alternates.filter(opt => opt.category === 'recommended');
    const compatible = alternates.filter(opt => opt.category === 'compatible');
    const incompatible = alternates.filter(opt => opt.category === 'incompatible');

    // Recommended templates
    if (recommended.length > 0) {
        optionsHtml += '<optgroup label="⭐ Recommended">';
        recommended.forEach(opt => {
            optionsHtml += `<option value="${opt.template.id}"
                data-category="recommended"
                data-note="${escapeHtml(opt.compatibilityNote || '')}">
                ${escapeHtml(opt.template.name)}
            </option>`;
        });
        optionsHtml += '</optgroup>';
    }

    // Compatible templates
    if (compatible.length > 0) {
        optionsHtml += '<optgroup label="✓ Compatible">';
        compatible.forEach(opt => {
            optionsHtml += `<option value="${opt.template.id}"
                data-category="compatible"
                data-note="${escapeHtml(opt.compatibilityNote || '')}">
                ${escapeHtml(opt.template.name)}
            </option>`;
        });
        optionsHtml += '</optgroup>';
    }

    // Incompatible templates (with warning)
    if (incompatible.length > 0) {
        optionsHtml += '<optgroup label="⚠ Not Recommended">';
        incompatible.forEach(opt => {
            optionsHtml += `<option value="${opt.template.id}"
                data-category="incompatible"
                data-note="${escapeHtml(opt.compatibilityNote || '')}">
                ${escapeHtml(opt.template.name)}
            </option>`;
        });
        optionsHtml += '</optgroup>';
    }

    return optionsHtml;
}

/**
 * Handler for template selection change
 * Updates preview with newly selected template
 */
async function onTemplateChanged() {
    const selector = document.getElementById('templateSelector');
    if (!selector) return;

    const selectedTemplateId = parseInt(selector.value);
    const selectedOption = selector.options[selector.selectedIndex];
    const category = selectedOption.dataset.category;
    const note = selectedOption.dataset.note;

    // Show compatibility note below dropdown
    const noteElement = document.getElementById('templateCompatibilityNote');
    if (noteElement) {
        if (note) {
            noteElement.textContent = note;
            noteElement.className = category === 'incompatible'
                ? 'form-text text-warning'
                : 'form-text text-muted';
        } else {
            noteElement.textContent = '';
        }
    }

    // Find the selected template
    let selectedTemplate;

    // Check if user selected the original matched template
    if (selectedTemplateId === deviceExportState.originalMatchedTemplate.id) {
        selectedTemplate = deviceExportState.originalMatchedTemplate;
    } else {
        // User selected an alternate template
        const alternateOption = deviceExportState.alternateTemplates?.find(
            opt => opt.template.id === selectedTemplateId
        );
        selectedTemplate = alternateOption?.template;
    }

    if (!selectedTemplate) {
        console.error('[Device Export] Selected template not found');
        return;
    }

    // Update current template in state
    deviceExportState.currentTemplate = selectedTemplate;

    // Re-render preview with newly selected template
    await updateDeviceExportPreview();
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
    const previewContainer = document.querySelector('#previewContainer');
    if (!previewContainer) return;

    try {
        const data = deviceExportState.currentExportData;
        const template = deviceExportState.currentTemplate; // Use current template from state
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

        // Create preview canvas and wait for QR images to load
        deviceExportState.previewCanvas = await createAndRenderPreviewCanvas(
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

    // Build base data map
    const dataMap = {
        device: {
            id: device.id,
            serial: device.serial || '',
            name: device.name || 'Unnamed Device',
            mac: device.mac || '',
            model: device.model || '',
            productType: device.productType || 'unknown',
            producttype: device.productType || 'unknown', // Lowercase alias
            type: device.type || 'unknown',
            status: device.status || 'unknown',
            firmware: device.firmware || '',
            tags: tags,
            tags_str: tagsStr,
            networkId: device.networkId,
            connectionId: device.connectionId,
            qrcode: device.qrCode || null // QR code data URI
        },
        network: exportData.network ? {
            id: exportData.network.id,
            name: exportData.network.name || '',
            organizationId: exportData.network.organizationId,
            qrcode: exportData.network.qrCode || null // QR code data URI
        } : null,
        organization: exportData.organization ? {
            id: exportData.organization.id,
            organizationid: exportData.organization.organizationId,
            name: exportData.organization.name || '',
            url: exportData.organization.url || '',
            qrcode: exportData.organization.qrCode || null // QR code data URI
        } : null,
        connection: {
            id: exportData.connection.id,
            displayName: exportData.connection.displayName || '',
            type: exportData.connection.type || '',
            companyLogoUrl: exportData.connection.companyLogoUrl || ''
        },
        global: exportData.globalVariables || {}
    };

    // Add custom images with lowercase binding keys
    // Format: customimage.image_42 → image data URI
    if (exportData.uploadedImages && exportData.uploadedImages.length > 0) {
        exportData.uploadedImages.forEach(image => {
            const bindingKey = `customimage.image_${image.id}`;
            // Add to root level for easy lookup
            dataMap[bindingKey] = image.dataUri;
        });
    }

    return dataMap;
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

    // Check for custom images first (special case: customImage.Image_42)
    // These are stored at root level with lowercase keys: customimage.image_42
    const customImageKey = `${entityLower}.${fieldLower}`;

    if (dataMap[customImageKey] !== undefined) {
        return dataMap[customImageKey];
    }

    // Try exact match first, then lowercase match for regular entities
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
    try {
        const data = deviceExportState.currentExportData;
        const template = deviceExportState.currentTemplate; // Use current template from state
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
        // Wait for QR images to load before exporting
        const exportCanvas = await createAndRenderPreviewCanvas(
            tempCanvas,
            exportTemplate,
            template.pageWidth,
            template.pageHeight,
            true, // Export at full resolution
            options
        );

        // Export based on format
        if (format === 'png') {
            exportPNGForDevice(exportCanvas, template.pageWidth, template.pageHeight, device.serial || 'device');
        } else if (format === 'svg') {
            exportSVGForDevice(exportCanvas, template.pageWidth, template.pageHeight, device.serial || 'device');
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
 * Extracts image IDs referenced in template JSON
 * Searches for customImage.Image_* patterns used in data bindings
 * @param {string} templateJson - Fabric.js template JSON
 * @returns {number[]} Array of unique image IDs
 */
function extractImageIdsFromTemplate(templateJson) {
    const regex = /customImage\.Image_(\d+)/g;
    const matches = templateJson.matchAll(regex);
    const ids = [...matches].map(m => parseInt(m[1]));
    return [...new Set(ids)]; // Remove duplicates
}

/**
 * Tracks usage of template and images (updates LastUsedAt timestamps)
 */
async function logExportToHistory(deviceId, templateId, format, options) {
    try {
        const templateJson = deviceExportState.currentExportData?.matchedTemplate?.templateJson;
        if (!templateJson) {
            console.warn('[Usage Tracking] No template JSON available');
            return;
        }

        const imageIds = extractImageIdsFromTemplate(templateJson);

        // Fire-and-forget API call to track usage
        await fetch('/api/usage/track', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({
                imageIds: imageIds,
                templateId: templateId
            })
        });
    } catch (error) {
        console.warn('[Usage Tracking] Failed to track usage:', error);
        // Silent failure - don't block export
    }
}

/**
 * Closes the device export modal
 */
function closeDeviceExportModal() {

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
 * Show notification message
 */
function showNotification(message, type = 'info') {
    const notification = document.createElement('div');
    notification.className = `notification notification-${type}`;
    notification.textContent = message;
    notification.style.cssText = `
        position: fixed;
        top: 20px;
        left: 50%;
        transform: translateX(-50%);
        padding: 15px 20px;
        background: ${type === 'success' ? '#4CAF50' : '#f44336'};
        color: white;
        border-radius: 4px;
        z-index: 10000;
        box-shadow: 0 4px 12px rgba(0,0,0,0.15);
        min-width: 300px;
        text-align: center;
        animation: slideDown 0.3s ease-in-out;
    `;

    document.body.appendChild(notification);

    setTimeout(() => {
        notification.style.animation = 'fadeOut 0.3s ease-in-out';
        setTimeout(() => {
            notification.remove();
        }, 300);
    }, 3000);
}

// Initialize on DOM ready
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', initDeviceExport);
} else {
    initDeviceExport();
}
