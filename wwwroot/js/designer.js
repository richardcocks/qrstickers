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

/**
 * Initialize the designer
 */
function initDesigner(templateData, editMode, systemTemplate) {
    currentTemplate = templateData;
    isEditMode = editMode;
    isSystemTemplate = systemTemplate;
    console.log(templateData);
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
            element = createImagePlaceholder({
                left: centerX - 50,
                top: centerY - 50,
                width: 100,
                height: 100,
                dataSource: 'connection.CompanyLogoUrl'
            });
            break;

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
            element = createImagePlaceholder({
                left: x - 50,
                top: y - 50,
                width: 100,
                height: 100,
                dataSource: 'connection.CompanyLogoUrl'
            });
            break;

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
        document.getElementById('imageDataSource').value = activeObject.get('dataSource') || '';
        document.getElementById('imageUrl').value = activeObject.get('src') || '';
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
                    fabricObject = createImagePlaceholder({
                        left: mmToPx(obj.left) + boundaryLeft,
                        top: mmToPx(obj.top) + boundaryTop,
                        width: mmToPx(obj.width || 50),
                        height: mmToPx(obj.height || 50),
                        dataSource: obj.properties?.dataSource || '',
                        src: obj.src || ''
                    });
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
