# Architecture

This document describes the technical architecture of the QRStickers application.

## Technology Stack

- **.NET 9.0** with ASP.NET Core Razor Pages
- **ASP.NET Identity** v9.0.0 - User authentication and authorization
- **Entity Framework Core** v9.0.0 with **SQL Server**:
  - **SQL Server LocalDB** - Local development (file-based, zero-config, free)
  - **Azure SQL Database** - Production (free tier, serverless, managed identity)
- **QRCoder** v1.7.0 - QR code generation library
- **Fabric.js** v5.3.0 - HTML5 Canvas library for visual template designer
- **SignalR** - Real-time sync status and export progress updates
- **Azure Service Connector** - Passwordless database authentication (production)
- **xUnit** - Unit testing framework (.NET 10.0 test project)
- **Moq** - Mocking framework for unit tests
- **Docker** - Multi-stage containerization
- **Azure** - Deployment targets (Container Apps and Web Apps)
- **GitHub Actions** - CI/CD pipelines

## Project Structure

```
QRStickers/
├── src/                                    # Main application
│   ├── Program.cs                          # Application configuration & startup
│   ├── Connection.cs                       # Base connection class (TPH pattern)
│   ├── MerakiConnection.cs                 # Meraki-specific connection (inherits Connection)
│   ├── ApplicationUser.cs                  # Identity user model (owns Connections collection)
│   ├── QRStickersDbContext.cs              # Entity Framework IdentityDbContext with TPH config
│   ├── StickerTemplate.cs                  # Sticker template data model
│   ├── ConnectionDefaultTemplate.cs        # Template defaults per connection
│   ├── UploadedImage.cs                    # Custom image storage model
│   ├── GlobalVariable.cs                   # Per-connection custom variables
│   ├── ExportHistory.cs                    # Export operation audit log
│   ├── SyncStatus.cs                       # Sync status per connection
│   ├── SyncStatusHub.cs                    # SignalR hub for real-time sync updates
│   ├── MerakiBackgroundSyncService.cs      # Background service for periodic syncing
│   ├── TemplateService.cs                  # Template business logic
│   ├── Device.cs, Network.cs, Organization.cs  # Domain entity models
│   │
│   ├── Models/                             # API request/response models
│   │   ├── ImageUploadRequest.cs           # Image upload request model
│   │   ├── ImageListResponse.cs            # Image list response model
│   │   ├── UsageTrackRequest.cs            # Usage tracking request model
│   │   └── PdfExportRequest.cs             # PDF export request model
│   │
│   ├── Data/                               # Database configuration and seeding
│   │   └── SystemTemplateSeeder.cs         # Seeds default system templates
│   │
│   ├── Services/                           # Business logic services
│   │   ├── TemplateMatchingService.cs      # Intelligent template-to-device matching
│   │   ├── QRCodeGenerationService.cs      # QR code generation during sync
│   │   ├── ImageUploadValidator.cs         # Image upload validation
│   │   ├── PdfExportService.cs             # PDF export functionality
│   │   └── DeviceExportHelper.cs           # Device data retrieval for exports
│   │
│   ├── Meraki/                             # Meraki-specific components
│   │   ├── MerakiOAuthToken.cs             # OAuth refresh token per connection
│   │   ├── MerakiAccessTokenCache.cs       # In-memory access token cache (singleton)
│   │   ├── MerakiApiClient.cs              # Meraki OAuth & API client (low-level HTTP)
│   │   ├── MerakiService.cs                # High-level Meraki service with auto token refresh
│   │   ├── MerakiServiceFactory.cs         # Factory for creating connection-specific services
│   │   ├── MerakiSyncOrchestrator.cs       # Orchestrates syncing from API to cache
│   │   ├── CachedOrganization.cs           # Cached Meraki organization per connection
│   │   ├── CachedNetwork.cs                # Cached Meraki network per connection
│   │   └── CachedDevice.cs                 # Cached Meraki device per connection
│   │
│   ├── Pages/                              # Razor Pages UI
│   │   ├── Index.cshtml                    # Home page (connection list)
│   │   ├── Privacy.cshtml, Terms.cshtml    # Legal pages
│   │   ├── Identity/Account/               # Identity pages
│   │   │   ├── Login.cshtml, Register.cshtml, Logout.cshtml
│   │   │   └── Manage/Index.cshtml         # User profile management
│   │   ├── Connections/                    # Connection management
│   │   │   ├── Index.cshtml, Create.cshtml, Delete.cshtml
│   │   ├── Meraki/                         # Meraki OAuth and data pages
│   │   │   ├── Connect.cshtml              # Initiate OAuth flow
│   │   │   ├── Callback.cshtml             # OAuth callback handler
│   │   │   ├── SyncStatus.cshtml           # Real-time sync progress
│   │   │   ├── Connection.cshtml           # Organizations view
│   │   │   └── Network.cshtml              # Networks and devices view
│   │   ├── Templates/                      # Sticker template management
│   │   │   ├── Index.cshtml                # List templates
│   │   │   ├── Create.cshtml, Edit.cshtml, Delete.cshtml
│   │   │   ├── Designer.cshtml             # Visual template designer
│   │   │   └── ConnectionDefaults.cshtml   # Default template settings
│   │   ├── Images/                         # Custom image management
│   │   │   └── Index.cshtml
│   │   └── Shared/                         # Shared layouts and partials
│   │       └── _Layout.cshtml              # Main layout with navigation
│   │
│   ├── wwwroot/                            # Static assets
│   │   ├── js/
│   │   │   ├── designer.js                 # Template designer canvas logic (~1000 lines)
│   │   │   ├── fabric-extensions.js        # Custom Fabric.js objects (QR codes, bound text)
│   │   │   ├── export-progress.js          # SignalR export progress UI
│   │   │   └── device-export.js            # Device export workflow
│   │   └── css/
│   │       └── designer.css                # Designer modal and UI styles
│   │
│   └── Migrations/                         # EF Core migrations
│
└── QRStickers.Tests/                       # Unit test project (.NET 10.0)
    ├── Services/                           # Service layer tests
    │   ├── TemplateMatchingServiceTests.cs # Template matching algorithm tests
    │   ├── QRCodeGenerationServiceTests.cs # QR code generation tests
    │   └── ImageUploadValidatorTests.cs    # Image validation tests
    └── Helpers/                            # Test helper utilities
        ├── InMemoryDbContextFactory.cs     # In-memory DB for testing
        └── TestDataBuilder.cs              # Test data generation
```

