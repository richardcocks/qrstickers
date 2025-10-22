/**
 * Multi-Device Export Module - Phase 5.3
 * Handles bulk export of multiple devices with ZIP file generation
 * Integrates with device-export.js and export-preview.js
 */

'use strict';

// State management
let bulkExportState = {
    selectedDevices: [],
    bulkExportModal: null,
    exportCancelled: false,
    currentExportData: []
};

/**
 * Toggle select all checkboxes
 */
function toggleSelectAll() {
    const selectAllCheckbox = document.getElementById('selectAllDevices');
    const deviceCheckboxes = document.querySelectorAll('.device-checkbox');

    deviceCheckboxes.forEach(checkbox => {
        checkbox.checked = selectAllCheckbox.checked;
    });

    onDeviceSelectionChanged();
}

/**
 * Handle device selection change
 * Updates bulk export button state and text
 */
function onDeviceSelectionChanged() {
    const selected = getSelectedDevices();
    const bulkBtn = document.getElementById('bulkExportBtn');
    const summary = document.getElementById('selectionSummary');

    // Update button state
    bulkBtn.disabled = selected.length === 0;
    bulkBtn.style.opacity = selected.length === 0 ? '0.5' : '1';
    bulkBtn.style.cursor = selected.length === 0 ? 'not-allowed' : 'pointer';
    bulkBtn.textContent = `📦 Export Selected (${selected.length})`;

    // Update summary text
    if (selected.length > 0) {
        summary.textContent = `${selected.length} device${selected.length !== 1 ? 's' : ''} selected`;
    } else {
        summary.textContent = '';
    }

    // Update "Select All" checkbox state
    const allCheckboxes = document.querySelectorAll('.device-checkbox');
    const selectAllCheckbox = document.getElementById('selectAllDevices');
    if (selectAllCheckbox) {
        selectAllCheckbox.checked = selected.length === allCheckboxes.length && allCheckboxes.length > 0;
    }
}

/**
 * Get list of currently selected devices
 */
function getSelectedDevices() {
    const checkboxes = document.querySelectorAll('.device-checkbox:checked');
    return Array.from(checkboxes).map(cb => ({
        id: parseInt(cb.getAttribute('data-device-id')),
        name: cb.getAttribute('data-device-name'),
        serial: cb.getAttribute('data-device-serial'),
        connectionId: parseInt(cb.getAttribute('data-connection-id'))
    }));
}

/**
 * Opens bulk export modal with selected devices
 */
async function openBulkExportModal() {
    console.log('[Bulk Export] Opening bulk export modal');

    const selected = getSelectedDevices();
    if (selected.length === 0) {
        showNotification('Please select at least one device to export.', 'error');
        return;
    }

    bulkExportState.selectedDevices = selected;
    bulkExportState.exportCancelled = false;

    // Create modal if not exists
    if (!bulkExportState.bulkExportModal) {
        createBulkExportModal();
    }

    const modal = bulkExportState.bulkExportModal;
    modal.style.display = 'flex';

    // Reset modal state (in case it was left in export state from previous use)
    const startExportBtn = modal.querySelector('.btn-start-export');
    if (startExportBtn) {
        startExportBtn.style.display = 'inline-block';
    }

    // Show loading state
    const modalBody = modal.querySelector('.modal-body');
    modalBody.innerHTML = '<div style="padding: 40px; text-align: center;">Loading device information...</div>';

    try {
        // Fetch template matches for all devices
        console.log(`[Bulk Export] Fetching data for ${selected.length} devices`);
        const exportDataPromises = selected.map(device =>
            fetchDeviceExportData(device.id, device.connectionId)
        );

        const exportDataResults = await Promise.all(exportDataPromises);
        bulkExportState.currentExportData = exportDataResults;

        // Render modal content
        renderBulkExportModalContent();

    } catch (error) {
        console.error('[Bulk Export] Error loading device data:', error);
        modalBody.innerHTML = `
            <div style="padding: 40px; color: red; text-align: center;">
                <strong>Error:</strong> ${error.message}
                <br><br>
                <button onclick="closeBulkExportModal()" class="btn-secondary">Close</button>
            </div>
        `;
    }
}

/**
 * Fetches device export data from API
 */
