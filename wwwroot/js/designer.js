/**
 * QR Sticker Designer - Main Application
 */

// Global variables
let canvas;
let currentTemplate;
let isEditMode = false;
let isSystemTemplate = false;
let gridSize = 5; // mm
let currentZoom = 1;
let stickerBoundary = null; // Reference to the boundary rectangle
let boundaryLeft = 0; // Boundary position
let boundaryTop = 0;
let uploadedImages = []; // Uploaded custom images for this connection

/**
 * Initialize the designer
 */
function initDesigner(templateData, editMode, systemTemplate, images) {
    currentTemplate = templateData;
    isEditMode = editMode;
    isSystemTemplate = systemTemplate;
    uploadedImages = images || [];
    console.log(templateData);
    console.log('[Designer] Loaded', uploadedImages.length, 'uploaded images');
    // Initialize Fabric.js canvas
    initCanvas(templateData.pageWidth, templateData.pageHeight);

    // Load existing template design if in edit mode
    if (editMode && templateData.templateJson) {
        loadTemplateDesign(templateData.templateJson);
    }

    // Initialize toolbar controls
    initToolbar();

    // Initialize element palette
    initElementPalette();

    // Initialize property inspector
    initPropertyInspector();

    // Initialize canvas event listeners
    initCanvasEvents();

    // Initialize export modal
    initExportModal();

    // Auto-save to localStorage
    setInterval(autoSaveToLocalStorage, 30000); // Every 30 seconds

    updateStatus('Designer loaded successfully');
}

/**
 * Initialize Fabric.js canvas
 */
function initCanvas(pageWidthMm, pageHeightMm) {
    // Calculate sticker size in pixels
    const stickerWidth = mmToPx(pageWidthMm);
    const stickerHeight = mmToPx(pageHeightMm);

    console.log('initCanvas - Sticker dimensions:', pageWidthMm, 'mm x', pageHeightMm, 'mm');
    console.log('initCanvas - Sticker in pixels:', stickerWidth, 'px x', stickerHeight, 'px');

    // Make canvas larger than sticker (3x or minimum 800x600)
    const canvasWidth = Math.max(stickerWidth * 3, 800);
    const canvasHeight = Math.max(stickerHeight * 3, 600);

    console.log('initCanvas - Canvas size:', canvasWidth, 'px x', canvasHeight, 'px');

    canvas = new fabric.Canvas('designCanvas', {
        width: canvasWidth,
        height: canvasHeight,
        backgroundColor: 'transparent', // Transparent so wrapper's grid shows through
        selection: true,
        preserveObjectStacking: true
    });

    // Verify canvas element dimensions
    const canvasElement = document.getElementById('designCanvas');
    console.log('Canvas element dimensions:', {
        canvasWidth: canvas.getWidth(),
        canvasHeight: canvas.getHeight(),
        domWidth: canvasElement.width,
        domHeight: canvasElement.height,
        styleWidth: canvasElement.style.width,
        styleHeight: canvasElement.style.height,
        offsetWidth: canvasElement.offsetWidth,
        offsetHeight: canvasElement.offsetHeight
    });

    // Enable object controls
    canvas.selectionColor = 'rgba(25, 118, 210, 0.1)';
    canvas.selectionBorderColor = '#1976d2';
    canvas.selectionLineWidth = 2;

    // Calculate boundary position (centered)
    boundaryLeft = (canvasWidth - stickerWidth) / 2;
    boundaryTop = (canvasHeight - stickerHeight) / 2;

    // Create sticker boundary rectangle with dashed border
    stickerBoundary = new fabric.Rect({
        left: boundaryLeft,
        top: boundaryTop,
        width: stickerWidth,
        height: stickerHeight,
        fill: 'white', // White background for sticker area
        stroke: '#ff0000', // Bright red dashed border
        strokeWidth: 4, // Thicker border for visibility
        strokeDashArray: [15, 10],
        selectable: false,
        evented: false,
        hoverCursor: 'default',
        excludeFromExport: true,
        name: 'stickerBoundary'
    });

    console.log('Boundary position:', boundaryLeft, ',', boundaryTop);
    console.log('Boundary size:', stickerWidth, 'x', stickerHeight);

    // Add boundary to canvas
    canvas.add(stickerBoundary);
    canvas.sendToBack(stickerBoundary); // Ensure it's behind all objects

    // Verify boundary was added
    console.log('Boundary added to canvas. Total objects:', canvas.getObjects().length);
    console.log('Boundary object:', {
        left: stickerBoundary.left,
        top: stickerBoundary.top,
        width: stickerBoundary.width,
        height: stickerBoundary.height,
        stroke: stickerBoundary.stroke,
        strokeWidth: stickerBoundary.strokeWidth,
        visible: stickerBoundary.visible
    });

    // Sync input fields with actual sticker dimensions
    document.getElementById('pageWidth').value = pageWidthMm;
    document.getElementById('pageHeight').value = pageHeightMm;

    // Initialize grid background
    updateGridBackground();
}

/**
 * Update grid background based on current zoom level
 */
function updateGridBackground() {
    const canvasWrapper = document.getElementById('canvasWrapper');
    const showGrid = document.getElementById('chkShowGrid').checked;

    if (showGrid) {
        // Calculate grid size in pixels accounting for zoom
        let gridSizePx = mmToPx(gridSize) * currentZoom;

        // Round to nearest integer at 100% zoom to avoid tiling artifacts
        if (currentZoom === 1) {
            gridSizePx = Math.round(gridSizePx);
        }

        // Create grid using CSS linear gradients (more reliable than SVG)
        canvasWrapper.style.backgroundImage = `
            linear-gradient(to right, #cccccc 1px, transparent 1px),
            linear-gradient(to bottom, #cccccc 1px, transparent 1px)
        `;
        canvasWrapper.style.backgroundSize = `${gridSizePx}px ${gridSizePx}px`;
    } else {
        // Hide grid
        canvasWrapper.style.backgroundImage = 'none';
    }
}

/**
 * Update sticker boundary size and position
 */
function updateStickerBoundary() {
    if (!stickerBoundary) return;

    const stickerWidth = mmToPx(currentTemplate.pageWidth);
    const stickerHeight = mmToPx(currentTemplate.pageHeight);

    // Recalculate boundary position (keep it centered)
    boundaryLeft = (canvas.getWidth() - stickerWidth) / 2;
    boundaryTop = (canvas.getHeight() - stickerHeight) / 2;

    // Update boundary rectangle
    stickerBoundary.set({
        left: boundaryLeft,
        top: boundaryTop,
        width: stickerWidth,
        height: stickerHeight
    });

    canvas.renderAll();
}

/**
 * Initialize toolbar controls
 */
