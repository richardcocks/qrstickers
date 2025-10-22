# Phase 6: Custom Image Upload System

**Status:** 📋 Planning
**Date:** 2025-10-22
**Epic:** Phase 6 Custom Assets
**Related:** Phase 5.7 (QR Code System), Phase 5 (Device Export)

---

## Overview

Phase 6 introduces a custom image upload system that allows users to upload logos, icons, and graphics to their connection scope for use in sticker templates. This feature complements the existing QR code system by enabling branding and custom visual elements.

### Key Goals

- 🎨 **Custom Branding** - Upload company logos, department icons, or custom graphics
- 🔗 **Connection-Scoped Assets** - Images tied to specific Meraki connections
- 📏 **Size-Optimized** - Realistic limits for sticker use cases (900×900px, 2MB)
- 🗑️ **Safe Deletion** - Transparent placeholder fallback when images deleted
- 💾 **Data URI Storage** - Consistent with QR code architecture
- 🎯 **Designer Integration** - Seamless drag-and-drop in template designer

---

## Problem Statement

### Current Limitations

**Issue 1: No Custom Graphics Support**

**Problem:**
- Templates can only use text fields and QR codes
- Users cannot add company logos or custom branding
- No way to include department icons, floor plan snippets, or visual identifiers
- Forces users to create logos as external images and manually overlay (outside system)

**Impact:** Limited customization, unprofessional appearance, reduced user adoption

---

**Issue 2: Inconsistent Branding Across Devices**

**Problem:**
- Users manually add logos to each template
- No centralized logo/asset management
- Updating company logo requires editing all templates individually
- Different teams may use different logo versions

**Impact:** Brand inconsistency, maintenance overhead, duplicate work

---

**Issue 3: QR Code System Not Extensible to Images**

**Problem:**
- QR codes use `device.QRCode` data binding
- No equivalent system for custom uploaded images
- `{{device.companyLogo}}` placeholder has no data source

**Impact:** Users expect image upload feature after seeing QR code generation

---

## Solution Architecture

### 1. Data URI Storage Pattern

**Pattern:** Store images as base64-encoded data URIs in database (matches QR code system)

**Rationale:**
- ✅ Consistent with existing QR code implementation (`CachedDevice.QRCodeDataUri`)
- ✅ No file system complexity (cloud-friendly)
- ✅ Transactional integrity with database
- ✅ Simple cascade delete (no orphaned files)
- ✅ Works in containerized environments (Docker, Azure Container Apps)

**Data URI Format:**
```
data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAA...
```

**Size Considerations:**
- Base64 encoding adds 33% overhead (1.5 MB file → 2 MB data URI)
- SQL Server NVARCHAR(MAX) supports up to 2 GB
- Practical limit: 2 MB per image → ~2.7 MB in database

---

### 2. Connection-Scoped Assets

**Pattern:** Images belong to Connection (not User, not System)

**Scoping Decision:**

| Scope | Use Case | Phase 6? |
|-------|----------|----------|
| **Connection** | Company logo for "Production Meraki" connection | ✅ Yes (MVP) |
| User | Personal images shared across all connections | ❌ No (Phase 6.2) |
| System | Pre-installed stock icons for all users | ❌ No (Phase 7) |

**Rationale:**
- Templates are already connection-scoped (`StickerTemplate.ConnectionId`)
- Logos are logically tied to specific organizations/integrations
- Simpler permission model (user owns connection → user owns images)
- Future-proof: Can add User/System scopes in later phases

---

### 3. Realistic Size Limits

**Sticker Math:**
- Typical sticker: 100mm × 50mm
- At 300 DPI: 1,181 px × 591 px
- Logo occupies 20-40% of sticker space
- Typical logo: 30mm wide = 354 px at 300 DPI

**Limits:**

| Property | Limit | Rationale |
|----------|-------|-----------|
| **Max Dimensions** | 900 × 900 px | Covers 76mm at 300 DPI (larger than typical logos) |
| **Max File Size** | 2 MB | PNG with transparency: 50-400 KB typical, 2 MB worst case |
| **Images Per Connection** | 25 images | Realistic for org logos + department icons |
| **Total Storage** | 20 MB per connection | 25 images × 800 KB avg = 20 MB |

**Validation:**
- ✅ Client-side: File size, dimensions, MIME type
- ✅ Server-side: Re-validate all checks + quota enforcement
- ✅ Supported formats: PNG, JPEG, WebP, SVG

---

### 4. Deletion Strategy: Transparent Placeholder

**Problem:** Deleting an image breaks templates that reference it

**Options Considered:**

| Strategy | Pros | Cons | Verdict |
|----------|------|------|---------|
| **A) Transparent Placeholder** | No broken templates, minimal code | Image "disappears" from template | ✅ **CHOSEN** |
| B) Reference Counting | Prevents deletion if in use | Complex parsing, doesn't handle clones | ❌ Overkill |
| C) Leave Broken Reference | Simple | Templates break, users confused | ❌ Poor UX |

**Implementation:**

