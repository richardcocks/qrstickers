# API Reference

This document describes all API endpoints available in the QRStickers application.

## Architecture

All API endpoints are implemented using **ASP.NET Core MVC Controllers** (not Minimal APIs), providing:
- Clear separation of concerns
- Dependency injection via constructor
- Standard `IActionResult` return types
- XML documentation comments for OpenAPI/Swagger
- Attribute-based routing

**Controllers:**
- `DeviceExportController` - Device export operations
- `TemplateController` - Template matching
- `ExportController` - PDF generation
- `UsageController` - Usage tracking
- `ImageController` - Image management

**Location:** All controllers are located in `src/Controllers/`

For implementation details, see [Controller Refactoring Documentation](Implementation/CONTROLLER_REFACTORING.md).

---

## Authentication

Most API endpoints require authentication via ASP.NET Identity cookie-based authentication.

**Public endpoints:** No authentication required
**Protected endpoints:** Require user to be logged in (`[Authorize]` attribute)

---

## Template Management Endpoints

### List Templates

Get all templates for the current user's connection.

**Endpoint:** `GET /api/templates`

**Authentication:** Required

**Parameters:**
- `connectionId` (optional) - Filter by connection ID

**Response:**
```json
[
  {
    "id": 1,
    "name": "Rack Mount Default",
    "description": "Default template for rack-mounted devices",
    "connectionId": 1,
    "isDefault": false,
    "isSystemTemplate": true,
    "pageWidth": 100.0,
    "pageHeight": 50.0,
    "productTypeFilter": null,
    "isRackMount": true,
    "createdAt": "2024-10-21T15:28:20Z",
    "updatedAt": "2024-10-21T15:28:20Z"
  }
]
```

### Get Template by ID

Get a specific template with full JSON definition.

**Endpoint:** `GET /api/templates/{id}`

**Authentication:** Required

**Parameters:**
- `id` (required) - Template ID

**Response:**
```json
{
  "id": 1,
  "name": "Rack Mount Default",
  "description": "Default template for rack-mounted devices",
  "connectionId": 1,
  "isDefault": false,
  "isSystemTemplate": true,
  "pageWidth": 100.0,
  "pageHeight": 50.0,
  "productTypeFilter": null,
  "isRackMount": true,
  "templateJson": "{ ... }",  // Full Fabric.js JSON
  "createdAt": "2024-10-21T15:28:20Z",
  "updatedAt": "2024-10-21T15:28:20Z"
}
```

**Error Responses:**
- `404 Not Found` - Template not found or user doesn't have access

### Create Template

Create a new sticker template.

**Endpoint:** `POST /api/templates`

**Authentication:** Required

**Request Body:**
```json
{
  "name": "My Custom Template",
  "description": "Custom template for switches",
  "connectionId": 1,
  "pageWidth": 100.0,
  "pageHeight": 50.0,
  "productTypeFilter": "switch",
  "isRackMount": true,
  "isDefault": false,
  "templateJson": "{ ... }"  // Fabric.js JSON
}
```

**Response:**
```json
{
  "id": 5,
  "name": "My Custom Template",
  ...
}
```

**Error Responses:**
- `400 Bad Request` - Invalid template data
- `401 Unauthorized` - User not authenticated
- `403 Forbidden` - Connection doesn't belong to user

### Update Template

Update an existing template.

**Endpoint:** `PUT /api/templates/{id}`

**Authentication:** Required

**Request Body:** Same as Create Template

**Response:** Updated template object

**Error Responses:**
- `404 Not Found` - Template not found or user doesn't have access
- `400 Bad Request` - Invalid template data
- `403 Forbidden` - Cannot modify system templates

### Delete Template

Delete a template.

**Endpoint:** `DELETE /api/templates/{id}`

**Authentication:** Required

**Response:**
```json
{
  "success": true,
  "message": "Template deleted successfully"
}
```

**Error Responses:**
- `404 Not Found` - Template not found
- `403 Forbidden` - Cannot delete system templates or templates in use

### Track Template Usage

Track usage of templates and images during export.

**Endpoint:** `POST /api/templates/usage-track`

**Authentication:** Required

**Request Body:**
```json
{
  "templateId": 1,
  "imageIds": [1, 2, 3]
}
```

**Response:**
```json
{
  "success": true,
  "message": "Usage tracked successfully"
}
```

**Purpose:** Records which templates and images were used in exports for analytics and to prevent deletion of active assets.

---

## Template Matching Endpoints

### Match Template to Device

Find the best matching template for a specific device.

**Endpoint:** `GET /api/templates/match`

**Authentication:** Required

**Parameters:**
- `deviceId` (required) - Device ID
- `connectionId` (required) - Connection ID

**Response:**
```json
{
  "template": {
    "id": 1,
    "name": "Rack Mount Default",
    ...
  },
  "matchReason": "model_match",
  "confidence": 1.0,
  "matchedBy": "MS225-48FP",
  "alternateTemplates": [
    { "id": 2, "name": "Switch Template", ... },
    { "id": 3, "name": "Generic Template", ... }
  ]
}
```