function initToolbar() {
    // Zoom controls
    document.getElementById('btnZoomIn').addEventListener('click', () => {
        currentZoom = Math.min(currentZoom + 0.1, 3);
        canvas.setZoom(currentZoom);
        updateZoomDisplay();
        updateGridBackground();
    });

    document.getElementById('btnZoomOut').addEventListener('click', () => {
        currentZoom = Math.max(currentZoom - 0.1, 0.1);
        canvas.setZoom(currentZoom);
        updateZoomDisplay();
        updateGridBackground();
    });

    document.getElementById('btnZoomReset').addEventListener('click', () => {
        currentZoom = 1;
        canvas.setZoom(1);
        updateZoomDisplay();
        updateGridBackground();
    });

    // Grid toggle
    document.getElementById('chkShowGrid').addEventListener('change', function() {
        updateGridBackground();
    });

    // Snap to grid toggle
    document.getElementById('chkSnapToGrid').addEventListener('change', function() {
        updateStatus(this.checked ? 'Snap to grid enabled' : 'Snap to grid disabled');
    });

    // Page size changes
    document.getElementById('pageWidth').addEventListener('change', function() {
        const newWidth = parseFloat(this.value);
        if (newWidth >= 10 && newWidth <= 500) {
            currentTemplate.pageWidth = newWidth;
            updateStickerBoundary();
            updateStatus(`Page width set to ${newWidth}mm`);
        }
    });

    document.getElementById('pageHeight').addEventListener('change', function() {
        const newHeight = parseFloat(this.value);
        if (newHeight >= 10 && newHeight <= 500) {
            currentTemplate.pageHeight = newHeight;
            updateStickerBoundary();
            updateStatus(`Page height set to ${newHeight}mm`);
        }
    });

    // Save button
    document.getElementById('btnSave').addEventListener('click', saveTemplate);

    // Layer ordering buttons
    document.getElementById('btnBringToFront').addEventListener('click', () => {
        const activeObject = canvas.getActiveObject();
        if (activeObject) {
            canvas.bringToFront(activeObject);
            canvas.renderAll();
            updateStatus('Brought object to front');
        }
    });

    document.getElementById('btnSendToBack').addEventListener('click', () => {
        const activeObject = canvas.getActiveObject();
        if (activeObject) {
            canvas.sendToBack(activeObject);
            canvas.renderAll();
            updateStatus('Sent object to back');
        }
    });

    document.getElementById('btnBringForward').addEventListener('click', () => {
        const activeObject = canvas.getActiveObject();
        if (activeObject) {
            canvas.bringForward(activeObject);
            canvas.renderAll();
            updateStatus('Brought object forward');
        }
    });

    document.getElementById('btnSendBackward').addEventListener('click', () => {
        const activeObject = canvas.getActiveObject();
        if (activeObject) {
            canvas.sendBackwards(activeObject);
            canvas.renderAll();
            updateStatus('Sent object backward');
        }
    });
}

/**
 * Initialize element palette (click to add to canvas, drag to position)
 */
function initElementPalette() {
    const paletteItems = document.querySelectorAll('.palette-item');
    const canvasContainer = document.querySelector('.designer-canvas-container');
    const canvasWrapper = document.querySelector('.canvas-wrapper');

    paletteItems.forEach(item => {
        // Click to add (centered)
        item.addEventListener('click', function() {
            const elementType = this.getAttribute('data-element-type');
            addElementToCanvas(elementType);
        });

        // Drag start
        item.addEventListener('dragstart', function(e) {
            const elementType = this.getAttribute('data-element-type');
            e.dataTransfer.setData('elementType', elementType);
            e.dataTransfer.effectAllowed = 'copy';
        });
    });

    // Allow drop on canvas container
    canvasContainer.addEventListener('dragover', function(e) {
        e.preventDefault();
        e.dataTransfer.dropEffect = 'copy';
    });

    // Handle drop
    canvasContainer.addEventListener('drop', function(e) {
        e.preventDefault();
        const elementType = e.dataTransfer.getData('elementType');

        if (elementType) {
            // Get drop position relative to canvas
            const canvasRect = document.getElementById('designCanvas').getBoundingClientRect();
            const containerRect = canvasContainer.getBoundingClientRect();

            // Calculate position accounting for scroll and zoom
            const x = (e.clientX - canvasRect.left) / currentZoom;
            const y = (e.clientY - canvasRect.top) / currentZoom;

            addElementToCanvasAtPosition(elementType, x, y);
        }
    });
}

/**
 * Add element to sticker boundary center
 */
function addElementToCanvas(elementType) {
    // Calculate sticker boundary center
    const stickerWidth = mmToPx(currentTemplate.pageWidth);
    const stickerHeight = mmToPx(currentTemplate.pageHeight);
    const centerX = boundaryLeft + stickerWidth / 2;
    const centerY = boundaryTop + stickerHeight / 2;

    let element;

    switch (elementType) {
        case 'qrcode':
            element = createQRCode({
                left: centerX - 50,
                top: centerY - 50,
                width: 100,
                height: 100,
                dataSource: 'device.Serial'
            });
            break;

        case 'text':
            element = createBoundText({
                left: centerX - 50,
                top: centerY - 10,
                text: '{{device.Name}}',
                dataSource: 'device.Name',
                fontSize: 16,
                fontFamily: 'Arial'
            });
            break;

        case 'image':
            // Open custom image selector modal instead of adding directly
            openCustomImageSelector({ x: centerX, y: centerY });
            return; // Don't add element yet

        case 'rectangle':
            element = createRectangle({
                left: centerX - 50,
                top: centerY - 25,
                width: 100,
                height: 50,
                fill: 'transparent',
                stroke: '#333',
                strokeWidth: 2
            });
            break;

        case 'line':
            element = createLine({
                x1: centerX - 50,
                y1: centerY,
                x2: centerX + 50,
                y2: centerY,
                stroke: '#333',
                strokeWidth: 2
            });
            break;
    }

    if (element) {
        canvas.add(element);
        canvas.setActiveObject(element);
        canvas.renderAll();
        updateStatus(`${elementType} added to canvas`);
    }
}

/**
 * Add element to canvas at specific position (for drag & drop)
 */
function addElementToCanvasAtPosition(elementType, x, y) {
    let element;

    switch (elementType) {
        case 'qrcode':
            element = createQRCode({
                left: x - 50,
                top: y - 50,
                width: 100,
                height: 100,
                dataSource: 'device.Serial'
            });
            break;

        case 'text':
            element = createBoundText({
                left: x,
                top: y,
                text: '{{device.Name}}',
                dataSource: 'device.Name',
                fontSize: 16,
                fontFamily: 'Arial'
            });
            break;

        case 'image':
            // Open custom image selector modal instead of adding directly
            openCustomImageSelector({ x: x, y: y });
            return; // Don't add element yet

        case 'rectangle':
            element = createRectangle({
                left: x - 50,
                top: y - 25,
                width: 100,
                height: 50,
                fill: 'transparent',
                stroke: '#333',
                strokeWidth: 2
            });
            break;

        case 'line':
            element = createLine({
                x1: x - 50,
                y1: y,
                x2: x + 50,
                y2: y,
                stroke: '#333',
                strokeWidth: 2
            });
            break;
    }

    if (element) {
        canvas.add(element);
        canvas.setActiveObject(element);
        canvas.renderAll();
        updateStatus(`${elementType} added at position`);
    }
}