When image deleted:
1. Replace `DataUri` with 1×1 transparent PNG in database
2. Preserve original `WidthPx` and `HeightPx` for layout
3. Mark with `IsDeleted = true` flag
4. Designer shows empty box (Fabric.js placeholder)
5. Export renders transparent area (no visual artifact)

**Transparent PNG Data URI (1×1 px):**
```
data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==
```

**Benefits:**
- ✅ Templates remain valid (no 404 errors)
- ✅ Layout preserved (dimensions maintained)
- ✅ Re-uploading same-named image restores appearance
- ✅ No complex reference tracking needed

---

## Database Schema

### New Table: `UploadedImages`

```csharp
public class UploadedImage
{
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// Foreign key to Connection (NULL not allowed - connection-scoped only)
    /// </summary>
    [Required]
    public int ConnectionId { get; set; }

    /// <summary>
    /// User-friendly name for the image (e.g., "CompanyLogo", "FloorPlanB2")
    /// Used in data binding: {{customImage.CompanyLogo}}
    /// </summary>
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = null!;

    /// <summary>
    /// Optional description
    /// </summary>
    [MaxLength(1000)]
    public string? Description { get; set; }

    /// <summary>
    /// Image data URI (data:image/png;base64,... or data:image/jpeg;base64,...)
    /// Set to transparent 1×1 PNG on deletion
    /// </summary>
    [Required]
    public string DataUri { get; set; } = null!;  // NVARCHAR(MAX)

    /// <summary>
    /// Original width in pixels (for aspect ratio preservation)
    /// </summary>
    [Required]
    public int WidthPx { get; set; }

    /// <summary>
    /// Original height in pixels (for aspect ratio preservation)
    /// </summary>
    [Required]
    public int HeightPx { get; set; }

    /// <summary>
    /// MIME type (image/png, image/jpeg, image/webp, image/svg+xml)
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string MimeType { get; set; } = null!;

    /// <summary>
    /// File size in bytes (for quota management)
    /// Includes base64 overhead (~33% larger than original file)
    /// </summary>
    [Required]
    public long FileSizeBytes { get; set; }

    /// <summary>
    /// Soft delete flag - true if image deleted but referenced by templates
    /// </summary>
    public bool IsDeleted { get; set; } = false;

    /// <summary>
    /// Upload timestamp
    /// </summary>
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last time this image was used in an export (for orphan detection)
    /// </summary>
    public DateTime? LastUsedAt { get; set; }

    // Navigation property
    public Connection Connection { get; set; } = null!;
}
```

### Indexes

```csharp
// DbContext configuration
modelBuilder.Entity<UploadedImage>()
    .HasIndex(i => i.ConnectionId)
    .HasDatabaseName("IX_UploadedImages_ConnectionId");

modelBuilder.Entity<UploadedImage>()
    .HasIndex(i => new { i.ConnectionId, i.Name })
    .IsUnique()
    .HasDatabaseName("IX_UploadedImages_ConnectionId_Name");

// Foreign key with cascade delete
modelBuilder.Entity<UploadedImage>()
    .HasOne(i => i.Connection)
    .WithMany()
    .HasForeignKey(i => i.ConnectionId)
    .OnDelete(DeleteBehavior.Cascade);
```

### Migration

```bash
dotnet ef migrations add AddUploadedImagesTable
```

**Migration Name:** `20251022_AddUploadedImagesTable.cs`

---

## API Design

### 1. Upload Image

**Endpoint:** `POST /api/images/upload`

**Authentication:** Required (user must own connection)

**Request Body:**
```json
{
  "connectionId": 123,
  "name": "CompanyLogo",
  "description": "Acme Corp primary logo",
  "dataUri": "data:image/png;base64,iVBORw0KGgo...",
  "widthPx": 800,
  "heightPx": 600
}
```

**Validation:**
- ✅ User owns connection
- ✅ Name unique within connection
- ✅ Name matches `^[A-Za-z0-9_-]+$` (alphanumeric + underscore/dash)
- ✅ Dimensions ≤ 900×900 px
- ✅ File size ≤ 2 MB (base64 encoded)
- ✅ MIME type in whitelist (PNG, JPEG, WebP, SVG)
- ✅ Connection hasn't exceeded quota (25 images, 20 MB total)

**Response (200):**
```json
{
  "success": true,
  "data": {
    "id": 456,
    "name": "CompanyLogo",
    "dataUri": "data:image/png;base64,...",
    "widthPx": 800,
    "heightPx": 600,
    "mimeType": "image/png",
    "fileSizeBytes": 245000,
    "uploadedAt": "2025-10-22T14:30:00Z"
  }
}
```

**Error Cases:**
- `400 Bad Request` - Validation failed (size, dimensions, format, name conflict)
- `401 Unauthorized` - Not logged in
- `403 Forbidden` - User doesn't own connection
- `413 Payload Too Large` - Image exceeds 2 MB limit
- `429 Too Many Requests` - Quota exceeded (25 images or 20 MB)

---

### 2. List Images

