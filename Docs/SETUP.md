# Getting Started

This guide will help you set up QRStickers for local development.

## Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- [SQL Server Express LocalDB](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) (included with Visual Studio)
- [Meraki Dashboard API credentials](https://developer.cisco.com/meraki/api-v1/) (optional, for OAuth features)

## Setup Steps

### 1. Clone the repository

```bash
git clone <repository-url>
cd QRStickers
```

### 2. Configure Meraki OAuth (optional)

If you want to use the Meraki integration features, configure your OAuth credentials:

```bash
dotnet user-secrets set meraki_client_id <YOUR_CLIENT_ID>
dotnet user-secrets set meraki_client_secret <YOUR_CLIENT_SECRET>
```

**Note:** You can obtain these credentials from the [Meraki Developer Dashboard](https://developer.cisco.com/meraki/api-v1/).

### 3. Run the application

```bash
dotnet restore
dotnet run
```

### 4. Access the application

- **HTTPS:** `https://localhost:7044` (recommended)
- **HTTP:** `http://localhost:5082`

### 5. Create your account

1. Navigate to the application URL
2. Click "Register" to create a new account
3. Login with your credentials
4. (Optional) Connect your Meraki account via OAuth

## First Run

On first run, the application will automatically:

- Create the LocalDB database (`QRStickers`)
- Apply all EF Core migrations (Identity tables, Connections, OAuth tokens, Templates, etc.)
- Seed default system templates (Rack Mount and Ceiling/Wall Mount)

**Note:** Requires SQL Server Express LocalDB (included with Visual Studio or SQL Server Express)

## Development Configuration

### Database

The application uses **SQL Server LocalDB** for development:

- **Database Name:** `QRStickers`
- **Connection String:** `Server=(localdb)\\mssqllocaldb;Database=QRStickers;Trusted_Connection=True;MultipleActiveResultSets=true`
- **Configuration File:** `appsettings.Development.json`

### User Secrets

User secrets are stored outside the project directory for security:

```bash
# View all secrets
dotnet user-secrets list

# Set a secret
dotnet user-secrets set <key> <value>

# Remove a secret
dotnet user-secrets remove <key>
```

**User Secrets ID:** `6a279b22-10a4-4afb-8c26-60b92fbb4e0e`

## Common Development Commands

### Build and Run

```bash
# Restore dependencies
dotnet restore

# Build the project
dotnet build

# Run with HTTP only
dotnet run --launch-profile http

# Run with HTTPS (default)
dotnet run --launch-profile https

# Run with IIS Express
dotnet run --launch-profile "IIS Express"
```

### Database Migrations

```bash
# Create a new migration
dotnet ef migrations add <MigrationName>

# Apply migrations to database
dotnet ef database update

# Remove last migration (if not applied)
dotnet ef migrations remove

# Generate SQL script for review
dotnet ef migrations script
```

### Clean and Reset

```bash
# Clean build artifacts
dotnet clean

# Drop and recreate database (WARNING: Deletes all data)
dotnet ef database drop
dotnet ef database update
```

## Troubleshooting

### Port Conflicts

If ports 7044 or 5082 are in use, modify `Properties/launchSettings.json` to use different ports.

### Database Connection Errors

Ensure SQL Server Express LocalDB is installed:

- **With Visual Studio:** Included automatically
- **Standalone:** Download from [Microsoft SQL Server Downloads](https://www.microsoft.com/en-us/sql-server/sql-server-downloads)

### OAuth Not Working Locally

The OAuth callback URL is currently hardcoded for production. For local development:

1. Temporarily update `Pages/Meraki/Connect.cshtml.cs` redirect URI to `https://localhost:7044/Meraki/Callback`
2. Update the same URI in your Meraki Dashboard OAuth settings
3. **Remember to revert before pushing to production**

### Login/Register Not Working

Ensure database migrations have been applied:

```bash
dotnet ef database update
```

## Next Steps

- **User Guide:** See main [README.md](README.md) for feature overview
- **Architecture:** See [ARCHITECTURE.md](ARCHITECTURE.md) for technical details
- **Deployment:** See [DEPLOYMENT.md](DEPLOYMENT.md) for production deployment
- **API Reference:** See [API_REFERENCE.md](API_REFERENCE.md) for API documentation