/**
 * Initialize property inspector
 */
function initPropertyInspector() {
    // QR Code properties
    document.getElementById('qrDataSource').addEventListener('change', function() {
        const activeObject = canvas.getActiveObject();
        if (activeObject && activeObject.type === 'qrcode') {
            activeObject.set('dataSource', this.value);
            canvas.renderAll();
        }
    });

    document.getElementById('qrEccLevel').addEventListener('change', function() {
        const activeObject = canvas.getActiveObject();
        if (activeObject && activeObject.type === 'qrcode') {
            activeObject.set('eccLevel', this.value);
            canvas.renderAll();
        }
    });

    document.getElementById('qrSize').addEventListener('input', function() {
        const activeObject = canvas.getActiveObject();
        if (activeObject && activeObject.type === 'qrcode') {
            const size = parseFloat(this.value);
            activeObject.set({ width: size, height: size });
            canvas.renderAll();
        }
    });

    // Text properties
    document.getElementById('textContent').addEventListener('input', function() {
        const activeObject = canvas.getActiveObject();
        if (activeObject && (activeObject.type === 'i-text' || activeObject.type === 'text')) {
            activeObject.set('text', this.value);
            canvas.renderAll();
        }
    });

    document.getElementById('textDataSource').addEventListener('change', function() {
        const activeObject = canvas.getActiveObject();
        if (activeObject && (activeObject.type === 'i-text' || activeObject.type === 'text')) {
            updateDataBinding(activeObject, this.value);
            canvas.renderAll();
        }
    });

    document.getElementById('textFontFamily').addEventListener('change', function() {
        const activeObject = canvas.getActiveObject();
        if (activeObject && (activeObject.type === 'i-text' || activeObject.type === 'text')) {
            activeObject.set('fontFamily', this.value);
            canvas.renderAll();
        }
    });

    document.getElementById('textFontSize').addEventListener('input', function() {
        const activeObject = canvas.getActiveObject();
        if (activeObject && (activeObject.type === 'i-text' || activeObject.type === 'text')) {
            activeObject.set('fontSize', parseInt(this.value));
            canvas.renderAll();
        }
    });

    document.getElementById('textFontWeight').addEventListener('change', function() {
        const activeObject = canvas.getActiveObject();
        if (activeObject && (activeObject.type === 'i-text' || activeObject.type === 'text')) {
            activeObject.set('fontWeight', this.value);
            canvas.renderAll();
        }
    });

    document.getElementById('textColor').addEventListener('input', function() {
        const activeObject = canvas.getActiveObject();
        if (activeObject && (activeObject.type === 'i-text' || activeObject.type === 'text')) {
            activeObject.set('fill', this.value);
            canvas.renderAll();
        }
    });

    // Image properties
    document.getElementById('imageDataSource').addEventListener('change', function() {
        const activeObject = canvas.getActiveObject();
        if (activeObject && (activeObject.type === 'image' || activeObject.get('type') === 'image')) {
            activeObject.set('dataSource', this.value);
            canvas.renderAll();
        }
    });

    document.getElementById('imageUrl').addEventListener('input', function() {
        const activeObject = canvas.getActiveObject();
        if (activeObject && (activeObject.type === 'image' || activeObject.get('type') === 'image')) {
            activeObject.set('src', this.value);
            canvas.renderAll();
        }
    });

    // Rectangle properties
    document.getElementById('rectFill').addEventListener('input', function() {
        const activeObject = canvas.getActiveObject();
        if (activeObject && activeObject.type === 'rect') {
            activeObject.set('fill', this.value);
            canvas.renderAll();
        }
    });

    document.getElementById('rectStroke').addEventListener('input', function() {
        const activeObject = canvas.getActiveObject();
        if (activeObject && activeObject.type === 'rect') {
            activeObject.set('stroke', this.value);
            canvas.renderAll();
        }
    });

    document.getElementById('rectStrokeWidth').addEventListener('input', function() {
        const activeObject = canvas.getActiveObject();
        if (activeObject && activeObject.type === 'rect') {
            activeObject.set('strokeWidth', parseInt(this.value));
            canvas.renderAll();
        }
    });

    // Line properties
    document.getElementById('lineStroke').addEventListener('input', function() {
        const activeObject = canvas.getActiveObject();
        if (activeObject && activeObject.type === 'line') {
            activeObject.set('stroke', this.value);
            canvas.renderAll();
        }
    });

    document.getElementById('lineStrokeWidth').addEventListener('input', function() {
        const activeObject = canvas.getActiveObject();
        if (activeObject && activeObject.type === 'line') {
            activeObject.set('strokeWidth', parseInt(this.value));
            canvas.renderAll();
        }
    });

    // Common properties (position/size)
    document.getElementById('posX').addEventListener('input', function() {
        const activeObject = canvas.getActiveObject();
        if (activeObject) {
            // Add boundary offset to convert from sticker-relative to canvas-absolute position
            activeObject.set('left', mmToPx(parseFloat(this.value)) + boundaryLeft);
            canvas.renderAll();
        }
    });

    document.getElementById('posY').addEventListener('input', function() {
        const activeObject = canvas.getActiveObject();
        if (activeObject) {
            // Add boundary offset to convert from sticker-relative to canvas-absolute position
            activeObject.set('top', mmToPx(parseFloat(this.value)) + boundaryTop);
            canvas.renderAll();
        }
    });

    document.getElementById('sizeWidth').addEventListener('input', function() {
        const activeObject = canvas.getActiveObject();
        if (activeObject) {
            const currentWidth = activeObject.getScaledWidth();
            const newWidth = mmToPx(parseFloat(this.value));
            activeObject.scaleX = (activeObject.scaleX * newWidth) / currentWidth;
            canvas.renderAll();
        }
    });

    document.getElementById('sizeHeight').addEventListener('input', function() {
        const activeObject = canvas.getActiveObject();
        if (activeObject) {
            const currentHeight = activeObject.getScaledHeight();
            const newHeight = mmToPx(parseFloat(this.value));
            activeObject.scaleY = (activeObject.scaleY * newHeight) / currentHeight;
            canvas.renderAll();
        }
    });

    document.getElementById('rotation').addEventListener('input', function() {
        const activeObject = canvas.getActiveObject();
        if (activeObject) {
            activeObject.set('angle', parseFloat(this.value));
            canvas.renderAll();
        }
    });
}

/**
 * Initialize canvas event listeners
 */