async function fetchDeviceExportData(deviceId, connectionId) {
    const response = await fetch(`/api/export/device/${deviceId}?connectionId=${connectionId}`, {
        method: 'GET',
        headers: {
            'Accept': 'application/json',
            'Content-Type': 'application/json'
        }
    });

    if (!response.ok) {
        if (response.status === 403) {
            throw new Error('Permission denied for device ' + deviceId);
        } else if (response.status === 404) {
            throw new Error('Device ' + deviceId + ' not found');
        } else {
            throw new Error(`API error ${response.status} for device ${deviceId}`);
        }
    }

    const result = await response.json();
    return result.data;
}

/**
 * Creates the bulk export modal HTML structure
 */
function createBulkExportModal() {
    const modal = document.createElement('div');
    modal.className = 'modal bulk-export-modal';
    modal.id = 'bulkExportModal';
    modal.innerHTML = `
        <div class="modal-content" style="max-width: 900px;">
            <div class="modal-header">
                <h2 id="bulkModalTitle">Bulk Export: 0 Devices</h2>
                <button class="modal-close-btn" onclick="closeBulkExportModal()">✕</button>
            </div>
            <div class="modal-body bulk-export-body">
                <!-- Content populated dynamically -->
            </div>
            <div class="modal-footer">
                <button onclick="closeBulkExportModal()" class="btn-secondary">Cancel</button>
                <button onclick="startBulkExport()" class="btn-primary btn-start-export">Start Export</button>
            </div>
        </div>
    `;

    document.body.appendChild(modal);
    bulkExportState.bulkExportModal = modal;

    // Close on background click
    modal.onclick = (e) => {
        if (e.target === modal) {
            closeBulkExportModal();
        }
    };
}

/**
 * Renders the bulk export modal content
 */
function renderBulkExportModalContent() {
    const modal = bulkExportState.bulkExportModal;
    const selected = bulkExportState.selectedDevices;
    const exportData = bulkExportState.currentExportData;

    // Update title
    modal.querySelector('#bulkModalTitle').textContent = `Bulk Export: ${selected.length} Device${selected.length !== 1 ? 's' : ''}`;

    // Render modal body
    const modalBody = modal.querySelector('.modal-body');
    modalBody.innerHTML = `
        <div class="selected-devices-section" style="max-height: 500px; overflow-y: auto; margin-bottom: 20px;">
            <h3>Selected Devices</h3>
            <div id="deviceListContainer" class="device-list">
                ${renderDeviceList(selected, exportData)}
            </div>
        </div>

        <div class="export-options-section">
            <h3>Export Settings</h3>

            <div class="form-section">
                <label><strong>Output Format:</strong></label>
                <div class="radio-group">
                    <label class="radio-label">
                        <input type="radio" name="bulk-format" value="zip-png" checked>
                        Individual PNG Files (ZIP)
                    </label>
                    <label class="radio-label">
                        <input type="radio" name="bulk-format" value="zip-svg">
                        Individual SVG Files (ZIP)
                    </label>
                </div>

                <div id="pngOptionsB bulk">
                    <label><strong>PNG DPI:</strong></label>
                    <select id="bulkDpi" style="padding: 8px; border: 1px solid #ccc; border-radius: 4px; width: 200px;">
                        <option value="96">96 DPI (Web)</option>
                        <option value="150">150 DPI (Medium)</option>
                        <option value="300" selected>300 DPI (Print)</option>
                    </select>
                </div>

                <label style="margin-top: 15px; display: block;"><strong>Background:</strong></label>
                <div class="radio-group">
                    <label class="radio-label">
                        <input type="radio" name="bulk-background" value="white" checked>
                        White
                    </label>
                    <label class="radio-label">
                        <input type="radio" name="bulk-background" value="transparent">
                        Transparent
                    </label>
                </div>
            </div>
        </div>

        <div id="exportProgressSection" class="progress-section" style="display: none; margin-top: 20px;">
            <h3>Exporting...</h3>
            <div class="progress-bar-container">
                <div class="progress-bar-fill" id="progressBarFill" style="width: 0%;"></div>
            </div>
            <p id="progressText" style="text-align: center; margin: 10px 0; color: #555;">Preparing export...</p>
            <button id="cancelExportBtn" onclick="cancelBulkExport()" class="btn-secondary" style="display: block; margin: 15px auto;">Cancel</button>
        </div>
    `;

    // Ensure Start Export button is visible (in case it was hidden from previous export)
    const startExportBtn = modal.querySelector('.btn-start-export');
    if (startExportBtn) {
        startExportBtn.style.display = 'inline-block';
    }
}

