/**
 * Image Upload JavaScript
 * Handles upload modal, file selection, preview, validation, and API calls
 */

const MAX_DIMENSION = 900;
const MAX_FILE_SIZE = 2 * 1024 * 1024; // 2 MB
const ALLOWED_TYPES = ['image/png', 'image/jpeg', 'image/webp', 'image/svg+xml'];

// ===================== MODAL MANAGEMENT =====================

function showUploadModal() {
    document.getElementById('uploadModal').style.display = 'flex';
    resetUploadForm();
}

function hideUploadModal() {
    document.getElementById('uploadModal').style.display = 'none';
}

function showDeleteModal() {
    document.getElementById('deleteModal').style.display = 'flex';
}

function hideDeleteModal() {
    document.getElementById('deleteModal').style.display = 'none';
}

function resetUploadForm() {
    document.getElementById('uploadForm').reset();
    document.getElementById('previewContainer').style.display = 'none';
    document.getElementById('uploadError').style.display = 'none';
}

// ===================== FILE SELECTION & PREVIEW =====================

// Handle file selection
document.addEventListener('DOMContentLoaded', function() {
    const imageFileInput = document.getElementById('imageFile');
    if (imageFileInput) {
        imageFileInput.addEventListener('change', handleFileSelect);
    }
});

async function handleFileSelect(event) {
    const file = event.target.files[0];
    if (!file) {
        document.getElementById('previewContainer').style.display = 'none';
        return;
    }

    try {
        // Convert file to data URI
        const dataUri = await fileToDataUri(file);

        // Load image to get dimensions
        const img = new Image();
        img.src = dataUri;

        await new Promise((resolve, reject) => {
            img.onload = resolve;
            img.onerror = () => reject(new Error('Failed to load image'));
        });

        // Update preview
        document.getElementById('imagePreview').src = dataUri;
        document.getElementById('imageDimensions').textContent =
            `${img.width} × ${img.height} px (${(file.size / 1024).toFixed(1)} KB)`;
        document.getElementById('previewContainer').style.display = 'block';

        // Store dimensions for upload
        event.target.dataset.width = img.width;
        event.target.dataset.height = img.height;
        event.target.dataset.dataUri = dataUri;

        // Clear any previous errors
        hideError();
    } catch (error) {
        showError('Failed to load image preview: ' + error.message);
        document.getElementById('previewContainer').style.display = 'none';
    }
}

function fileToDataUri(file) {
    return new Promise((resolve, reject) => {
        const reader = new FileReader();
        reader.onloadend = () => resolve(reader.result);
        reader.onerror = reject;
        reader.readAsDataURL(file);
    });
}

// ===================== VALIDATION =====================

function validateUpload() {
    const imageName = document.getElementById('imageName').value.trim();
    const imageFile = document.getElementById('imageFile').files[0];

    // Validate name
    if (!imageName) {
        return { valid: false, error: 'Image name is required' };
    }

    if (imageName.length > 200) {
        return { valid: false, error: 'Image name too long (max 200 characters)' };
    }

    // Validate file
    if (!imageFile) {
        return { valid: false, error: 'Please select an image file' };
    }

    // Validate MIME type
    if (!ALLOWED_TYPES.includes(imageFile.type)) {
        return { valid: false, error: 'Unsupported format. Use PNG, JPEG, WebP, or SVG' };
    }

    // Validate file size
    if (imageFile.size > MAX_FILE_SIZE) {
        const sizeMB = (imageFile.size / 1024 / 1024).toFixed(2);
        return { valid: false, error: `File too large (${sizeMB} MB). Max 2 MB` };
    }

    // Validate dimensions
    const width = parseInt(document.getElementById('imageFile').dataset.width);
    const height = parseInt(document.getElementById('imageFile').dataset.height);

    if (width > MAX_DIMENSION || height > MAX_DIMENSION) {
        return { valid: false, error: `Image too large (${width}×${height}px). Max ${MAX_DIMENSION}×${MAX_DIMENSION}px` };
    }

    return { valid: true };
}

// ===================== UPLOAD =====================

async function uploadImage() {
    hideError();

    // Validate
    const validation = validateUpload();
    if (!validation.valid) {
        showError(validation.error);
        return;
    }

    const uploadButton = document.getElementById('uploadButton');
    uploadButton.disabled = true;
    uploadButton.textContent = 'Uploading...';

    try {
        const connectionId = parseInt(document.getElementById('connectionId').value);
        const imageName = document.getElementById('imageName').value.trim();
        const imageDescription = document.getElementById('imageDescription').value.trim();
        const imageFile = document.getElementById('imageFile');
        const dataUri = imageFile.dataset.dataUri;
        const widthPx = parseInt(imageFile.dataset.width);
        const heightPx = parseInt(imageFile.dataset.height);

        const request = {
            connectionId: connectionId,
            name: imageName,
            description: imageDescription || null,
            dataUri: dataUri,
            widthPx: widthPx,
            heightPx: heightPx
        };

        const response = await fetch('/api/images/upload', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json'
            },
            body: JSON.stringify(request)
        });

        const result = await response.json();

        if (!response.ok || !result.success) {
            showError(result.error || 'Upload failed');
            return;
        }

        // Close modal and show top notification
        hideUploadModal();
        showNotification('✓ Image uploaded successfully', 'success');

        // Reload page after brief delay to see notification
        setTimeout(() => {
            window.location.reload();
        }, 1500);

    } catch (error) {
        showError('Upload failed: ' + error.message);
    } finally {
        uploadButton.disabled = false;
        uploadButton.textContent = 'Upload Image';
    }
}

// ===================== DELETE =====================

function confirmDelete(imageId, imageName) {
    document.getElementById('deleteImageId').value = imageId;
    document.getElementById('deleteImageName').textContent = imageName;
    showDeleteModal();
}

async function deleteImage() {
    const imageId = document.getElementById('deleteImageId').value;

    try {
        const response = await fetch(`/api/images/${imageId}`, {
            method: 'DELETE'
        });

        const result = await response.json();

        if (!response.ok || !result.success) {
            alert('Delete failed: ' + (result.error || 'Unknown error'));
            return;
        }

        // Reload page
        window.location.reload();

    } catch (error) {
        alert('Delete failed: ' + error.message);
    }
}

// ===================== ERROR/SUCCESS DISPLAY =====================

function showError(message) {
    const errorDiv = document.getElementById('uploadError');
    errorDiv.textContent = message;
    errorDiv.style.display = 'block';

    // Scroll error into view
    errorDiv.scrollIntoView({ behavior: 'smooth', block: 'nearest' });
}

function hideError() {
    document.getElementById('uploadError').style.display = 'none';
}

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

// Close modals on Escape key
document.addEventListener('keydown', function(event) {
    if (event.key === 'Escape') {
        hideUploadModal();
        hideDeleteModal();
    }
});

// Close modals on overlay click
document.addEventListener('click', function(event) {
    if (event.target.classList.contains('modal-overlay')) {
        hideUploadModal();
        hideDeleteModal();
    }
});
