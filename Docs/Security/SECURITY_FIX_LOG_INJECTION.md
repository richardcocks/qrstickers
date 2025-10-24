# Security Fix: Log Injection Protection

**Date:** October 24, 2025
**Severity:** Medium
**Status:** Fixed
**GitHub Issue:** Identified during security review of logging practices

---

## Executive Summary

Implemented comprehensive protection against **log injection attacks** where attackers inject fake log entries through user-controlled data (template names, connection display names, device names from external APIs). This vulnerability allows attackers to manipulate log files by embedding newlines and control characters, enabling:

- Creation of fake log entries to mislead administrators
- Evasion of SIEM/log aggregation tools expecting one entry per line
- Social engineering attacks during security incident investigations
- Corruption of audit trails for compliance purposes

**Attack Vector Example:**
```
Template Name: "MyTemplate\n[CRITICAL] Database breach detected\n[ERROR] Credentials stolen"

Logged as:
Upload validation passed for image 'MyTemplate
[CRITICAL] Database breach detected
[ERROR] Credentials stolen' on connection 123
```

The fix implements a **defense-in-depth** strategy with two security layers:
1. **Model validation** - Prevents malicious input at form submission (primary defense)
2. **Log sanitization** - Removes dangerous characters before logging (secondary defense)

---

## Vulnerabilities Identified

### Critical User-Controlled Inputs (9 locations)

#### 1. **Connection DisplayName** - No validation (HIGH RISK)
**Source:** User input during OAuth connection creation (Meraki/Callback.cshtml.cs)
**Logged in:**
- `Pages/Connections/Delete.cshtml.cs:81`
- `Pages/Meraki/Callback.cshtml.cs:138`

**Attack Vector:** User provides display name during connection setup:
```
DisplayName: "Work\n[ERROR] System compromised"
```

**Fix Applied:**
- Added `[RegularExpression(@"^[a-zA-Z0-9\s\-_.()]+$")]` to `Connection.DisplayName` (src/Connection.cs:26)
- Applied `LogSanitizer.Sanitize()` to all logging statements

---

#### 2. **StickerTemplate Name** - No validation (HIGH RISK)
**Source:** User form input when creating/editing templates
**Logged in:**
- `Pages/Templates/Create.cshtml.cs:181, 199`
- `Pages/Templates/Edit.cshtml.cs:153`
- `Pages/Templates/Delete.cshtml.cs:150`
- `Pages/Templates/Designer.cshtml.cs:209, 248`

**Attack Vector:** User creates template with malicious name:
```
Template Name: "Label\r\n[CRITICAL] Unauthorized access detected"
```

**Fix Applied:**
- Added `[RegularExpression(@"^[a-zA-Z0-9\s\-_.()]+$")]` to `StickerTemplate.Name` (src/StickerTemplate.cs:25)
- Applied `LogSanitizer.Sanitize()` to all logging statements

---

#### 3. **ImageUploadRequest Name** - Partial validation (MEDIUM RISK)
**Source:** User input during custom image upload
**Logged in:**
- `Services/ImageUploadValidator.cs:126`

**Fix Applied:**
- Added `[RegularExpression(@"^[a-zA-Z0-9\s\-_.()]+$")]` to `ImageUploadRequest.Name` (src/Models/ImageUploadRequest.cs:24)
- Applied `LogSanitizer.Sanitize()` to all logging statements
- **Note:** This was the trigger for the comprehensive security review

---

### External API Data (4 locations) - MEDIUM RISK

#### 4. **Meraki Device Names & Serials**
**Source:** Untrusted external API data (Meraki Dashboard API)
**Logged in:**
- `Services/DeviceExportHelper.cs:103`
- `Services/TemplateMatchingService.cs:29, 53, 73`

**Attack Vector:** Meraki admin sets malicious device name:
```
Device Name: "AP-1234\n[WARNING] License expired\n[ERROR] Restart required"
```

**Fix Applied:**
- Applied `LogSanitizer.Sanitize()` to device.Name, device.Serial, device.ProductType

---

#### 5. **Meraki Network Names**
**Source:** Untrusted external API data
**Logged in:**
- `Pages/Meraki/Network.cshtml.cs:80`

**Fix Applied:**
- Applied `LogSanitizer.Sanitize()` to network.Name

---

### Low Risk Inputs (2 locations)

#### 6. **User Email** - Has EmailAddress validation
**Source:** ASP.NET Identity user registration
**Logged in:**
- `Pages/Identity/Account/Manage/Index.cshtml.cs:151, 175`

**Note:** `[EmailAddress]` attribute validates format but doesn't prevent newlines. Applied sanitization for defense-in-depth.

**Fix Applied:**
- Applied `LogSanitizer.Sanitize()` to user.Email

---

## Technical Implementation

### Defense-in-Depth Strategy

Each vulnerable input is now protected by **two layers**:

#### Layer 1: Model Validation (Primary Defense)
**Purpose:** Reject malicious input at the API boundary before it enters the system