**Endpoint:** `GET /api/images?connectionId={id}`

**Authentication:** Required

**Query Parameters:**
- `connectionId` (required) - Connection ID to list images for
- `includeDeleted` (optional, default: false) - Include soft-deleted images

**Response (200):**
```json
{
  "success": true,
  "data": {
    "images": [
      {
        "id": 456,
        "name": "CompanyLogo",
        "description": "Acme Corp primary logo",
        "dataUri": "data:image/png;base64,...",
        "widthPx": 800,
        "heightPx": 600,
        "mimeType": "image/png",
        "fileSizeBytes": 245000,
        "isDeleted": false,
        "uploadedAt": "2025-10-22T14:30:00Z",
        "lastUsedAt": "2025-10-22T15:45:00Z"
      }
    ],
    "quota": {
      "imagesUsed": 5,
      "imagesLimit": 25,
      "storageUsed": 1250000,
      "storageLimit": 20000000
    }
  }
}
```

---

### 3. Delete Image

**Endpoint:** `DELETE /api/images/{id}`

**Authentication:** Required

**Behavior:**
1. Soft delete: Set `IsDeleted = true`
2. Replace `DataUri` with transparent 1×1 PNG
3. Keep `WidthPx`, `HeightPx`, `Name` for layout preservation
4. Decrement quota counters

**Response (200):**
```json
{
  "success": true,
  "message": "Image deleted. Templates using this image will show a transparent placeholder."
}
```

**Future Enhancement (Phase 6.3):** Add `GET /api/images/{id}/usages` endpoint to find templates referencing image

---

### 4. Update Image Metadata

**Endpoint:** `PATCH /api/images/{id}`

**Authentication:** Required

**Request Body:**
```json
{
  "name": "UpdatedLogoName",
  "description": "New description"
}
```

**Note:** Cannot update `DataUri` (must delete and re-upload)

---

## UI/UX Design

### 1. Image Management Page

**Route:** `/Images/Index?connectionId={id}`

**Layout:**

```
┌─────────────────────────────────────────────────┐
│ My Images - Production Meraki                   │
│                                                 │
│ [+ Upload Image]   Quota: 5/25 images (3.2 MB) │
├─────────────────────────────────────────────────┤
│ ┌─────────────────┐ ┌─────────────────┐        │
│ │   Thumbnail     │ │   Thumbnail     │        │
│ │ [Preview Image] │ │ [Preview Image] │        │
│ ├─────────────────┤ ├─────────────────┤        │
│ │ CompanyLogo     │ │ DeptIconIT      │        │
│ │ 800×600 px      │ │ 512×512 px      │        │
│ │ 245 KB          │ │ 128 KB          │        │
│ │ Last used: 2h   │ │ Last used: 30d  │        │
│ │ [Edit] [Delete] │ │ [Edit] [Delete] │        │
│ └─────────────────┘ └─────────────────┘        │
└─────────────────────────────────────────────────┘
```

**Features:**
- Grid layout with image previews (thumbnails scaled to 150×150 px)
- Name, dimensions, file size, last used date
- Upload modal with file picker + name/description inputs
- Edit modal (name and description only)
- Delete button with confirmation warning
- Quota display at top
- Filter: "Show deleted images" toggle

---

### 2. Upload Modal

```
┌─────────────────────────────────────────┐
│ Upload Image                        [×] │
├─────────────────────────────────────────┤
│ Connection: Production Meraki           │
│                                         │
│ Image Name: *                           │
│ [CompanyLogo_____________]              │
│ (Used in templates as                   │
│  {{customImage.CompanyLogo}})           │
│                                         │
│ Description:                            │
│ [Optional description___________]       │
│                                         │
│ File: *                                 │
│ [Choose File] No file chosen            │
│                                         │
│ Limits:                                 │
│ • Max 900×900 px                        │
│ • Max 2 MB                              │
│ • PNG, JPEG, WebP, SVG                  │
│                                         │
│ [Cancel]              [Upload Image]    │
└─────────────────────────────────────────┘
```

**Validation:**
- Real-time file size check
- Preview thumbnail after file selected
- Dimensions displayed after file loaded
- Error messages inline (red text)

**Security Note:**
- ⚠️ Original filename from file picker (`file.name`) is **NEVER** used or displayed
- ✅ User must provide separate "Image Name" in text input
- ✅ Prevents XSS attacks via malicious filenames (e.g., `<script>alert('xss')</script>.png`)

---

### 3. Template Designer Integration

**Location:** `/Templates/Designer/{id}` left panel

**New Button:** "Add Custom Image" (below "Add QR Code")

```
┌─────────────────────┐
│ Add Elements        │
├─────────────────────┤
│ [Add Text Field]    │
│ [Add QR Code]       │
│ [Add Custom Image]  │ ← NEW
│ [Add Rectangle]     │
│ [Add Circle]        │
└─────────────────────┘
```

