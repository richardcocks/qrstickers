# Account Settings

User self-management features including profile customization, login tracking, and session management.

## Overview

The Account Settings page provides users with tools to:
- Set a personalized display name
- Monitor login activity for security awareness
- Revoke active sessions on other devices

**Access:** Navigate to **Account Settings** in the top navigation bar (authenticated users only)

## Features

### 1. Display Name

**Purpose:** Personalize your greeting throughout the application

**Details:**
- **Optional field** - Defaults to your email address if not set
- **Maximum length:** 100 characters
- **Where it appears:**
  - Home page greeting: "Hello [Display Name]!"
  - Navigation bar (coming soon)

**How to set:**
1. Go to **Account Settings**
2. Enter your preferred name in the "Display Name" field
3. Click **Save Changes**
4. Your name will immediately appear on the home page

**Examples:**
- "John Doe"
- "Jane Smith (Marketing)"
- "IT Admin"

**Privacy note:** Display name is only visible to you (not shared with other users).

---

### 2. Login Activity Tracking

**Purpose:** Monitor your account access for security awareness

**What's tracked:**
- **Current Session Started:** When you logged in to your current session
- **Previous Login:** When you last logged in before this session

**How it works:**
- Timestamps are recorded automatically on every successful login
- Timestamps are displayed in your local timezone
- No action required - tracking is automatic

**Security best practice:**
Check your "Previous Login" timestamp regularly:
- ✅ **Expected:** Matches when you last used the app
- ⚠️ **Unexpected:** Shows a time/location you don't recognize → **Revoke all sessions immediately**

**Example:**
```
Current Session Started: Oct 23, 2024 at 2:45 PM
Previous Login: Oct 22, 2024 at 9:30 AM
```

If you only logged in at 2:45 PM today and don't recognize the Oct 22 login, someone else may have accessed your account.

---

### 3. Session Management

**Purpose:** Remotely log out other devices for security

#### Revoke All Other Sessions

**When to use:**
- You logged in on a public/shared computer and forgot to log out
- You suspect unauthorized access to your account
- You lost a device that was logged in
- You want to ensure only your current device has access

**How it works:**
1. Click **Revoke All Other Sessions**
2. Confirm the action
3. All other devices/browsers will be logged out within **0-30 seconds** (configurable)
4. **Your current session remains active** - you stay logged in

**Technical details:**
- Uses ASP.NET Identity's SecurityStamp validation
- Updates a security token in the database
- Other sessions are invalidated when they next check the token
- Validation interval is configurable per environment (see Configuration section)

**What happens on other devices:**
- User is redirected to the login page on their next request
- No data is lost - they can simply log in again
- Session cookies are invalidated

#### Validation Intervals by Environment

- **Development:** Instant (validates on every request)
- **Production:** Within 10 seconds (default)

This means in production, other sessions will be logged out within 10 seconds of clicking "Revoke All Other Sessions".

---

## Configuration

### SecurityStamp Validation Interval

Administrators can adjust how quickly sessions are revoked after clicking "Revoke All Other Sessions".

**Location:** `appsettings.json` (production) or `appsettings.Development.json` (development)

```json
{
  "Identity": {
    "SecurityStampValidationIntervalSeconds": 10
  }
}
```

**Recommended values:**
- **0 seconds** = Validate on every request (instant revocation, more DB queries)
- **5-10 seconds** = Near-instant revocation with minimal overhead ✅ (default production)
- **30 seconds** = Good balance for high-traffic applications
- **60+ seconds** = Low-security, high-traffic scenarios (not recommended)

**Trade-offs:**

| Interval | Revocation Speed | Database Load | Use Case |
|----------|------------------|---------------|----------|
| 0s | Instant | High | Development, testing |
| 10s | Near-instant | Low | Production (default) ✅ |
| 30s | Acceptable | Very low | High-traffic apps |
| 60s+ | Slow | Minimal | Low-security scenarios |

**To change:**
1. Edit `appsettings.json` (for production) or `appsettings.Development.json` (for development)
2. Change `SecurityStampValidationIntervalSeconds` to desired value
3. Restart the application (or deploy new configuration to Azure)

**Example production configuration (instant revocation):**
```json
{
  "Identity": {
    "SecurityStampValidationIntervalSeconds": 0
  }
}
```

**Note:** Lower values = more database queries but faster session revocation. Choose based on your security and performance requirements.

---

## User Flows

### First-Time Setup

1. Register account → Email is used as display name
2. Go to **Account Settings**
3. Set a friendly display name (e.g., "John Doe")
4. Return to home page → See "Hello John Doe!"

### Security Check Routine

**Recommended: Check your account weekly**

1. Go to **Account Settings**
2. Review "Previous Login" timestamp
3. **If recognized:**
   - ✅ All good - no action needed
4. **If NOT recognized:**
   - ⚠️ Click "Revoke All Other Sessions"
   - Change your password immediately
   - Review recent activity (coming soon)

### Lost/Stolen Device

1. Log in from a secure device
2. Go to **Account Settings**
3. Click **Revoke All Other Sessions**
4. Confirm action
5. Lost device is now logged out
6. (Optional) Change password for extra security

### Public Computer Cleanup

**Scenario:** You logged in on a library/hotel computer and forgot to log out

1. Get to a secure device
2. Log in to your account
3. Go to **Account Settings**
4. Click **Revoke All Other Sessions**
5. Public computer session is now invalid

---

## Database Schema

### ApplicationUser Table (AspNetUsers)

New columns added:

| Column | Type | Nullable | Description |
|--------|------|----------|-------------|
| DisplayName | nvarchar(100) | Yes | User's optional display name |
| LastLoginAt | datetime2 | Yes | Most recent login timestamp |
| PreviousLoginAt | datetime2 | Yes | Previous login (before current) |
| CurrentSessionStartedAt | datetime2 | Yes | When current session started |