/**
 * Renders device list cards
 */
function renderDeviceList(devices, exportDataList) {
    return devices.map((device, index) => {
        const exportData = exportDataList[index];
        const template = exportData?.matchedTemplate;
        const matchReason = template?.matchReason || 'unknown';
        const confidence = template?.confidence ? Math.round(template.confidence * 100) : 0;

        return `
            <div class="device-card" style="padding: 12px; margin-bottom: 10px; background: #f9f9f9; border: 1px solid #ddd; border-radius: 4px;">
                <div style="display: flex; justify-content: space-between; align-items: center;">
                    <div>
                        <strong>${device.name}</strong>
                        <div style="font-size: 0.85em; color: #666; margin-top: 4px;">
                            <code>${device.serial}</code>
                        </div>
                    </div>
                    <div style="text-align: right; font-size: 0.85em;">
                        <div style="color: #666;">Template: ${template?.name || 'Unknown'}</div>
                        <div style="margin-top: 4px;">
                            <span class="match-badge match-${matchReason}" style="padding: 2px 8px; border-radius: 3px; font-size: 0.8em; background: ${getMatchBadgeColor(matchReason)}; color: white;">
                                ${formatMatchReason(matchReason)} (${confidence}%)
                            </span>
                        </div>
                    </div>
                </div>
            </div>
        `;
    }).join('');
}

/**
 * Get badge color for match reason
 */
function getMatchBadgeColor(reason) {
    const colors = {
        'model_match': '#4CAF50',
        'type_match': '#2196F3',
        'user_default': '#FF9800',
        'system_default': '#9E9E9E',
        'fallback': '#f44336'
    };
    return colors[reason] || '#9E9E9E';
}

/**
 * Format match reason for display
 */
function formatMatchReason(reason) {
    const reasons = {
        'model_match': '✓ Model',
        'type_match': '✓ Type',
        'user_default': '⚙ User Default',
        'system_default': '⚙ System Default',
        'fallback': '⚠ Fallback'
    };
    return reasons[reason] || reason;
}

/**
 * Starts the bulk export process
 */
async function startBulkExport() {
    console.log('[Bulk Export] Starting bulk export');

    const selected = bulkExportState.selectedDevices;
    const exportDataList = bulkExportState.currentExportData;
    const format = document.querySelector('input[name="bulk-format"]:checked')?.value || 'zip-png';
    const dpi = parseInt(document.getElementById('bulkDpi')?.value || '300');
    const background = document.querySelector('input[name="bulk-background"]:checked')?.value || 'white';

    // Hide options, show progress
    const modal = bulkExportState.bulkExportModal;
    modal.querySelector('.export-options-section').style.display = 'none';
    modal.querySelector('.selected-devices-section').style.display = 'none';
    modal.querySelector('#exportProgressSection').style.display = 'block';
    modal.querySelector('.btn-start-export').style.display = 'none';

    // Initialize ZIP
    const zip = new JSZip();
    const exportedFiles = [];
    const failedDevices = [];
    bulkExportState.exportCancelled = false;

    // Export each device
    for (let i = 0; i < selected.length; i++) {
        if (bulkExportState.exportCancelled) {
            console.log('[Bulk Export] Export cancelled by user');
            break;
        }

        const device = selected[i];
        const exportData = exportDataList[i];

        updateProgress(i + 1, selected.length, device.name);

        try {
            // Create device data map
            const deviceDataMap = createDeviceDataMap(exportData);

            // Render device to blob
            const blob = await renderDeviceToBlob(
                exportData.matchedTemplate,
                deviceDataMap,
                format.includes('png') ? 'png' : 'svg',
                { dpi, background }
            );

            // Generate filename
            const filename = generateFilename(device, format, dpi);

            // Add to ZIP
            zip.file(filename, blob);
            exportedFiles.push(filename);

            console.log(`[Bulk Export] Exported ${i + 1}/${selected.length}: ${filename}`);

        } catch (error) {
            console.error(`[Bulk Export] Failed to export device ${device.name}:`, error);
            failedDevices.push({ device: device.name, error: error.message });
        }

        // Add small delay to show progress (better UX feedback)
        await new Promise(resolve => setTimeout(resolve, 50));
    }

    // Generate and download ZIP if not cancelled
    if (!bulkExportState.exportCancelled && exportedFiles.length > 0) {
        try {
            console.log('[Bulk Export] Generating ZIP file...');
            updateProgress(selected.length, selected.length, 'Generating ZIP file...');

            // Small delay to show ZIP generation step
            await new Promise(resolve => setTimeout(resolve, 300));

            const zipBlob = await zip.generateAsync({ type: 'blob' });
            const zipFilename = `device-stickers-${exportedFiles.length}-devices-${Date.now()}.zip`;

            downloadBlob(zipBlob, zipFilename);

            showCompletionSummary(exportedFiles.length, failedDevices);

        } catch (error) {
            console.error('[Bulk Export] Error generating ZIP:', error);
            showNotification('Error generating ZIP file: ' + error.message, 'error');
        }
    }

    closeBulkExportModal();
}

