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
 * Fire-and-forget - silent failure won't block exports
 */
async function trackUsage(templateId, templateJson) {
    try {
        const imageIds = extractImageIdsFromTemplate(templateJson);

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
        <div class="selected-devices-section" style="margin-bottom: 20px;">
            <h3>Selected Devices</h3>
            <div id="deviceListContainer" class="device-list"></div>
        </div>

        <div class="export-options-section">
            <h3>Export Settings</h3>

            <div class="form-section">
                <label><strong>Output Format:</strong></label>
                <div class="radio-group">
                    <label class="radio-label">
                        <input type="radio" name="bulk-format" value="zip-png" checked onchange="toggleBulkFormatOptions()">
                        Individual PNG Files (ZIP)
                    </label>
                    <label class="radio-label">
                        <input type="radio" name="bulk-format" value="zip-svg" onchange="toggleBulkFormatOptions()">
                        Individual SVG Files (ZIP)
                    </label>
                    <label class="radio-label">
                        <input type="radio" name="bulk-format" value="pdf" onchange="toggleBulkFormatOptions()">
                        Multi-Device PDF (Grid Layout)
                    </label>
                </div>

                <div id="pngOptionsBulk" style="margin-top: 15px;">
                    <label><strong>PNG DPI:</strong></label>
                    <select id="bulkDpi" style="padding: 8px; border: 1px solid #ccc; border-radius: 4px; width: 200px;">
                        <option value="96">96 DPI (Web)</option>
                        <option value="150">150 DPI (Medium)</option>
                        <option value="300" selected>300 DPI (Print)</option>
                    </select>
                </div>

                <div id="pdfOptionsBulk" style="margin-top: 15px; display: none;">
                    <label><strong>PDF Layout:</strong></label>
                    <div class="radio-group">
                        <label class="radio-label">
                            <input type="radio" name="pdf-layout" value="auto-fit" checked>
                            Auto-fit (maximize per page)
                        </label>
                        <label class="radio-label">
                            <input type="radio" name="pdf-layout" value="one-per-page">
                            One sticker per page
                        </label>
                    </div>

                    <label style="margin-top: 10px; display: block;"><strong>Page Size:</strong></label>
                    <select id="pdfPageSize" style="padding: 8px; border: 1px solid #ccc; border-radius: 4px; width: 200px;">
                        <option value="A4" selected>A4 (210mm × 297mm)</option>
                        <option value="A5">A5 (148mm × 210mm)</option>
                        <option value="A6">A6 (105mm × 148mm)</option>
                        <option value="4x6">4" × 6" (Packing Label)</option>
                        <option value="Letter">US Letter (8.5" × 11")</option>
                        <option value="Legal">US Legal (8.5" × 14")</option>
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

    // Populate device list with safe DOM manipulation
    const deviceListContainer = modalBody.querySelector('#deviceListContainer');
    deviceListContainer.appendChild(renderDeviceList(selected, exportData));

    // Ensure Start Export button is visible (in case it was hidden from previous export)
    const startExportBtn = modal.querySelector('.btn-start-export');
    if (startExportBtn) {
        startExportBtn.style.display = 'inline-block';
    }
}

/**
 * Renders device list cards
 * Uses textContent for user data to prevent XSS (consistent with Razor)
 */
function renderDeviceList(devices, exportDataList) {
    const fragment = document.createDocumentFragment();

    devices.forEach((device, index) => {
        const exportData = exportDataList[index];
        const template = exportData?.matchedTemplate;
        const matchReason = template?.matchReason || 'unknown';
        const confidence = template?.confidence ? Math.round(template.confidence * 100) : 0;

        // Create card structure (static HTML, safe)
        const card = document.createElement('div');
        card.className = 'device-card';
        card.style.cssText = 'padding: 12px; margin-bottom: 10px; background: #f9f9f9; border: 1px solid #ddd; border-radius: 4px;';

        card.innerHTML = `
            <div style="display: flex; justify-content: space-between; align-items: center;">
                <div>
                    <strong data-field="device-name"></strong>
                    <div style="font-size: 0.85em; color: #666; margin-top: 4px;">
                        <code data-field="device-serial"></code>
                    </div>
                </div>
                <div style="text-align: right; font-size: 0.85em;">
                    <div style="color: #666;">Template: <span data-field="template-name"></span></div>
                </div>
            </div>
        `;

        // Populate user data with textContent (safe, like Razor's @Model.Property)
        card.querySelector('[data-field="device-name"]').textContent = device.name;
        card.querySelector('[data-field="device-serial"]').textContent = device.serial;
        card.querySelector('[data-field="template-name"]').textContent = template?.name || 'Unknown';

        fragment.appendChild(card);
    });

    return fragment;
}

/**
 * HTML-escapes a string to prevent XSS attacks
 */
function escapeHtml(text) {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
}

/**
 * Validates that stickers fit on selected page size
 * Returns error object { message, deviceName } if validation fails, null if valid
 */
function validateStickersForPageSize(exportDataList, pageSizeName) {
    if (!exportDataList || !exportDataList.length) {
        return null;
    }

    // Page size definitions (in mm) - match server-side
    const pageSizes = {
        'A4': { width: 210, height: 297, name: 'A4' },
        'A5': { width: 148, height: 210, name: 'A5' },
        'A6': { width: 105, height: 148, name: 'A6' },
        '4x6': { width: 101.6, height: 152.4, name: '4"×6"' },
        'Letter': { width: 215.9, height: 279.4, name: 'US Letter' },
        'Legal': { width: 215.9, height: 355.6, name: 'US Legal' }
    };

    const pageSize = pageSizes[pageSizeName];
    if (!pageSize) {
        return null; // Unknown page size, let server validate
    }

    // Calculate usable area (no horizontal margins, 2mm vertical margins, +2mm tolerance)
    const verticalMarginMm = 2;
    const horizontalMarginMm = 0;
    const tolerance = 2;
    const usableWidth = pageSize.width - (2 * horizontalMarginMm);
    const usableHeight = pageSize.height - (2 * verticalMarginMm);

    // Check each template (match server-side dual-orientation validation)
    for (const exportData of exportDataList) {
        const template = exportData.matchedTemplate;
        if (!template) continue;

        const stickerWidth = template.pageWidth;
        const stickerHeight = template.pageHeight;

        // Check if sticker fits in landscape orientation
        const fitsLandscape = stickerWidth <= usableWidth + tolerance &&
                             stickerHeight <= usableHeight + tolerance;

        // Check if sticker fits in portrait orientation (rotated 90°)
        const fitsPortrait = stickerHeight <= usableWidth + tolerance &&
                            stickerWidth <= usableHeight + tolerance;

        // Sticker must fit in at least one orientation
        if (!fitsLandscape && !fitsPortrait) {
            const deviceName = exportData.device?.name || 'Unknown device';
            // Return object with separate fields to avoid HTML injection
            return {
                stickerWidth: stickerWidth.toFixed(1),
                stickerHeight: stickerHeight.toFixed(1),
                pageName: pageSize.name,
                usableWidth: usableWidth.toFixed(1),
                usableHeight: usableHeight.toFixed(1),
                deviceName: deviceName  // Will be escaped when displayed
            };
        }
    }

    return null; // All stickers fit
}

/**
 * Exports devices as a multi-page PDF with grid layout (server-side rendering)
 */
async function exportBulkAsPdf(selected, exportDataList, dpi, background) {
    const layout = document.querySelector('input[name="pdf-layout"]:checked')?.value || 'auto-fit';
    const pageSize = document.getElementById('pdfPageSize')?.value || 'A4';

    // Validate stickers fit on selected page size
    const validationError = validateStickersForPageSize(exportDataList, pageSize);
    if (validationError) {
        // Show error in modal instead of notification
        const modal = bulkExportState.bulkExportModal;
        const progressSection = modal.querySelector('#exportProgressSection');
        progressSection.style.display = 'block';

        // Build error message with HTML-escaped device name
        const escapedDeviceName = escapeHtml(validationError.deviceName);
        progressSection.innerHTML = `
            <div style="padding: 20px; text-align: center;">
                <h3 style="color: #f44336; margin-bottom: 15px;">⚠ Page Size Error</h3>
                <p style="margin-bottom: 20px; line-height: 1.6;">
                    Sticker size <strong>${validationError.stickerWidth}mm × ${validationError.stickerHeight}mm</strong> is too large for <strong>${validationError.pageName}</strong> page.<br><br>
                    Usable area: <strong>${validationError.usableWidth}mm × ${validationError.usableHeight}mm</strong> (no side margins, 2mm top/bottom margins).<br><br>
                    Device: <em>${escapedDeviceName}</em><br><br>
                    <em>Note: Both landscape and portrait (90° rotated) orientations were checked.</em><br><br>
                    <strong>Suggestion:</strong> Choose a larger page size (e.g., Letter or A4), or use a smaller sticker template.
                </p>
                <button onclick="closeBulkExportModal()" class="btn-primary" style="padding: 10px 20px;">
                    Close and Change Page Size
                </button>
            </div>
        `;
        return;
    }

    const images = [];
    const failedDevices = [];
    bulkExportState.exportCancelled = false;

    // Render each device to PNG base64
    for (let i = 0; i < selected.length; i++) {
        if (bulkExportState.exportCancelled) {
            break;
        }

        const device = selected[i];
        const exportData = exportDataList[i];

        updateProgress(i + 1, selected.length, device.name);

        try {
            // Create device data map
            const deviceDataMap = createDeviceDataMap(exportData);

            // Render device to blob (PNG only for PDF)
            const blob = await renderDeviceToBlob(
                exportData.matchedTemplate,
                deviceDataMap,
                'png',
                { dpi, background }
            );

            // Convert blob to base64
            const base64 = await blobToBase64(blob);

            // Collect image data
            images.push({
                imageBase64: base64.split(',')[1], // Remove "data:image/png;base64," prefix
                widthMm: exportData.matchedTemplate.pageWidth,
                heightMm: exportData.matchedTemplate.pageHeight,
                deviceName: device.name,
                deviceSerial: device.serial
            });

            // Track usage (fire-and-forget)
            trackUsage(exportData.matchedTemplate.id, exportData.matchedTemplate.templateJson);

        } catch (error) {
            console.error(`[PDF Export] Failed to render device ${device.name}:`, error);
            failedDevices.push({ device: device.name, error: error.message });
        }

        // Add small delay for progress visibility
        await new Promise(resolve => setTimeout(resolve, 50));
    }

    // Send to server for PDF generation
    if (!bulkExportState.exportCancelled && images.length > 0) {
        try {
            updateProgress(selected.length, selected.length, 'Generating PDF on server...');

            const response = await fetch('/api/export/pdf/bulk', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json'
                },
                body: JSON.stringify({
                    images: images,
                    layout: layout,
                    pageSize: pageSize
                })
            });

            if (!response.ok) {
                const error = await response.json();
                throw new Error(error.error || `Server error: ${response.status}`);
            }

            // Download PDF
            const pdfBlob = await response.blob();
            const filename = response.headers.get('Content-Disposition')?.match(/filename="(.+)"/)?.[1] || `devices-${images.length}-${Date.now()}.pdf`;
            downloadBlob(pdfBlob, filename);

            showCompletionSummary(images.length, failedDevices);

        } catch (error) {
            console.error('[PDF Export] Error generating PDF:', error);
            showNotification('Error generating PDF: ' + error.message, 'error');
        }
    }

    closeBulkExportModal();
}