function initCanvasEvents() {
    // Selection changed
    canvas.on('selection:created', updatePropertyInspector);
    canvas.on('selection:updated', updatePropertyInspector);
    canvas.on('selection:cleared', clearPropertyInspector);

    // Object modified (for snap to grid)
    canvas.on('object:moving', function(e) {
        if (document.getElementById('chkSnapToGrid').checked) {
            const obj = e.target;
            const gridSizePx = mmToPx(gridSize);
            obj.set({
                left: Math.round(obj.left / gridSizePx) * gridSizePx,
                top: Math.round(obj.top / gridSizePx) * gridSizePx
            });
        }
    });

    // Mouse move (update cursor position)
    canvas.on('mouse:move', function(e) {
        const pointer = canvas.getPointer(e.e);
        const xMm = pxToMm(pointer.x).toFixed(1);
        const yMm = pxToMm(pointer.y).toFixed(1);
        document.getElementById('cursorPos').textContent = `X: ${xMm}mm, Y: ${yMm}mm`;
    });

    // Keyboard shortcuts
    document.addEventListener('keydown', function(e) {
        // Delete selected object (Delete key)
        if (e.key === 'Delete') {
            const activeObject = canvas.getActiveObject();
            if (activeObject) {
                canvas.remove(activeObject);
                updateStatus('Object deleted');
            }
        }

        // Save (Ctrl+S)
        if (e.ctrlKey && e.key === 's') {
            e.preventDefault();
            saveTemplate();
        }
    });
}

/**
 * Update property inspector when selection changes
 */
function updatePropertyInspector() {
    const activeObject = canvas.getActiveObject();
    if (!activeObject) {
        clearPropertyInspector();
        return;
    }

    // Hide all property panels
    document.querySelectorAll('.property-panel').forEach(panel => {
        panel.style.display = 'none';
    });

    // Show common properties
    document.getElementById('commonProperties').style.display = 'flex';

    // Update common properties (relative to sticker boundary)
    document.getElementById('posX').value = pxToMm(activeObject.left - boundaryLeft).toFixed(1);
    document.getElementById('posY').value = pxToMm(activeObject.top - boundaryTop).toFixed(1);
    document.getElementById('sizeWidth').value = pxToMm(activeObject.getScaledWidth()).toFixed(1);
    document.getElementById('sizeHeight').value = pxToMm(activeObject.getScaledHeight()).toFixed(1);
    document.getElementById('rotation').value = activeObject.angle || 0;

    // Show type-specific properties
    const objectType = activeObject.type === 'group' ? (activeObject.get('type') || 'group') : activeObject.type;

    if (objectType === 'qrcode') {
        document.getElementById('qrcodeProperties').style.display = 'flex';
        document.getElementById('qrDataSource').value = activeObject.get('dataSource') || '';
        document.getElementById('qrEccLevel').value = activeObject.get('eccLevel') || 'Q';
        document.getElementById('qrSize').value = pxToMm(activeObject.width).toFixed(1);
    } else if (objectType === 'i-text' || objectType === 'text') {
        document.getElementById('textProperties').style.display = 'flex';
        document.getElementById('textContent').value = activeObject.text || '';
        document.getElementById('textDataSource').value = activeObject.get('dataSource') || '';
        document.getElementById('textFontFamily').value = activeObject.fontFamily || 'Arial';
        document.getElementById('textFontSize').value = activeObject.fontSize || 16;
        document.getElementById('textFontWeight').value = activeObject.fontWeight || 'normal';
        document.getElementById('textColor').value = activeObject.fill || '#000000';
    } else if (objectType === 'image') {
        document.getElementById('imageProperties').style.display = 'flex';

        // Check if this is a custom image
        const customImageId = activeObject.get('customImageId');
        const customImageName = activeObject.get('customImageName');

        if (customImageId) {
            // Custom image - show custom info
            document.getElementById('imageDataSource').value = activeObject.get('dataSource') || '';
            document.getElementById('imageUrl').value = '(Custom Image)';
            document.getElementById('imageUrl').disabled = true;

            // Show custom image info
            const customImageInfo = document.getElementById('customImageInfo');
            if (customImageInfo) {
                customImageInfo.style.display = 'block';
                customImageInfo.innerHTML = `
                    <div style="padding: 10px; background: #f0f0f0; border-radius: 4px; margin: 10px 0;">
                        <p style="margin: 5px 0;"><strong>Custom Image:</strong> ${customImageName}</p>
                        <p style="margin: 5px 0; font-size: 11px;"><code>${activeObject.get('dataSource')}</code></p>
                        <button type="button" class="btn-primary" style="margin-top: 10px; padding: 5px 10px; font-size: 12px;" onclick="replaceCustomImage()">Replace Image</button>
                    </div>
                `;
            }
        } else {
            // Regular image
            document.getElementById('imageDataSource').value = activeObject.get('dataSource') || '';
            document.getElementById('imageUrl').value = activeObject.get('src') || '';
            document.getElementById('imageUrl').disabled = false;

            // Hide custom image info
            const customImageInfo = document.getElementById('customImageInfo');
            if (customImageInfo) {
                customImageInfo.style.display = 'none';
            }
        }
    } else if (objectType === 'rect') {
        document.getElementById('rectangleProperties').style.display = 'flex';
        document.getElementById('rectFill').value = activeObject.fill || '#ffffff';
        document.getElementById('rectStroke').value = activeObject.stroke || '#000000';
        document.getElementById('rectStrokeWidth').value = activeObject.strokeWidth || 1;
    } else if (objectType === 'line') {
        document.getElementById('lineProperties').style.display = 'flex';
        document.getElementById('lineStroke').value = activeObject.stroke || '#000000';
        document.getElementById('lineStrokeWidth').value = activeObject.strokeWidth || 1;
    }

    // Update status bar
    document.getElementById('selectedInfo').textContent = `Selected: ${objectType}`;
}

/**
 * Clear property inspector
 */
function clearPropertyInspector() {
    document.querySelectorAll('.property-panel').forEach(panel => {
        panel.style.display = 'none';
    });
    document.getElementById('noSelection').style.display = 'block';
    document.getElementById('selectedInfo').textContent = 'No selection';
}

/**
 * Save template
 */