/**
 * Renders a device to a blob
 */
async function renderDeviceToBlob(template, deviceDataMap, format, options) {
    const templateJson = JSON.parse(template.templateJson || '{}');

    // Clone and merge template with device data
    const mergedTemplate = JSON.parse(JSON.stringify(templateJson));
    replacePlaceholdersInTemplate(mergedTemplate, deviceDataMap);

    // Create temporary canvas
    const tempCanvas = document.createElement('canvas');

    // Render using export-preview.js functions
    const fabricCanvas = createAndRenderPreviewCanvas(
        tempCanvas,
        mergedTemplate,
        template.pageWidth,
        template.pageHeight,
        true, // Export at full resolution
        options
    );

    // Convert to blob
    return new Promise((resolve, reject) => {
        try {
            if (format === 'png') {
                fabricCanvas.lowerCanvasEl.toBlob((blob) => {
                    fabricCanvas.dispose(); // Free memory
                    resolve(blob);
                }, 'image/png');
            } else {
                const svgData = fabricCanvas.toSVG();
                const blob = new Blob([svgData], { type: 'image/svg+xml' });
                fabricCanvas.dispose(); // Free memory
                resolve(blob);
            }
        } catch (error) {
            fabricCanvas.dispose(); // Free memory on error
            reject(error);
        }
    });
}

/**
 * Generates filename for device export
 */
function generateFilename(device, format, dpi) {
    const serial = (device.serial || device.name || 'device')
        .toLowerCase()
        .replace(/[^a-z0-9]/g, '-');
    const ext = format.includes('png') ? 'png' : 'svg';
    const dpiStr = format.includes('png') ? `-${dpi}dpi` : '';
    return `sticker-${serial}${dpiStr}.${ext}`;
}

/**
 * Updates export progress
 */
function updateProgress(current, total, text) {
    const percent = Math.round((current / total) * 100);
    const progressBar = document.getElementById('progressBarFill');
    const progressText = document.getElementById('progressText');

    if (progressBar) {
        progressBar.style.width = `${percent}%`;
    }

    if (progressText) {
        progressText.textContent = `Processing device ${current} of ${total}: ${text}`;
    }
}

/**
 * Downloads a blob as a file
 */
function downloadBlob(blob, filename) {
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = filename;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
}

/**
 * Shows completion summary
 */
function showCompletionSummary(successCount, failed) {
    // Show success notification
    const successMessage = `Successfully exported ${successCount} device${successCount !== 1 ? 's' : ''}`;
    showNotification(successMessage, 'success');

    // If there were failures, show warning and log details
    if (failed.length > 0) {
        setTimeout(() => {
            const failMessage = `${failed.length} device${failed.length !== 1 ? 's' : ''} failed to export`;
            showNotification(failMessage, 'error');
        }, 500); // Delay slightly so both notifications are visible

        // Log failure details to console for debugging
        console.warn('[Bulk Export] Failed devices:');
        failed.forEach(f => {
            console.warn(`  • ${f.device}: ${f.error}`);
        });
    }
}

/**
 * Cancels the bulk export
 */
function cancelBulkExport() {
    console.log('[Bulk Export] User cancelled export');
    bulkExportState.exportCancelled = true;
}

/**
 * Closes the bulk export modal
 */
function closeBulkExportModal() {
    console.log('[Bulk Export] Closing bulk export modal');

    if (bulkExportState.bulkExportModal) {
        bulkExportState.bulkExportModal.style.display = 'none';
    }

    bulkExportState.selectedDevices = [];
    bulkExportState.currentExportData = [];
    bulkExportState.exportCancelled = false;
}

console.log('[Bulk Export] Multi-device export module loaded');
