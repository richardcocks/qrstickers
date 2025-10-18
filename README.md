# QR Stickers

An ASP.NET Core learning project demonstrating Razor Pages, ASP.NET Identity authentication, OAuth 2.0 integration, containerization, and cloud deployment.

## Overview

QRStickers is a web application that combines user authentication with Cisco Meraki OAuth integration. Users can register accounts, log in, connect their Meraki Dashboard account via OAuth 2.0, and access their organization data. The application also provides a public QR code generation API.

## Key Features

- **User Authentication** - Email/password registration and login using ASP.NET Identity
- **Meraki OAuth 2.0** - Connect your Cisco Meraki Dashboard account to access organization data
- **Token Management** - Automatic OAuth token refresh and secure database storage
- **QR Code Generation** - Public API endpoint for generating QR codes with rate limiting
- **Responsive UI** - Clean Razor Pages interface with shared layout
- **Containerized** - Docker support for easy deployment
- **Cloud Ready** - Configured for Azure deployment (Web Apps and Container Apps)

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

- `Pages/` - Razor Pages (Identity, Meraki, Home)
  - `Identity/Account/` - Login, Register, Logout pages
  - `Meraki/` - OAuth connection, callback, organizations, networks, disconnect pages
- `Program.cs` - Application configuration and startup
- `ApplicationUser.cs` - Identity user model
- `OAuthToken.cs` - Meraki OAuth token storage
- `QRStickersDbContext.cs` - Entity Framework DbContext
- `MerakiApiClient.cs` - Low-level Meraki OAuth and API client
- `MerakiService.cs` - High-level Meraki service with automatic token refresh
- `MerakiServiceFactory.cs` - Factory for creating user-specific Meraki services
- `AccessTokenCache.cs` - In-memory token caching for performance
- `Organization.cs`, `Network.cs`, `Device.cs` - Meraki data models

## API Endpoints

- `GET /qrcode?q={text}` - Generate QR code (public, rate limited)

## User Flows

### Authentication Flow
1. Register → Email + Password → Account created
2. Login → Authentication cookie → Access protected pages
3. Logout → Clear session → Return to home

### Meraki Integration Flow
1. Login required → Click "Connect Meraki Account"
2. Redirect to Meraki OAuth → User authorizes
3. Callback receives token → Store refresh token in database (linked to user)
4. Access token cached in-memory for performance
5. View organizations → Auto-refresh expired tokens from database
6. View networks → See networks and device counts per network
7. Disconnect → Remove token from database and clear cache

## Database

The application uses SQLite with the following schema:
- **ASP.NET Identity tables** - User accounts, roles, claims
- **OAuthTokens** - Meraki access/refresh tokens (FK to Users)

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

- **Refresh Tokens** - Stored securely in SQLite database per user (90-day lifetime)
- **Access Tokens** - Cached in-memory only via singleton `AccessTokenCache` (1-hour lifetime)
- **Benefits:**
  - Reduces database queries by 50-80% during active sessions
  - Survives application restarts (refresh tokens persist)
  - Automatic refresh when access token expires
  - Secure: refresh tokens never exposed to client

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
- Separating authentication (identity) from authorization (access tokens)
- Hybrid token persistence strategy (in-memory caching + database storage)
- Singleton pattern for cross-request token caching
- Factory pattern for scoped service creation
- Entity Framework Core migrations and relationships
- Foreign key relationships in EF Core
- Cookie-based session management
- Razor tag helpers and page models
- Rate limiting middleware
- Docker multi-stage containerization
- Azure deployment patterns (Web Apps and Container Apps)
- Dependency injection and service lifetimes
- Performance optimization through caching

## License

This is a learning project for educational purposes.