function saveTemplate() {
    if (isSystemTemplate && isEditMode) {
        alert('Cannot modify system templates. Please clone the template first.');
        return;
    }

    // Get template JSON from canvas
    const templateJson = canvasToTemplateJson(
        canvas,
        parseFloat(document.getElementById('pageWidth').value),
        parseFloat(document.getElementById('pageHeight').value)
    );

    // Update hidden form fields
    document.getElementById('templatePageWidth').value = document.getElementById('pageWidth').value;
    document.getElementById('templatePageHeight').value = document.getElementById('pageHeight').value;
    document.getElementById('templateJson').value = JSON.stringify(templateJson, null, 2);

    // Show save status tick with animation
    const saveStatus = document.getElementById('saveStatus');
    saveStatus.classList.remove('show');
    // Trigger reflow to restart animation
    void saveStatus.offsetWidth;
    saveStatus.classList.add('show');

    // Update status
    updateStatus('Saving template...');

    // Use fetch to save without page reload
    const form = document.getElementById('saveForm');
    const formData = new FormData(form);

    fetch(window.location.href, {
        method: 'POST',
        body: formData
    })
    .then(response => {
        if (response.ok) {
            updateStatus('Template saved successfully');
            // Update browser URL if this was a new template (ID will be in redirect)
            if (!isEditMode && response.redirected) {
                const redirectUrl = new URL(response.url);
                const templateId = redirectUrl.searchParams.get('id');
                if (templateId) {
                    // Update URL without reload
                    window.history.replaceState(null, '', `?id=${templateId}`);
                    isEditMode = true;
                    // Update form ID field for subsequent saves
                    document.querySelector('input[name="Template.Id"]').value = templateId;
                }
            }
        } else {
            updateStatus('Error saving template');
            console.error('Save failed:', response.status, response.statusText);
        }
    })
    .catch(error => {
        updateStatus('Error saving template');
        console.error('Save error:', error);
    });
}

/**
 * Load template design from JSON
 */
function loadTemplateDesign(templateJsonString) {
    try {
        const templateData = JSON.parse(templateJsonString);

        if (!templateData.objects || !Array.isArray(templateData.objects)) {
            console.warn('No objects found in template');
            return;
        }

        templateData.objects.forEach(obj => {
            let fabricObject;

            switch (obj.type) {
                case 'qrcode':
                    fabricObject = createQRCode({
                        left: mmToPx(obj.left) + boundaryLeft,
                        top: mmToPx(obj.top) + boundaryTop,
                        width: mmToPx(obj.width || 30),
                        height: mmToPx(obj.height || 30),
                        dataSource: obj.properties?.dataSource || 'device.Serial',
                        eccLevel: obj.properties?.eccLevel || 'Q'
                    });
                    break;

                case 'text':
                case 'i-text':
                    fabricObject = createBoundText({
                        left: mmToPx(obj.left) + boundaryLeft,
                        top: mmToPx(obj.top) + boundaryTop,
                        text: obj.text || '',
                        dataSource: obj.properties?.dataSource || '',
                        fontFamily: obj.fontFamily || 'Arial',
                        fontSize: obj.fontSize || 16,
                        fontWeight: obj.fontWeight || 'normal',
                        fill: obj.fill || '#000000'
                    });
                    break;

                case 'image':
                    // Check if this is a custom image
                    if (obj.properties?.customImageId) {
                        // Load custom image from uploaded images
                        const customImageData = uploadedImages.find(img => img.id === obj.properties.customImageId);
                        if (customImageData) {
                            // Create fabric.Image from data URI (async, so handle differently)
                            fabric.Image.fromURL(customImageData.dataUri, function(img) {
                                const left = mmToPx(obj.left) + boundaryLeft;
                                const top = mmToPx(obj.top) + boundaryTop;
                                const width = mmToPx(obj.width || 50);
                                const height = mmToPx(obj.height || 50);

                                img.set({
                                    left: left,
                                    top: top,
                                    scaleX: width / img.width,
                                    scaleY: height / img.height,
                                    angle: obj.angle || 0
                                });

                                // Restore custom properties
                                img.set('customImageId', obj.properties.customImageId);
                                img.set('customImageName', obj.properties.customImageName);
                                img.set('dataSource', obj.properties.dataSource);
                                img.set('type', 'image');

                                canvas.add(img);
                                canvas.renderAll();
                            }, { crossOrigin: 'anonymous' });
                            fabricObject = null; // Skip adding placeholder
                        } else {
                            // Image not found, use placeholder
                            fabricObject = createImagePlaceholder({
                                left: mmToPx(obj.left) + boundaryLeft,
                                top: mmToPx(obj.top) + boundaryTop,
                                width: mmToPx(obj.width || 50),
                                height: mmToPx(obj.height || 50),
                                dataSource: obj.properties?.dataSource || '',
                                src: obj.src || ''
                            });
                        }
                    } else {
                        // Regular image placeholder
                        fabricObject = createImagePlaceholder({
                            left: mmToPx(obj.left) + boundaryLeft,
                            top: mmToPx(obj.top) + boundaryTop,
                            width: mmToPx(obj.width || 50),
                            height: mmToPx(obj.height || 50),
                            dataSource: obj.properties?.dataSource || '',
                            src: obj.src || ''
                        });
                    }
                    break;

                case 'rect':
                    fabricObject = createRectangle({
                        left: mmToPx(obj.left) + boundaryLeft,
                        top: mmToPx(obj.top) + boundaryTop,
                        width: mmToPx(obj.width || 50),
                        height: mmToPx(obj.height || 50),
                        fill: obj.fill || 'transparent',
                        stroke: obj.stroke || '#000000',
                        strokeWidth: obj.strokeWidth || 1
                    });
                    break;

                case 'line':
                    const x1 = mmToPx(obj.left) + boundaryLeft;
                    const y1 = mmToPx(obj.top) + boundaryTop;
                    const x2 = x1 + mmToPx(obj.width || 50);
                    const y2 = y1;
                    fabricObject = createLine({
                        x1: x1,
                        y1: y1,
                        x2: x2,
                        y2: y2,
                        stroke: obj.stroke || '#000000',
                        strokeWidth: obj.strokeWidth || 1
                    });
                    break;
            }

            if (fabricObject) {
                // Apply rotation if present
                if (obj.angle) {
                    fabricObject.set('angle', obj.angle);
                }

                canvas.add(fabricObject);
            }
        });

        canvas.renderAll();
        updateStatus('Template loaded successfully');
    } catch (error) {
        console.error('Error loading template:', error);
        updateStatus('Error loading template');
    }
}

/**
 * Auto-save to localStorage
 */
function autoSaveToLocalStorage() {
    const templateJson = canvasToTemplateJson(
        canvas,
        parseFloat(document.getElementById('pageWidth').value),
        parseFloat(document.getElementById('pageHeight').value)
    );

    localStorage.setItem('qrstickers_autosave', JSON.stringify(templateJson));
    console.log('Auto-saved to localStorage');
}

/**
 * Update zoom level display
 */
function updateZoomDisplay() {
    const zoomPercent = Math.round(currentZoom * 100);
    document.getElementById('zoomLevel').textContent = `${zoomPercent}%`;
    updateStatus(`Zoom: ${zoomPercent}%`);
}

/**
 * Update status bar
 */
function updateStatus(message) {
    document.getElementById('statusText').textContent = message;
}

/**
 * Custom Image Selector Modal
 */

