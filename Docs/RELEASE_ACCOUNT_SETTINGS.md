# Release Notes: Account Settings & Security Enhancements

**Release Date:** October 23, 2024
**Version:** 1.1.0
**Type:** Feature Release + Security Fixes

---

## Overview

This release introduces user account self-management features and critical security enhancements to prevent Denial of Service (DoS) attacks.

---

## ✨ New Features

### Account Settings Page

**Location:** `/Identity/Account/Manage` (accessible via top navigation)

#### 1. Display Name Customization
- Set a personalized display name (optional, max 100 characters)
- Defaults to email address if not set
- Appears on home page greeting and throughout the app

**User Benefit:** More personalized, professional user experience

#### 2. Login Activity Tracking
- View current session start time
- View previous login timestamp
- Automatic tracking on every login
- Displayed in local timezone

**User Benefit:** Security awareness - detect unauthorized access

#### 3. Session Management
- "Revoke All Other Sessions" button
- Logs out all other devices/browsers
- Current session remains active
- Configurable validation interval (0-30 seconds)

**User Benefit:** Remote logout for lost devices or public computer cleanup

---

## 🔒 Security Enhancements

### DoS Protection via String Length Constraints

#### Fixed Vulnerabilities

1. **ApplicationUser.DisplayName**
   - Added `[MaxLength(100)]` database constraint
   - Prevents multi-gigabyte display names

2. **Connection.ConnectionType**
   - Added `[MaxLength(50)]` database constraint
   - Protects discriminator column from abuse

3. **StickerTemplate.TemplateJson**
   - Added 5 MB server-side validation
   - Prevents huge JSON payloads
   - Allows complex templates while blocking DoS

#### Defense in Depth

All user inputs now protected by three layers:
1. **Client-side:** HTML maxlength attributes (UX)
2. **Server-side:** Model validation (application layer)
3. **Database:** Column constraints (ultimate security boundary)

**Risk Mitigation:**
- Before: Unlimited string sizes → High DoS risk
- After: Constrained sizes → Low DoS risk

---

## 🔧 Configuration

### SecurityStamp Validation Interval

**New Setting:** `Identity:SecurityStampValidationIntervalSeconds`

**Location:** `appsettings.json` (production) or `appsettings.Development.json` (development)

**Values:**
- `0` = Validate on every request (instant session revocation, more DB queries)
- `10` = Validate every 10 seconds (default production) ✅
- `30` = Validate every 30 seconds (high-traffic apps)

**Example Configuration:**
```json
{
  "Identity": {
    "SecurityStampValidationIntervalSeconds": 10
  }
}
```

**Purpose:** Controls how quickly "Revoke All Other Sessions" takes effect on other devices

---

## 📦 Database Changes

### New Migrations

```bash
# Migration 1: Add user profile fields
dotnet ef migrations add AddUserProfileFields

# Migration 2: Limit DisplayName length (if not in Migration 1)
dotnet ef migrations add LimitDisplayNameLength

# Migration 3: Limit ConnectionType length
dotnet ef migrations add LimitConnectionTypeLength
```

### Schema Changes

**AspNetUsers table:**
- `DisplayName` nvarchar(100) NULL
- `LastLoginAt` datetime2 NULL
- `PreviousLoginAt` datetime2 NULL
- `CurrentSessionStartedAt` datetime2 NULL

**Connections table:**
- `ConnectionType` altered from nvarchar(max) to nvarchar(50)

---

## 📚 Documentation

### New Documentation Files

1. **[Account Settings User Guide](ACCOUNT_SETTINGS.md)**
   - Feature overview
   - How-to guides
   - Configuration
   - Troubleshooting

2. **[DoS Protection Security Doc](Security/SECURITY_FIX_DOS_PROTECTION.md)**
   - Vulnerability details
   - Technical implementation
   - Attack scenarios
   - Testing and monitoring

### Updated Documentation

3. **[Documentation Index](INDEX.md)**
   - Added Account Settings section
   - Added DoS Protection to Security section

---

## 🚀 Deployment Instructions

### Local Development

```bash
# 1. Pull latest code
git pull origin main

# 2. Create migrations
dotnet ef migrations add AddUserProfileFields
dotnet ef migrations add LimitConnectionTypeLength

# 3. Apply migrations
dotnet ef database update

# 4. Run application
dotnet run
```

