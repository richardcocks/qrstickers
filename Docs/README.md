# QR Stickers

An ASP.NET Core learning project demonstrating Razor Pages, ASP.NET Identity authentication, OAuth 2.0 integration, multi-connection architecture, containerization, and cloud deployment.

## Overview

QRStickers is a web application that combines user authentication with a flexible multi-connection architecture. Users can register accounts, log in, and connect multiple third-party service accounts (currently Cisco Meraki) via OAuth 2.0. Each connection has a user-defined display name and can be managed independently. The application also provides a public QR code generation API.

## Key Features

### Core Platform
- **User Authentication** - Email/password registration and login using ASP.NET Identity
- **Multi-Connection Architecture** - Connect multiple third-party accounts with user-defined display names
- **Meraki OAuth 2.0** - Connect one or more Cisco Meraki Dashboard accounts to access organization data
- **Connection Management** - Add, view, and delete connections through a dedicated management UI
- **Token Management** - Automatic OAuth token refresh and secure per-connection database storage
- **QR Code Generation** - Public API endpoint for generating QR codes with rate limiting

### QR Sticker Designer & Export (Phases 2-5)
- **Visual Template Designer** - Drag-and-drop canvas editor with Fabric.js for creating sticker templates
- **Template Management** - Create, clone, edit, and delete templates with connection-specific storage
- **Data Binding** - Dynamic placeholders ({{device.serial}}, {{device.name}}, etc.) for device data
- **Device Export** - Export individual devices from Network page with real device data
- **Intelligent Template Matching** - 6-level priority matching (Model → Type → ProductTypeFilter → User Default → System Default → Fallback)
- **Live Preview** - Real-time canvas preview with actual device data before export
- **Multi-Format Export** - PNG (96/150/300 DPI) and SVG formats with white or transparent backgrounds
- **Export History** - Automatic logging of all exports for analytics and audit trails

### UI & Deployment
- **Responsive UI** - Clean Razor Pages interface with shared layout and connection selectors
- **Containerized** - Docker support for easy deployment
- **Cloud Ready** - Configured for Azure deployment (Web Apps and Container Apps)
- **Extensible** - Table-per-Hierarchy design ready for adding new connection types (LogicMonitor, etc.)

## Technology Stack

- .NET 9.0 with ASP.NET Core Razor Pages
- ASP.NET Identity for user authentication
- Entity Framework Core 9.0 with **SQL Server**:
  - **SQL Server LocalDB** - Local development (file-based, zero-config, free)
  - **Azure SQL Database** - Production (free tier, serverless, managed identity)
- QRCoder library for QR code generation
- Fabric.js for visual template designer
- SignalR for real-time sync status updates
- Azure Service Connector for passwordless database authentication
- xUnit and Moq for unit testing (.NET 10.0 test project)
- Docker multi-stage builds
- GitHub Actions CI/CD

## Getting Started

### Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [Meraki Dashboard API credentials](https://developer.cisco.com/meraki/api-v1/) (optional, for OAuth features)

### Setup

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd QRStickers
   ```

2. **Configure Meraki OAuth (optional)**
   ```bash
   dotnet user-secrets set meraki_client_id <YOUR_CLIENT_ID>
   dotnet user-secrets set meraki_client_secret <YOUR_CLIENT_SECRET>
   ```

3. **Run the application**
   ```bash
   dotnet restore
   dotnet run
   ```

4. **Access the application**
   - Navigate to `https://localhost:7044` (HTTPS) or `http://localhost:5082` (HTTP)
   - Register a new account or login
   - Optionally connect your Meraki account via OAuth

### First Run

On first run, the application will automatically:
- Create the LocalDB database (`QRStickers`)
- Apply all EF Core migrations (Identity tables, Connections, OAuth tokens, etc.)
- **Note:** Requires SQL Server Express LocalDB (included with Visual Studio or SQL Server Express)

## Project Structure