let customImageSelectorModal = null;
let customImageSelectorPosition = null;

/**
 * Open custom image selector modal
 */
function openCustomImageSelector(position) {
    customImageSelectorPosition = position;

    // Check if there are any uploaded images
    if (!uploadedImages || uploadedImages.length === 0) {
        alert('No custom images uploaded yet.\n\nPlease upload images from the Connections page → Manage Images');
        return;
    }

    // Create modal if doesn't exist
    if (!customImageSelectorModal) {
        createCustomImageSelectorModal();
    }

    // Populate modal with images
    renderCustomImageGrid();

    // Show modal
    customImageSelectorModal.style.display = 'flex';
    updateStatus('Select a custom image');
}

/**
 * Create custom image selector modal HTML
 */
function createCustomImageSelectorModal() {
    const modal = document.createElement('div');
    modal.className = 'modal';
    modal.id = 'customImageSelectorModal';
    modal.style.cssText = 'display: none; position: fixed; z-index: 1000; left: 0; top: 0; width: 100%; height: 100%; background: rgba(0,0,0,0.5); align-items: center; justify-content: center;';

    modal.innerHTML = `
        <div class="modal-content" style="max-width: 800px; max-height: 80vh; overflow-y: auto;">
            <div class="modal-header">
                <h3>Select Custom Image</h3>
                <button class="modal-close" onclick="closeCustomImageSelector()">&times;</button>
            </div>
            <div class="modal-body" style="flex-direction: column;">
                <div id="customImageGrid" class="custom-image-grid"></div>
            </div>
            <div class="modal-footer">
                <button onclick="closeCustomImageSelector()" class="btn-secondary">Cancel</button>
            </div>
        </div>
    `;

    document.body.appendChild(modal);
    customImageSelectorModal = modal;

    // Close on overlay click
    modal.addEventListener('click', (e) => {
        if (e.target === modal) {
            closeCustomImageSelector();
        }
    });
}

/**
 * Render grid of uploaded images
 */
function renderCustomImageGrid() {
    const grid = document.getElementById('customImageGrid');
    if (!grid) return;

    grid.innerHTML = uploadedImages.map(img => `
        <div class="custom-image-card" style="border: 1px solid #ddd; padding: 15px; margin: 10px; display: inline-block; width: 200px; text-align: center; cursor: pointer; border-radius: 4px;" onclick="selectCustomImage(${img.id})">
            <div style="width: 150px; height: 150px; margin: 0 auto; display: flex; align-items: center; justify-content: center; background: #f5f5f5; border: 1px solid #ccc;">
                <img src="${img.dataUri}" style="max-width: 100%; max-height: 100%; object-fit: contain;" alt="${img.name}" />
            </div>
            <h4 style="margin: 10px 0 5px 0; font-size: 14px; font-weight: bold;">${img.name}</h4>
            <code style="font-size: 11px; color: #666; display: block; margin: 5px 0;">{{customImage.Image_${img.id}}}</code>
            <p style="font-size: 11px; color: #999; margin: 5px 0;">${img.widthPx} × ${img.heightPx} px</p>
            <button class="btn-primary" style="margin-top: 10px; padding: 5px 15px; font-size: 12px;">Select</button>
        </div>
    `).join('');
}

/**
 * Select a custom image and add to canvas
 */
function selectCustomImage(imageId) {
    const imageData = uploadedImages.find(img => img.id === imageId);
    if (!imageData) {
        console.error('[Designer] Image not found:', imageId);
        return;
    }

    // Close modal
    closeCustomImageSelector();

    // Add to canvas at stored position
    addCustomImageToCanvas(imageData, customImageSelectorPosition);
}

/**
 * Add custom image to canvas with validation
 */
function addCustomImageToCanvas(imageData, position) {
    // Check 4-image limit
    const existingCustomImages = canvas.getObjects().filter(obj =>
        obj.get('customImageId') !== undefined
    );

    if (existingCustomImages.length >= 4) {
        alert('Maximum 4 custom images per template.\n\nPlease remove an existing custom image before adding another.');
        return;
    }

    // Create image object using fabric.Image.fromURL for real image
    fabric.Image.fromURL(imageData.dataUri, function(img) {
        // Scale image to reasonable size (max 100px)
        const maxSize = 100;
        const scale = Math.min(maxSize / img.width, maxSize / img.height);

        img.set({
            left: position.x - (img.width * scale) / 2,
            top: position.y - (img.height * scale) / 2,
            scaleX: scale,
            scaleY: scale
        });

        // Add custom properties for data binding
        img.set('customImageId', imageData.id);
        img.set('customImageName', imageData.name);
        img.set('dataSource', `customImage.Image_${imageData.id}`);
        img.set('type', 'image'); // Ensure type is 'image'

        // Add to canvas
        canvas.add(img);
        canvas.setActiveObject(img);
        canvas.renderAll();

        updateStatus(`Added custom image: ${imageData.name}`);
    }, { crossOrigin: 'anonymous' });
}

/**
 * Close custom image selector modal
 */
function closeCustomImageSelector() {
    if (customImageSelectorModal) {
        customImageSelectorModal.style.display = 'none';
    }
    updateStatus('Ready');
}

/**
 * Replace selected custom image with another one
 */
function replaceCustomImage() {
    const activeObject = canvas.getActiveObject();
    if (!activeObject || !activeObject.get('customImageId')) {
        return;
    }

    // Store current position and size
    const currentLeft = activeObject.left;
    const currentTop = activeObject.top;
    const currentScaleX = activeObject.scaleX;
    const currentScaleY = activeObject.scaleY;

    // Remove current image
    canvas.remove(activeObject);

    // Open selector at current position
    openCustomImageSelector({ x: currentLeft, y: currentTop });
}

/**
 * Export Modal Integration
 */

let exportModal = null;
let previewCanvas = null;
let currentExportFormat = 'png';
let currentExportOptions = {
    dpi: 96,
    background: 'white'
};

/**
 * Initialize export modal handlers
 */
function initExportModal() {
    // Open modal button
    document.getElementById('btnExport').addEventListener('click', openExportModal);

    // Close modal buttons
    document.getElementById('btnCloseModal').addEventListener('click', closeExportModal);
    document.getElementById('btnCancelExport').addEventListener('click', closeExportModal);

    // Modal overlay click to close
    document.getElementById('exportModalOverlay').addEventListener('click', closeExportModal);

    // Format selection change
    document.querySelectorAll('input[name="exportFormat"]').forEach(input => {
        input.addEventListener('change', function() {
            currentExportFormat = this.value;
            updateExportOptions();
            updatePreviewDisplay();
        });
    });

    // PNG DPI selection
    document.querySelectorAll('input[name="pngDpi"]').forEach(input => {
        input.addEventListener('change', function() {
            currentExportOptions.dpi = parseInt(this.value);
            updatePreviewDisplay();
        });
    });

    // PNG background selection
    document.querySelectorAll('input[name="pngBackground"]').forEach(input => {
        input.addEventListener('change', function() {
            currentExportOptions.background = this.value;
            updatePreviewDisplay();
        });
    });

    // Download button
    document.getElementById('btnDownload').addEventListener('click', downloadExport);
}

