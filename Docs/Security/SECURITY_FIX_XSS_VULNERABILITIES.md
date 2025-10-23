# XSS Vulnerability Security Fix Plan

**Date:** 2025-01-XX
**Priority:** CRITICAL
**Severity:** HIGH

---

## Executive Summary

Found **4 critical XSS vulnerabilities** where user-controlled input (device names, serials, models, custom image names) is rendered directly into HTML without sanitization. This allows attackers to inject malicious scripts through device/image names.

**Attack Vector Example:**
```
Device Name: <img src=x onerror=alert('XSS')>
Result: Script executes when device export modal opens
```

---

## Vulnerabilities Identified

### 1. **multi-device-export.js** (Lines 308, 310, 314)
**Risk Level:** HIGH
**Function:** `renderDeviceList()`
**Vulnerable Variables:**
- `device.name` - User-controlled via Meraki API sync
- `device.serial` - Could contain special characters
- `template.name` - User-controlled template name

```javascript
// VULNERABLE CODE
return `
    <strong>${device.name}</strong>
    <code>${device.serial}</code>
    Template: ${template?.name || 'Unknown'}
`;
```

### 2. **device-export.js** (Lines 170-173, 182)
**Risk Level:** HIGH
**Function:** `renderDeviceExportModalUI()`
**Vulnerable Variables:**
- `device.name`
- `device.serial`
- `device.model`
- `device.productType`
- `template.name`

```javascript
// VULNERABLE CODE
deviceInfo.innerHTML = `
    <p><strong>Name:</strong> ${device.name || 'Unnamed'}</p>
    <p><strong>Serial:</strong> <code>${device.serial || 'N/A'}</code></p>
    <p><strong>Model:</strong> ${device.model || 'N/A'}</p>
`;
```

### 3. **designer.js** (Lines 1095, 1097) - **MOST CRITICAL**
**Risk Level:** CRITICAL
**Function:** `renderCustomImageGrid()`
**Vulnerable Variables:**
- `img.name` - Directly user-controlled via upload form
- `img.dataUri` - Could potentially contain malicious data URI schemes

```javascript
// VULNERABLE CODE
grid.innerHTML = uploadedImages.map(img => `
    <img src="${img.dataUri}" ... alt="${img.name}" />
    <h4>${img.name}</h4>
`).join('');
```

### 4. **designer.js** (Line 738)
**Risk Level:** MEDIUM
**Function:** Object selection handler
**Vulnerable Variables:**
- `customImageName` - From user upload

```javascript
// VULNERABLE CODE
customImageInfo.innerHTML = `
    <p><strong>Custom Image:</strong> ${customImageName}</p>
`;
```

---

## Fix Approach A: DOM Manipulation (RECOMMENDED)

**Benefits:**
- ✅ Maximum security - no HTML parsing of user input
- ✅ No escaping function needed
- ✅ Clear separation of structure and data
- ✅ Easier to test and maintain

**Drawbacks:**
- ⚠️ More verbose code
- ⚠️ Requires careful selector design

### Implementation Strategy

#### Key Principle: Use Data Attributes for Targeting

Instead of generic selectors like `querySelector('strong')`, use data attributes to uniquely identify insertion points:

```javascript
// GOOD: Unambiguous targeting
const container = document.createElement('div');
container.innerHTML = `
    <strong data-field="device-name"></strong>
    <code data-field="device-serial"></code>
`;
container.querySelector('[data-field="device-name"]').textContent = device.name;
container.querySelector('[data-field="device-serial"]').textContent = device.serial;
```

```javascript
// BAD: Ambiguous - which <strong>?
container.innerHTML = `<strong></strong> <strong></strong>`;
container.querySelector('strong').textContent = device.name; // Gets first one only!
```

---

### Fix 1: multi-device-export.js - `renderDeviceList()`