## Database Architecture

### Database Provider Strategy

The application uses **SQL Server** for both development and production to avoid migration conflicts:

| Environment | Provider | Database | Authentication |
|------------|----------|----------|----------------|
| **Development** | SQL Server LocalDB | File-based local database | Windows Authentication |
| **Production** | Azure SQL Database | Cloud-hosted serverless | Managed Identity (passwordless) |

**Why SQL Server Only?**

EF Core migrations are database-provider-specific. SQLite and SQL Server use different:
- SQL syntax
- Data types
- Constraint implementations

Using the same provider eliminates migration conflicts between environments.

### Database Schema

#### Core Tables

**ASP.NET Identity Tables:**
- `AspNetUsers` - User accounts
- `AspNetRoles` - User roles
- `AspNetUserRoles` - User-role mappings
- `AspNetUserClaims` - User claims
- `AspNetUserLogins` - External login providers
- `AspNetUserTokens` - User tokens
- `AspNetRoleClaims` - Role claims

#### Application Tables

**Connections (Table-per-Hierarchy):**
- `Connections` - Base connection table (TPH with discriminator)
  - **Discriminator:** `ConnectionType` ("Meraki", future: "LogicMonitor", etc.)
  - Stores: `Id`, `UserId` (FK), `DisplayName`, `ConnectionType`, `IsActive`, `CreatedAt`, `UpdatedAt`, `CompanyLogoUrl`

**OAuth & Sync:**
- `MerakiOAuthTokens` - Refresh tokens per connection (FK: `ConnectionId`)
- `SyncStatuses` - Sync progress per connection (1-to-1 with Connection)

**Cached Meraki Data:**
- `CachedOrganizations` - Meraki orgs per connection (FK: `ConnectionId`)
- `CachedNetworks` - Meraki networks per connection (FK: `ConnectionId`)
- `CachedDevices` - Meraki devices per connection (FK: `ConnectionId`)

**Templates:**
- `StickerTemplates` - Template definitions with Fabric.js JSON
- `TemplateDeviceModels` - Model-specific template mappings (many-to-one with Templates)
- `TemplateDeviceTypes` - Device type template mappings (many-to-one with Templates)

**Assets:**
- `CustomImages` - User-uploaded images (logos, icons)
- `GlobalVariables` - Per-connection custom variables

**Tracking:**
- `TemplateUsageHistory` - Template and image usage analytics
- `ExportHistory` - Export operation audit log (dormant - for future use)

### Table-per-Hierarchy (TPH) Pattern

The `Connection` table uses **TPH inheritance** for extensibility:

