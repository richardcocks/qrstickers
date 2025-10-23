# QRStickers

An ASP.NET Core learning project demonstrating Razor Pages, ASP.NET Identity authentication, OAuth 2.0 integration, multi-connection architecture, containerization, and cloud deployment.

## Overview

QRStickers is a web application that enables users to design custom QR code stickers for network devices. It combines user authentication with a flexible multi-connection architecture, allowing users to connect multiple third-party service accounts (currently Cisco Meraki) and create professional device labels with drag-and-drop design tools.

## Key Features

- **User Authentication** - Email/password registration and login using ASP.NET Identity
- **Multi-Connection Architecture** - Connect multiple third-party accounts with user-defined display names
- **Visual Template Designer** - Drag-and-drop canvas editor with Fabric.js for creating sticker templates
- **Intelligent Template Matching** - Automatic template selection based on device type and model
- **Bulk Export** - Export stickers for multiple devices with real-time progress tracking
- **Multi-Format Support** - PNG (96/150/300 DPI), SVG, and PDF export formats
- **Meraki OAuth 2.0** - Secure OAuth integration with Cisco Meraki Dashboard
- **Custom Images** - Upload and manage company logos and icons
- **Global Variables** - Define custom per-connection variables for templates
- **QR Code Generation** - Public API endpoint for generating QR codes with rate limiting

## Quick Start

### Prerequisites

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download/dotnet/9.0)
- SQL Server Express LocalDB (included with Visual Studio)

### Run Locally

```bash
# Clone the repository
git clone <repository-url>
cd QRStickers

# Restore dependencies and run
dotnet restore
dotnet run

# Access the application
# HTTPS: https://localhost:7044
# HTTP: http://localhost:5082
```

On first run, the application will automatically create the database and apply migrations.

### Configure Meraki OAuth (Optional)

```bash
dotnet user-secrets set meraki_client_id <YOUR_CLIENT_ID>
dotnet user-secrets set meraki_client_secret <YOUR_CLIENT_SECRET>
```

## Documentation

Comprehensive documentation is available in the [Docs/](Docs/) directory:

### Getting Started
- **[Setup Guide](Docs/SETUP.md)** - Local development setup and configuration
- **[Full Project Overview](Docs/README.md)** - Complete feature list and user flows

### Technical Documentation
- **[Architecture](Docs/ARCHITECTURE.md)** - Technical architecture, database design, and patterns
- **[API Reference](Docs/API_REFERENCE.md)** - Complete API endpoint documentation
- **[Deployment Guide](Docs/DEPLOYMENT.md)** - Production deployment to Azure

### Reference
- **[Documentation Index](Docs/INDEX.md)** - Complete table of contents for all documentation

## Technology Stack

- **.NET 9.0** with ASP.NET Core Razor Pages
- **ASP.NET Identity** - User authentication
- **Entity Framework Core 9.0** with SQL Server (LocalDB for dev, Azure SQL for production)
- **Fabric.js 5.3.0** - HTML5 Canvas library for visual designer
- **SignalR** - Real-time sync status and export progress
- **QRCoder 1.7.0** - QR code generation
- **Azure** - Production deployment (Web Apps, Container Apps, SQL Database)
- **Docker** - Multi-stage containerization

## Architecture Highlights

### Multi-Connection System
- Users can connect multiple third-party accounts (e.g., "Work Account", "Home Network")
- Table-per-Hierarchy (TPH) pattern for extensibility
- Per-connection OAuth tokens and data caching

### Template Designer
- Drag-and-drop canvas editor with Fabric.js
- Dynamic data binding (`{{device.serial}}`, `{{device.name}}`, etc.)
- 6-level intelligent template matching (Model → Type → ProductTypeFilter → User Default → System Default → Fallback)
- Real-time preview with actual device data

### Token Management
- Hybrid strategy: In-memory access token cache + database refresh token persistence
- Automatic token refresh with 50-80% reduction in database queries
- Per-connection token isolation

### Database
- **Development:** SQL Server LocalDB (file-based, zero-config, free)
- **Production:** Azure SQL Database (serverless, auto-pause, free tier)
- **No migration conflicts** - Same provider for dev and production

