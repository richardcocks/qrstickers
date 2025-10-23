# Security Fix: DoS Protection via String Length Constraints

**Date:** October 23, 2024
**Severity:** High
**Status:** Fixed

## Summary

Implemented database-level length constraints on all user-facing string inputs to prevent Denial of Service (DoS) attacks via oversized payloads. Without these constraints, attackers could bypass client-side validation and POST gigabytes of data directly to API endpoints, causing database bloat, memory exhaustion, and service degradation.

## Vulnerabilities Identified

### Critical Issues (Fixed)

1. **ApplicationUser.DisplayName** - No length constraint
   - **Risk:** Users could set multi-gigabyte display names
   - **Fix:** Added `[MaxLength(100)]` attribute
   - **Database:** `nvarchar(100)`

2. **Connection.ConnectionType** - No length constraint
   - **Risk:** Discriminator column could be exploited with huge strings
   - **Fix:** Added `[MaxLength(50)]` attribute
   - **Database:** `nvarchar(50)`

3. **StickerTemplate.TemplateJson** - No size validation
   - **Risk:** Complex Fabric.js JSON could be weaponized for DoS
   - **Fix:** Added server-side 5 MB validation in `Designer.cshtml.cs`
   - **Database:** Remains `nvarchar(max)` (large JSON is legitimate use case)

### Already Protected (No Action Required)

All other user-facing inputs already had proper constraints:
- Connection.DisplayName → 100 chars
- Connection.CompanyLogoUrl → 5,000 chars (allows data URIs)
- StickerTemplate.Name → 200 chars
- StickerTemplate.Description → 1,000 chars
- StickerTemplate.ProductTypeFilter → 50 chars
- GlobalVariable.VariableName → 100 chars
- GlobalVariable.VariableValue → 500 chars
- GlobalVariable.Description → 500 chars
- UploadedImage.Name → 200 chars
- UploadedImage.Description → 1,000 chars
- UploadedImage.MimeType → 50 chars
- UploadedImage.DataUri → Protected by `ImageUploadValidator` service:
  - Max 2.7 MB per image (with base64 overhead)
  - Max 25 images per connection
  - Max 20 MB total storage per connection

## Technical Details

### Defense in Depth Strategy

Each user input is protected by **three layers** of validation:

#### Layer 1: Client-Side Validation
- HTML5 `maxlength` attributes on input fields
- JavaScript validation before form submission
- **Purpose:** User-friendly immediate feedback
- **Limitation:** Can be bypassed (view source, disable JS, direct HTTP POST)

#### Layer 2: Server-Side Model Validation
- ASP.NET Core `[StringLength]` attributes on InputModels
- `[MaxLength]` attributes on entity properties
- ModelState validation rejects invalid input before processing
- **Purpose:** Catch tampered form submissions
- **Limitation:** Can be bypassed if entity is modified directly

#### Layer 3: Database Constraints (Ultimate Protection)
- `[MaxLength]` attributes map to `nvarchar(N)` columns
- SQL Server enforces constraint at INSERT/UPDATE
- **Purpose:** Ultimate security boundary
- **Cannot be bypassed:** Even direct SQL with oversized strings will fail

### Implementation Pattern

**Entity Model:**
```csharp
using System.ComponentModel.DataAnnotations;

public class ApplicationUser : IdentityUser
{
    [MaxLength(100)]
    public string? DisplayName { get; set; }
}
```

**Input Model (optional, for custom validation messages):**
```csharp
public class InputModel
{
    [StringLength(100, ErrorMessage = "The {0} must be at most {1} characters long.")]
    public string? DisplayName { get; set; }
}
```

**View (optional, for immediate feedback):**
```html
<input asp-for="Input.DisplayName" class="form-control" maxlength="100" />
```

**Result:**
- User sees friendly error before submission (Layer 1)
- Server rejects tampered input (Layer 2)
- Database rejects direct SQL attacks (Layer 3)

### Special Cases

#### Large JSON Fields (TemplateJson)

**Why nvarchar(max) is kept:**
- Fabric.js canvas JSON can legitimately be large (100 KB - 2 MB for complex templates)
- EF Core would still create `nvarchar(max)` for MaxLength > 4000 anyway
- Application-level validation provides flexible limit (5 MB)

**Validation:**
```csharp
const int MAX_TEMPLATE_JSON_SIZE = 5_000_000; // 5 MB
if (Template.TemplateJson.Length > MAX_TEMPLATE_JSON_SIZE)
{
    var sizeMB = Template.TemplateJson.Length / 1024.0 / 1024.0;
    ModelState.AddModelError("", $"Template too complex ({sizeMB:F2} MB). Maximum size: 5 MB");
    return Page();
}
```

**Risk Assessment:**
- Typical templates: 5-100 KB
- Complex templates: 500 KB - 2 MB
- Limit: 5 MB (allows 2.5x-10x headroom)
- DoS risk: **Low** (5 MB per template is manageable)