### Azure Production

```bash
# 1. Deploy code (GitHub Actions will run automatically)
git push origin main

# 2. Apply migrations
# Option A: Auto-apply (Program.cs does this on startup)
# Option B: Manual via Azure Data Studio
dotnet ef migrations script -o migrations.sql
# Run migrations.sql against Azure SQL Database

# 3. Update configuration (if needed)
# Go to Azure Web App → Configuration → Application Settings
# Add: Identity__SecurityStampValidationIntervalSeconds = 10
# Note: Double underscore (__) for nested JSON

# 4. Restart web app
```

---

## 🧪 Testing Checklist

### Feature Testing

- [ ] Display name can be set and appears on home page
- [ ] Display name falls back to email if empty
- [ ] Display name limited to 100 characters (client + server + DB)
- [ ] Login timestamps populate on login
- [ ] "Previous Login" shows second-to-last login
- [ ] "Revoke All Other Sessions" logs out other browsers
- [ ] Current session remains active after revoke
- [ ] SecurityStamp validation interval works as configured

### Security Testing

- [ ] Cannot set display name > 100 characters (try bypassing client validation)
- [ ] Cannot set ConnectionType > 50 characters
- [ ] Cannot save template > 5 MB JSON
- [ ] Database rejects oversized strings (try direct SQL)
- [ ] Session revocation cannot be bypassed

### Regression Testing

- [ ] Existing user logins still work
- [ ] OAuth flows (Meraki) still work
- [ ] Template designer still works
- [ ] Image uploads still work
- [ ] All existing pages load without errors

---

## 🐛 Known Issues

None at this time.

---

## 📊 Impact Analysis

### Performance Impact

**Minimal:**
- SecurityStamp validation adds 1 DB query per interval
- With 10s interval: ~6 queries/minute per active user
- Negligible impact for < 1000 concurrent users

**Optimizations:**
- Validation only runs for authenticated users
- Cached in memory between validation intervals
- Can adjust interval based on traffic patterns

### User Experience Impact

**Positive:**
- More personalized greeting
- Better security awareness
- Remote session control

**Neutral:**
- Account Settings adds one nav link
- No changes to existing workflows

---

## 🔮 Future Enhancements

Potential features for future releases:

### Account Settings
- Profile picture upload
- Timezone selection
- Email notification preferences
- Two-factor authentication (2FA)

### Session Management
- View active sessions with details (device, IP, browser)
- Selective session revocation (revoke specific device)
- Session expiration settings

### Security
- Login history table (last 10 logins with location)
- Suspicious login detection
- Account activity log
- Password strength meter

---

## 📖 References

- **[Account Settings Documentation](ACCOUNT_SETTINGS.md)** - User guide
- **[DoS Protection Documentation](Security/SECURITY_FIX_DOS_PROTECTION.md)** - Security details
- **[Architecture Documentation](ARCHITECTURE.md)** - Technical architecture
- **[ASP.NET Identity Docs](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity)** - Microsoft documentation

---

## 👥 Contributors

- Claude Code (AI Assistant)
- Rich (Project Lead)

---

## 📝 Changelog

### Added
- ✅ Display name customization (optional, 100 char max)
- ✅ Login activity tracking (current + previous login)
- ✅ Session revocation (revoke all other sessions)
- ✅ Configurable SecurityStamp validation interval
- ✅ Database constraints for DoS protection
- ✅ Comprehensive documentation

### Changed
- ApplicationUser.DisplayName: null → nvarchar(100)
- Connection.ConnectionType: nvarchar(max) → nvarchar(50)
- StickerTemplate.TemplateJson: No validation → 5 MB limit

### Security
- ✅ Fixed DoS vulnerability in DisplayName (unlimited → 100 chars)
- ✅ Fixed DoS vulnerability in ConnectionType (unlimited → 50 chars)
- ✅ Fixed DoS vulnerability in TemplateJson (unlimited → 5 MB)
- ✅ Implemented three-layer validation (client, server, database)

### Documentation
- ✅ Created ACCOUNT_SETTINGS.md (user guide)
- ✅ Created SECURITY_FIX_DOS_PROTECTION.md (security documentation)
- ✅ Updated INDEX.md (documentation index)
- ✅ Created RELEASE_ACCOUNT_SETTINGS.md (this file)

---

**End of Release Notes**