```
QRStickers/
├── src/                              # Main application
│   ├── Program.cs                    # Application configuration, startup, API endpoints
│   ├── Connection.cs                 # Base connection class (TPH pattern)
│   ├── MerakiConnection.cs           # Meraki-specific connection specialization
│   ├── ApplicationUser.cs            # Identity user model (owns Connections collection)
│   ├── QRStickersDbContext.cs        # Entity Framework DbContext with TPH discriminator
│   ├── StickerTemplate.cs            # Sticker template data model
│   ├── ConnectionDefaultTemplate.cs  # Template defaults per connection
│   ├── ExportHistory.cs              # Export operation audit log
│   ├── SyncStatus.cs                 # Sync status tracking per connection
│   ├── SyncStatusHub.cs              # SignalR hub for real-time sync updates
│   ├── MerakiBackgroundSyncService.cs # Background service for periodic syncing
│   ├── TemplateService.cs            # Template business logic
│   ├── Device.cs, Network.cs, Organization.cs  # Domain entity models
│   ├── UploadedImage.cs              # Custom image storage model
│   ├── GlobalVariable.cs             # Per-connection custom variables
│   │
│   ├── Models/                       # API request/response models
│   │   ├── ImageUploadRequest.cs
│   │   ├── ImageListResponse.cs
│   │   ├── UsageTrackRequest.cs
│   │   └── PdfExportRequest.cs
│   │
│   ├── Services/                     # Business logic services
│   │   ├── DeviceExportHelper.cs     # Device data retrieval and export
│   │   ├── TemplateMatchingService.cs # Intelligent template-to-device matching
│   │   ├── QRCodeGenerationService.cs # QR code generation during sync
│   │   ├── ImageUploadValidator.cs   # Image upload validation
│   │   └── PdfExportService.cs       # PDF export functionality
│   │
│   ├── Data/                         # Database configuration and seeding
│   │   └── SystemTemplateSeeder.cs   # Seeds default system templates
│   │
│   ├── Meraki/                       # Meraki-specific components
│   │   ├── MerakiOAuthToken.cs       # OAuth token storage per connection
│   │   ├── MerakiAccessTokenCache.cs # In-memory token caching
│   │   ├── MerakiApiClient.cs        # Low-level Meraki OAuth and API client
│   │   ├── MerakiService.cs          # High-level Meraki service with auto token refresh
│   │   ├── MerakiServiceFactory.cs   # Factory for connection-specific services
│   │   ├── MerakiSyncOrchestrator.cs # Orchestrates syncing from API to cache
│   │   ├── CachedOrganization.cs     # Cached Meraki organization per connection
│   │   ├── CachedNetwork.cs          # Cached Meraki network per connection
│   │   └── CachedDevice.cs           # Cached Meraki device per connection
│   │
│   ├── Pages/                        # Razor Pages UI
│   │   ├── Index.cshtml              # Home page (connection list)
│   │   ├── Privacy.cshtml, Terms.cshtml # Legal pages
│   │   ├── Identity/Account/         # Identity pages
│   │   │   ├── Login.cshtml, Register.cshtml, Logout.cshtml
│   │   │   └── Manage/Index.cshtml   # User profile management
│   │   ├── Connections/              # Connection management
│   │   │   ├── Index.cshtml, Create.cshtml, Delete.cshtml
│   │   ├── Meraki/                   # Meraki OAuth and data pages
│   │   │   ├── Connect.cshtml, Callback.cshtml
│   │   │   ├── SyncStatus.cshtml     # Real-time sync progress
│   │   │   ├── Connection.cshtml     # Organizations view
│   │   │   └── Network.cshtml        # Networks and devices view
│   │   ├── Templates/                # Sticker template management
│   │   │   ├── Index.cshtml, Create.cshtml, Edit.cshtml, Delete.cshtml
│   │   │   ├── Designer.cshtml       # Visual template designer
│   │   │   └── ConnectionDefaults.cshtml # Default template settings
│   │   ├── Images/                   # Custom image management
│   │   │   └── Index.cshtml
│   │   └── Shared/
│   │       └── _Layout.cshtml        # Main layout with navigation
│   │
│   ├── wwwroot/                      # Static assets
│   │   ├── js/
│   │   │   ├── designer.js           # Template designer canvas logic
│   │   │   ├── fabric-extensions.js  # Custom Fabric.js objects (QR, text, images)
│   │   │   ├── export-progress.js    # SignalR export progress UI
│   │   │   └── device-export.js      # Device export workflow
│   │   └── css/
│   │       └── designer.css          # Designer modal and UI styles
│   │
│   └── Migrations/                   # EF Core migrations
│
└── QRStickers.Tests/                 # Unit test project
    ├── Services/
    │   ├── TemplateMatchingServiceTests.cs
    │   ├── QRCodeGenerationServiceTests.cs
    │   └── ImageUploadValidatorTests.cs
    └── Helpers/
        ├── InMemoryDbContextFactory.cs
        └── TestDataBuilder.cs
```

## API Endpoints

**Device Export (Authenticated):**
- `GET /api/export/device/{id}?connectionId={id}` - Retrieve device data + matched template
- `GET /api/templates/match?deviceId={id}&connectionId={id}` - Template matching + alternatives

## User Flows

### Authentication Flow
1. Register → Email + Password → Account created
2. Login → Authentication cookie → Access protected pages
3. Logout → Clear session → Return to home

### Connection Management Flow
1. Login required → Navigate to "Connections"
2. Click "Add New Connection"
3. Enter display name (e.g., "Work Account", "Home Network")
4. Select connection type (currently: Cisco Meraki)
5. Redirect to provider OAuth → User authorizes
6. Callback creates connection → Store refresh token linked to connection
7. Background sync starts automatically
8. View/manage multiple connections independently
9. Delete connection → Cascade delete token and all cached data