**Click Behavior:**
1. Opens dropdown showing all uploaded images for this connection
2. User selects image
3. Adds Fabric.js image object to canvas with:
   - `src`: Image data URI
   - `properties.dataSource`: `"customImage.CompanyLogo"`
   - Default size: Original dimensions scaled to fit 20mm × 20mm at current zoom

**Properties Panel:**
- Show image name
- Show dimensions (width/height in mm)
- "Replace Image" button → reopens dropdown
- Standard Fabric.js transforms (move, scale, rotate)

**Validation (Max 4 Custom Images):**

Templates are limited to **4 custom images** to:
- Prevent cluttered, unprofessional designs
- Avoid performance issues with Fabric.js rendering
- Enforce focused sticker design (100mm × 50mm has limited space)

**Client-Side Validation (JavaScript):**
```javascript
// Before adding image to canvas
function addCustomImageToCanvas(imageData) {
    const canvas = fabricCanvas; // Current Fabric.js canvas

    // Count existing custom images
    const customImageCount = canvas.getObjects().filter(obj =>
        obj.type === 'image' &&
        obj.properties?.dataSource?.startsWith('customImage.')
    ).length;

    if (customImageCount >= 4) {
        showNotification('Maximum 4 custom images per template', 'error');
        return;
    }

    // Add image to canvas...
    fabric.Image.fromURL(imageData.dataUri, function(img) {
        img.set({
            properties: {
                dataSource: `customImage.${imageData.name}`
            }
        });
        canvas.add(img);
    });
}
```