/**
 * Converts a Blob to base64 string
 */
function blobToBase64(blob) {
    return new Promise((resolve, reject) => {
        const reader = new FileReader();
        reader.onloadend = () => resolve(reader.result);
        reader.onerror = reject;
        reader.readAsDataURL(blob);
    });
}

/**
 * Starts the bulk export process
 */
async function startBulkExport() {
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

    // Branch based on format
    if (format === 'pdf') {
        // PDF export - send to server for PDF generation
        await exportBulkAsPdf(selected, exportDataList, dpi, background);
        return;
    }

    // ZIP export (PNG or SVG)
    const zip = new JSZip();
    const exportedFiles = [];
    const failedDevices = [];
    bulkExportState.exportCancelled = false;

    // Export each device
    for (let i = 0; i < selected.length; i++) {
        if (bulkExportState.exportCancelled) {
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

            // Track usage (fire-and-forget)
            trackUsage(exportData.matchedTemplate.id, exportData.matchedTemplate.templateJson);

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

    // Render using export-preview.js functions (async, returns Promise)
    const fabricCanvas = await createAndRenderPreviewCanvas(
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
 * Toggles format-specific options visibility based on selected format
 */
function toggleBulkFormatOptions() {
    const format = document.querySelector('input[name="bulk-format"]:checked')?.value;
    const pngOptions = document.getElementById('pngOptionsBulk');
    const pdfOptions = document.getElementById('pdfOptionsBulk');

    if (format === 'pdf') {
        // Show PDF options, hide PNG options
        if (pngOptions) pngOptions.style.display = 'none';
        if (pdfOptions) pdfOptions.style.display = 'block';
    } else {
        // Show PNG options (for both zip-png and zip-svg), hide PDF options
        if (pngOptions) pngOptions.style.display = (format === 'zip-png') ? 'block' : 'none';
        if (pdfOptions) pdfOptions.style.display = 'none';
    }
}

/**
 * Cancels the bulk export
 */
function cancelBulkExport() {
    bulkExportState.exportCancelled = true;
}

/**
 * Closes the bulk export modal
 */
function closeBulkExportModal() {
    if (bulkExportState.bulkExportModal) {
        bulkExportState.bulkExportModal.style.display = 'none';
    }

    bulkExportState.selectedDevices = [];
    bulkExportState.currentExportData = [];
    bulkExportState.exportCancelled = false;
}