## Project Structure

```
QRStickers/
├── Docs/                           # Documentation (you are here)
│   ├── README.md                   # Full project overview
│   ├── SETUP.md                    # Setup guide
│   ├── ARCHITECTURE.md             # Technical architecture
│   ├── DEPLOYMENT.md               # Deployment guide
│   ├── API_REFERENCE.md            # API documentation
│   ├── INDEX.md                    # Documentation index
│   ├── Implementation/             # Phase-by-phase development notes
│   └── Security/                   # Security documentation
├── Pages/                          # Razor Pages UI
├── Services/                       # Business logic services
├── Meraki/                         # Meraki integration components
├── Data/                           # Database configuration and seeding
├── wwwroot/                        # Static assets (JS, CSS)
├── Migrations/                     # EF Core migrations
├── Program.cs                      # Application startup
├── CLAUDE.md                       # Claude Code instructions
└── README.md                       # This file
```

## Screenshots

*(Coming soon)*

## User Flows

### 1. Authentication
Register → Login → Access protected pages

### 2. Connection Management
Login → Add Connection → Enter Display Name → OAuth Authorization → Background Sync

### 3. Template Design
Templates → Designer → Drag & Drop Elements → Bind Data → Save Template

### 4. Device Export
Networks → Select Device → Preview → Export (PNG/SVG/PDF)

### 5. Bulk Export
Templates → Export → Select Devices → Configure Options → Real-time Progress → Download ZIP

## API Endpoints

### Public
- `GET /qrcode?q={text}` - Generate QR code (rate limited)

### Authenticated
- `GET /api/templates` - List templates
- `POST /api/templates` - Create template
- `GET /api/templates/match` - Match template to device
- `POST /api/templates/export-multiple` - Bulk export with progress
- `POST /api/images` - Upload custom image
- `GET /api/export/device/{id}` - Get device export data

See [API_REFERENCE.md](Docs/API_REFERENCE.md) for complete documentation.

## Deployment

### Azure Web Apps (Recommended)

```bash
# Deploy to Azure Web App
az webapp up --name qrstickers --runtime "DOTNET:9.0"
```

### Docker

```bash
# Build and run with Docker
docker build -t qrstickers:latest .
docker run -p 8080:8080 qrstickers:latest
```

See [DEPLOYMENT.md](Docs/DEPLOYMENT.md) for complete deployment instructions including:
- Azure Service Connector setup (passwordless database)
- Azure SQL Database free tier configuration
- GitHub Actions CI/CD
- Container Apps deployment

## Azure SQL Free Tier

Production database runs on **Azure SQL Database free tier**:
- ✅ 100,000 vCore seconds/month
- ✅ 32 GB storage
- ✅ Auto-pause when inactive (zero cost during pause)
- ✅ Forever free (renews monthly)

## Development

### Common Commands

```bash
# Run the application
dotnet run

# Apply database migrations
dotnet ef database update

# Create a new migration
dotnet ef migrations add <MigrationName>

# Build for production
dotnet build --configuration Release
```

### Troubleshooting

See [SETUP.md](Docs/SETUP.md#troubleshooting) for common issues and solutions.

## Learning Objectives

This project demonstrates:
- ASP.NET Core Razor Pages architecture
- ASP.NET Identity authentication
- OAuth 2.0 authorization code flow
- Multi-connection architecture with TPH inheritance
- Entity Framework Core with SQL Server
- Fabric.js canvas library for visual editors
- Client-side rendering and data binding
- SignalR for real-time updates
- Azure deployment (Web Apps, Container Apps, SQL Database)
- Managed identity for passwordless authentication
- Docker containerization
- GitHub Actions CI/CD

## Contributing

This is a learning project. For questions or suggestions, please open an issue.

## License

This is a learning project for educational purposes.

---

**Documentation:** [Docs/INDEX.md](Docs/INDEX.md) | **Setup Guide:** [Docs/SETUP.md](Docs/SETUP.md) | **API Reference:** [Docs/API_REFERENCE.md](Docs/API_REFERENCE.md)