**Server-Side Validation (C#):**
```csharp
// On template save
public async Task<IActionResult> SaveTemplate(SaveTemplateRequest request)
{
    // Parse TemplateJson
    var templateJson = JsonSerializer.Deserialize<TemplateData>(request.TemplateJson);

    // Count custom images
    var customImageCount = templateJson.Objects
        .Count(obj => obj.Type == "image" &&
                     obj.Properties?.DataSource?.StartsWith("customImage.") == true);

    if (customImageCount > 4)
        return BadRequest("Template cannot contain more than 4 custom images");

    // Save template...
}
```

---

### 4. Data Binding Convention

**Pattern:** `customImage.{ImageName}` (normalized to lowercase)

**Examples:**
- `{{customImage.CompanyLogo}}` → User uploaded "CompanyLogo"
- `{{customImage.DeptIconIT}}` → User uploaded "DeptIconIT"
- `{{customImage.FloorPlanB2}}` → User uploaded "FloorPlanB2"

**Comparison with QR Codes:**
- QR codes: `device.qrcode`, `network.qrcode`, `organization.qrcode` (entity data)
- Custom images: `customImage.CompanyLogo`, `customImage.DeptIcon` (connection assets)

**Export Preview Mapping:**
```javascript
// In device-export.js createDeviceDataMap()
deviceDataMap['customimage.companylogo'] = uploadedImages['CompanyLogo'].dataUri;
deviceDataMap['customimage.depticonit'] = uploadedImages['DeptIconIT'].dataUri;
```

---

## Validation Strategy

### Client-Side Validation (JavaScript)

```javascript
/**
 * Validates image upload before sending to server
 */
async function validateImageUpload(file) {
    const MAX_DIMENSION = 900;
    const MAX_FILE_SIZE = 2 * 1024 * 1024; // 2 MB
    const ALLOWED_TYPES = ['image/png', 'image/jpeg', 'image/webp', 'image/svg+xml'];

    // Check MIME type
    if (!ALLOWED_TYPES.includes(file.type)) {
        return {
            valid: false,
            error: 'Unsupported format. Use PNG, JPEG, WebP, or SVG.'
        };
    }

    // Check file size
    if (file.size > MAX_FILE_SIZE) {
        return {
            valid: false,
            error: `File too large (${(file.size / 1024 / 1024).toFixed(2)} MB). Max 2 MB.`
        };
    }

    // Load image to check dimensions
    const img = new Image();
    const url = URL.createObjectURL(file);
    img.src = url;

    try {
        await img.decode();
        URL.revokeObjectURL(url);

        if (img.width > MAX_DIMENSION || img.height > MAX_DIMENSION) {
            return {
                valid: false,
                error: `Image too large (${img.width}×${img.height}px). Max ${MAX_DIMENSION}×${MAX_DIMENSION}px.`
            };
        }

        return {
            valid: true,
            width: img.width,
            height: img.height,
            mimeType: file.type
        };
    } catch (error) {
        return {
            valid: false,
            error: 'Failed to load image. File may be corrupted.'
        };
    }
}

/**
 * Converts file to data URI
 */
function fileToDataUri(file) {
    return new Promise((resolve, reject) => {
        const reader = new FileReader();
        reader.onloadend = () => resolve(reader.result);
        reader.onerror = reject;
        reader.readAsDataURL(file);
    });
}
```

---

### Server-Side Validation (C#)

```csharp
public class ImageUploadValidator
{
    private const int MAX_DIMENSION = 900;
    private const long MAX_FILE_SIZE = 2_000_000; // 2 MB (pre-base64)
    private const long MAX_BASE64_SIZE = 2_700_000; // 2.7 MB (with base64 overhead)
    private const int MAX_IMAGES_PER_CONNECTION = 25;
    private const long MAX_TOTAL_STORAGE = 20_000_000; // 20 MB

    private static readonly string[] AllowedMimeTypes = new[]
    {
        "image/png",
        "image/jpeg",
        "image/webp",
        "image/svg+xml"
    };

    public async Task<ValidationResult> ValidateUpload(
        UploadImageRequest request,
        string userId)
    {
        // 1. Validate user owns connection
        var connection = await _db.Connections
            .FirstOrDefaultAsync(c => c.Id == request.ConnectionId && c.UserId == userId);

        if (connection == null)
            return ValidationResult.Fail("Connection not found or access denied");

        // 2. Validate MIME type from data URI
        if (!request.DataUri.StartsWith("data:image/"))
            return ValidationResult.Fail("Invalid image format");

        var mimeType = ExtractMimeType(request.DataUri);
        if (!AllowedMimeTypes.Contains(mimeType))
            return ValidationResult.Fail($"Unsupported MIME type: {mimeType}");

        // 3. Validate dimensions
        if (request.WidthPx > MAX_DIMENSION || request.HeightPx > MAX_DIMENSION)
            return ValidationResult.Fail($"Image dimensions too large (max {MAX_DIMENSION}×{MAX_DIMENSION}px)");

        if (request.WidthPx <= 0 || request.HeightPx <= 0)
            return ValidationResult.Fail("Invalid dimensions");

        // 4. Validate file size
        var sizeBytes = Encoding.UTF8.GetByteCount(request.DataUri);
        if (sizeBytes > MAX_BASE64_SIZE)
            return ValidationResult.Fail("Image too large (max 2 MB)");

        // 5. Validate name
        if (string.IsNullOrWhiteSpace(request.Name))
            return ValidationResult.Fail("Image name is required");

        if (!Regex.IsMatch(request.Name, @"^[A-Za-z0-9_-]+$"))
            return ValidationResult.Fail("Image name can only contain letters, numbers, underscores, and dashes");

        // 6. Check for name conflicts
        var existingWithName = await _db.UploadedImages
            .AnyAsync(i => i.ConnectionId == request.ConnectionId
                        && i.Name == request.Name
                        && !i.IsDeleted);

        if (existingWithName)
            return ValidationResult.Fail($"Image name '{request.Name}' already exists");

        // 7. Check quota: image count
        var imageCount = await _db.UploadedImages
            .CountAsync(i => i.ConnectionId == request.ConnectionId && !i.IsDeleted);

        if (imageCount >= MAX_IMAGES_PER_CONNECTION)
            return ValidationResult.Fail($"Image limit reached ({MAX_IMAGES_PER_CONNECTION} images per connection)");

        // 8. Check quota: total storage
        var totalStorage = await _db.UploadedImages
            .Where(i => i.ConnectionId == request.ConnectionId && !i.IsDeleted)
            .SumAsync(i => i.FileSizeBytes);

        if (totalStorage + sizeBytes > MAX_TOTAL_STORAGE)
        {
            var available = (MAX_TOTAL_STORAGE - totalStorage) / 1024 / 1024;
            return ValidationResult.Fail($"Storage quota exceeded (max 20 MB). Available: {available:F2} MB");
        }

        return ValidationResult.Success();
    }

    private string ExtractMimeType(string dataUri)
    {
        // data:image/png;base64,... → image/png
        var match = Regex.Match(dataUri, @"^data:([^;]+);");
        return match.Success ? match.Groups[1].Value : string.Empty;
    }
}
```

---

## Orphaned Image Management

### Definition: When is an Image "Orphaned"?

An image is considered orphaned if:
1. **Not referenced by any template** (no `properties.dataSource = "customImage.{name}"`)
2. **Last used > 90 days ago** (via `LastUsedAt` field)
3. **Template was deleted** but image still exists

### Phase 6.1 Approach: Soft Warnings

**Features:**
- ✅ Display "Last Used" date in image list
- ✅ Highlight images unused for 90+ days with warning badge
- ✅ Show "Never Used" badge for newly uploaded images
- ✅ Allow manual deletion of any image
- ❌ **NO automatic deletion** (too risky for MVP)

**UI Indicators:**
```
┌─────────────────┐
│   Thumbnail     │
│ [Preview Image] │
├─────────────────┤
│ OldDeptIcon     │
│ 512×512 px      │
│ 128 KB          │
│ ⚠️ Not used (120d) │ ← Warning badge (orange)
│ [Edit] [Delete] │
└─────────────────┘
```

### Phase 6.2 Enhancement: Usage Tracking

**Track Last Used:**
```csharp
// In DeviceExportHelper.GetDeviceExportDataAsync()
foreach (var image in referencedImages)
{
    image.LastUsedAt = DateTime.UtcNow;
}
await _db.SaveChangesAsync();
```

### Phase 6.3 Enhancement: Find Usages

**Endpoint:** `GET /api/images/{id}/usages`

**Response:**
```json
{
  "success": true,
  "data": {
    "imageId": 456,
    "imageName": "CompanyLogo",
    "usages": [
      {
        "templateId": 789,
        "templateName": "Device Sticker - Switch",
        "connectionId": 123,
        "lastExportedAt": "2025-10-22T15:30:00Z"
      }
    ]
  }
}
```

**Implementation:**
- Scan all `StickerTemplate.TemplateJson` for `properties.dataSource` containing `customImage.{name}`
- Cache results for 5 minutes
- Display in UI: "Used by 3 templates" with links

---

## Security Considerations

### 1. Authorization

**Checks:**
- ✅ User must be authenticated (`[Authorize]` attribute)
- ✅ User must own the connection (via `Connection.UserId`)
- ✅ No cross-connection access (strict foreign key validation)
- ✅ No public image gallery (connection-scoped only)

### 2. Input Validation

**XSS Prevention (Defense in Depth):**

**Layer 1: Ignore Original Filename**
- ✅ Original `file.name` from upload is **NEVER** used in any output
- ✅ User provides separate "Image Name" via text input field
- ✅ Data URI contains no filename reference
- ✅ Prevents attacks like: `<script>alert('xss')</script>.png`

**Example (Upload Modal):**
```html
<!-- SAFE: Separate text input (NOT using file.name) -->
<label>Image Name: *</label>
<input type="text" id="imageNameInput" placeholder="CompanyLogo" />

<label>File: *</label>
<input type="file" id="fileInput" accept="image/png,image/jpeg,image/webp,image/svg+xml" />
<!-- file.name is NEVER displayed or stored -->
```

**Layer 2: Input Validation (Whitelist)**
- ✅ Name validated with regex: `^[A-Za-z0-9_-]+$`
- ✅ Rejects special characters, HTML tags, JavaScript syntax
- ✅ Server re-validates (never trust client-side validation alone)
- ✅ Max length: 200 characters

**Example:**
```javascript
// Client-side validation
function validateImageName(name) {
    if (!/^[A-Za-z0-9_-]+$/.test(name)) {
        return { valid: false, error: 'Name can only contain letters, numbers, underscores, and dashes' };
    }
    return { valid: true };
}

// Server-side validation (same check)
if (!Regex.IsMatch(request.Name, @"^[A-Za-z0-9_-]+$"))
    return BadRequest("Invalid image name format");
```

**Layer 3: Output Encoding**
- ✅ **Razor:** `@Model.Name` (HTML-encoded by default)
- ✅ **JavaScript:** `element.textContent = name` (safe)
- ✅ **JavaScript:** `element.innerText = name` (safe)
- ❌ **JavaScript:** `element.innerHTML = name` (UNSAFE - never use)
- ✅ **JSON API:** ASP.NET serializer handles escaping

**Example (Safe Output):**
```csharp
<!-- Razor (auto-escaped) -->
<td>@image.Name</td>
<div title="@image.Description"></div>

<script>
// JavaScript (safe methods)
document.getElementById('imageName').textContent = image.name;  // SAFE
document.getElementById('imageDesc').innerText = image.description;  // SAFE

// NEVER use:
// element.innerHTML = image.name;  // UNSAFE - XSS risk
</script>
```

**Other Threats:**
- ❌ SQL injection → Mitigated by EF Core parameterized queries
- ❌ Path traversal → Not applicable (no file system)
- ❌ Malicious file upload → Mitigated by MIME type whitelist + dimension limits
- ❌ MIME type spoofing → Client sends MIME type, server re-validates from data URI prefix

### 3. Rate Limiting

**Recommended:**
- Upload endpoint: 10 requests/minute per user
- List endpoint: 60 requests/minute per user
- Delete endpoint: 30 requests/minute per user

**Implementation:**
```csharp
[RateLimit(
    PermitLimit = 10,
    Window = 60,
    SegmentedBy = "User"
)]
public async Task<IActionResult> UploadImage(...)
```

### 4. Data Privacy

**Concerns:**
- ✅ Images are user-uploaded (copyright responsibility with user)
- ✅ No image scanning/analysis (privacy-friendly)
- ✅ Images deleted with connection (cascade delete)
- ✅ No cross-tenant data leakage (strict connection scoping)

**Terms of Service Recommendation:**
- Users certify they have rights to uploaded images
- Users responsible for copyright compliance
- System does not claim ownership of uploaded content

---

## Implementation Phases

### Phase 6.1: Core Image Upload (MVP)
**Status:** 📋 Planned
**Effort:** 6-8 hours
**Priority:** High

**Deliverables:**
1. ✅ Database migration: `UploadedImages` table with indexes
2. ✅ API endpoints: Upload, List, Delete
3. ✅ Image management page: `/Images/Index?connectionId={id}`
4. ✅ Upload modal with validation
5. ✅ Client-side + server-side validation (size, MIME, dimensions, quota)
6. ✅ Transparent placeholder on deletion
7. ✅ Unit tests for validation logic
8. ✅ Documentation: API specs and usage guide

**Success Criteria:**
- User can upload PNG logo (500 KB, 800×600 px)
- Upload fails if dimensions > 900×900 px
- Upload fails if connection has 25 images already
- Deleting image replaces with transparent placeholder
- Image list shows quota usage

---

### Phase 6.2: Designer Integration
**Status:** 📋 Planned
**Effort:** 4-6 hours
**Priority:** High

**Deliverables:**
1. ✅ "Add Custom Image" button in designer left panel
2. ✅ Dropdown showing available images for connection
3. ✅ Data binding: `customImage.{name}` convention
4. ✅ Export preview support: Map `customImage.*` to data URIs
5. ✅ Properties panel: Show image name, dimensions, "Replace Image" button
6. ✅ `LastUsedAt` tracking on export
7. ✅ **Limit templates to max 4 custom images** (validation on add + save)
8. ✅ Integration tests: Upload → Add to template → Export

**Success Criteria:**
- User can add "CompanyLogo" to template in designer
- Preview shows actual logo (not placeholder)
- Export PDF includes logo at correct position/size
- Re-opening template loads logo correctly
- Deleting logo shows transparent box in designer
- Adding 5th custom image shows error: "Maximum 4 custom images per template"

---

### Phase 6.3: Orphan Management (Optional)
**Status:** 📋 Future
**Effort:** 3-4 hours
**Priority:** Low

**Deliverables:**
1. ✅ "Last Used" badges in image list (90+ days warning)
2. ✅ "Find Usages" button → lists templates using image
3. ✅ Bulk delete UI for unused images
4. ✅ Background job: Monthly email notification of unused images (optional)
5. ✅ "Safe Delete" mode: Prevents deletion if in use (Phase 6.3.1)

**Success Criteria:**
- Image unused for 120 days shows orange warning badge
- "Find Usages" scans all templates in <2 seconds
- User can bulk-select and delete 5 unused images at once

---

## Testing Strategy

### Unit Tests

**Validation Logic:**
- ✅ Image size validation (pass: 1.5 MB, fail: 2.5 MB)
- ✅ Dimension validation (pass: 800×600, fail: 1000×1000)
- ✅ MIME type validation (pass: PNG/JPEG, fail: GIF/BMP)
- ✅ Name validation (pass: "CompanyLogo", fail: "Company Logo!@#")
- ✅ Quota enforcement (pass: 24 images, fail: 25 images)
- ✅ Template image limit (pass: 4 images, fail: 5 images)

**Data URI Handling:**
- ✅ Base64 encoding/decoding
- ✅ MIME type extraction
- ✅ Transparent placeholder generation

---

### Integration Tests

**Upload Flow:**
1. Upload valid PNG (500 KB, 800×600) → Success
2. Upload oversized PNG (2.5 MB) → Fail with 413
3. Upload invalid MIME (GIF) → Fail with 400
4. Upload with duplicate name → Fail with 400
5. Upload as unauthorized user → Fail with 401

**Designer Integration:**
1. Add image to template → Saves correctly
2. Export template with image → PDF includes image
3. Delete image → Template shows transparent box
4. Re-upload image with same name → Template restores appearance

---

### Manual Testing Checklist

**Phase 6.1:**
- [ ] Upload 800×600 PNG logo (valid)
- [ ] Upload 1000×1000 PNG (fail dimension check)
- [ ] Upload 3 MB JPEG (fail size check)
- [ ] Upload GIF file (fail MIME check)
- [ ] Upload 25th image (success)
- [ ] Upload 26th image (fail quota)
- [ ] Delete image → Verify transparent placeholder
- [ ] List images → Verify quota display

**Phase 6.2:**
- [ ] Open designer → Click "Add Custom Image"
- [ ] Select uploaded logo → Appears on canvas
- [ ] Move/scale logo → Saves correctly
- [ ] Add 4 custom images to template (success)
- [ ] Try to add 5th custom image → Error: "Maximum 4 custom images per template"
- [ ] Export device → PDF includes logo
- [ ] Re-open template → Logo loads correctly
- [ ] Delete logo from Images page → Designer shows transparent box

---

## Risk Analysis

| Risk | Likelihood | Impact | Mitigation |
|------|-----------|--------|------------|
| **SQL Server NVARCHAR(MAX) limits** | Low | High | Enforce 2 MB per-image + 20 MB per-connection limits |
| **Memory issues with large images** | Low | High | Client-side dimension validation (900×900 max) |
| **Users upload copyrighted images** | Medium | Legal | Terms of service disclaimer + user responsibility |
| **Accidental deletion breaks templates** | High | Medium | Transparent placeholder fallback + "Last Used" warnings |
| **Base64 overhead (33% size increase)** | Low | Low | Acceptable (2 MB file → 2.7 MB in DB) |
| **Image management page loads slowly** | Low | Low | Grid layout with 25 thumbnails (150×150px each) - trivial performance |
| **Template canvas cluttered with images** | Low | Low | **Limit templates to max 4 custom images** - enforces professional design, prevents performance issues |
| **Orphaned images accumulate** | Medium | Low | Phase 6.3 adds "Last Used" tracking + cleanup UI |

---

## Success Metrics

### Functional Completeness
- ✅ Users can upload PNG/JPEG logos up to 900×900 px
- ✅ Images stored as data URIs in database
- ✅ Designer integrates custom images with drag-and-drop
- ✅ Export includes custom images at correct position/size
- ✅ Deletion uses transparent placeholder (no broken templates)
- ✅ Quota enforced (25 images, 20 MB per connection)

### User Experience
- ✅ Upload takes <2 seconds for 500 KB image
- ✅ Image list loads in <1 second for 25 images
- ✅ Designer adds image to canvas in <500ms
- ✅ Export with 5 custom images completes in <5 seconds
- ✅ Clear error messages for validation failures

### Quality
- ✅ No breaking changes to existing templates
- ✅ Backward compatible (templates without images unaffected)
- ✅ All validation unit tests pass
- ✅ No SQL injection or XSS vulnerabilities
- ✅ No memory leaks in Fabric.js rendering

---

## Database Impact

**Schema Changes:**
- New table: `UploadedImages` with 5 indexes
- Foreign key: `UploadedImages.ConnectionId` → `Connections.Id` (cascade delete)

**Data Size Estimation:**
- 10 users × 2 connections × 10 images × 500 KB avg = **100 MB**
- 100 users × 2 connections × 10 images × 500 KB avg = **1 GB**
- Acceptable for SQL Server (Azure SQL free tier: 32 GB)

**Performance Impact:**
- Minimal (data URIs loaded on-demand, not on every page load)
- Indexes on `ConnectionId` and `ConnectionId + Name` optimize queries

**Backup Considerations:**
- Data URIs increase backup size (~33% overhead)
- Consider excluding `UploadedImages` from frequent backups (user can re-upload)

---

## Deployment Notes

**Build Required:** Yes (new migration + API endpoints)

**Migration Required:** Yes
```bash
dotnet ef migrations add AddUploadedImagesTable
dotnet ef database update
```

**Configuration Changes:** None

**Rollback Plan:**
1. Remove migration: `dotnet ef migrations remove`
2. Revert code changes: `git revert <commit>`
3. No data loss (new table only)

**Deployment Steps:**
1. Deploy code changes (API + UI)
2. Run database migration
3. Test image upload in staging environment
4. Monitor database size growth
5. Deploy to production

**Zero Downtime:** Yes (new feature, no breaking changes)

---

## Future Enhancements

### Phase 6.4: User-Scoped Images (Optional)
**Goal:** Allow users to upload images shared across all their connections

**Use Case:** Personal signature, consistent branding across multiple orgs

**Complexity:** Medium (add `UserId` nullable FK to `UploadedImages`)

---

### Phase 6.5: System Template Images (Optional)
**Goal:** Pre-install stock icons (network, device type icons, etc.)

**Use Case:** Users without design skills can use professional icons

**Complexity:** Medium (seed system images via migration)

---

### Phase 7: External Image URLs (Optional)
**Goal:** Reference images via HTTPS URL instead of upload

**Use Case:** Company logo hosted on corporate website

**Complexity:** Medium (CORS handling, caching, broken link detection)

**Risk:** External URL may break, requires fallback strategy

---

## Related Documentation

- **Phase 5.7 QR Migration:** `PHASE5.7_QR_MIGRATION.md` (data URI pattern)
- **Phase 5 Device Export:** `PHASE5_MVP_IMPLEMENTATION.md` (template matching)
- **Phase 4 Template System:** `PHASE4_IMPLEMENTATION_SUMMARY.md` (Fabric.js integration)
- **CLAUDE.md:** Project conventions and patterns

---

## Conclusion

Phase 6 introduces a production-ready custom image upload system that:
- ✅ Matches QR code architecture (data URI storage)
- ✅ Uses realistic size limits for sticker use cases (900×900 px, 2 MB)
- ✅ Provides safe deletion strategy (transparent placeholder)
- ✅ Integrates seamlessly with existing designer
- ✅ Enforces sensible quotas (25 images, 20 MB per connection)
- ✅ Includes comprehensive validation (client + server)

The phased implementation allows incremental delivery:
- **Phase 6.1:** Core upload/list/delete functionality (MVP)
- **Phase 6.2:** Designer integration and export support
- **Phase 6.3:** Orphan detection and cleanup tools (optional)

All design decisions prioritize simplicity, security, and consistency with existing patterns. The system is ready for production deployment with proper testing and monitoring.

---

**Phase 6 Status: PLANNING COMPLETE** ✅

**Next Steps:**
1. Review plan with stakeholders
2. Approve database schema and API design
3. Begin Phase 6.1 implementation
4. Create unit tests for validation logic
5. Deploy to staging for user acceptance testing

**Signed off by:** Claude
**Date:** 2025-10-22
**Confidence Level:** High - Architecture aligns with existing patterns
**Ready for:** Implementation Phase 6.1