#### Base64 Image Data URIs (CompanyLogoUrl, UploadedImage.DataUri)

**CompanyLogoUrl (5,000 chars):**
- Allows small base64-encoded logos
- ~3.5 KB decoded (adequate for small logos)
- Database constraint prevents abuse

**UploadedImage.DataUri (nvarchar(max)):**
- Protected by `ImageUploadValidator` service (application logic)
- Server-side validation enforces:
  - MIME type whitelist (PNG, JPEG, WebP, SVG)
  - Dimension limits (900x900 px max)
  - File size limits (2.7 MB with base64 overhead)
  - Quota limits (25 images, 20 MB total per connection)
- **No database constraint needed** - validation is comprehensive

## Migrations

### Applied Migrations

```bash
# Add DisplayName with length constraint
dotnet ef migrations add AddUserProfileFields
# Adds: DisplayName nvarchar(100), LastLoginAt, PreviousLoginAt, CurrentSessionStartedAt

# Limit DisplayName length (redundant if above migration includes MaxLength)
dotnet ef migrations add LimitDisplayNameLength
# Alters: DisplayName to nvarchar(100) if previously nvarchar(max)

# Limit ConnectionType length
dotnet ef migrations add LimitConnectionTypeLength
# Alters: ConnectionType to nvarchar(50)
```

### Generated SQL

```sql
-- AddUserProfileFields
ALTER TABLE [AspNetUsers] ADD [DisplayName] nvarchar(100) NULL;
ALTER TABLE [AspNetUsers] ADD [LastLoginAt] datetime2 NULL;
ALTER TABLE [AspNetUsers] ADD [PreviousLoginAt] datetime2 NULL;
ALTER TABLE [AspNetUsers] ADD [CurrentSessionStartedAt] datetime2 NULL;

-- LimitConnectionTypeLength
ALTER TABLE [Connections] ALTER COLUMN [ConnectionType] nvarchar(50) NOT NULL;
```

## Attack Scenarios Mitigated

### Scenario 1: Display Name DoS
**Before fix:**
1. Attacker registers account
2. Uses browser DevTools to remove `maxlength="100"` from input
3. POSTs display name with 1 GB of text
4. Database stores 1 GB in `nvarchar(max)` column
5. Repeat 100 times → 100 GB database bloat

**After fix:**
1. Attacker registers account
2. Removes client-side validation
3. POSTs display name with 1 GB of text
4. **Server rejects:** ModelState validation fails
5. If bypassed, **Database rejects:** SQL Server enforces nvarchar(100)

### Scenario 2: Template JSON DoS
**Before fix:**
1. Attacker creates template
2. Crafts HTTP POST with 10 MB JSON payload
3. Server accepts and stores in database
4. Repeat 1000 times → 10 GB database bloat
5. Queries fetching templates exhaust memory

**After fix:**
1. Attacker creates template
2. Crafts HTTP POST with 10 MB JSON payload
3. **Server rejects:** Validation fails with clear error message
4. Attack blocked at application layer

### Scenario 3: Image Upload DoS
**Already protected:**
1. Attacker uploads image
2. `ImageUploadValidator` checks MIME type, dimensions, file size
3. Checks quota: 25 images, 20 MB total
4. **Request rejected** if limits exceeded
5. No database bloat possible

## Configuration

### Adjusting Limits

Limits are hardcoded as constants for security. To change limits, modify source code and redeploy:

**Template JSON size limit:**
```csharp
// Pages/Templates/Designer.cshtml.cs
const int MAX_TEMPLATE_JSON_SIZE = 5_000_000; // Change to desired limit
```

**Image upload limits:**
```csharp
// Services/ImageUploadValidator.cs
private const int MAX_DIMENSION = 900;
private const long MAX_FILE_SIZE = 2_000_000; // 2 MB (pre-base64)
private const long MAX_BASE64_SIZE = 2_700_000; // 2.7 MB (with base64 overhead)
private const int MAX_IMAGES_PER_CONNECTION = 25;
private const long MAX_TOTAL_STORAGE = 20_000_000; // 20 MB
```

**Why constants instead of configuration?**
- Security limits should not be easily adjustable by operators
- Prevents accidental misconfiguration (e.g., setting to 0 or int.MaxValue)
- Code review required for limit changes

## Testing

### Manual Testing

**Test 1: Display Name Constraint**
```bash
# Valid input
curl -X POST https://localhost:7044/Identity/Account/Manage \
  -d "Input.DisplayName=John Doe"
# Expected: Success

# Oversized input
curl -X POST https://localhost:7044/Identity/Account/Manage \
  -d "Input.DisplayName=$(python -c 'print("A"*101)')"
# Expected: 400 Bad Request with validation error
```

