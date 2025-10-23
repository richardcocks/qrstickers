# Phase 5: Device Data Export - Technical Specification

**Date:** 2025-10-22
**Version:** 1.0
**Status:** Ready for Development

---

## Table of Contents

1. [API Endpoints](#api-endpoints)
2. [Database Schema Changes](#database-schema-changes)
3. [Data Structures](#data-structures)
4. [Algorithms](#algorithms)
5. [Component Interactions](#component-interactions)
6. [Error Handling](#error-handling)
7. [Performance Optimization](#performance-optimization)

---

## API Endpoints

### Device Export Data Retrieval

#### GET `/api/export/device/{deviceId}`

**Purpose:** Retrieve all data needed to export a single device

**Parameters:**
```
deviceId (int) - ID of device to export
connectionId (int) - ID of connection device belongs to
```

**Request:**
```http
GET /api/export/device/123?connectionId=456
Authorization: Bearer {token}
```

**Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "device": {
      "id": 123,
      "serial": "MS-1234-ABCD-5678",
      "name": "Switch-Main-Office",
      "mac": "00:1A:2B:3C:4D:5E",
      "model": "MS225-48FP",
      "ipAddress": "192.168.1.10",
      "tags": ["production", "datacenter"],
      "type": "switch",
      "status": "online",
      "firmware": "15.18.3",
      "connectionId": 456,
      "networkId": 789
    },
    "network": {
      "id": 789,
      "name": "Production Network",
      "organizationId": 100,
      "type": "network"
    },
    "connection": {
      "id": 456,
      "displayName": "Meraki - Production",
      "type": "meraki",
      "companyLogoUrl": "https://..."
    },
    "globalVariables": {
      "supportUrl": "support.example.com",
      "supportPhone": "+1-555-0100",
      "website": "www.example.com"
    }
  }
}
```

**Error Responses:**
- `401 Unauthorized` - Not authenticated
- `403 Forbidden` - Device doesn't belong to user
- `404 Not Found` - Device/connection not found
- `500 Server Error` - Database error

---

#### POST `/api/export/devices/bulk`

**Purpose:** Retrieve data for multiple devices (batch export)

**Request:**
```json
{
  "deviceIds": [123, 124, 125],
  "connectionId": 456
}
```

**Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "devices": [
      {
        "id": 123,
        "serial": "MS-1234-ABCD-5678",
        "name": "Switch-Main-Office",
        // ... full device object
      },
      // ... more devices
    ],
    "connection": { /* ... */ },
    "globalVariables": { /* ... */ }
  },
  "metadata": {
    "totalCount": 3,
    "successCount": 3,
    "failedCount": 0,
    "failed": []
  }
}
```

---

#### GET `/api/templates/match?deviceId={id}&connectionId={id}`

**Purpose:** Find best matching template for a device

**Response (200 OK):**
```json
{
  "success": true,
  "data": {
    "templateId": 999,
    "templateName": "Device Sticker - Switch",
    "matchReason": "model_match",  // model_match, type_match, user_default, system_default
    "confidence": 0.95,
    "alternateTemplates": [
      {
        "templateId": 1000,
        "templateName": "Generic Device Sticker",
        "matchReason": "type_match"
      }
    ]
  }
}
```

---

## Database Schema Changes

### New Tables

```sql
-- Template matching metadata
CREATE TABLE TemplateDeviceModels (
    Id INT PRIMARY KEY IDENTITY(1,1),
    TemplateId INT NOT NULL,
    DeviceModel NVARCHAR(100) NOT NULL,
    Priority INT DEFAULT 0,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 DEFAULT GETUTCDATE(),
    FOREIGN KEY (TemplateId) REFERENCES Templates(Id) ON DELETE CASCADE,
    UNIQUE (TemplateId, DeviceModel)
);

CREATE TABLE TemplateDeviceTypes (
    Id INT PRIMARY KEY IDENTITY(1,1),
    TemplateId INT NOT NULL,
    DeviceType NVARCHAR(50) NOT NULL,  -- 'switch', 'ap', 'gateway', 'firewall'
    Priority INT DEFAULT 0,
    CreatedAt DATETIME2 DEFAULT GETUTCDATE(),
    UpdatedAt DATETIME2 DEFAULT GETUTCDATE(),
    FOREIGN KEY (TemplateId) REFERENCES Templates(Id) ON DELETE CASCADE,
    UNIQUE (TemplateId, DeviceType)
);

-- Export history tracking
CREATE TABLE ExportHistory (
    Id INT PRIMARY KEY IDENTITY(1,1),
    UserId NVARCHAR(450) NOT NULL,
    TemplateId INT,
    DeviceId INT,
    ConnectionId INT,
    ExportFormat NVARCHAR(10),  -- PNG, SVG, PDF
    ExportDpi INT,
    BackgroundType NVARCHAR(20),  -- white, transparent
    ExportedAt DATETIME2 DEFAULT GETUTCDATE(),
    FileSize INT,
    FilePath NVARCHAR(500),  -- If stored server-side
    UserAgent NVARCHAR(500),
    IpAddress NVARCHAR(50),
    FOREIGN KEY (UserId) REFERENCES AspNetUsers(Id),
    FOREIGN KEY (TemplateId) REFERENCES Templates(Id),
    FOREIGN KEY (ConnectionId) REFERENCES Connections(Id)
);

-- Indexes for performance
CREATE INDEX IX_TemplateDeviceModels_TemplateId ON TemplateDeviceModels(TemplateId);
CREATE INDEX IX_TemplateDeviceModels_DeviceModel ON TemplateDeviceModels(DeviceModel);
CREATE INDEX IX_TemplateDeviceTypes_TemplateId ON TemplateDeviceTypes(TemplateId);
CREATE INDEX IX_TemplateDeviceTypes_DeviceType ON TemplateDeviceTypes(DeviceType);
CREATE INDEX IX_ExportHistory_UserId ON ExportHistory(UserId);
CREATE INDEX IX_ExportHistory_ExportedAt ON ExportHistory(ExportedAt);
CREATE INDEX IX_ExportHistory_TemplateId ON ExportHistory(TemplateId);
CREATE INDEX IX_ExportHistory_DeviceId ON ExportHistory(DeviceId);
```

### Modified Tables

```sql
-- Add to AspNetUsers table
ALTER TABLE AspNetUsers ADD
    DefaultTemplateId INT NULL,
    ExportPreferenceFormat NVARCHAR(10) DEFAULT 'PNG',
    ExportPreferenceDpi INT DEFAULT 96,
    FOREIGN KEY (DefaultTemplateId) REFERENCES Templates(Id);

-- Add to Templates table
ALTER TABLE Templates ADD
    IsDefaultTemplate BIT DEFAULT 0,
    TemplateCategory NVARCHAR(50),  -- 'device', 'network', 'switch', 'ap'
    ModelFilterJson NVARCHAR(MAX),  -- JSON array: ["MS225-48FP", "MS250-48FP"]
    TypeFilterJson NVARCHAR(MAX);   -- JSON array: ["switch", "ap"]

-- Add index for template queries
CREATE INDEX IX_Templates_IsDefault ON Templates(IsDefaultTemplate) WHERE IsDefaultTemplate = 1;
```

---

## Data Structures

### DeviceExportContext

```csharp
public class DeviceExportContext {
    public CachedDevice Device { get; set; }
    public CachedNetwork Network { get; set; }
    public CachedOrganization Organization { get; set; }
    public Connection Connection { get; set; }
    public Dictionary<string, string> GlobalVariables { get; set; }
    public Template MatchedTemplate { get; set; }
}

public class CachedDevice {
    public int Id { get; set; }
    public int ConnectionId { get; set; }
    public string Serial { get; set; }
    public string Name { get; set; }
    public string Mac { get; set; }
    public string Model { get; set; }  // e.g., "MS225-48FP"
    public string Type { get; set; }   // e.g., "switch", "ap"
    public string IpAddress { get; set; }
    public string Status { get; set; }
    public string Firmware { get; set; }
    public List<string> Tags { get; set; }
    public int NetworkId { get; set; }
    public CachedNetwork Network { get; set; }
    public Connection Connection { get; set; }
    public DateTime SyncedAt { get; set; }
}
```

### ExportRequest

```csharp
public class ExportRequest {
    public int[] DeviceIds { get; set; }
    public int TemplateId { get; set; }  // Optional, will auto-match if not provided
    public int ConnectionId { get; set; }
    public ExportFormat Format { get; set; }  // PNG, SVG, PDF
    public int DpiLevel { get; set; }  // 96, 150, 300
    public BackgroundType Background { get; set; }  // white, transparent
    public LayoutType LayoutType { get; set; }  // grid, rows, columns (multi-device only)
    public PageSize PageSize { get; set; }  // letter, a4, custom
}

public enum ExportFormat { PNG, SVG, PDF }
public enum BackgroundType { White, Transparent }
public enum LayoutType { Grid, Rows, Columns }
public enum PageSize { Letter, A4, Legal, Custom }
```

---

## Algorithms

### Template Matching Algorithm

**Goal:** Find the best template for a device

**Algorithm:**
```
Function FindTemplateForDevice(device, user):
    availableTemplates = GetUserTemplates(user)

    // 1. Exact model match
    for template in availableTemplates:
        if template.ModelFilter contains device.model:
            return (template, confidence=1.0, reason="model_match")

    // 2. Device type match
    for template in availableTemplates:
        if template.TypeFilter contains device.type:
            return (template, confidence=0.8, reason="type_match")

    // 3. User's default template
    if user.DefaultTemplateId exists:
        return (GetTemplate(user.DefaultTemplateId), confidence=0.5, reason="user_default")

    // 4. System default template
    systemDefault = GetTemplate(where IsDefaultTemplate=true)
    if systemDefault exists:
        return (systemDefault, confidence=0.3, reason="system_default")

    // 5. Any available template
    if availableTemplates.count > 0:
        return (availableTemplates[0], confidence=0.1, reason="first_available")

    throw NoTemplateAvailableException()
```

**Complexity:** O(n) where n = number of device models/types across templates

**Optimization:** Cache matching results for 30 minutes

---

### Multi-Device Layout Algorithm

**Goal:** Position multiple device stickers on a page optimally

**Input:**
- devices: List of devices to export
- layout: 'grid' | 'rows' | 'columns'
- pageWidth: mm
- pageHeight: mm
- stickerWidth: mm
- stickerHeight: mm
- margin: mm

**Algorithm:**

```javascript
function calculateMultiDeviceLayout(devices, layout, pageSize, stickerSize, margin) {
    const pageWidth = pageSize.width - (margin * 2);
    const pageHeight = pageSize.height - (margin * 2);
    const positions = [];
    let currentPage = 0;

    if (layout === 'grid') {
        // Calculate grid dimensions
        const tilesPerRow = Math.floor(pageWidth / stickerSize.width);
        const tilesPerCol = Math.floor(pageHeight / stickerSize.height);
        const tilesPerPage = tilesPerRow * tilesPerCol;

        for (let i = 0; i < devices.length; i++) {
            const posInPage = i % tilesPerPage;
            const row = Math.floor(posInPage / tilesPerRow);
            const col = posInPage % tilesPerRow;
            currentPage = Math.floor(i / tilesPerPage);

            positions.push({
                deviceId: devices[i].id,
                page: currentPage,
                x: margin + (col * stickerSize.width),
                y: margin + (row * stickerSize.height),
                width: stickerSize.width,
                height: stickerSize.height
            });
        }
    }
    else if (layout === 'rows') {
        // One device per row
        const devicesPerPage = Math.floor(pageHeight / stickerSize.height);

        for (let i = 0; i < devices.length; i++) {
            const posInPage = i % devicesPerPage;
            currentPage = Math.floor(i / devicesPerPage);

            positions.push({
                deviceId: devices[i].id,
                page: currentPage,
                x: margin,
                y: margin + (posInPage * stickerSize.height),
                width: pageWidth,
                height: stickerSize.height
            });
        }
    }
    else if (layout === 'columns') {
        // One device per column
        const devicesPerPage = Math.floor(pageWidth / stickerSize.width);

        for (let i = 0; i < devices.length; i++) {
            const posInPage = i % devicesPerPage;
            currentPage = Math.floor(i / devicesPerPage);

            positions.push({
                deviceId: devices[i].id,
                page: currentPage,
                x: margin + (posInPage * stickerSize.width),
                y: margin,
                width: stickerSize.width,
                height: pageHeight
            });
        }
    }

    return {
        positions: positions,
        totalPages: Math.max(...positions.map(p => p.page)) + 1,
        devicesPerPage: pageSize.contains(stickerSize)
    };
}
```

**Complexity:** O(n) where n = number of devices

**Example Output:**
```json
{
  "positions": [
    { "deviceId": 123, "page": 0, "x": 10, "y": 10, "width": 100, "height": 50 },
    { "deviceId": 124, "page": 0, "x": 110, "y": 10, "width": 100, "height": 50 },
    { "deviceId": 125, "page": 0, "x": 210, "y": 10, "width": 100, "height": 50 },
    { "deviceId": 126, "page": 1, "x": 10, "y": 10, "width": 100, "height": 50 }
  ],
  "totalPages": 2,
  "devicesPerPage": 6
}
```

---

### QR Code Data Generation

**Challenge:** How to generate QR codes with device data on client side?

**Option A: Pre-generate on Server (Recommended)**
```csharp
// In ExportHelper.cs
public async Task<string> GenerateDeviceQrCodeAsync(CachedDevice device) {
    // Endpoint: /api/qrcode?q={device.serial}
    var qrUrl = $"/api/qrcode?q={Uri.EscapeDataString(device.Serial)}";
    return qrUrl;
}
```

**Option B: QR Code Library on Client**
```javascript
// Use qrcode.js library
import QRCode from 'qrcode';

async function generateQrCode(data) {
    return QRCode.toDataURL(data, {
        errorCorrectionLevel: 'H',
        type: 'image/png',
        quality: 0.95,
        margin: 1,
        width: 300
    });
}
```

**Recommendation:** Option A (server-generated) because:
- Leverages existing `/api/qrcode` endpoint
- No additional client library needed
- More robust

---

## Component Interactions

### Export Flow Diagram

```
┌─────────────────────┐
│  Devices Page       │
│  [📥 Export Btn]    │
└──────────┬──────────┘
           │
           ▼
┌─────────────────────────────────────────┐
│ Device Selected                         │
│ User clicks "Export"                    │
└──────────┬──────────────────────────────┘
           │
           ▼
┌─────────────────────────────────────────┐
│ GET /api/export/device/{id}             │
│ Server retrieves device + connection    │
│ data                                    │
└──────────┬──────────────────────────────┘
           │
           ▼
┌─────────────────────────────────────────┐
│ GET /api/templates/match                │
│ Find best template for device           │
└──────────┬──────────────────────────────┘
           │
           ▼
┌─────────────────────────────────────────┐
│ Modal Opens                             │
│ Shows device + template + preview       │
└──────────┬──────────────────────────────┘
           │
           ▼
┌─────────────────────────────────────────┐
│ User Confirms Export Settings           │
│ (Format, DPI, Background)               │
└──────────┬──────────────────────────────┘
           │
           ▼
┌─────────────────────────────────────────┐
│ Merge Device Data into Template         │
│ Replace {{bindings}} with actual values │
└──────────┬──────────────────────────────┘
           │
           ▼
┌─────────────────────────────────────────┐
│ Create Canvas + Render                  │
│ Apply export settings (DPI, background) │
└──────────┬──────────────────────────────┘
           │
           ▼
┌─────────────────────────────────────────┐
│ Export PNG/SVG/PDF                      │
│ Generate file                           │
└──────────┬──────────────────────────────┘
           │
           ▼
┌─────────────────────────────────────────┐
│ POST /api/export/history                │
│ Log export to database                  │
└──────────┬──────────────────────────────┘
           │
           ▼
┌─────────────────────────────────────────┐
│ Download File                           │
│ Browser downloads exported file         │
└─────────────────────────────────────────┘
```

---

## Error Handling

### Error Cases

| Scenario | Error | HTTP Code | Message |
|----------|-------|-----------|---------|
| Device not found | DeviceNotFoundException | 404 | "Device not found" |
| No permission | UnauthorizedAccessException | 403 | "You don't have permission to export this device" |
| No matching template | TemplateNotFoundException | 404 | "No template available for this device" |
| Invalid export format | InvalidOperationException | 400 | "Invalid export format" |
| Canvas rendering failed | RenderException | 500 | "Failed to render export" |
| File download failed | IOException | 500 | "Failed to create export file" |

### Error Recovery

```javascript
try {
    const data = await fetchDeviceExportData(deviceId);
    const template = await matchTemplate(deviceId);
    const preview = await renderPreview(data, template);
} catch (error) {
    if (error.status === 404) {
        // Device not found - show user message
        showError("Device no longer exists. Please refresh.");
    } else if (error.status === 403) {
        // No permission
        showError("You don't have permission to export this device.");
    } else {
        // Generic error
        showError("Export failed. Please try again.");
    }
}
```

---

## Performance Optimization

### Caching Strategy

```csharp
// Cache template matches for 30 minutes
public class TemplateMatchCache : IMemoryCache {
    private static Dictionary<string, CacheEntry> _cache = new();
    private const int CACHE_DURATION_MINUTES = 30;

    public CacheEntry Get(string key) {
        if (_cache.TryGetValue(key, out var entry)) {
            if (DateTime.UtcNow < entry.ExpiresAt) {
                return entry;
            }
            _cache.Remove(key);
        }
        return null;
    }

    public void Set(string key, Template template) {
        _cache[key] = new CacheEntry {
            Template = template,
            ExpiresAt = DateTime.UtcNow.AddMinutes(CACHE_DURATION_MINUTES)
        };
    }
}

// Usage
var cacheKey = $"template_match_{deviceId}_{userId}";
if (cache.Get(cacheKey) is var cached && cached != null) {
    return cached;
}
var matched = await MatchTemplate(deviceId, userId);
cache.Set(cacheKey, matched);
return matched;
```

### Query Optimization

```csharp
// Eager load related data
var device = await _db.CachedDevices
    .Include(d => d.Network)
    .Include(d => d.Connection)
    .Where(d => d.Id == deviceId && d.Connection.UserId == userId)
    .FirstOrDefaultAsync();
```

### Batch Export Progress

```javascript
async function exportMultipleDevices(deviceIds, options) {
    let completed = 0;
    const total = deviceIds.length;

    for (const deviceId of deviceIds) {
        updateProgress(completed, total);

        await exportDevice(deviceId, options);

        completed++;
    }

    updateProgress(total, total);
}

function updateProgress(current, total) {
    const percent = Math.round((current / total) * 100);
    document.querySelector('#progress-bar').style.width = percent + '%';
    document.querySelector('#progress-text').textContent = `${current}/${total}`;
}
```

---

**Document Version:** 1.0
**Author:** Claude
**Last Updated:** 2025-10-22