**Current Code (Lines 297-325):**
```javascript
function renderDeviceList(devices, exportDataList) {
    return devices.map((device, index) => {
        const exportData = exportDataList[index];
        const template = exportData?.matchedTemplate;
        const matchReason = template?.matchReason || 'unknown';
        const confidence = template?.confidence ? Math.round(template.confidence * 100) : 0;

        return `
            <div class="device-card" style="...">
                <div style="display: flex; justify-content: space-between;">
                    <div>
                        <strong>${device.name}</strong>
                        <div style="font-size: 0.85em; color: #666; margin-top: 4px;">
                            <code>${device.serial}</code>
                        </div>
                    </div>
                    <div style="text-align: right; font-size: 0.85em;">
                        <div style="color: #666;">Template: ${template?.name || 'Unknown'}</div>
                        <div style="margin-top: 4px;">
                            <span class="match-badge match-${matchReason}" style="...">
                                ${formatMatchReason(matchReason)} (${confidence}%)
                            </span>
                        </div>
                    </div>
                </div>
            </div>
        `;
    }).join('');
}
```

**Fixed Code (Approach A):**
```javascript
function renderDeviceList(devices, exportDataList) {
    const fragment = document.createDocumentFragment();

    devices.forEach((device, index) => {
        const exportData = exportDataList[index];
        const template = exportData?.matchedTemplate;
        const matchReason = template?.matchReason || 'unknown';
        const confidence = template?.confidence ? Math.round(template.confidence * 100) : 0;

        // Create structure with data attributes for targeting
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
                    <div style="margin-top: 4px;">
                        <span class="match-badge match-${matchReason}" style="padding: 2px 8px; border-radius: 3px; font-size: 0.8em; background: ${getMatchBadgeColor(matchReason)}; color: white;" data-field="match-info"></span>
                    </div>
                </div>
            </div>
        `;

        // Safely inject user data using textContent
        card.querySelector('[data-field="device-name"]').textContent = device.name;
        card.querySelector('[data-field="device-serial"]').textContent = device.serial;
        card.querySelector('[data-field="template-name"]').textContent = template?.name || 'Unknown';
        card.querySelector('[data-field="match-info"]').textContent = `${formatMatchReason(matchReason)} (${confidence}%)`;

        fragment.appendChild(card);
    });

    return fragment;
}
```

**Caller Update (Line 202):**
```javascript
// OLD:
modalBody.innerHTML = `
    <div id="deviceListContainer" class="device-list">
        ${renderDeviceList(selected, exportData)}
    </div>
`;

// NEW:
modalBody.innerHTML = `
    <div id="deviceListContainer" class="device-list"></div>
`;
const deviceListContainer = modalBody.querySelector('#deviceListContainer');
deviceListContainer.appendChild(renderDeviceList(selected, exportData));
```

---

### Fix 2: device-export.js - `renderDeviceExportModalUI()`

**Current Code (Lines 165-186):**
```javascript
// Device Information Section
const deviceInfo = modal.querySelector('#deviceInfo');
deviceInfo.innerHTML = `
    <h3>Device Information</h3>
    <div class="info-box">
        <p><strong>Name:</strong> ${device.name || 'Unnamed'}</p>
        <p><strong>Serial:</strong> <code>${device.serial || 'N/A'}</code></p>
        <p><strong>Model:</strong> ${device.model || 'N/A'}</p>
        <p><strong>Product Type:</strong> ${device.productType || 'Unknown'}</p>
    </div>
`;

// Template Information Section
const templateInfo = modal.querySelector('#templateInfo');
templateInfo.innerHTML = `
    <h3>Template</h3>
    <div class="info-box">
        <p><strong>Matched Template:</strong> ${template.name}</p>
        <p><strong>Match Reason:</strong> <span class="match-badge match-${template.matchReason}">${formatMatchReason(template.matchReason)}</span></p>
        <p><strong>Confidence:</strong> ${Math.round(template.confidence * 100)}%</p>
    </div>
`;
```

**Fixed Code (Approach A):**
```javascript
// Device Information Section
const deviceInfo = modal.querySelector('#deviceInfo');
deviceInfo.innerHTML = `
    <h3>Device Information</h3>
    <div class="info-box">
        <p><strong>Name:</strong> <span data-field="device-name"></span></p>
        <p><strong>Serial:</strong> <code data-field="device-serial"></code></p>
        <p><strong>Model:</strong> <span data-field="device-model"></span></p>
        <p><strong>Product Type:</strong> <span data-field="device-type"></span></p>
    </div>
`;
deviceInfo.querySelector('[data-field="device-name"]').textContent = device.name || 'Unnamed';
deviceInfo.querySelector('[data-field="device-serial"]').textContent = device.serial || 'N/A';
deviceInfo.querySelector('[data-field="device-model"]').textContent = device.model || 'N/A';
deviceInfo.querySelector('[data-field="device-type"]').textContent = device.productType || 'Unknown';