**Test 2: Template JSON Size Limit**
1. Create template via UI
2. Open browser DevTools → Network tab
3. Intercept POST to /Templates/Designer
4. Modify `TemplateJson` field to 6 MB string
5. Submit request
6. **Expected:** Error message "Template too complex (6.00 MB). Maximum size: 5 MB"

**Test 3: Database Constraint Enforcement**
```sql
-- Should fail with constraint violation
UPDATE AspNetUsers
SET DisplayName = REPLICATE('A', 101)
WHERE Id = 'some-user-id';
-- Expected: String or binary data would be truncated
```

### Automated Testing (Recommended)

```csharp
[Fact]
public async Task DisplayName_Exceeds_MaxLength_Should_Fail()
{
    var user = new ApplicationUser { DisplayName = new string('A', 101) };
    var result = await _userManager.UpdateAsync(user);
    Assert.False(result.Succeeded);
}

[Fact]
public async Task TemplateJson_Exceeds_5MB_Should_Return_ValidationError()
{
    var template = new StickerTemplate { TemplateJson = new string('A', 5_000_001) };
    var result = await _pageModel.OnPostAsync();
    Assert.False(_pageModel.ModelState.IsValid);
}
```

## Monitoring

### Database Size Monitoring

Monitor for unusual growth in key tables:

```sql
-- Check table sizes
SELECT
    t.NAME AS TableName,
    SUM(p.rows) AS RowCount,
    SUM(a.total_pages) * 8 / 1024.0 AS TotalSizeMB,
    SUM(a.used_pages) * 8 / 1024.0 AS UsedSizeMB
FROM sys.tables t
INNER JOIN sys.partitions p ON t.object_id = p.object_id
INNER JOIN sys.allocation_units a ON p.partition_id = a.container_id
WHERE p.index_id IN (0,1)
GROUP BY t.NAME
ORDER BY UsedSizeMB DESC;

-- Check for oversized TemplateJson
SELECT
    Id,
    Name,
    LEN(TemplateJson) / 1024.0 / 1024.0 AS SizeMB,
    UpdatedAt
FROM StickerTemplates
WHERE LEN(TemplateJson) > 1000000 -- Templates > 1 MB
ORDER BY LEN(TemplateJson) DESC;

-- Check image storage quota usage
SELECT
    c.Id AS ConnectionId,
    c.DisplayName,
    COUNT(i.Id) AS ImageCount,
    SUM(i.FileSizeBytes) / 1024.0 / 1024.0 AS TotalStorageMB
FROM Connections c
LEFT JOIN UploadedImages i ON c.Id = i.ConnectionId AND i.IsDeleted = 0
GROUP BY c.Id, c.DisplayName
HAVING COUNT(i.Id) > 20 OR SUM(i.FileSizeBytes) > 15000000; -- Alert at 80% quota
```

### Application Logging

Monitor for validation failures (potential attack attempts):

```csharp
_logger.LogWarning("User {UserId} attempted to save template exceeding size limit: {SizeMB} MB",
    userId, Template.TemplateJson.Length / 1024.0 / 1024.0);
```

Look for patterns:
- Repeated validation failures from same IP/user
- Sudden spike in failed uploads
- Attempts to upload near-limit files repeatedly

## Future Considerations

### Rate Limiting
Consider adding rate limiting for resource-intensive operations:
- Template creation: 10 per hour per user
- Image uploads: 5 per minute per connection
- Profile updates: 3 per minute per user

### Content Security Policy
Consider adding CSP headers to prevent XSS in user-generated content:
```csharp
app.Use(async (context, next) =>
{
    context.Response.Headers.Add("Content-Security-Policy",
        "default-src 'self'; img-src 'self' data:; script-src 'self' https://cdn.jsdelivr.net");
    await next();
});
```

### Input Sanitization
Consider sanitizing HTML in Description fields:
```csharp
using Ganss.Xss;

var sanitizer = new HtmlSanitizer();
template.Description = sanitizer.Sanitize(template.Description);
```

## References

- [OWASP: Denial of Service](https://owasp.org/www-community/attacks/Denial_of_Service)
- [CWE-400: Uncontrolled Resource Consumption](https://cwe.mitre.org/data/definitions/400.html)
- [Microsoft: Data Annotations](https://learn.microsoft.com/en-us/ef/core/modeling/entity-properties#maximum-length)
- [SQL Server: nvarchar Data Type](https://learn.microsoft.com/en-us/sql/t-sql/data-types/nchar-and-nvarchar-transact-sql)

## Changelog

- **2024-10-23:** Initial implementation of string length constraints
  - Added MaxLength to ApplicationUser.DisplayName (100 chars)
  - Added MaxLength to Connection.ConnectionType (50 chars)
  - Added server-side validation for StickerTemplate.TemplateJson (5 MB)
  - Created migrations: `AddUserProfileFields`, `LimitDisplayNameLength`, `LimitConnectionTypeLength`
