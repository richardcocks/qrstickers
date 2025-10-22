/**
 * Fabric.js Extensions for QR Sticker Designer
 * Custom objects and helper functions
 */

// Constants
const MM_TO_PX_RATIO = 3.7795275591; // 96 DPI conversion (1mm = 3.7795275591px)

/**
 * Convert millimeters to pixels for canvas rendering
 */
function mmToPx(mm) {
    return mm * MM_TO_PX_RATIO;
}

/**
 * Convert pixels to millimeters
 */
function pxToMm(px) {
    return px / MM_TO_PX_RATIO;
}

/**
 * Custom QR Code object for Fabric.js
 * Renders as a placeholder group with QR code pattern
 */
fabric.QRCode = fabric.util.createClass(fabric.Group, {
    type: 'qrcode',

    initialize: function(options) {
        options = options || {};

        // Default size
        const size = options.width || 100;

        // Create QR code visual placeholder
        const background = new fabric.Rect({
            width: size,
            height: size,
            fill: 'white',
            stroke: '#333',
            strokeWidth: 2
        });

        // Create simple QR-like pattern
        const patternSize = size / 5;
        const patterns = [
            // Top-left corner pattern
            new fabric.Rect({
                left: patternSize * 0.3,
                top: patternSize * 0.3,
                width: patternSize * 0.8,
                height: patternSize * 0.8,
                fill: 'black'
            }),
            // Top-right corner pattern
            new fabric.Rect({
                left: patternSize * 3.9,
                top: patternSize * 0.3,
                width: patternSize * 0.8,
                height: patternSize * 0.8,
                fill: 'black'
            }),
            // Bottom-left corner pattern
            new fabric.Rect({
                left: patternSize * 0.3,
                top: patternSize * 3.9,
                width: patternSize * 0.8,
                height: patternSize * 0.8,
                fill: 'black'
            }),
            // Center pattern
            new fabric.Rect({
                left: patternSize * 2,
                top: patternSize * 2,
                width: patternSize,
                height: patternSize,
                fill: 'black'
            }),
            // QR label text
            new fabric.Text('QR', {
                left: size / 2,
                top: size / 2,
                fontSize: size / 4,
                fill: '#666',
                originX: 'center',
                originY: 'center',
                fontWeight: 'bold'
            })
        ];

        const objects = [background, ...patterns];

        this.callSuper('initialize', objects, options);

        this.set({
            // Custom properties for data binding
            dataSource: options.dataSource || 'device.Serial',
            eccLevel: options.eccLevel || 'Q',
            quietZone: options.quietZone || 2
        });
    },

    toObject: function() {
        return fabric.util.object.extend(this.callSuper('toObject'), {
            dataSource: this.get('dataSource'),
            eccLevel: this.get('eccLevel'),
            quietZone: this.get('quietZone')
        });
    },

    _render: function(ctx) {
        this.callSuper('_render', ctx);
    }
});

fabric.QRCode.fromObject = function(object, callback) {
    return fabric.Object._fromObject('QRCode', object, callback);
};

/**
 * Helper function to create a QR code object
 */
function createQRCode(options) {
    return new fabric.QRCode({
        left: options.left || 50,
        top: options.top || 50,
        width: options.width || 100,
        height: options.height || 100,
        dataSource: options.dataSource || 'device.Serial',
        eccLevel: options.eccLevel || 'Q'
    });
}

/**
 * Alias for createQRCode - used by export-preview.js
 */
function createQRCodePlaceholder(options) {
    return createQRCode(options);
}

/**
 * Helper function to create a text object with data binding
 */
function createBoundText(options) {
    const text = new fabric.IText(options.text || 'Text', {
        left: options.left || 50,
        top: options.top || 50,
        fontFamily: options.fontFamily || 'Arial',
        fontSize: options.fontSize || 16,
        fill: options.fill || '#000000',
        fontWeight: options.fontWeight || 'normal'
    });

    // Add custom data binding property
    text.set('dataSource', options.dataSource || '');
    text.set('maxLength', options.maxLength || null);
    text.set('overflow', options.overflow || 'truncate');

    return text;
}

/**
 * Alias for createBoundText - used by export-preview.js
 */
function createTextObject(options) {
    return createBoundText(options);
}

/**
 * Helper function to create an image placeholder
 */
function createImagePlaceholder(options) {
    const rect = new fabric.Rect({
        left: options.left || 50,
        top: options.top || 50,
        width: options.width || 100,
        height: options.height || 100,
        fill: '#f0f0f0',
        stroke: '#999',
        strokeWidth: 2,
        strokeDashArray: [5, 5]
    });

    const text = new fabric.Text('IMAGE', {
        left: (options.left || 50) + (options.width || 100) / 2,
        top: (options.top || 50) + (options.height || 100) / 2,
        fontSize: 14,
        fill: '#999',
        originX: 'center',
        originY: 'center'
    });

    const group = new fabric.Group([rect, text], {
        left: options.left || 50,
        top: options.top || 50,
        selectable: true
    });

    // Add custom properties
    group.set('type', 'image');
    group.set('dataSource', options.dataSource || 'connection.CompanyLogoUrl');
    group.set('src', options.src || '');
    group.set('aspectRatio', options.aspectRatio || 'contain');

    return group;
}