### Meraki Integration Flow
1. Create Meraki connection (see above)
2. Redirected to sync status page → Shows real-time sync progress
3. View organizations → Select connection if multiple exist
4. View networks → See networks and device counts per organization
5. Access token auto-refreshed when expired (cached in-memory for performance)
6. Switch between connections via dropdown selector
7. Delete connection → Remove token, cached data, and sync status

## Database

The application uses **SQL Server** for both development and production:

### Development (SQL Server LocalDB)
- **Provider:** SQL Server LocalDB - file-based, free, included with Visual Studio
- **Database Name:** `QRStickers`
- **Connection String:** `Server=(localdb)\\mssqllocaldb;Database=QRStickers;Trusted_Connection=True;MultipleActiveResultSets=true`
- **Configuration:** Set in `appsettings.Development.json`
- **Benefits:**
  - Zero-cost (included with Visual Studio or SQL Server Express)
  - File-based automatic database creation (like SQLite)
  - Full SQL Server features (same as production)
  - **No migration conflicts** - same provider as production
- **Installation:** Comes with Visual Studio or install [SQL Server Express](https://www.microsoft.com/en-us/sql-server/sql-server-downloads)

### Production (Azure SQL Database)
- **Provider:** Azure SQL Database - serverless, auto-pause
- **Free Tier:** 100,000 vCore seconds/month + 32 GB storage (**FREE FOREVER**)
- **Authentication:** Managed identity (passwordless) via Azure Service Connector
- **Auto-pause:** Database pauses after 1 hour of inactivity (zero cost when paused)
- **Benefits:** Production-ready, proper FK constraints, auto-scaling, no passwords needed
- **Setup Instructions:** See "Production Deployment" section below

### Why SQL Server Only?
EF Core migrations are database-provider-specific. SQLite migrations won't work with SQL Server due to different SQL syntax, data types, and constraints. Using the same provider for development and production eliminates migration conflicts.

### Database Schema
- **ASP.NET Identity tables** - User accounts, roles, claims
- **Connections** - User's third-party connections (TPH: MerakiConnection, etc.)
- **MerakiOAuthTokens** - OAuth refresh tokens per connection (FK to Connections)
- **CachedOrganizations** - Cached Meraki organizations per connection
- **CachedNetworks** - Cached Meraki networks per connection
- **CachedDevices** - Cached Meraki devices per connection
- **SyncStatuses** - Sync status and progress per connection

### Table-per-Hierarchy (TPH) Pattern

The `Connection` table uses TPH inheritance:
- Base class: `Connection` (Id, UserId, DisplayName, ConnectionType, IsActive, CreatedAt, UpdatedAt)
- Discriminator column: `ConnectionType` ("Meraki", future: "LogicMonitor", etc.)
- Derived classes stored in same table with shared columns

### Migrations

```bash
# Create new migration
dotnet ef migrations add <MigrationName>

# Apply migrations
dotnet ef database update

# Remove last migration (if not applied)
dotnet ef migrations remove
```

## Token Management

The application uses a hybrid token persistence strategy:

- **Refresh Tokens** - Stored securely in SQL Server per connection (LocalDB for dev, Azure SQL for prod)
  - Lifetime: 90 days for Meraki
  - Survives application restarts
- **Access Tokens** - Cached in-memory only via singleton `MerakiAccessTokenCache`
  - Lifetime: 1 hour
  - Cleared on application restart
- **Benefits:**
  - Reduces database queries by 50-80% during active sessions
  - Automatic refresh when access token expires
  - Secure: refresh tokens never exposed to client
  - Per-connection isolation: each connection has independent token lifecycle
  - Consistent database provider across environments (no migration conflicts)

## Docker

Build and run with Docker:

```bash
docker build -t qrstickers:latest .
docker run -p 8080:8080 qrstickers:latest
```

**Note:** Production uses Azure SQL Database (cloud-hosted). No volume mounting needed for database persistence.

## Production Deployment (Azure)

### Option 1: Azure Service Connector (Recommended - Passwordless)

Azure Service Connector automates the connection between your Web App and Azure SQL Database using managed identity.

**Prerequisites:**
- Azure subscription
- Azure Web App deployed
- Azure SQL Database created (free tier available)

**Steps:**

1. **Create Azure SQL Database** (via Azure Portal):
   - Navigate to **Create a resource** → **SQL Database**
   - Configure:
     - Compute tier: **Serverless**
     - Service tier: **General Purpose**
     - vCores: **0.5 - 4 vCores**
     - Data max size: **32 GB** (within free tier)
     - Auto-pause: **Yes** (1 hour delay)
   - Enable **Microsoft Entra authentication** (required for managed identity)

2. **Connect Web App to SQL Database**:
   - Go to your **Azure Web App** → **Service Connector** (left menu)
   - Click **+ Create** → **SQL Database**
   - Select your SQL server and database
   - Client type: **.NET**
   - Authentication: **System-assigned managed identity** (passwordless!)
   - Click **Create**

3. **Configure Application Settings**:
   - Go to **Web App** → **Configuration** → **Application settings**
   - Rename `AZURE_SQL_CONNECTIONSTRING` to `ConnectionStrings__DefaultConnection`
     - Note: Double underscore `__` is Azure's separator for nested JSON
   - Click **Save** and restart

4. **Deploy and Verify**:
   - App auto-runs migrations on startup
   - Connection deletion now works with proper cascade deletes
   - No passwords stored anywhere!

**What Service Connector Does Automatically:**
- ✅ Enables managed identity on Web App
- ✅ Grants database permissions to the identity
- ✅ Creates passwordless connection string
- ✅ Configures Microsoft Entra (Azure AD) authentication
- ✅ No secrets in configuration

### Option 2: Manual Connection String (SQL Authentication)

If you prefer username/password authentication:

1. **Get Connection String** from Azure Portal → SQL Database → Connection strings
2. **Configure Web App**:
   - Go to **Configuration** → **Connection strings**
   - Add connection string:
     - Name: `DefaultConnection`
     - Value: `Server=tcp:{server}.database.windows.net,1433;Database=qrstickers;User ID={admin};Password={password};Encrypt=True;`
     - Type: `SQLServer`
3. Save and restart

### Azure SQL Free Tier Limits

- ✅ **100,000 vCore seconds/month** (enough for low-traffic apps running continuously)
- ✅ **32 GB storage** + 32 GB backup
- ✅ **Auto-pause when inactive** (no charges during pause)
- ✅ **Forever free** (renews monthly, no expiration)
- ✅ **Monitor usage** in Azure Portal → SQL Database → Metrics

## Learning Objectives

This project demonstrates:

### Core Platform Architecture
- ASP.NET Core Razor Pages architecture
- ASP.NET Identity authentication and authorization
- OAuth 2.0 authorization code flow with token refresh and rotation
- **Multi-connection architecture** - Users managing multiple third-party integrations
- **Table-per-Hierarchy (TPH) inheritance** - Flexible connection type extensibility
- **User-defined connection naming** - Passing state through OAuth flow
- Separating authentication (identity) from authorization (access tokens)
- **SQL Server architecture** - LocalDB for dev, Azure SQL for prod (no migration conflicts)
- **Azure Service Connector integration** - Automated passwordless database connections
- **Managed identity authentication** - No passwords or secrets in configuration
- Hybrid token persistence strategy (in-memory caching + database storage)
- Singleton pattern for cross-request token caching
- Factory pattern for connection-scoped service creation
- Entity Framework Core migrations and relationships
- Foreign key relationships and cascade deletions in EF Core
- **Connection selector UI pattern** - Managing context across multiple pages
- Cookie-based session management
- Razor tag helpers and page models
- Rate limiting middleware
- **Background data synchronization** - Fire-and-forget async tasks with SignalR updates
- Docker multi-stage containerization
- Azure deployment patterns (Web Apps, Container Apps, SQL Database)
- **Azure SQL Database free tier** - Production database at zero cost
- Dependency injection and service lifetimes
- Performance optimization through caching and query optimization
- Environment-specific configuration (appsettings.Development.json vs production)

### QR Sticker Designer & Export (Phases 2-5)
- **Fabric.js canvas library** - Building visual editors with HTML5 Canvas
- **Client-side rendering** - Real-time preview and export without server round-trips
- **Coordinate system conversion** - Millimeters (storage) to pixels (rendering) with DPI scaling
- **Data binding system** - Dynamic placeholder replacement ({{device.serial}}) with case-insensitive matching
- **Template matching algorithms** - Multi-level priority cascades (model → type → user default → fallback)
- **Export strategies** - PNG (raster at multiple DPI) vs SVG (vector) formats
- **Canvas-to-image conversion** - Using toDataURL() and Blob API for downloads
- **Custom Fabric.js objects** - Extending Fabric.js with QR code and bound text objects
- **Form state management** - ASP.NET Core model binding with hidden fields and JavaScript FormData
- **Boolean serialization** - Handling ASP.NET "True"/"False" vs JavaScript "true"/"false"
- **Service layer patterns** - Business logic separation with DeviceExportHelper and TemplateMatchingService
- **Authorization in APIs** - Ensuring users can only access their own data
- **Audit logging** - Tracking operations (exports) for compliance and analytics
- **Testing-driven bug fixes** - Iterative testing revealing 12 bugs in coordinate systems, data binding, and form handling

## License

This is a learning project for educational purposes.