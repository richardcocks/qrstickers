# QR Stickers

An ASP.NET Core learning project demonstrating Razor Pages, ASP.NET Identity authentication, OAuth 2.0 integration, multi-connection architecture, containerization, and cloud deployment.

## Overview

QRStickers is a web application that combines user authentication with a flexible multi-connection architecture. Users can register accounts, log in, and connect multiple third-party service accounts (currently Cisco Meraki) via OAuth 2.0. Each connection has a user-defined display name and can be managed independently. The application also provides a public QR code generation API.

## Key Features

- **User Authentication** - Email/password registration and login using ASP.NET Identity
- **Multi-Connection Architecture** - Connect multiple third-party accounts with user-defined display names
- **Meraki OAuth 2.0** - Connect one or more Cisco Meraki Dashboard accounts to access organization data
- **Connection Management** - Add, view, and delete connections through a dedicated management UI
- **Token Management** - Automatic OAuth token refresh and secure per-connection database storage
- **QR Code Generation** - Public API endpoint for generating QR codes with rate limiting
- **Responsive UI** - Clean Razor Pages interface with shared layout and connection selectors
- **Containerized** - Docker support for easy deployment
- **Cloud Ready** - Configured for Azure deployment (Web Apps and Container Apps)
- **Extensible** - Table-per-Hierarchy design ready for adding new connection types (LogicMonitor, etc.)

## Technology Stack

- .NET 9.0 with ASP.NET Core Razor Pages
- ASP.NET Identity for user authentication
- Entity Framework Core 9.0 with SQLite
- QRCoder library for QR code generation
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
- Create the SQLite database (`qrstickers.db`)
- Apply all EF Core migrations (Identity tables + OAuthTokens table)

## Project Structure

- `Pages/` - Razor Pages (Identity, Connections, Meraki, Home)
  - `Identity/Account/` - Login, Register, Logout pages
  - `Connections/` - Connection management (Index, Create, Delete)
  - `Meraki/` - OAuth connection, callback, organizations, networks, sync status pages
- `Program.cs` - Application configuration and startup
- `Connection.cs` - Base connection class (TPH pattern)
- `MerakiConnection.cs` - Meraki-specific connection specialization
- `ApplicationUser.cs` - Identity user model (owns Connections collection)
- `QRStickersDbContext.cs` - Entity Framework DbContext with TPH discriminator
- `SyncStatus.cs` - Sync status tracking per connection
- `MerakiBackgroundSyncService.cs` - Background service for periodic connection syncing
- `Meraki/` - Meraki-specific components
  - `MerakiOAuthToken.cs` - OAuth token storage per connection
  - `MerakiAccessTokenCache.cs` - In-memory token caching for performance
  - `MerakiApiClient.cs` - Low-level Meraki OAuth and API client
  - `MerakiService.cs` - High-level Meraki service with automatic token refresh
  - `MerakiServiceFactory.cs` - Factory for creating connection-specific Meraki services
  - `MerakiSyncOrchestrator.cs` - Orchestrates syncing connection data from API to cache
  - `CachedOrganization.cs` - Cached Meraki organization data per connection
  - `CachedNetwork.cs` - Cached Meraki network data per connection
  - `CachedDevice.cs` - Cached Meraki device data per connection

## API Endpoints

- `GET /qrcode?q={text}` - Generate QR code (public, rate limited)

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

The application uses SQLite with the following schema:
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

- **Refresh Tokens** - Stored securely in SQLite database per connection (90-day lifetime for Meraki)
- **Access Tokens** - Cached in-memory only via singleton `MerakiAccessTokenCache` (1-hour lifetime)
- **Benefits:**
  - Reduces database queries by 50-80% during active sessions
  - Survives application restarts (refresh tokens persist in database)
  - Automatic refresh when access token expires
  - Secure: refresh tokens never exposed to client
  - Per-connection isolation: each connection has independent token lifecycle

## Docker

Build and run with Docker:

```bash
docker build -t qrstickers:latest .
docker run -p 8080:8080 qrstickers:latest
```

**Note:** Mount the `/App/` volume to persist the SQLite database across container restarts:
```bash
docker run -p 8080:8080 -v /path/to/local:/App qrstickers:latest
```

## Learning Objectives

This project demonstrates:
- ASP.NET Core Razor Pages architecture
- ASP.NET Identity authentication and authorization
- OAuth 2.0 authorization code flow with token refresh and rotation
- **Multi-connection architecture** - Users managing multiple third-party integrations
- **Table-per-Hierarchy (TPH) inheritance** - Flexible connection type extensibility
- **User-defined connection naming** - Passing state through OAuth flow
- Separating authentication (identity) from authorization (access tokens)
- Hybrid token persistence strategy (in-memory caching + database storage)
- Singleton pattern for cross-request token caching
- Factory pattern for connection-scoped service creation
- Entity Framework Core migrations and relationships
- Foreign key relationships and cascade deletions in EF Core
- **Connection selector UI pattern** - Managing context across multiple pages
- Cookie-based session management
- Razor tag helpers and page models
- Rate limiting middleware
- **Background data synchronization** - Fire-and-forget async tasks
- Docker multi-stage containerization
- Azure deployment patterns (Web Apps and Container Apps)
- Dependency injection and service lifetimes
- Performance optimization through caching and query optimization

## License

This is a learning project for educational purposes.