// Template Information Section
const templateInfo = modal.querySelector('#templateInfo');
templateInfo.innerHTML = `
    <h3>Template</h3>
    <div class="info-box">
        <p><strong>Matched Template:</strong> <span data-field="template-name"></span></p>
        <p><strong>Match Reason:</strong> <span class="match-badge match-${template.matchReason}" data-field="match-reason"></span></p>
        <p><strong>Confidence:</strong> ${Math.round(template.confidence * 100)}%</p>
    </div>
`;
templateInfo.querySelector('[data-field="template-name"]').textContent = template.name;
templateInfo.querySelector('[data-field="match-reason"]').textContent = formatMatchReason(template.matchReason);
```

---

### Fix 3: designer.js - `renderCustomImageGrid()` (MOST CRITICAL)

**Current Code (Lines 1088-1103):**
```javascript
function renderCustomImageGrid() {
    const grid = document.getElementById('customImageGrid');
    if (!grid) return;

    grid.innerHTML = uploadedImages.map(img => `
        <div class="custom-image-card" style="..." onclick="selectCustomImage(${img.id})">
            <div style="...">
                <img src="${img.dataUri}" style="..." alt="${img.name}" />
            </div>
            <h4 style="...">${img.name}</h4>
            <code style="...">{{customImage.Image_${img.id}}}</code>
            <p style="...">${img.widthPx} × ${img.heightPx} px</p>
            <button class="btn-primary" style="...">Select</button>
        </div>
    `).join('');
}
```

**Fixed Code (Approach A):**
```javascript
function renderCustomImageGrid() {
    const grid = document.getElementById('customImageGrid');
    if (!grid) return;

    // Clear existing content
    grid.innerHTML = '';

    uploadedImages.forEach(img => {
        const card = document.createElement('div');
        card.className = 'custom-image-card';
        card.style.cssText = 'border: 1px solid #ddd; padding: 15px; margin: 10px; display: inline-block; width: 200px; text-align: center; cursor: pointer; border-radius: 4px;';
        card.onclick = () => selectCustomImage(img.id);

        card.innerHTML = `
            <div style="width: 150px; height: 150px; margin: 0 auto; display: flex; align-items: center; justify-content: center; background: #f5f5f5; border: 1px solid #ccc;">
                <img data-field="image-preview" style="max-width: 100%; max-height: 100%; object-fit: contain;" alt="" />
            </div>
            <h4 style="margin: 10px 0 5px 0; font-size: 14px; font-weight: bold;" data-field="image-name"></h4>
            <code style="font-size: 11px; color: #666; display: block; margin: 5px 0;" data-field="binding-key"></code>
            <p style="font-size: 11px; color: #999; margin: 5px 0;" data-field="dimensions"></p>
            <button class="btn-primary" style="margin-top: 10px; padding: 5px 15px; font-size: 12px;">Select</button>
        `;

        // Safely inject user data
        const imgElement = card.querySelector('[data-field="image-preview"]');
        imgElement.src = img.dataUri; // Note: dataUri should be validated server-side
        imgElement.alt = img.name; // Alt text is safe with textContent assignment

        card.querySelector('[data-field="image-name"]').textContent = img.name;
        card.querySelector('[data-field="binding-key"]').textContent = `{{customImage.Image_${img.id}}}`;
        card.querySelector('[data-field="dimensions"]').textContent = `${img.widthPx} × ${img.heightPx} px`;

        grid.appendChild(card);
    });
}
```

**IMPORTANT NOTE on `img.dataUri`:**
Even with DOM manipulation, data URIs can be dangerous if not validated. Ensure server-side validation:
```csharp
// Server-side validation (C#)
if (!dataUri.StartsWith("data:image/png;base64,") &&
    !dataUri.StartsWith("data:image/jpeg;base64,") &&
    !dataUri.StartsWith("data:image/webp;base64,"))
{
    throw new ArgumentException("Invalid image data URI");
}
```

---

### Fix 4: designer.js - Object Selection Handler (Line 738)

**Current Code:**
```javascript
customImageInfo.innerHTML = `
    <div style="...">
        <p style="..."><strong>Custom Image:</strong> ${customImageName}</p>
        <p style="..."><code>${activeObject.get('dataSource')}</code></p>
        <button type="button" class="..." onclick="replaceCustomImage()">Replace Image</button>
    </div>
`;
```

**Fixed Code (Approach A):**
```javascript
customImageInfo.innerHTML = `
    <div style="padding: 10px; background: #f0f0f0; border-radius: 4px; margin: 10px 0;">
        <p style="margin: 5px 0;"><strong>Custom Image:</strong> <span data-field="image-name"></span></p>
        <p style="margin: 5px 0; font-size: 11px;"><code data-field="data-source"></code></p>
        <button type="button" class="btn-primary" style="margin-top: 10px; padding: 5px 10px; font-size: 12px;" onclick="replaceCustomImage()">Replace Image</button>
    </div>
`;
customImageInfo.querySelector('[data-field="image-name"]').textContent = customImageName;
customImageInfo.querySelector('[data-field="data-source"]').textContent = activeObject.get('dataSource');
```

---

## Fix Approach B: HTML Escaping (ALTERNATIVE)

**Benefits:**
- ✅ More concise code
- ✅ Maintains template string readability
- ✅ Good for simple cases

**Drawbacks:**
- ⚠️ Must remember to escape EVERY variable
- ⚠️ Easy to miss one and introduce vulnerability
- ⚠️ Requires centralized utility function

### Implementation Strategy

#### Step 1: Create Centralized Utility (NEW FILE)

**File:** `wwwroot/js/html-utils.js`
```javascript
/**
 * HTML Utilities
 * Provides security functions for safe HTML rendering
 */