/**
 * Helper function to create a rectangle
 */
function createRectangle(options) {
    return new fabric.Rect({
        left: options.left || 50,
        top: options.top || 50,
        width: options.width || 100,
        height: options.height || 50,
        fill: options.fill || 'transparent',
        stroke: options.stroke || '#333',
        strokeWidth: options.strokeWidth || 2
    });
}

/**
 * Helper function to create a line
 */
function createLine(options) {
    return new fabric.Line([
        options.x1 || 50,
        options.y1 || 50,
        options.x2 || 150,
        options.y2 || 50
    ], {
        stroke: options.stroke || '#333',
        strokeWidth: options.strokeWidth || 2
    });
}

/**
 * Extract data binding information from an object
 */
function getDataBinding(fabricObject) {
    if (!fabricObject) return null;

    const type = fabricObject.type;
    const dataSource = fabricObject.dataSource || fabricObject.get('dataSource');

    return {
        type: type,
        dataSource: dataSource,
        hasBinding: !!dataSource
    };
}

/**
 * Update data binding on an object
 */
function updateDataBinding(fabricObject, dataSource) {
    if (!fabricObject) return;

    fabricObject.set('dataSource', dataSource);

    // Update text content to show binding if it's a text object
    if (fabricObject.type === 'i-text' || fabricObject.type === 'text') {
        if (dataSource) {
            fabricObject.set('text', `{{${dataSource}}}`);
        }
    }
}

/**
 * Serialize canvas to template JSON format
 */
function canvasToTemplateJson(canvas, pageWidth, pageHeight) {
    // Get boundary offset (passed from designer.js globals)
    const offsetX = typeof boundaryLeft !== 'undefined' ? boundaryLeft : 0;
    const offsetY = typeof boundaryTop !== 'undefined' ? boundaryTop : 0;

    // Filter out boundary and other non-exportable objects
    const objects = canvas.getObjects()
        .filter(obj => !obj.excludeFromExport && obj.name !== 'stickerBoundary')
        .map(obj => {
        // Adjust positions to be relative to sticker boundary (0,0)
        const baseObj = {
            type: obj.type,
            id: obj.id || generateId(),
            left: pxToMm(obj.left - offsetX),
            top: pxToMm(obj.top - offsetY),
            width: pxToMm(obj.getScaledWidth()),
            height: pxToMm(obj.getScaledHeight()),
            scaleX: obj.scaleX || 1,
            scaleY: obj.scaleY || 1,
            angle: obj.angle || 0
        };

        // Add type-specific properties
        if (obj.type === 'qrcode') {
            baseObj.properties = {
                dataSource: obj.dataSource || obj.get('dataSource'),
                eccLevel: obj.eccLevel || obj.get('eccLevel') || 'Q',
                quietZone: obj.quietZone || obj.get('quietZone') || 2
            };
        } else if (obj.type === 'i-text' || obj.type === 'text') {
            baseObj.text = obj.text;
            baseObj.fontFamily = obj.fontFamily;
            baseObj.fontSize = obj.fontSize;
            baseObj.fontWeight = obj.fontWeight;
            baseObj.fill = obj.fill;
            baseObj.properties = {
                dataSource: obj.dataSource || obj.get('dataSource') || '',
                maxLength: obj.maxLength || obj.get('maxLength'),
                overflow: obj.overflow || obj.get('overflow') || 'truncate'
            };
        } else if (obj.type === 'image' || (obj.type === 'group' && obj.get('type') === 'image')) {
            baseObj.src = obj.src || obj.get('src') || '';
            baseObj.properties = {
                dataSource: obj.dataSource || obj.get('dataSource') || '',
                aspectRatio: obj.aspectRatio || obj.get('aspectRatio') || 'contain',
                placeholder: true
            };
        } else if (obj.type === 'rect') {
            baseObj.fill = obj.fill;
            baseObj.stroke = obj.stroke;
            baseObj.strokeWidth = obj.strokeWidth;
        } else if (obj.type === 'line') {
            baseObj.stroke = obj.stroke;
            baseObj.strokeWidth = obj.strokeWidth;
        }

        return baseObj;
    });

    return {
        version: '1.0',
        fabricVersion: fabric.version,
        pageSize: {
            width: pageWidth,
            height: pageHeight,
            unit: 'mm'
        },
        objects: objects
    };
}

/**
 * Generate a unique ID for objects
 */
function generateId() {
    return 'obj-' + Date.now() + '-' + Math.random().toString(36).substr(2, 9);
}