```csharp
// Base class
public abstract class Connection
{
    public int Id { get; set; }
    public string UserId { get; set; }  // FK to ApplicationUser
    public string DisplayName { get; set; }
    public string ConnectionType { get; set; }  // Discriminator
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public string? CompanyLogoUrl { get; set; }
}

// Derived class
public class MerakiConnection : Connection
{
    // Meraki-specific properties (if any)
}
```

**Benefits:**
- Single table for all connection types
- Efficient queries with discriminator filtering
- Easy to add new connection types (e.g., `LogicMonitorConnection`)
- Shared columns (DisplayName, IsActive, etc.)

### Foreign Key Relationships

```
ApplicationUser (1) ──→ (∞) Connection
    │
    └──→ Cascade Delete: Deletes all connections when user deleted

Connection (1) ──→ (1) MerakiOAuthToken
    │
    ├──→ (∞) CachedOrganization
    ├──→ (∞) CachedNetwork
    ├──→ (∞) CachedDevice
    ├──→ (1) SyncStatus
    ├──→ (∞) StickerTemplate
    ├──→ (∞) CustomImage
    └──→ (∞) GlobalVariable

    └──→ Cascade Delete: All related data deleted when connection deleted

StickerTemplate (1) ──→ (∞) TemplateDeviceModel
StickerTemplate (1) ──→ (∞) TemplateDeviceType
StickerTemplate (1) ──→ (∞) TemplateUsageHistory

CustomImage (1) ──→ (∞) TemplateUsageHistory
```

## Token Management

The application uses a **hybrid token persistence strategy** to balance security with performance:

### Access Tokens (In-Memory Cache)

- **Storage:** `MerakiAccessTokenCache` singleton
- **Lifetime:** 1 hour (Meraki default)
- **Key:** `connectionId` (per-connection isolation)
- **Behavior:** Auto-removed when expired
- **Benefit:** Reduces database queries by 50-80% during active sessions
- **Limitation:** Lost on application restart

### Refresh Tokens (Database Persistent)

- **Storage:** `MerakiOAuthTokens` table (SQL Server)
- **Lifetime:** 90 days (Meraki default)
- **Key:** `ConnectionId` (unique index - one token per connection)
- **Columns:**
  - `Id` - Primary key
  - `ConnectionId` - Foreign key to Connection (unique)
  - `RefreshToken` - The refresh token string
  - `RefreshTokenExpiresAt` - Expiration timestamp
  - `CreatedAt` - Token creation timestamp
  - `UpdatedAt` - Last update timestamp
- **Benefit:** Survives app restarts, enables refresh per connection
- **Lifecycle:** Cascade deleted with connection or user

### Token Refresh Flow

1. API request made for specific `connectionId`
2. `MerakiService.GetAccessTokenAsync()` called
3. Check `MerakiAccessTokenCache` for valid token (5-min expiry buffer)
4. **If cache miss:**
   - Retrieve `RefreshToken` from database via `ConnectionId`
   - Call `MerakiApiClient.RefreshAccessTokenAsync()`
   - Store new access token in cache (keyed by `connectionId`)
   - Update refresh token in database if rotated
5. API request proceeds with fresh access token

### Service Layers

**Low-Level:**
- `MerakiApiClient` - HTTP client for OAuth and API endpoints

**High-Level:**
- `MerakiService` - Scoped service with automatic token refresh (per connection)
- `MerakiServiceFactory` - Creates connection-specific `MerakiService` instances
- `MerakiAccessTokenCache` - Singleton cache preventing repeated token refreshes

## Template Designer Architecture

### Client-Side Rendering with Fabric.js

The template designer uses **Fabric.js** for canvas-based visual editing:

```
User Interaction
    ↓
designer.js (~1000 lines)
    ↓
Fabric.js Canvas (HTML5)
    ↓
Custom Objects (fabric-extensions.js)
    ├── QRCodeGroup - QR code with internal structure
    ├── BoundText - Text with data binding
    └── BoundImage - Image with placeholder support
    ↓
Template JSON (stored in StickerTemplate.TemplateJson)
```

### Template Matching Algorithm

The `TemplateMatchingService` uses a **6-level priority cascade**:

1. **Model Match** - Exact device model (e.g., "MS225-48FP")
2. **Type Match** - Device type from model (e.g., "switch" from "MS225")
3. **ProductTypeFilter Match** - Templates with matching `ProductTypeFilter`
4. **User Default** - User's default template for connection
5. **System Default** - Global fallback template
6. **Any Template** - First available template

**ProductTypeFilter as Hard Filter:**