**Match Reasons:**
- `model_match` - Exact device model match (confidence: 1.0)
- `type_match` - Device type match (confidence: 0.8 or 0.75)
- `user_default` - User's default template (confidence: 0.5)
- `system_default` - System default template (confidence: 0.3)
- `fallback` - First available template (confidence: 0.1)

**Error Responses:**
- `404 Not Found` - Device not found
- `400 Bad Request` - No templates available

---

## Export Endpoints

### Export Single Device

Export a sticker for a single device.

**Endpoint:** `POST /api/templates/{templateId}/export`

**Authentication:** Required

**Request Body:**
```json
{
  "deviceId": 123,
  "connectionId": 1,
  "format": "png",
  "dpi": 300,
  "background": "white"
}
```

**Parameters:**
- `format` - Export format: `png`, `svg`, or `pdf`
- `dpi` - DPI for PNG exports: `96`, `150`, or `300`
- `background` - Background type: `white` or `transparent`

**Response:**
```json
{
  "success": true,
  "templateJson": "{ ... }",  // Bound template with device data
  "device": {
    "serial": "Q2XX-XXXX-XXXX",
    "name": "Switch-01",
    "model": "MS225-48FP",
    ...
  }
}
```

**Error Responses:**
- `404 Not Found` - Template or device not found
- `403 Forbidden` - User doesn't have access to device/template

### Export Multiple Devices

Export stickers for multiple devices with progress tracking.

**Endpoint:** `POST /api/templates/export-multiple`

**Authentication:** Required

**Request Body:**
```json
{
  "deviceIds": [1, 2, 3, 4, 5],
  "connectionId": 1,
  "format": "png",
  "dpi": 300,
  "background": "white"
}
```

**Response:**
```json
{
  "success": true,
  "exportId": "abc123",  // For tracking progress via SignalR
  "message": "Export started"
}
```

**Progress Tracking:**

Connect to SignalR hub at `/hubs/export-progress` to receive real-time updates:

```javascript
const connection = new signalR.HubConnectionBuilder()
  .withUrl("/hubs/export-progress")
  .build();

connection.on("ExportProgress", (data) => {
  console.log(`Progress: ${data.completed}/${data.total}`);
  console.log(`Current device: ${data.currentDevice}`);
});
```

**Progress Events:**
- `ExportStarted` - Export job started
- `ExportProgress` - Progress update (includes completed count, total, current device)
- `ExportCompleted` - Export finished successfully
- `ExportFailed` - Export failed with error message

---

## Device Data Endpoints

### Get Device Export Data

Retrieve device data with matched template for export preview.

**Endpoint:** `GET /api/export/device/{deviceId}`

**Authentication:** Required

**Parameters:**
- `deviceId` (required) - Device ID
- `connectionId` (required) - Connection ID

**Response:**
```json
{
  "device": {
    "id": 123,
    "serial": "Q2XX-XXXX-XXXX",
    "name": "Switch-01",
    "model": "MS225-48FP",
    "productType": "switch",
    "networkId": "L_123456",
    "qrCodeDataUri": "data:image/png;base64,...",
    "network": {
      "name": "Main Office",
      "qrCodeDataUri": "data:image/png;base64,..."
    },
    "organization": {
      "name": "Acme Corp",
      "qrCodeDataUri": "data:image/png;base64,..."
    }
  },
  "matchedTemplate": {
    "id": 1,
    "name": "Rack Mount Default",
    "templateJson": "{ ... }"
  },
  "matchReason": "model_match",
  "confidence": 1.0
}
```

**Error Responses:**
- `404 Not Found` - Device not found
- `403 Forbidden` - User doesn't have access to device

---

## Custom Image Endpoints

### List Custom Images

Get all custom images for a connection.

**Endpoint:** `GET /api/images`

**Authentication:** Required

**Parameters:**
- `connectionId` (required) - Connection ID

**Response:**
```json
[
  {
    "id": 1,
    "name": "Company Logo",
    "description": "Main company logo",
    "connectionId": 1,
    "imageDataUri": "data:image/png;base64,...",
    "fileSizeBytes": 12345,
    "width": 200,
    "height": 100,
    "mimeType": "image/png",
    "usageCount": 5,
    "createdAt": "2024-10-21T15:28:20Z"
  }
]
```

### Upload Custom Image

Upload a new custom image.

**Endpoint:** `POST /api/images`

**Authentication:** Required

**Content-Type:** `multipart/form-data`

**Form Data:**
- `file` (required) - Image file (PNG, JPEG, GIF, or SVG)
- `name` (required) - Image name
- `description` (optional) - Image description
- `connectionId` (required) - Connection ID

**Response:**
```json
{
  "id": 1,
  "name": "Company Logo",
  "imageDataUri": "data:image/png;base64,...",
  ...
}
```

**File Restrictions:**
- Max file size: 5 MB
- Supported formats: PNG, JPEG, GIF, SVG
- Automatic image optimization and resizing