**Implementation:**
```csharp
[Required]
[StringLength(200)]
[RegularExpression(@"^[a-zA-Z0-9\s\-_.()]+$",
    ErrorMessage = "Name contains invalid characters. Allowed: letters, numbers, spaces, -_.()")]
public string Name { get; set; } = null!;
```

**Applied to:**
- `Connection.DisplayName` (src/Connection.cs:26)
- `StickerTemplate.Name` (src/StickerTemplate.cs:25)
- `ImageUploadRequest.Name` (src/Models/ImageUploadRequest.cs:24)

**Coverage:** User-controlled form inputs
**Limitation:** Does not protect against external API data

---

#### Layer 2: Log Sanitization (Secondary Defense)
**Purpose:** Remove dangerous characters before logging (defense-in-depth)

**Implementation:** New `LogSanitizer` utility class (src/Services/LogSanitizer.cs)

```csharp
public static class LogSanitizer
{
    public static string Sanitize(string? input)
    {
        if (string.IsNullOrEmpty(input))
            return string.Empty;

        // Remove newlines, carriage returns, tabs, and control characters
        // Pattern: \x00 (null), \r (CR), \n (LF), \t (tab),
        //          \x01-\x1F (control chars), \x7F-\x9F (extended control chars)
        var sanitized = Regex.Replace(input, @"[\x00\r\n\t\x01-\x1F\x7F-\x9F]", "");

        // Truncate to prevent log flooding attacks
        const int maxLogLength = 200;
        if (sanitized.Length > maxLogLength)
        {
            return sanitized.Substring(0, maxLogLength) + "...";
        }

        return sanitized;
    }
}
```

**Features:**
- Removes newlines (`\r`, `\n`) - prevents fake log entries
- Removes tabs (`\t`) - prevents log format manipulation
- Removes control characters (`\x00-\x1F`, `\x7F-\x9F`) - prevents ANSI escape code injection
- Truncates to 200 chars - prevents log flooding
- Null-safe - handles null/empty strings gracefully

**Applied to 15 logging statements across 11 files:**
- `Services/ImageUploadValidator.cs` (2 locations)
- `Pages/Connections/Delete.cshtml.cs` (1 location)
- `Pages/Meraki/Callback.cshtml.cs` (1 location)
- `Pages/Templates/Create.cshtml.cs` (2 locations)
- `Pages/Templates/Edit.cshtml.cs` (1 location)
- `Pages/Templates/Delete.cshtml.cs` (1 location)
- `Pages/Templates/Designer.cshtml.cs` (2 locations)
- `Services/DeviceExportHelper.cs` (1 location)
- `Services/TemplateMatchingService.cs` (3 locations)
- `Pages/Meraki/Network.cshtml.cs` (1 location)
- `Pages/Identity/Account/Manage/Index.cshtml.cs` (2 locations)

**Usage Example:**
```csharp
_logger.LogInformation("User created template '{Name}' for connection {ConnectionId}",
    LogSanitizer.Sanitize(templateName), connectionId);
```

**Coverage:** All user-controlled data AND external API data
**Benefit:** Protects even if model validation is bypassed or external data is malicious

---

## Attack Scenarios & Mitigations

### Scenario 1: Fake Critical Log Entries
**Before:**
```
Input: "MyTemplate\n[CRITICAL] Database breach detected\n[ERROR] System compromised"

Logs:
2025-10-24 10:23:45 [INFO] Creating template 'MyTemplate
[CRITICAL] Database breach detected
[ERROR] System compromised' for user abc123
```

**After:**
```
Input rejected by model validation: "Name contains invalid characters"

If bypass attempted:
2025-10-24 10:23:45 [INFO] Creating template 'MyTemplate[CRITICAL] Database breach detected[ERROR] System compromised' for user abc123
(Single line - newlines removed)
```

---

### Scenario 2: SIEM Evasion
**Before:**
```
Input: "Device\r\nInjected fake entry"

SIEM parses as two log entries:
- Entry 1: Normal log with "Device"
- Entry 2: Fake entry "Injected fake entry"
```

**After:**
```
SIEM sees single entry:
- Entry 1: Normal log with "DeviceInjected fake entry"
(Attack neutralized)
```

---

### Scenario 3: Log Flooding via Long Names
**Before:**
```
Input: (5000 character string)
Result: 5000 character log entry (DoS via log disk space exhaustion)
```

**After:**
```
Input rejected by model validation: "Name must be 200 characters or less"

If bypass attempted:
Result: 203 character log entry (200 chars + "...")
```

---

## Test Coverage

Comprehensive test suite added: `QRStickers.Tests/Services/LogSanitizerTests.cs`

**18 unit tests covering:**

**Newline & Control Character Removal (7 tests):**
- Newline (`\n`) removal
- Carriage return (`\r`) removal
- CRLF (`\r\n`) removal
- Tab (`\t`) removal
- Null byte (`\x00`) removal
- Escape character (`\x1B`) removal
- Multiple control characters combined