/**
 * Escapes HTML special characters to prevent XSS attacks
 * @param {string} text - The text to escape
 * @returns {string} - The escaped text safe for HTML insertion
 *
 * @example
 * const safe = escapeHtml('<script>alert("XSS")</script>');
 * // Returns: &lt;script&gt;alert(&quot;XSS&quot;)&lt;/script&gt;
 */
function escapeHtml(text) {
    if (text === null || text === undefined) {
        return '';
    }

    const div = document.createElement('div');
    div.textContent = String(text);
    return div.innerHTML;
}

/**
 * Alternative escapeHtml using replace (faster for large text)
 */
function escapeHtmlFast(text) {
    if (text === null || text === undefined) {
        return '';
    }

    return String(text)
        .replace(/&/g, '&amp;')
        .replace(/</g, '&lt;')
        .replace(/>/g, '&gt;')
        .replace(/"/g, '&quot;')
        .replace(/'/g, '&#039;');
}
```

#### Step 2: Update HTML Files to Load Utility

**Files to Update:**
- `Pages/Meraki/Networks.cshtml` (for multi-device-export.js)
- `Pages/Meraki/Networks.cshtml` (for device-export.js)
- `Pages/Templates/Designer.cshtml` (for designer.js)

Add BEFORE existing script tags:
```html
<script src="~/js/html-utils.js"></script>
```

#### Step 3: Apply Escaping to All Vulnerable Code

**Fix 1: multi-device-export.js**
```javascript
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
                        <strong>${escapeHtml(device.name)}</strong>
                        <div style="font-size: 0.85em; color: #666; margin-top: 4px;">
                            <code>${escapeHtml(device.serial)}</code>
                        </div>
                    </div>
                    <div style="text-align: right; font-size: 0.85em;">
                        <div style="color: #666;">Template: ${escapeHtml(template?.name || 'Unknown')}</div>
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

// REMOVE duplicate escapeHtml function at line 358
```

**Fix 2: device-export.js**
```javascript
// Device Information Section
const deviceInfo = modal.querySelector('#deviceInfo');
deviceInfo.innerHTML = `
    <h3>Device Information</h3>
    <div class="info-box">
        <p><strong>Name:</strong> ${escapeHtml(device.name || 'Unnamed')}</p>
        <p><strong>Serial:</strong> <code>${escapeHtml(device.serial || 'N/A')}</code></p>
        <p><strong>Model:</strong> ${escapeHtml(device.model || 'N/A')}</p>
        <p><strong>Product Type:</strong> ${escapeHtml(device.productType || 'Unknown')}</p>
    </div>
`;

// Template Information Section
const templateInfo = modal.querySelector('#templateInfo');
templateInfo.innerHTML = `
    <h3>Template</h3>
    <div class="info-box">
        <p><strong>Matched Template:</strong> ${escapeHtml(template.name)}</p>
        <p><strong>Match Reason:</strong> <span class="match-badge match-${template.matchReason}">${formatMatchReason(template.matchReason)}</span></p>
        <p><strong>Confidence:</strong> ${Math.round(template.confidence * 100)}%</p>
    </div>
`;
```

**Fix 3: designer.js - renderCustomImageGrid()**
```javascript
function renderCustomImageGrid() {
    const grid = document.getElementById('customImageGrid');
    if (!grid) return;

    grid.innerHTML = uploadedImages.map(img => `
        <div class="custom-image-card" style="border: 1px solid #ddd; padding: 15px; margin: 10px; display: inline-block; width: 200px; text-align: center; cursor: pointer; border-radius: 4px;" onclick="selectCustomImage(${img.id})">
            <div style="width: 150px; height: 150px; margin: 0 auto; display: flex; align-items: center; justify-content: center; background: #f5f5f5; border: 1px solid #ccc;">
                <img src="${escapeHtml(img.dataUri)}" style="max-width: 100%; max-height: 100%; object-fit: contain;" alt="${escapeHtml(img.name)}" />
            </div>
            <h4 style="margin: 10px 0 5px 0; font-size: 14px; font-weight: bold;">${escapeHtml(img.name)}</h4>
            <code style="font-size: 11px; color: #666; display: block; margin: 5px 0;">{{customImage.Image_${img.id}}}</code>
            <p style="font-size: 11px; color: #999; margin: 5px 0;">${img.widthPx} × ${img.heightPx} px</p>
            <button class="btn-primary" style="margin-top: 10px; padding: 5px 15px; font-size: 12px;">Select</button>
        </div>
    `).join('');
}
```

**Fix 4: designer.js - Line 738**
```javascript
customImageInfo.innerHTML = `
    <div style="padding: 10px; background: #f0f0f0; border-radius: 4px; margin: 10px 0;">
        <p style="margin: 5px 0;"><strong>Custom Image:</strong> ${escapeHtml(customImageName)}</p>
        <p style="margin: 5px 0; font-size: 11px;"><code>${activeObject.get('dataSource')}</code></p>
        <button type="button" class="btn-primary" style="margin-top: 10px; padding: 5px 10px; font-size: 12px;" onclick="replaceCustomImage()">Replace Image</button>
    </div>
`;
```

---

## Approach Comparison

| Criteria | Approach A (DOM) | Approach B (Escape) |
|----------|------------------|---------------------|
| **Security** | ✅ Maximum | ✅ Good (if consistent) |
| **Code Length** | ⚠️ More verbose | ✅ Concise |
| **Readability** | ⚠️ Split structure/data | ✅ Template strings |
| **Maintenance** | ✅ Clear separation | ⚠️ Must check every variable |
| **Performance** | ✅ No string parsing | ⚠️ Extra function calls |
| **Risk of Mistakes** | ✅ Low | ⚠️ High (easy to forget escaping) |
| **Best For** | Complex HTML with many insertions | Simple HTML with few insertions |

---

## Testing Plan

### Manual XSS Testing

After implementing either approach, test with these malicious payloads:

**Test Device Names:**
```
<script>alert('XSS')</script>
<img src=x onerror=alert('XSS')>
<svg onload=alert('XSS')>
" onclick="alert('XSS')
'; alert('XSS'); //
```

**Test Custom Image Names:**
```
"><script>alert('XSS')</script>
<iframe src="javascript:alert('XSS')">
javascript:alert('XSS')
data:text/html,<script>alert('XSS')</script>
```

**Expected Result:** All special characters should be visible as literal text, NOT executed.

### Automated Testing

```javascript
// Unit test for escapeHtml (Approach B only)
function testEscapeHtml() {
    const tests = [
        { input: '<script>alert("XSS")</script>', expected: '&lt;script&gt;alert(&quot;XSS&quot;)&lt;/script&gt;' },
        { input: '" onclick="alert(1)', expected: '&quot; onclick=&quot;alert(1)' },
        { input: "' onload='alert(1)", expected: '&#039; onload=&#039;alert(1)' },
        { input: '& < > "', expected: '&amp; &lt; &gt; &quot;' }
    ];

    tests.forEach((test, i) => {
        const result = escapeHtml(test.input);
        console.assert(result === test.expected, `Test ${i} failed: ${result}`);
    });
}
```

---

## Deployment Checklist

- [ ] Choose Approach A or Approach B
- [ ] Create backup of all affected JS files
- [ ] Implement fixes in dev environment
- [ ] Run manual XSS tests with malicious payloads
- [ ] Test all modal dialogs (device export, bulk export, custom images)
- [ ] Verify HTML formatting still works (bold, code blocks, etc.)
- [ ] Run automated tests (if approach B)
- [ ] Code review with security focus
- [ ] Deploy to staging
- [ ] Final security verification in staging
- [ ] Deploy to production
- [ ] Monitor for errors in production logs

---

## Additional Security Recommendations

### 1. Server-Side Validation Enhancement
Even with client-side XSS protection, validate on the server:

```csharp
// Validate device/image names on API endpoints
public class ImageUploadRequest
{
    [Required]
    [StringLength(200)]
    [RegularExpression(@"^[a-zA-Z0-9\s\-_.()]+$",
        ErrorMessage = "Image name contains invalid characters")]
    public string Name { get; set; }

    // ... other properties
}
```

### 2. Content Security Policy (CSP)
Add CSP header to prevent inline script execution:

```csharp
// Startup.cs or Program.cs
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("Content-Security-Policy",
        "default-src 'self'; script-src 'self' 'unsafe-inline' cdn.jsdelivr.net");
    await next();
});
```

### 3. Data URI Validation
Ensure `img.dataUri` only contains valid base64-encoded images:

```csharp
// ImagesController.cs
private bool IsValidDataUri(string dataUri)
{
    var allowedPrefixes = new[] {
        "data:image/png;base64,",
        "data:image/jpeg;base64,",
        "data:image/webp;base64,"
    };

    if (!allowedPrefixes.Any(p => dataUri.StartsWith(p)))
        return false;

    // Validate base64 format
    var base64 = dataUri.Substring(dataUri.IndexOf(',') + 1);
    try {
        Convert.FromBase64String(base64);
        return true;
    }
    catch {
        return false;
    }
}
```

---

## References

- [OWASP XSS Prevention Cheat Sheet](https://cheatsheetseries.owasp.org/cheatsheets/Cross_Site_Scripting_Prevention_Cheat_Sheet.html)
- [MDN: textContent vs innerHTML](https://developer.mozilla.org/en-US/docs/Web/API/Node/textContent)
- [Google: DOM-based XSS Prevention](https://security.googleblog.com/2009/03/reducing-xss-by-way-of-automatic.html)

---

**END OF SECURITY FIX PLAN**
