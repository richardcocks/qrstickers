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

/**
 * Initialize the designer
 */
function initDesigner(templateData, editMode, systemTemplate) {
    currentTemplate = templateData;
    isEditMode = editMode;
    isSystemTemplate = systemTemplate;

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
    const canvasWidth = mmToPx(pageWidthMm);
    const canvasHeight = mmToPx(pageHeightMm);

    canvas = new fabric.Canvas('designCanvas', {
        width: canvasWidth,
        height: canvasHeight,
        backgroundColor: 'white',
        selection: true,
        preserveObjectStacking: true
    });

    // Enable object controls
    canvas.selectionColor = 'rgba(25, 118, 210, 0.1)';
    canvas.selectionBorderColor = '#1976d2';
    canvas.selectionLineWidth = 2;

    // Draw grid
    drawGrid();
}

/**
 * Draw grid on canvas
 */
function drawGrid() {
    if (!document.getElementById('chkShowGrid').checked) {
        canvas.backgroundImage = null;
        canvas.renderAll();
        return;
    }

    const gridSizePx = mmToPx(gridSize);
    const width = canvas.getWidth();
    const height = canvas.getHeight();

    // Create grid pattern
    const gridCanvas = document.createElement('canvas');
    gridCanvas.width = width;
    gridCanvas.height = height;
    const ctx = gridCanvas.getContext('2d');

    ctx.strokeStyle = '#e0e0e0';
    ctx.lineWidth = 0.5;

    // Vertical lines
    for (let x = 0; x <= width; x += gridSizePx) {
        ctx.beginPath();
        ctx.moveTo(x, 0);
        ctx.lineTo(x, height);
        ctx.stroke();
    }

    // Horizontal lines
    for (let y = 0; y <= height; y += gridSizePx) {
        ctx.beginPath();
        ctx.moveTo(0, y);
        ctx.lineTo(width, y);
        ctx.stroke();
    }

    // Set as background
    fabric.Image.fromURL(gridCanvas.toDataURL(), function(img) {
        canvas.setBackgroundImage(img, canvas.renderAll.bind(canvas));
    });
}

/**
 * Initialize toolbar controls
 */
function initToolbar() {
    // Zoom controls
    document.getElementById('btnZoomIn').addEventListener('click', () => {
        currentZoom = Math.min(currentZoom + 0.1, 3);
        canvas.setZoom(currentZoom);
        updateStatus(`Zoom: ${Math.round(currentZoom * 100)}%`);
    });

    document.getElementById('btnZoomOut').addEventListener('click', () => {
        currentZoom = Math.max(currentZoom - 0.1, 0.1);
        canvas.setZoom(currentZoom);
        updateStatus(`Zoom: ${Math.round(currentZoom * 100)}%`);
    });

    document.getElementById('btnZoomReset').addEventListener('click', () => {
        currentZoom = 1;
        canvas.setZoom(1);
        updateStatus('Zoom reset to 100%');
    });

    // Grid toggle
    document.getElementById('chkShowGrid').addEventListener('change', function() {
        drawGrid();
    });

    // Snap to grid toggle
    document.getElementById('chkSnapToGrid').addEventListener('change', function() {
        updateStatus(this.checked ? 'Snap to grid enabled' : 'Snap to grid disabled');
    });

    // Page size changes
    document.getElementById('pageWidth').addEventListener('change', function() {
        const newWidth = parseFloat(this.value);
        if (newWidth >= 10 && newWidth <= 500) {
            canvas.setWidth(mmToPx(newWidth));
            currentTemplate.pageWidth = newWidth;
            drawGrid();
            updateStatus(`Page width set to ${newWidth}mm`);
        }
    });

    document.getElementById('pageHeight').addEventListener('change', function() {
        const newHeight = parseFloat(this.value);
        if (newHeight >= 10 && newHeight <= 500) {
            canvas.setHeight(mmToPx(newHeight));
            currentTemplate.pageHeight = newHeight;
            drawGrid();
            updateStatus(`Page height set to ${newHeight}mm`);
        }
    });

    // Save button
    document.getElementById('btnSave').addEventListener('click', saveTemplate);
}

/**
 * Initialize element palette (click to add to canvas)
 */
function initElementPalette() {
    const paletteItems = document.querySelectorAll('.palette-item');

    paletteItems.forEach(item => {
        item.addEventListener('click', function() {
            const elementType = this.getAttribute('data-element-type');
            addElementToCanvas(elementType);
        });
    });
}

/**
 * Add element to canvas center
 */
function addElementToCanvas(elementType) {
    const centerX = canvas.getWidth() / 2;
    const centerY = canvas.getHeight() / 2;

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
            activeObject.set('left', mmToPx(parseFloat(this.value)));
            canvas.renderAll();
        }
    });

    document.getElementById('posY').addEventListener('input', function() {
        const activeObject = canvas.getActiveObject();
        if (activeObject) {
            activeObject.set('top', mmToPx(parseFloat(this.value)));
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

    // Update common properties
    document.getElementById('posX').value = pxToMm(activeObject.left).toFixed(1);
    document.getElementById('posY').value = pxToMm(activeObject.top).toFixed(1);
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

    // Submit form
    updateStatus('Saving template...');
    document.getElementById('saveForm').submit();
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
                        left: mmToPx(obj.left),
                        top: mmToPx(obj.top),
                        width: mmToPx(obj.width || 30),
                        height: mmToPx(obj.height || 30),
                        dataSource: obj.properties?.dataSource || 'device.Serial',
                        eccLevel: obj.properties?.eccLevel || 'Q'
                    });
                    break;

                case 'text':
                case 'i-text':
                    fabricObject = createBoundText({
                        left: mmToPx(obj.left),
                        top: mmToPx(obj.top),
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
                        left: mmToPx(obj.left),
                        top: mmToPx(obj.top),
                        width: mmToPx(obj.width || 50),
                        height: mmToPx(obj.height || 50),
                        dataSource: obj.properties?.dataSource || '',
                        src: obj.src || ''
                    });
                    break;

                case 'rect':
                    fabricObject = createRectangle({
                        left: mmToPx(obj.left),
                        top: mmToPx(obj.top),
                        width: mmToPx(obj.width || 50),
                        height: mmToPx(obj.height || 50),
                        fill: obj.fill || 'transparent',
                        stroke: obj.stroke || '#000000',
                        strokeWidth: obj.strokeWidth || 1
                    });
                    break;

                case 'line':
                    const x1 = mmToPx(obj.left);
                    const y1 = mmToPx(obj.top);
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
 * Update status bar
 */
function updateStatus(message) {
    document.getElementById('statusText').textContent = message;
}