/**
 * Open export modal
 */
function openExportModal() {
    // Show modal and overlay
    const modal = document.getElementById('exportModal');
    const overlay = document.getElementById('exportModalOverlay');

    modal.style.display = 'flex';
    overlay.style.display = 'block';

    // Generate initial preview
    updatePreviewDisplay();

    updateStatus('Export modal opened');
}

/**
 * Close export modal
 */
function closeExportModal() {
    const modal = document.getElementById('exportModal');
    const overlay = document.getElementById('exportModalOverlay');

    modal.style.display = 'none';
    overlay.style.display = 'none';

    // Clean up preview canvas if exists
    if (previewCanvas) {
        previewCanvas.dispose();
        previewCanvas = null;
    }

    updateStatus('Export modal closed');
}

/**
 * Update PNG options visibility
 */
function updateExportOptions() {
    const pngOptions = document.getElementById('pngOptions');

    if (currentExportFormat === 'png') {
        pngOptions.style.display = 'flex';
    } else {
        pngOptions.style.display = 'none';
    }
}

/**
 * Update preview display based on current options
 */
function updatePreviewDisplay() {
    try {
        // Get template JSON from current canvas
        const templateJson = canvasToTemplateJson(
            canvas,
            parseFloat(document.getElementById('pageWidth').value),
            parseFloat(document.getElementById('pageHeight').value)
        );

        console.log('[Preview] Starting preview update', {
            hasTemplateJson: !!templateJson,
            objectCount: templateJson?.objects?.length || 0
        });

        // Create preview canvas
        const previewElement = document.getElementById('previewCanvas');
        if (!previewElement) {
            console.error('[Preview] Canvas element not found');
            return;
        }

        console.log('[Preview] Canvas element found:', previewElement.id);

        // Dispose of existing preview canvas
        if (previewCanvas) {
            console.log('[Preview] Disposing existing preview canvas');
            previewCanvas.dispose();
            previewCanvas = null;
        }

        // Calculate preview dimensions (scale down for display)
        const templateWidth = currentTemplate.pageWidth;
        const templateHeight = currentTemplate.pageHeight;
        const maxPreviewWidth = 400;
        const maxPreviewHeight = 300;

        let previewWidth = mmToPx(templateWidth);
        let previewHeight = mmToPx(templateHeight);

        console.log('[Preview] Original template size:', { templateWidth, templateHeight, previewWidth: Math.round(previewWidth), previewHeight: Math.round(previewHeight) });

        // Scale down if too large
        const widthScale = maxPreviewWidth / previewWidth;
        const heightScale = maxPreviewHeight / previewHeight;
        const scale = Math.min(widthScale, heightScale, 1);

        previewWidth = Math.round(previewWidth * scale);
        previewHeight = Math.round(previewHeight * scale);

        console.log('[Preview] Calculated dimensions:', { previewWidth, previewHeight, scale });

        if (scale <= 0 || isNaN(scale)) {
            console.error('[Preview] Invalid scale calculated:', scale);
            updateStatus('Error: Invalid preview dimensions');
            return;
        }

        // Create Fabric.js canvas for preview using canvas ID
        // Let Fabric.js handle all dimension setting via constructor
        previewCanvas = new fabric.Canvas('previewCanvas', {
            width: previewWidth,
            height: previewHeight,
            backgroundColor: currentExportOptions.background === 'white' ? 'white' : 'rgba(0,0,0,0)',
            selection: false,
            renderOnAddRemove: false
        });

        // Ensure canvas and wrapper dimensions are correctly set
        previewCanvas.setDimensions({
            width: previewWidth,
            height: previewHeight
        }, {
            cssOnly: false,
            backstoreOnly: false
        });

        // Hide the upper canvas (Fabric.js creates this for interactions)
        // Fabric.js may set inline styles that override CSS, so we must hide it with JavaScript
        if (previewCanvas.upperCanvasEl) {
            previewCanvas.upperCanvasEl.style.display = 'none';
            previewCanvas.upperCanvasEl.style.visibility = 'hidden';
            previewCanvas.upperCanvasEl.style.pointerEvents = 'none';
            console.log('[Preview] Upper canvas hidden with inline styles');
        }

        console.log('[Preview] Fabric.js canvas created:', {
            canvasWidth: previewCanvas.getWidth(),
            canvasHeight: previewCanvas.getHeight(),
            wrapperElement: previewCanvas.wrapperEl?.className || 'no wrapper',
            upperCanvasHidden: previewCanvas.upperCanvasEl?.style.display === 'none'
        });

        // Load template objects with scaling
        if (templateJson && templateJson.objects && templateJson.objects.length > 0) {
            console.log('[Preview] Loading', templateJson.objects.length, 'objects');
            loadPreviewTemplateObjects(templateJson, previewCanvas, scale);
            const loadedObjects = previewCanvas.getObjects();
            console.log('[Preview] Objects loaded on canvas:', loadedObjects.length);
        } else {
            console.log('[Preview] No objects to load');
        }

        // Render preview
        previewCanvas.renderAll();
        console.log('[Preview] Canvas rendered');

        // Update preview container background to show transparency visually
        const previewContainer = document.getElementById('previewContainer');
        if (currentExportOptions.background === 'transparent') {
            previewContainer.classList.add('transparent-bg');
            console.log('[Preview] Transparent background activated - checkerboard visible');
        } else {
            previewContainer.classList.remove('transparent-bg');
            console.log('[Preview] White background activated');
        }

        console.log('[Preview] Updated:', { format: currentExportFormat, dpi: currentExportOptions.dpi, bg: currentExportOptions.background });
        updateStatus('Preview updated');
    } catch (error) {
        console.error('[Preview] Error updating preview:', error);
        console.error('[Preview] Error stack:', error.stack);
        updateStatus('Error generating preview');
    }
}

/**
 * Load template objects to preview canvas with scaling
 */
