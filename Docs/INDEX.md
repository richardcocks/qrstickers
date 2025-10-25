# QRStickers Documentation Index

Complete documentation for the QRStickers application.

## Quick Start

- **[README](README.md)** - Project overview, features, and user flows
- **[SETUP](SETUP.md)** - Getting started guide for local development
- **[API_REFERENCE](API_REFERENCE.md)** - Complete API endpoint documentation
- **[ARCHITECTURE](ARCHITECTURE.md)** - Technical architecture and design patterns
- **[DEPLOYMENT](DEPLOYMENT.md)** - Production deployment to Azure

---

## User Guides

### Getting Started

1. **[Setup Guide](SETUP.md)**
   - Prerequisites and installation
   - Database setup (SQL Server LocalDB)
   - Configuration (User Secrets)
   - Common development commands
   - Troubleshooting

### Using the Application

2. **[Project Overview](README.md#overview)**
   - Key features
   - Technology stack
   - User flows

3. **[Account Settings](ACCOUNT_SETTINGS.md)**
   - Display name customization
   - Login activity tracking
   - Session management (revoke other sessions)
   - Configuration options

4. **[User Flows](README.md#user-flows)**
   - Authentication flow
   - Connection management
   - Meraki integration
   - Template designer workflow

---

## Developer Guides

### Architecture

5. **[Architecture Overview](ARCHITECTURE.md)**
   - Technology stack details
   - Project structure
   - Database architecture
   - Token management
   - Template designer architecture
   - Performance optimizations
   - Security considerations

6. **[Database Architecture](ARCHITECTURE.md#database-architecture)**
   - Database provider strategy (SQL Server)
   - Schema and tables
   - Table-per-Hierarchy (TPH) pattern
   - Foreign key relationships
   - Migrations

7. **[Token Management](ARCHITECTURE.md#token-management)**
   - Access token cache (in-memory)
   - Refresh token persistence (database)
   - Token refresh flow
   - Service layers

8. **[Template Designer](ARCHITECTURE.md#template-designer-architecture)**
   - Client-side rendering with Fabric.js
   - Template matching algorithm
   - Data binding system
   - Export pipeline

### API Reference

9. **[Public Endpoints](API_REFERENCE.md#public-endpoints)**
   - QR code generation API

10. **[Template Management](API_REFERENCE.md#template-management-endpoints)**
    - List, create, update, delete templates
    - Track template usage

11. **[Template Matching](API_REFERENCE.md#template-matching-endpoints)**
    - Match template to device
    - Get alternate templates

12. **[Export Endpoints](API_REFERENCE.md#export-endpoints)**
    - Export single device
    - Export multiple devices (bulk)
    - Progress tracking with SignalR

13. **[Custom Images](API_REFERENCE.md#custom-image-endpoints)**
    - Upload, list, delete images
    - Usage tracking

14. **[Global Variables](API_REFERENCE.md#global-variables-endpoints)**
    - Create custom template variables
    - Manage per-connection variables

### Deployment

15. **[Azure Deployment](DEPLOYMENT.md)**
    - Azure Service Connector setup (passwordless)
    - Manual connection string setup
    - Docker deployment
    - Azure Container Apps
    - GitHub Actions CI/CD

16. **[Azure SQL Database](DEPLOYMENT.md#azure-sql-database-free-tier)**
    - Free tier setup and limits
    - Monitoring usage
    - Cost management

17. **[Troubleshooting Deployment](DEPLOYMENT.md#troubleshooting)**
    - Database connection issues
    - Migration problems
    - OAuth callback failures
    - Performance issues

---

## Implementation Notes

Development history and implementation details for each phase.

### Phase 2: Template Designer

18. **[Phase 2 Progress](Implementation/Phase2-Designer-Progress.md)**
    - Initial template designer implementation
    - Fabric.js integration
    - Canvas rendering

### Phase 3: Template Management

18. **[Phase 3 Planning](Implementation/Phase3-Template-Management-Plan.md)**
    - Template management system design

19. **[Phase 3 Progress](Implementation/Phase3-TemplateManagement-Progress.md)**
    - Template CRUD operations
    - Template list UI

### Phase 4: Export & Preview

20. **[Phase 4 Export Planning](Implementation/Phase4-Export-Planning.md)**
    - Export system architecture

21. **[Phase 4 Template Preview Planning](Implementation/Phase4-TemplatePreview-Planning.md)**
    - Live preview implementation

22. **[Phase 4 Implementation Summary](Implementation/PHASE4_IMPLEMENTATION_SUMMARY.md)**
    - Completed features summary

23. **[Phase 4 Completion Notes](Implementation/PHASE4_COMPLETION_NOTES.md)**
    - Final implementation details

### Phase 5: Device Export & Multi-Device Support

24. **[Phase 5 Technical Spec](Implementation/Phase5-TechnicalSpec.md)**
    - Technical requirements

25. **[Phase 5 UX Design](Implementation/Phase5-UX-Design.md)**
    - User experience design

26. **[QR Sticker Designer Plan](Implementation/QR-sticker-designer-plan.md)**
    - Original designer plan

27. **[Phase 5 Completion Notes](Implementation/PHASE5_COMPLETION_NOTES.md)**
    - Phase 5 wrap-up

28. **[Phase 5 MVP Implementation](Implementation/PHASE5_MVP_IMPLEMENTATION.md)**
    - Minimum viable product

29. **[Phase 5 Device Export Planning](Implementation/Phase5-DeviceExport-Planning.md)**
    - Single device export design

30. **[Phase 5.3 Multi-Device Export Plan](Implementation/Phase5.3-MultiDeviceExport-Plan.md)**
    - Bulk export architecture

31. **[Phase 5.3 Testing Notes](Implementation/Phase5.3-TestingNotes.md)**
    - Testing results and bug fixes

32. **[Phase 5.3 Completion Notes](Implementation/PHASE5.3_COMPLETION_NOTES.md)**
    - Multi-device export completion

33. **[Phase 5.5 PDF Export](Implementation/PHASE5.5_PDF_EXPORT.md)**
    - PDF export implementation

34. **[Phase 5.7 QR Migration](Implementation/PHASE5.7_QR_MIGRATION.md)**
    - QR code data migration

35. **[Phase 5.8 Bug Fixes](Implementation/PHASE5.8_BUG_FIXES.md)**
    - Bug fixes from testing

### Phase 6: Custom Images & Advanced Features

36. **[Phase 6 Custom Images Planning](Implementation/PHASE6_CUSTOM_IMAGES_PLANNING.md)**
    - Custom image upload system

37. **[Phase 6.1 Implementation Notes](Implementation/PHASE6.1_IMPLEMENTATION_NOTES.md)**
    - Image processing service

38. **[Phase 6.2 Designer Preview Fix](Implementation/PHASE6.2_DESIGNER_PREVIEW_FIX.md)**
    - Designer preview improvements

39. **[Phase 6 Progress Summary](Implementation/PHASE6_PROGRESS_SUMMARY.md)**
    - Phase 6 completion status

### Architectural Improvements

40. **[Controller Refactoring](Implementation/CONTROLLER_REFACTORING.md)** ⭐ **NEW**
    - Migration from Minimal APIs to MVC Controllers
    - 81% reduction in Program.cs size (1,025 → 195 lines)
    - Improved code organization and testability
    - Helper method extraction to services
    - Complete endpoint mapping and testing guide

41. **[Controller Quick Reference](CONTROLLER_QUICK_REFERENCE.md)** ⭐ **NEW**
    - Quick reference guide for working with controllers
    - Controller locations and routes
    - Common patterns and examples
    - Testing and debugging tips

---

## Security

42. **[XSS Vulnerability Fixes](Security/SECURITY_FIX_XSS_VULNERABILITIES.md)**
    - Critical security fixes
    - XSS prevention measures
    - Input sanitization

43. **[DoS Protection via String Length Constraints](Security/SECURITY_FIX_DOS_PROTECTION.md)**
    - String length constraint implementation
    - Defense in depth strategy
    - Database-level protection
    - Attack scenario mitigation
    - Configuration and monitoring

44. **[Log Injection Protection](Security/SECURITY_FIX_LOG_INJECTION.md)**
    - Log injection vulnerability fixes
    - LogSanitizer utility implementation
    - Input validation and sanitization
    - Defense-in-depth logging security
    - Comprehensive test coverage (18 tests)

---

## Additional Resources

### External Documentation

- **[.NET 9.0 Documentation](https://learn.microsoft.com/en-us/dotnet/core/whats-new/dotnet-9)**
- **[ASP.NET Core Documentation](https://learn.microsoft.com/en-us/aspnet/core/)**
- **[Entity Framework Core](https://learn.microsoft.com/en-us/ef/core/)**
- **[Azure SQL Database](https://learn.microsoft.com/en-us/azure/azure-sql/)**
- **[Fabric.js Documentation](http://fabricjs.com/docs/)**
- **[Meraki API Documentation](https://developer.cisco.com/meraki/api-v1/)**
- **[SignalR Documentation](https://learn.microsoft.com/en-us/aspnet/core/signalr/)**

### Tools & Libraries

- **[QRCoder](https://github.com/codebude/QRCoder)** - QR code generation
- **[Azure Service Connector](https://learn.microsoft.com/en-us/azure/service-connector/)** - Passwordless database connections
- **[Docker](https://docs.docker.com/)** - Containerization
- **[GitHub Actions](https://docs.github.com/en/actions)** - CI/CD pipelines

---

## Contributing

This is a learning project. For questions or suggestions, please open an issue on the repository.

## License

This is a learning project for educational purposes.

---

**Last Updated:** 2025-10-24