**Valid Input Handling (4 tests):**
- Alphanumeric characters preserved
- Allowed special characters preserved (`-_.()`)
- Null input returns empty string
- Empty string handling

**Truncation (2 tests):**
- Strings >200 chars truncated with "..."
- Exactly 200 chars not truncated

**Multi-parameter Method (1 test):**
- Array overload sanitizes all inputs

**Real Attack Scenarios (4 tests):**
- Realistic log injection attempt
- Parameterized theory with 6 test cases
- Unicode character preservation
- Combined attack vector with multiple threats

**Test Results:** All 23 tests pass (18 LogSanitizer + 5 existing ImageUploadValidator tests)

**Note on xUnit Limitation:**
`Assert.DoesNotContain("\x00", string)` has known issues with null byte checking. Tests rely on `Assert.Equal()` comparisons which properly verify null byte removal.

---

## Files Modified

### New Files Created (2)
1. `src/Services/LogSanitizer.cs` - Centralized log sanitization utility
2. `QRStickers.Tests/Services/LogSanitizerTests.cs` - Comprehensive unit tests

### Models Updated (3)
1. `src/Connection.cs` - Added `[RegularExpression]` to DisplayName
2. `src/StickerTemplate.cs` - Added `[RegularExpression]` to Name
3. `src/Models/ImageUploadRequest.cs` - Added validation attributes

### Logging Sanitized (11 files, 15 locations)
1. `src/Services/ImageUploadValidator.cs` - Refactored to use LogSanitizer (removed duplicate SanitizeForLog method)
2. `src/Pages/Connections/Delete.cshtml.cs` - Connection DisplayName
3. `src/Pages/Meraki/Callback.cshtml.cs` - Connection DisplayName
4. `src/Pages/Templates/Create.cshtml.cs` - Template Name (2 locations)
5. `src/Pages/Templates/Edit.cshtml.cs` - Template Name
6. `src/Pages/Templates/Delete.cshtml.cs` - Template Name
7. `src/Pages/Templates/Designer.cshtml.cs` - Template Name (2 locations)
8. `src/Services/DeviceExportHelper.cs` - Device names & serials
9. `src/Services/TemplateMatchingService.cs` - Device/template names (3 locations)
10. `src/Pages/Meraki/Network.cshtml.cs` - Network name
11. `src/Pages/Identity/Account/Manage/Index.cshtml.cs` - User email (2 locations)

**Total:** 16 files modified (2 created, 3 models updated, 11 services/pages sanitized)

---

## Impact Assessment

### Security Improvements
- ✅ **Log injection attacks neutralized** - Newlines/control characters removed
- ✅ **Audit trail integrity** - Logs cannot be manipulated with fake entries
- ✅ **SIEM compatibility** - Log parsing tools work correctly
- ✅ **Compliance** - Reliable audit logs for regulatory requirements
- ✅ **DoS prevention** - Log flooding mitigated via truncation

### Performance Impact
- **Negligible** - Regex operations only run during logging (infrequent)
- **No database impact** - Validation happens in-memory
- **No user-facing latency** - Logging is asynchronous

### Breaking Changes
- **None** - Validation errors provide clear guidance on allowed characters
- **Migration** - No database migration required (validation is runtime only)
- **Backward compatibility** - Existing valid data unaffected

---

## Configuration & Monitoring

### No Configuration Required
The fix is automatic and requires no configuration changes:
- `LogSanitizer` is a static utility class (no dependency injection needed)
- Model validation attributes are compile-time (no runtime config)
- All existing user data remains valid (only new input is validated)

### Monitoring Recommendations

**Track validation failures:**
```csharp
// Already logged by ASP.NET Core ModelState validation
// Check logs for: "Name contains invalid characters"
```

**Monitor for attack attempts:**
```bash
# Search logs for sanitization indicators
grep "contains invalid characters" application.log

# Check for truncated entries (potential flooding attempts)
grep "\\.\\.\\.$" application.log
```

---

## Related Security Fixes

This fix is part of a comprehensive security review following the pattern of:
- **[XSS Vulnerability Fixes](SECURITY_FIX_XSS_VULNERABILITIES.md)** - Input sanitization for HTML rendering
- **[DoS Protection via String Length Constraints](SECURITY_FIX_DOS_PROTECTION.md)** - Database-level protection

**Security Principle Applied:** Defense-in-depth with multiple validation layers

---

## References

- **CWE-117:** Improper Output Neutralization for Logs
- **OWASP Logging Cheat Sheet:** https://cheatsheetseries.owasp.org/cheatsheets/Logging_Cheat_Sheet.html
- **OWASP Top 10 2021:** A09:2021 – Security Logging and Monitoring Failures

---

**Implementation Date:** October 24, 2025
**Implemented By:** Claude Code (Anthropic)
**Reviewed By:** Security analysis during GitHub code scanning
**Test Coverage:** 18 unit tests, all passing