**Migration:**
```bash
dotnet ef migrations add AddUserProfileFields
dotnet ef database update
```

**Generated SQL:**
```sql
ALTER TABLE [AspNetUsers] ADD [DisplayName] nvarchar(100) NULL;
ALTER TABLE [AspNetUsers] ADD [LastLoginAt] datetime2 NULL;
ALTER TABLE [AspNetUsers] ADD [PreviousLoginAt] datetime2 NULL;
ALTER TABLE [AspNetUsers] ADD [CurrentSessionStartedAt] datetime2 NULL;
```

---

## Security Considerations

### Data Privacy

- **Display names** are stored in plain text (no encryption needed - not sensitive)
- **Login timestamps** are stored in UTC and converted to local time for display
- **Session revocation** uses ASP.NET Identity's SecurityStamp (industry standard)

### Input Validation

Display name is protected by **three layers** of validation:

1. **Client-side:** HTML `maxlength="100"` for immediate feedback
2. **Server-side:** `[StringLength(100)]` validation in page model
3. **Database:** `nvarchar(100)` constraint prevents oversized strings

This prevents DoS attacks via gigabyte-sized display names. See [Security: DoS Protection](Security/SECURITY_FIX_DOS_PROTECTION.md) for details.

### Session Security

**How SecurityStamp works:**
1. Each user has a `SecurityStamp` GUID in the database
2. When a user logs in, the SecurityStamp is stored in their auth cookie
3. On each request (or at validation interval), ASP.NET Identity checks:
   - Does the cookie's SecurityStamp match the database?
   - If **yes** → Allow request
   - If **no** → Reject request, redirect to login
4. When "Revoke All Other Sessions" is clicked:
   - A new SecurityStamp GUID is generated
   - Current session is immediately re-signed in with new stamp
   - Other sessions still have old stamp → rejected on next validation

**Security properties:**
- ✅ Cannot be bypassed (enforced by ASP.NET Identity middleware)
- ✅ Current session preserved (re-signed immediately)
- ✅ Other sessions invalidated (checked at validation interval)
- ✅ No custom code needed (uses built-in ASP.NET Identity features)

---

## Troubleshooting

### Display name not updating

**Symptom:** Changed display name but home page still shows email

**Solution:**
1. Refresh the page (Ctrl+R or Cmd+R)
2. Check that "Your profile has been updated" message appeared
3. If still not working, clear browser cache

### "Revoke All Other Sessions" not working

**Symptom:** Other devices still logged in after revoking sessions

**Possible causes:**
1. **Validation interval delay** - Other sessions may take up to 30 seconds to log out (check `appsettings.json`)
2. **Old browser cache** - Other device may need a hard refresh

**How to verify:**
1. Check `appsettings.json` → `Identity:SecurityStampValidationIntervalSeconds`
2. Wait for interval to elapse
3. On other device, try to navigate to any page
4. Should be redirected to login

**Quick fix for instant revocation:**
Set `SecurityStampValidationIntervalSeconds: 0` in `appsettings.json` and restart app.

### "Previous Login" shows "No previous login recorded"

**Cause:** First login after feature was deployed, or database was reset

**Solution:** This is normal. After your next login, this field will populate.

---

## API Reference

### Account Settings Page

**Route:** `/Identity/Account/Manage`

**GET Request:**
- Loads user's current display name, login timestamps
- Requires authentication

**POST Requests:**

#### Update Profile
**Handler:** `OnPostUpdateProfileAsync`

**Form Data:**
```
Input.DisplayName=John Doe
```

**Validation:**
- Display name max 100 characters
- Connection must belong to authenticated user

**Response:**
- Success: Redirect to same page with status message
- Failure: Return page with validation errors

#### Revoke Other Sessions
**Handler:** `OnPostRevokeOtherSessionsAsync`

**Form Data:** (none)

**Confirmation:** JavaScript `confirm()` dialog

**Actions:**
1. Updates user's SecurityStamp
2. Re-signs in current user with new SecurityStamp
3. Redirects to same page with success message

**Response:**
- Success: "All other sessions have been revoked. You remain logged in on this device."

---

## Future Enhancements

Planned features for future releases:

### Profile Enhancements
- Profile picture upload
- Timezone selection (for login timestamp display)
- Email notifications preference

### Security Enhancements
- Two-factor authentication (2FA)
- Login history table (last 10 logins with IP/device info)
- Suspicious login detection
- Password strength meter
- Account activity log

### Session Management
- View active sessions with details:
  - Device type (desktop/mobile)
  - Browser and version
  - IP address and location
  - Last active timestamp
- Selective session revocation (revoke specific devices)
- Session expiration settings

---

## Related Documentation

- [Architecture: Authentication & Authorization](ARCHITECTURE.md#authentication--authorization)
- [Security: DoS Protection](Security/SECURITY_FIX_DOS_PROTECTION.md)
- [API Reference: Identity Endpoints](API_REFERENCE.md#identity-endpoints)
- [Setup: Local Development](SETUP.md)

---

## Changelog

### Version 1.0 (October 23, 2024)

**Features:**
- ✅ Display name customization (optional, 100 char limit)
- ✅ Login activity tracking (current + previous login timestamps)
- ✅ Session revocation (revoke all other sessions)
- ✅ Configurable SecurityStamp validation interval

**Security:**
- ✅ Three-layer validation for display name
- ✅ DoS protection via string length constraints
- ✅ Uses ASP.NET Identity SecurityStamp (industry standard)

**Configuration:**
- ✅ `SecurityStampValidationIntervalSeconds` setting in appsettings.json
- ✅ Development default: 0s (instant)
- ✅ Production default: 10s (near-instant)