function loadPreviewTemplateObjects(templateJson, canvas, scale) {
    if (!templateJson || !templateJson.objects) {
        console.log('[Preview.Load] No objects in template');
        return;
    }

    console.log('[Preview.Load] Starting load, scale:', scale);
    const placeholders = generatePlaceholderMap(templateJson);
    console.log('[Preview.Load] Placeholders generated:', Object.keys(placeholders).length, 'entries');
    console.log('[Preview.Load] Placeholder keys:', Object.keys(placeholders));
    console.log('[Preview.Load] Has device.qrcode?', 'device.qrcode' in placeholders);
    console.log('[Preview.Load] Has network.qrcode?', 'network.qrcode' in placeholders);

    let objectsCreated = 0;
    let objectsFailed = 0;

    templateJson.objects.forEach((obj, index) => {
        let fabricObject;

        try {
            console.log(`[Preview.Load] Object ${index}: type=${obj.type}, pos=(${obj.left},${obj.top}), size=(${obj.width},${obj.height})`);

            switch (obj.type) {
                case 'qrcode':
                    console.log('[Preview.Load] QR object details:', {
                        hasProperties: !!obj.properties,
                        properties: obj.properties,
                        dataSource: obj.properties?.dataSource,
                        dataSourceField: obj.dataSource
                    });
                    fabricObject = createQRCode({
                        left: mmToPx(obj.left) * scale,
                        top: mmToPx(obj.top) * scale,
                        width: mmToPx(obj.width || 30) * scale,
                        height: mmToPx(obj.height || 30) * scale,
                        dataSource: obj.properties?.dataSource || 'device.Serial'
                    });
                    break;

                case 'text':
                case 'i-text':
                    const originalText = obj.text || '';
                    const replacedText = replacePlaceholders(originalText, placeholders);
                    fabricObject = createBoundText({
                        left: mmToPx(obj.left) * scale,
                        top: mmToPx(obj.top) * scale,
                        text: replacedText,
                        dataSource: obj.properties?.dataSource || '',
                        fontFamily: obj.fontFamily || 'Arial',
                        fontSize: (obj.fontSize || 16) * scale,
                        fontWeight: obj.fontWeight || 'normal',
                        fill: obj.fill || '#000000'
                    });
                    break;

                case 'image':
                    fabricObject = createImagePlaceholder({
                        left: mmToPx(obj.left) * scale,
                        top: mmToPx(obj.top) * scale,
                        width: mmToPx(obj.width || 50) * scale,
                        height: mmToPx(obj.height || 50) * scale,
                        dataSource: obj.properties?.dataSource || '',
                        src: obj.src || ''
                    });
                    break;

                case 'rect':
                    fabricObject = createRectangle({
                        left: mmToPx(obj.left) * scale,
                        top: mmToPx(obj.top) * scale,
                        width: mmToPx(obj.width || 50) * scale,
                        height: mmToPx(obj.height || 50) * scale,
                        fill: obj.fill || 'transparent',
                        stroke: obj.stroke || '#000000',
                        strokeWidth: obj.strokeWidth || 1
                    });
                    break;

                case 'line':
                    fabricObject = createLine({
                        x1: mmToPx(obj.left) * scale,
                        y1: mmToPx(obj.top) * scale,
                        x2: (mmToPx(obj.left) + mmToPx(obj.width || 50)) * scale,
                        y2: mmToPx(obj.top) * scale,
                        stroke: obj.stroke || '#000000',
                        strokeWidth: obj.strokeWidth || 1
                    });
                    break;
            }

            if (fabricObject) {
                if (obj.angle) {
                    fabricObject.set('angle', obj.angle);
                }

                let shouldAddToCanvas = true;

                // Replace QR code placeholder with real image if we have QR data
                console.log('[Preview.Load] About to check QR condition:', {
                    type: obj.type,
                    isQrcode: obj.type === 'qrcode',
                    hasDataSource: !!obj.properties?.dataSource,
                    condition: obj.type === 'qrcode' && obj.properties?.dataSource
                });
                if (obj.type === 'qrcode' && obj.properties?.dataSource) {
                    const dataSource = obj.properties.dataSource.toLowerCase();
                    console.log('[Preview.Load] QR code detected, dataSource:', dataSource);

                    if (dataSource === 'device.qrcode' || dataSource === 'network.qrcode') {
                        // Use placeholder value for QR code data
                        const qrDataUri = placeholders[dataSource];
                        console.log('[Preview.Load] QR data URI found:', !!qrDataUri, 'length:', qrDataUri?.length);

                        if (qrDataUri) {
                            console.log('[Preview.Load] Loading real QR image...');
                            fabric.Image.fromURL(qrDataUri, function(img) {
                                if (!img || !img.width) {
                                    console.error('[Preview.Load] QR image loaded but has no dimensions');
                                    return;
                                }
                                console.log('[Preview.Load] QR image loaded - dimensions:', img.width, 'x', img.height);

                                img.set({
                                    left: fabricObject.left,
                                    top: fabricObject.top,
                                    scaleX: fabricObject.width / img.width,
                                    scaleY: fabricObject.height / img.height,
                                    angle: fabricObject.angle || 0,
                                    originX: fabricObject.originX || 'center',
                                    originY: fabricObject.originY || 'center'
                                });

                                canvas.add(img);
                                canvas.renderAll();
                                console.log('[Preview.Load] QR image added to canvas successfully');
                            }, function(error) {
                                console.error('[Preview.Load] Error loading QR image:', error);
                            }, { crossOrigin: 'anonymous' });

                            shouldAddToCanvas = false;  // Don't add placeholder
                            objectsCreated++;
                            console.log(`[Preview.Load] Object ${index} queued for QR loading, type: ${obj.type}`);
                        }
                    }
                }

                if (shouldAddToCanvas) {
                    canvas.add(fabricObject);
                    objectsCreated++;
                    console.log(`[Preview.Load] Object ${index} created successfully, type: ${obj.type}`);
                }
            } else {
                console.warn(`[Preview.Load] Object ${index} creation returned null/undefined, type: ${obj.type}`);
                objectsFailed++;
            }
        } catch (error) {
            console.error(`[Preview.Load] Error creating object ${index} (type ${obj.type}):`, error);
            objectsFailed++;
        }
    });

    console.log('[Preview.Load] Loading complete. Created:', objectsCreated, 'Failed:', objectsFailed);
}

/**
 * Download the export file
 */
async function downloadExport() {
    try {
        // Get template JSON from current canvas
        const templateJson = canvasToTemplateJson(
            canvas,
            parseFloat(document.getElementById('pageWidth').value),
            parseFloat(document.getElementById('pageHeight').value)
        );

        // Create export canvas (full resolution, not scaled) and wait for async images to load
        const exportCanvas = await createPreviewCanvas(
            templateJson,
            parseFloat(document.getElementById('pageWidth').value),
            parseFloat(document.getElementById('pageHeight').value)
        );

        if (currentExportFormat === 'png') {
            exportPNG(exportCanvas, currentExportOptions.dpi, currentExportOptions.background);
        } else if (currentExportFormat === 'svg') {
            exportSVG(exportCanvas);
        }

        // Clean up
        exportCanvas.dispose();

        // Keep modal open for multiple exports
        updateStatus(`${currentExportFormat.toUpperCase()} exported successfully`);
    } catch (error) {
        console.error('Export error:', error);
        updateStatus('Error during export');
        alert('Error during export: ' + error.message);
    }
}