**Error Responses:**
- `400 Bad Request` - Invalid file or file too large
- `415 Unsupported Media Type` - Invalid image format

### Delete Custom Image

Delete a custom image.

**Endpoint:** `DELETE /api/images/{id}`

**Authentication:** Required

**Response:**
```json
{
  "success": true,
  "message": "Image deleted successfully"
}
```

**Error Responses:**
- `404 Not Found` - Image not found
- `403 Forbidden` - Image is in use by templates (check `usageCount`)

---

## Global Variables Endpoints

### List Global Variables

Get all global variables for a connection.

**Endpoint:** `GET /api/global-variables`

**Authentication:** Required

**Parameters:**
- `connectionId` (required) - Connection ID

**Response:**
```json
[
  {
    "id": 1,
    "connectionId": 1,
    "variableName": "supportUrl",
    "variableValue": "https://support.example.com",
    "description": "Company support website URL",
    "createdAt": "2024-10-21T15:28:20Z",
    "updatedAt": "2024-10-21T15:28:20Z"
  }
]
```

**Usage in Templates:**

Variables can be referenced using `{{global.variableName}}` syntax:

```json
{
  "type": "text",
  "text": "Support: {{global.supportUrl}}"
}
```

### Create Global Variable

Create a new global variable.

**Endpoint:** `POST /api/global-variables`

**Authentication:** Required

**Request Body:**
```json
{
  "connectionId": 1,
  "variableName": "supportUrl",
  "variableValue": "https://support.example.com",
  "description": "Company support website URL"
}
```

**Response:** Created variable object

**Error Responses:**
- `400 Bad Request` - Duplicate variable name for connection
- `403 Forbidden` - User doesn't own connection

### Update Global Variable

Update an existing global variable.

**Endpoint:** `PUT /api/global-variables/{id}`

**Authentication:** Required

**Request Body:** Same as Create Global Variable

**Response:** Updated variable object

### Delete Global Variable

Delete a global variable.

**Endpoint:** `DELETE /api/global-variables/{id}`

**Authentication:** Required

**Response:**
```json
{
  "success": true,
  "message": "Variable deleted successfully"
}
```

---

## Rate Limiting

### Endpoints

No rate limiting currently applied to authenticated endpoints. Future enhancements may add per-user limits for export operations.

---

## Error Responses

### Standard Error Format

All API errors return JSON with the following structure:

```json
{
  "error": "Error message",
  "details": "Detailed error information (development only)",
  "statusCode": 400
}
```

### HTTP Status Codes

- `200 OK` - Request successful
- `201 Created` - Resource created successfully
- `204 No Content` - Successful request with no response body
- `400 Bad Request` - Invalid request parameters or body
- `401 Unauthorized` - Authentication required
- `403 Forbidden` - User doesn't have permission
- `404 Not Found` - Resource not found
- `415 Unsupported Media Type` - Invalid content type
- `429 Too Many Requests` - Rate limit exceeded
- `500 Internal Server Error` - Server error (check logs)

---

## Authentication Flow

### Login

```http
POST /Identity/Account/Login
Content-Type: application/x-www-form-urlencoded

Email=user@example.com&Password=SecurePassword123!&RememberMe=true
```

**Response:**
- Sets authentication cookie
- Redirects to return URL or home page

### Logout

```http
POST /Identity/Account/Logout
```

**Response:**
- Clears authentication cookie
- Redirects to home page

### Register

```http
POST /Identity/Account/Register
Content-Type: application/x-www-form-urlencoded

Email=user@example.com&Password=SecurePassword123!&ConfirmPassword=SecurePassword123!
```

**Response:**
- Creates user account
- Sets authentication cookie
- Redirects to home page

---

## SignalR Hubs

### Export Progress Hub

**Hub URL:** `/hubs/export-progress`

**Events (Server → Client):**

1. **ExportStarted**
   ```json
   {
     "exportId": "abc123",
     "totalDevices": 10
   }
   ```

2. **ExportProgress**
   ```json
   {
     "exportId": "abc123",
     "completed": 5,
     "total": 10,
     "currentDevice": "Switch-05",
     "percentage": 50
   }
   ```

3. **ExportCompleted**
   ```json
   {
     "exportId": "abc123",
     "downloadUrl": "/exports/abc123.zip",
     "fileSize": 1234567
   }
   ```

4. **ExportFailed**
   ```json
   {
     "exportId": "abc123",
     "error": "Device not found"
   }
   ```

**Connection Example:**
```javascript
const connection = new signalR.HubConnectionBuilder()
  .withUrl("/hubs/export-progress")
  .build();

await connection.start();
connection.on("ExportProgress", handleProgress);
```

---

## Next Steps

- **Setup Guide:** [SETUP.md](SETUP.md)
- **Architecture Details:** [ARCHITECTURE.md](ARCHITECTURE.md)
- **Deployment Guide:** [DEPLOYMENT.md](DEPLOYMENT.md)