Templates with `ProductTypeFilter` set (e.g., "sensor") ONLY match devices with that product type. `NULL` ProductTypeFilter acts as universal template.

```csharp
// Step 3: User default with ProductTypeFilter filter
.Where(t => t.ProductTypeFilter == null || t.ProductTypeFilter.ToLower() == productType)
```

### Data Binding System

Templates support dynamic placeholders replaced at export time:

**Syntax:** `{{source.property}}`

**Available Sources:**
- `device.Serial`, `device.Name`, `device.Model`, `device.QRCode`
- `network.Name`, `network.Id`, `network.QRCode`
- `organization.Name`, `organization.Id`, `organization.QRCode`
- `connection.CompanyLogoUrl`
- `global.variableName` (custom user-defined variables)

**Matching:** Case-insensitive with fallback to original text

### Export Pipeline

```
Template JSON + Device Data
    ↓
bindTemplateData() - Replace placeholders
    ↓
Fabric.js loadFromJSON()
    ↓
Canvas rendering (mm → px conversion with DPI)
    ↓
Export Format Selection:
    ├── PNG (96/150/300 DPI) - canvas.toDataURL()
    ├── SVG - canvas.toSVG()
    └── PDF - Multiple canvas pages combined
    ↓
Client-side download (Blob + download link)
    ↓
Track usage (TemplateUsageHistory)
```

## Performance Optimizations

### Caching Strategy

1. **Access Token Cache** (singleton) - 50-80% reduction in DB queries
2. **Template Matching Cache** (30-minute TTL) - Fast device-to-template mapping
3. **QR Code Pre-generation** - Generated once during sync, stored as data URIs

### Database Optimizations

- **Indexes:**
  - `ConnectionId` on all cached tables (fast lookups)
  - `ConnectionId` unique index on `MerakiOAuthTokens`
  - Composite indexes on template matching tables
- **No Tracking:** Most queries use `.AsNoTracking()` for read-only data
- **Eager Loading:** `.Include()` navigation properties to avoid N+1 queries

### Background Sync

- **Fire-and-forget:** Sync runs in background after OAuth callback
- **SignalR updates:** Real-time progress without polling
- **Incremental sync:** Only updates changed data (checks `LastSyncedAt`)

## Security Considerations

### Authentication & Authorization

- **Cookie-based authentication** - ASP.NET Identity with 7-day persistent cookies
- **Authorization attributes** - `[Authorize]` on all protected pages
- **Connection ownership validation** - All queries filter by `UserId` or `ConnectionId`

### Token Security

- **Refresh tokens** stored in database (not cookies or localStorage)
- **Access tokens** cached in-memory only (never exposed to client)
- **Client secrets** stored in User Secrets (development) and Azure App Configuration (production)

### Input Validation

- **XSS Protection** - All user input sanitized before rendering (see `SECURITY_FIX_XSS_VULNERABILITIES.md`)
- **File Upload Validation** - Image type and size validation in `ImageProcessingService`
- **SQL Injection Prevention** - Parameterized queries via EF Core

### Rate Limiting

- **QR Code API** - Token bucket algorithm (5000 tokens, 100/sec replenishment)
- **Export API** - Per-user concurrency limits (future enhancement)

## Learning Objectives

This architecture demonstrates:

### Core Platform
- ASP.NET Core Razor Pages architecture
- ASP.NET Identity authentication and authorization
- OAuth 2.0 authorization code flow with token refresh
- Multi-connection architecture with user-defined naming
- Table-per-Hierarchy (TPH) inheritance pattern
- SQL Server architecture (LocalDB + Azure SQL)
- Azure Service Connector for passwordless auth
- Managed identity authentication
- Hybrid token persistence (in-memory + database)
- Singleton pattern for cross-request caching
- Factory pattern for scoped service creation
- EF Core migrations and relationships
- Foreign key relationships and cascade deletions
- Background data synchronization with SignalR
- Docker multi-stage containerization
- Azure deployment (Web Apps, Container Apps, SQL Database)
- Dependency injection and service lifetimes
- Performance optimization through caching

### Template Designer
- Fabric.js canvas library for visual editors
- Client-side rendering with HTML5 Canvas
- Coordinate system conversion (mm ↔ px with DPI)
- Data binding with dynamic placeholders
- Template matching algorithms with priority cascades
- Export strategies (PNG raster vs SVG vector)
- Canvas-to-image conversion (toDataURL, Blob API)
- Custom Fabric.js object extensions
- Form state management with ASP.NET Core
- Service layer patterns for business logic
- Authorization in APIs
- Audit logging for compliance
- Testing-driven bug fixes
