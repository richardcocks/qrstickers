using System.Net.Mime;
using QRCoder;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using QRStickers;
using QRStickers.Meraki;
using QRStickers.Data;
using QRStickers.Services;
using QRStickers.Models;
using QuestPDF.Infrastructure;

// ===================== APPLICATION SETUP =====================

var builder = WebApplication.CreateBuilder(args);

// Configure QuestPDF license (Community License (MIT) )
QuestPDF.Settings.License = LicenseType.Community;

// Enable debugging for detailed layout error messages
QuestPDF.Settings.EnableDebugging = false;

// Configure logging explicitly for Azure
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.AddEventSourceLogger();

// Configure database (SQL Server only)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<QRStickersDbContext>(options =>
    options.UseSqlServer(connectionString));

// Configure ASP.NET Core Identity
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    // Password settings
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = true;
    options.Password.RequiredLength = 6;

    // Lockout settings
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;

    // User settings
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedEmail = false; // Set to true if you add email confirmation
})
.AddEntityFrameworkStores<QRStickersDbContext>()
.AddDefaultTokenProviders();

// Configure security stamp validation for session revocation
// This enables "Revoke All Other Sessions" to work by periodically checking the SecurityStamp
var validationIntervalSeconds = builder.Configuration
    .GetValue<int?>("Identity:SecurityStampValidationIntervalSeconds")
    ?? 10; // Default to 10 seconds if not configured

builder.Services.Configure<SecurityStampValidatorOptions>(options =>
{
    options.ValidationInterval = TimeSpan.FromSeconds(validationIntervalSeconds);
});

// Configure cookie settings
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Identity/Account/Login";
    options.LogoutPath = "/Identity/Account/Logout";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
    options.ExpireTimeSpan = TimeSpan.FromDays(7);
    options.SlidingExpiration = true;
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

// Configure designer settings
builder.Services.Configure<DesignerSettings>(
    builder.Configuration.GetSection("Designer"));

// Add Razor Pages
builder.Services.AddRazorPages();

// Add SignalR for real-time updates
builder.Services.AddSignalR();

// Register services
builder.Services.AddSingleton(new QRCodeGenerator());
builder.Services.AddHttpClient<MerakiApiClient>();

// Register MerakiAccessTokenCache as singleton (shared across all requests)
builder.Services.AddSingleton<MerakiAccessTokenCache>();

// Register MerakiServiceFactory as scoped (per request)
builder.Services.AddScoped<MerakiServiceFactory>();

// Register Meraki sync services
builder.Services.AddScoped<MerakiSyncOrchestrator>();
builder.Services.AddHostedService<MerakiBackgroundSyncService>();

// Register sticker template service
builder.Services.AddScoped<TemplateService>();

// Register Phase 5 device export services
builder.Services.AddScoped<DeviceExportHelper>();
builder.Services.AddScoped<TemplateMatchingService>();

// Register Phase 5.5 PDF export service
builder.Services.AddScoped<PdfExportService>();

// Register Phase 5.6 QR code generation service
builder.Services.AddScoped<QRCodeGenerationService>();

// Register Phase 6.1 image upload validation service
builder.Services.AddScoped<ImageUploadValidator>();

// Add memory caching for template matching
builder.Services.AddMemoryCache();

builder.Services.AddRateLimiter(rateLimiterOptions => rateLimiterOptions
    .AddTokenBucketLimiter(policyName: "tokenBucket", options =>
    {
        options.AutoReplenishment = true;
        options.TokenLimit = 5_000;
        options.ReplenishmentPeriod = TimeSpan.FromSeconds(1);
        options.TokensPerPeriod = 100;
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        options.QueueLimit = 10;
    }));

builder.Services.AddLogging(loggingBuilder =>
    {
        loggingBuilder.AddConsole();
        loggingBuilder.AddAzureWebAppDiagnostics();
    }
);

var app = builder.Build();

// Initialize database
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<QRStickersDbContext>();
    await dbContext.Database.MigrateAsync();

    // Seed system templates
    await SystemTemplateSeeder.SeedTemplatesAsync(dbContext);

    // Seed demo account (for marketing screenshots) - only in development
    var enableDemoSeeding = app.Configuration.GetValue<bool>("DemoData:EnableDemoDataSeeding");
    if (enableDemoSeeding)
    {
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        await DemoAccountSeeder.SeedDemoAccountAsync(dbContext, userManager);

        // Sync demo data (populate cached organizations, networks, devices)
        var orchestrator = scope.ServiceProvider.GetRequiredService<MerakiSyncOrchestrator>();
        await DemoAccountSeeder.SyncDemoDataAsync(dbContext, orchestrator);
    }
}

// Enable static files (CSS, JS, images from wwwroot)
app.UseStaticFiles();

// Add authentication and authorization middleware
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

// ===================== API ENDPOINTS =====================

// Device export API endpoints (Phase 5)
app.MapGet("/api/export/device/{deviceId}", async (
    int deviceId,
    [FromQuery] int connectionId,
    [FromQuery] bool includeAlternates,
    HttpContext httpContext,
    DeviceExportHelper exportHelper,
    TemplateMatchingService templateMatcher,
    TemplateService templateService,
    UserManager<ApplicationUser> userManager) =>
{
    try
    {
        var user = await userManager.GetUserAsync(httpContext.User);
        if (user == null)
            return Results.Unauthorized();

        var exportData = await exportHelper.GetDeviceExportDataAsync(deviceId, connectionId, user);

        // Get template match
        var templateMatch = await templateMatcher.FindTemplateForDeviceAsync(exportData.Device, user);
        exportData.MatchedTemplate = templateMatch.Template;

        // Get alternate templates if requested
        object? alternateTemplates = null;
        if (includeAlternates && !string.IsNullOrEmpty(exportData.Device.ProductType))
        {
            var filterResult = await templateService.GetTemplatesForExportAsync(
                connectionId,
                exportData.Device.ProductType
            );

            alternateTemplates = BuildTemplateOptionsArray(filterResult, templateMatch.Template.Id);
        }

        return Results.Ok(new
        {
            success = true,
            data = new
            {
                device = new
                {
                    id = exportData.Device.Id,
                    serial = exportData.Device.Serial,
                    name = exportData.Device.Name,
                    model = exportData.Device.Model,
                    productType = exportData.Device.ProductType,
                    networkId = exportData.Device.NetworkId,
                    connectionId = exportData.Device.ConnectionId,
                    qrCode = exportData.Device.QRCodeDataUri
                },
                network = exportData.Network != null ? new
                {
                    id = exportData.Network.Id,
                    networkId = exportData.Network.NetworkId,
                    name = exportData.Network.Name,
                    organizationId = exportData.Network.OrganizationId,
                    qrCode = exportData.Network.QRCodeDataUri
                } : null,
                organization = exportData.Organization != null ? new
                {
                    id = exportData.Organization.Id,
                    organizationId = exportData.Organization.OrganizationId,
                    name = exportData.Organization.Name,
                    url = exportData.Organization.Url,
                    qrCode = exportData.Organization.QRCodeDataUri
                } : null,
                connection = new
                {
                    id = exportData.Connection.Id,
                    displayName = exportData.Connection.DisplayName,
                    type = exportData.Connection.GetType().Name
                },
                globalVariables = exportData.GlobalVariables,
                uploadedImages = exportData.UploadedImages.Select(img => new
                {
                    id = img.Id,
                    name = img.Name,
                    dataUri = img.DataUri,
                    widthPx = img.WidthPx,
                    heightPx = img.HeightPx
                }).ToList(),
                matchedTemplate = new
                {
                    id = templateMatch.Template.Id,
                    name = templateMatch.Template.Name,
                    templateJson = templateMatch.Template.TemplateJson,
                    pageWidth = templateMatch.Template.PageWidth,
                    pageHeight = templateMatch.Template.PageHeight,
                    matchReason = templateMatch.MatchReason,
                    confidence = templateMatch.Confidence
                },
                alternateTemplates = alternateTemplates // NEW: Include alternate templates if requested
            }
        });
    }
    catch (UnauthorizedAccessException)
    {
        return Results.Forbid();
    }
    catch (ArgumentException ex)
    {
        return Results.NotFound(new { error = ex.Message });
    }
    catch (Exception)
    {
        return Results.StatusCode(500);
    }
}).RequireAuthorization();

// Template matching API endpoint
app.MapGet("/api/templates/match", async (
    [FromQuery] int deviceId,
    [FromQuery] int connectionId,
    HttpContext httpContext,
    DeviceExportHelper exportHelper,
    TemplateMatchingService templateMatcher,
    UserManager<ApplicationUser> userManager) =>
{
    try
    {
        var user = await userManager.GetUserAsync(httpContext.User);
        if (user == null)
            return Results.Unauthorized();

        var exportData = await exportHelper.GetDeviceExportDataAsync(deviceId, connectionId, user);
        var templateMatch = await templateMatcher.FindTemplateForDeviceAsync(exportData.Device, user);
        var alternates = await templateMatcher.GetAlternateTemplatesAsync(exportData.Device, user, templateMatch.Template.Id);

        return Results.Ok(new
        {
            success = true,
            data = new
            {
                matchedTemplate = new
                {
                    id = templateMatch.Template.Id,
                    name = templateMatch.Template.Name,
                    matchReason = templateMatch.MatchReason,
                    confidence = templateMatch.Confidence
                },
                alternateTemplates = alternates.Select(t => new
                {
                    id = t.Id,
                    name = t.Name
                }).ToList()
            }
        });
    }
    catch (UnauthorizedAccessException)
    {
        return Results.Forbid();
    }
    catch (ArgumentException)
    {
        return Results.NotFound();
    }
}).RequireAuthorization();

// Bulk device export data endpoint (Phase 6.1 - Performance Optimization)
// Fetches export data for multiple devices in a single request with reference-based deduplication
app.MapPost("/api/export/bulk-devices", async (
    [FromBody] BulkDeviceExportRequest request,
    HttpContext httpContext,
    QRStickersDbContext db,
    TemplateMatchingService templateMatcher,
    TemplateService templateService,
    UserManager<ApplicationUser> userManager,
    ILogger<Program> logger) =>
{
    try
    {
        var user = await userManager.GetUserAsync(httpContext.User);
        if (user == null)
            return Results.Unauthorized();

        // Validate request
        if (request.DeviceIds == null || request.DeviceIds.Length == 0)
            return Results.BadRequest(new { error = "No device IDs provided" });

        if (request.DeviceIds.Length > 100)
            return Results.BadRequest(new { error = "Maximum 100 devices per request" });

        logger.LogInformation("[Bulk Export] Fetching data for {DeviceCount} devices, connection {ConnectionId}", request.DeviceIds.Length, request.ConnectionId);

        // Verify connection ownership
        var connection = await db.Connections
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.ConnectionId && c.UserId == user.Id);

        if (connection == null)
            return Results.NotFound(new { error = "Connection not found or access denied" });

        // Fetch all devices in a single query with eager loading
        var devices = await db.CachedDevices
            .AsNoTracking()
            .Where(d => request.DeviceIds.Contains(d.Id) && d.ConnectionId == request.ConnectionId)
            .ToListAsync();

        if (devices.Count != request.DeviceIds.Length)
            return Results.NotFound(new { error = "Some devices not found or not owned by connection" });

        // Fetch template matches for all devices (batch optimized)
        var templateMatches = await templateMatcher.FindTemplatesForDevicesBatchAsync(
            devices,
            request.ConnectionId,
            user
        );

        // Group devices by ProductType and fetch templates once per type
        var devicesByProductType = devices
            .Where(d => !string.IsNullOrEmpty(d.ProductType))
            .GroupBy(d => d.ProductType!)
            .ToList();

        var templatesByProductType = new Dictionary<string, TemplateFilterResult>();
        foreach (var group in devicesByProductType)
        {
            var filterResult = await templateService.GetTemplatesForExportAsync(
                request.ConnectionId,
                group.Key
            );
            templatesByProductType[group.Key] = filterResult;
        }

        // Fetch networks and organizations in bulk
        var networkIds = devices.Select(d => d.NetworkId).Distinct().ToList();
        var networks = await db.CachedNetworks
            .AsNoTracking()
            .Where(n => networkIds.Contains(n.NetworkId) && n.ConnectionId == request.ConnectionId)
            .ToListAsync();

        var orgIds = networks.Select(n => n.OrganizationId).Distinct().ToList();
        var organizations = await db.CachedOrganizations
            .AsNoTracking()
            .Where(o => orgIds.Contains(o.OrganizationId) && o.ConnectionId == request.ConnectionId)
            .ToListAsync();

        // Fetch global variables
        var globalVariables = await db.GlobalVariables
            .AsNoTracking()
            .Where(gv => gv.ConnectionId == request.ConnectionId)
            .ToDictionaryAsync(gv => gv.VariableName, gv => gv.VariableValue);

        // Fetch uploaded images
        var uploadedImages = await db.UploadedImages
            .AsNoTracking()
            .Where(i => i.ConnectionId == request.ConnectionId && !i.IsDeleted)
            .ToListAsync();

        // Build reference-based response
        var response = BuildBulkExportResponse(
            devices,
            templateMatches,
            templatesByProductType,
            networks,
            organizations,
            connection,
            globalVariables,
            uploadedImages
        );

        logger.LogInformation("[Bulk Export] Successfully prepared data for {DeviceCount} devices", devices.Count);

        return Results.Ok(new
        {
            success = true,
            data = response
        });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[Bulk Export] Error fetching bulk device data: {ErrorMessage}", ex.Message);
        return Results.Json(new { success = false, error = ex.Message }, statusCode: 500);
    }
}).RequireAuthorization();

// Bulk PDF export endpoint (Phase 5.5)
app.MapPost("/api/export/pdf/bulk", async (
    [FromBody] PdfExportRequest request,
    HttpContext httpContext,
    PdfExportService pdfService,
    UserManager<ApplicationUser> userManager,
    ILogger<Program> logger) =>
{
    try
    {
        var user = await userManager.GetUserAsync(httpContext.User);
        if (user == null)
            return Results.Unauthorized();

        // Validate request
        if (request.Images == null || !request.Images.Any())
            return Results.BadRequest(new { error = "No images provided" });

        if (request.Images.Count > 100)
            return Results.BadRequest(new { error = "Maximum 100 devices per PDF export" });

        // Generate PDF
        var pdfBytes = await pdfService.GenerateBulkPdfAsync(request);

        // Generate filename
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var filename = $"devices-{request.Images.Count}-{timestamp}.pdf";

        // Return PDF file
        return Results.File(pdfBytes, "application/pdf", filename);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[PDF Export] Error generating PDF: {ErrorMessage}", ex.Message);
        return Results.Json(new { error = ex.Message }, statusCode: 500);
    }
}).RequireAuthorization();

// Usage tracking API endpoint (tracks last used timestamps for templates and images)
app.MapPost("/api/usage/track", async (
    [FromBody] UsageTrackRequest request,
    HttpContext httpContext,
    DeviceExportHelper exportHelper,
    QRStickersDbContext db,
    UserManager<ApplicationUser> userManager,
    ILogger<Program> logger) =>
{
    try
    {
        var user = await userManager.GetUserAsync(httpContext.User);
        if (user == null)
            return Results.Unauthorized();

        // Track image usage (updates LastUsedAt for all referenced images)
        if (request.ImageIds?.Length > 0)
        {
            await exportHelper.TrackImageUsageAsync(request.ImageIds);
            logger.LogDebug("[Usage Tracking] Updated LastUsedAt for {Count} images", request.ImageIds.Length);
        }

        // Track template usage
        if (request.TemplateId > 0)
        {
            var template = await db.StickerTemplates
                .FirstOrDefaultAsync(t => t.Id == request.TemplateId);

            if (template != null)
            {
                template.LastUsedAt = DateTime.UtcNow;
                await db.SaveChangesAsync();
                logger.LogDebug("[Usage Tracking] Updated LastUsedAt for template {TemplateId}", request.TemplateId);
            }
        }

        return Results.Ok(new { success = true });
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "[Usage Tracking] Error tracking usage");
        // Silent failure - don't block exports
        return Results.Ok(new { success = false, error = ex.Message });
    }
}).RequireAuthorization();

// Image upload API endpoints (Phase 6.1)
app.MapPost("/api/images/upload", async (
    [FromBody] ImageUploadRequest request,
    HttpContext httpContext,
    ImageUploadValidator validator,
    QRStickersDbContext db,
    UserManager<ApplicationUser> userManager) =>
{
    try
    {
        var user = await userManager.GetUserAsync(httpContext.User);
        if (user == null)
            return Results.Unauthorized();

        // Validate upload
        var validationResult = await validator.ValidateUploadAsync(
            request.ConnectionId,
            request.Name,
            request.DataUri,
            request.WidthPx,
            request.HeightPx,
            user.Id);

        if (!validationResult.IsValid)
        {
            return Results.BadRequest(new { success = false, error = validationResult.ErrorMessage });
        }

        // Create uploaded image entity
        var uploadedImage = new UploadedImage
        {
            ConnectionId = request.ConnectionId,
            Name = request.Name,
            Description = request.Description,
            DataUri = request.DataUri,
            WidthPx = request.WidthPx,
            HeightPx = request.HeightPx,
            MimeType = validationResult.MimeType!,
            FileSizeBytes = validationResult.FileSizeBytes,
            IsDeleted = false,
            UploadedAt = DateTime.UtcNow
        };

        db.UploadedImages.Add(uploadedImage);
        await db.SaveChangesAsync();

        return Results.Ok(new
        {
            success = true,
            data = new
            {
                id = uploadedImage.Id,
                name = uploadedImage.Name,
                dataUri = uploadedImage.DataUri,
                widthPx = uploadedImage.WidthPx,
                heightPx = uploadedImage.HeightPx,
                mimeType = uploadedImage.MimeType,
                fileSizeBytes = uploadedImage.FileSizeBytes,
                uploadedAt = uploadedImage.UploadedAt
            }
        });
    }
    catch (Exception ex)
    {
        return Results.Json(new { success = false, error = "Upload failed: " + ex.Message }, statusCode: 500);
    }
}).RequireAuthorization();

app.MapGet("/api/images", async (
    [FromQuery] int connectionId,
    [FromQuery] bool includeDeleted,
    HttpContext httpContext,
    ImageUploadValidator validator,
    QRStickersDbContext db,
    UserManager<ApplicationUser> userManager) =>
{
    try
    {
        var user = await userManager.GetUserAsync(httpContext.User);
        if (user == null)
            return Results.Unauthorized();

        // Verify user owns connection
        var connection = await db.Connections
            .FirstOrDefaultAsync(c => c.Id == connectionId && c.UserId == user.Id);

        if (connection == null)
            return Results.Forbid();

        // Get images
        var query = db.UploadedImages.Where(i => i.ConnectionId == connectionId);

        if (!includeDeleted)
            query = query.Where(i => !i.IsDeleted);

        var images = await query
            .OrderByDescending(i => i.UploadedAt)
            .ToListAsync();

        // Get quota info
        var quota = await validator.GetQuotaInfoAsync(connectionId);

        var response = new ImageListResponse
        {
            Images = images.Select(i => new ImageDto
            {
                Id = i.Id,
                Name = i.Name,
                Description = i.Description,
                DataUri = i.DataUri,
                WidthPx = i.WidthPx,
                HeightPx = i.HeightPx,
                MimeType = i.MimeType,
                FileSizeBytes = i.FileSizeBytes,
                IsDeleted = i.IsDeleted,
                UploadedAt = i.UploadedAt,
                LastUsedAt = i.LastUsedAt
            }).ToList(),
            Quota = new QuotaDto
            {
                ImagesUsed = quota.ImagesUsed,
                ImagesLimit = quota.ImagesLimit,
                StorageUsed = quota.StorageUsed,
                StorageLimit = quota.StorageLimit
            }
        };

        return Results.Ok(new { success = true, data = response });
    }
    catch (Exception ex)
    {
        return Results.Json(new { success = false, error = ex.Message }, statusCode: 500);
    }
}).RequireAuthorization();

app.MapDelete("/api/images/{id}", async (
    int id,
    HttpContext httpContext,
    QRStickersDbContext db,
    UserManager<ApplicationUser> userManager) =>
{
    try
    {
        var user = await userManager.GetUserAsync(httpContext.User);
        if (user == null)
            return Results.Unauthorized();

        // Find image
        var image = await db.UploadedImages
            .Include(i => i.Connection)
            .FirstOrDefaultAsync(i => i.Id == id);

        if (image == null)
            return Results.NotFound(new { success = false, error = "Image not found" });

        // Verify user owns the connection
        if (image.Connection.UserId != user.Id)
            return Results.Forbid();

        // Soft delete: Replace DataUri with transparent 1×1 PNG
        const string transparentPng = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==";

        image.DataUri = transparentPng;
        image.IsDeleted = true;

        await db.SaveChangesAsync();

        return Results.Ok(new
        {
            success = true,
            message = "Image deleted. Templates using this image will show a transparent placeholder."
        });
    }
    catch (Exception ex)
    {
        return Results.Json(new { success = false, error = ex.Message }, statusCode: 500);
    }
}).RequireAuthorization();

// ===================== RAZOR PAGES =====================

// Map Razor Pages (will handle all page routes including Index, Meraki pages, and Identity pages)
app.MapRazorPages();

// ===================== SIGNALR HUBS =====================

// Map SignalR hub for real-time sync status updates
app.MapHub<SyncStatusHub>("/syncStatusHub");

// ===================== HELPER FUNCTIONS =====================

/// <summary>
/// Builds an array of template options from filter result for API response
/// </summary>
static object[] BuildTemplateOptionsArray(TemplateFilterResult filterResult, int matchedTemplateId)
{
    var options = new List<object>();

    // Add recommended template (if different from matched)
    if (filterResult.RecommendedTemplate != null &&
        filterResult.RecommendedTemplate.Id != matchedTemplateId)
    {
        options.Add(new
        {
            template = new
            {
                id = filterResult.RecommendedTemplate.Id,
                name = filterResult.RecommendedTemplate.Name,
                templateJson = filterResult.RecommendedTemplate.TemplateJson,
                pageWidth = filterResult.RecommendedTemplate.PageWidth,
                pageHeight = filterResult.RecommendedTemplate.PageHeight
            },
            category = "recommended",
            isRecommended = true,
            isCompatible = true,
            compatibilityNote = "Default template for this device type"
        });
    }

    // Add compatible templates
    foreach (var template in filterResult.CompatibleTemplates)
    {
        if (template.Id == matchedTemplateId) continue; // Skip matched

        options.Add(new
        {
            template = new
            {
                id = template.Id,
                name = template.Name,
                templateJson = template.TemplateJson,
                pageWidth = template.PageWidth,
                pageHeight = template.PageHeight
            },
            category = "compatible",
            isRecommended = false,
            isCompatible = true,
            compatibilityNote = "Compatible with this device type"
        });
    }

    // Add incompatible templates (with warning)
    foreach (var template in filterResult.IncompatibleTemplates)
    {
        options.Add(new
        {
            template = new
            {
                id = template.Id,
                name = template.Name,
                templateJson = template.TemplateJson,
                pageWidth = template.PageWidth,
                pageHeight = template.PageHeight
            },
            category = "incompatible",
            isRecommended = false,
            isCompatible = false,
            compatibilityNote = "Not designed for this device type"
        });
    }

    return options.ToArray();
}

/// <summary>
/// Builds a reference-based bulk export response with deduplicated templates and shared data
/// </summary>
static object BuildBulkExportResponse(
    List<CachedDevice> devices,
    Dictionary<int, TemplateMatchResult> templateMatches,
    Dictionary<string, TemplateFilterResult> templatesByProductType,
    List<CachedNetwork> networks,
    List<CachedOrganization> organizations,
    Connection connection,
    Dictionary<string, string> globalVariables,
    List<UploadedImage> uploadedImages)
{
    // Build unique template dictionary
    var templateDict = new Dictionary<int, object>();
    var networkDict = new Dictionary<string, object>();
    var orgDict = new Dictionary<string, object>();
    var imageDict = new Dictionary<int, object>();

    // Add all matched templates
    foreach (var match in templateMatches.Values)
    {
        AddTemplateIfNotExists(templateDict, match.Template);
    }

    // Add all alternate templates (avoiding duplicates)
    foreach (var filterResult in templatesByProductType.Values)
    {
        AddTemplateIfNotExists(templateDict, filterResult.RecommendedTemplate);

        if (filterResult.CompatibleTemplates != null)
        {
            foreach (var t in filterResult.CompatibleTemplates)
                AddTemplateIfNotExists(templateDict, t);
        }

        if (filterResult.IncompatibleTemplates != null)
        {
            foreach (var t in filterResult.IncompatibleTemplates)
                AddTemplateIfNotExists(templateDict, t);
        }
    }

    // Build network dictionary
    foreach (var network in networks)
    {
        var networkRef = $"net_{network.Id}";
        if (!networkDict.ContainsKey(networkRef))
        {
            networkDict[networkRef] = new
            {
                id = network.Id,
                networkId = network.NetworkId,
                name = network.Name,
                organizationRef = $"org_{network.OrganizationId}",
                qrCode = network.QRCodeDataUri
            };
        }
    }

    // Build organization dictionary
    foreach (var org in organizations)
    {
        var orgRef = $"org_{org.Id}";
        if (!orgDict.ContainsKey(orgRef))
        {
            orgDict[orgRef] = new
            {
                id = org.Id,
                organizationId = org.OrganizationId,
                name = org.Name,
                url = org.Url,
                qrCode = org.QRCodeDataUri
            };
        }
    }

    // Build image dictionary
    foreach (var img in uploadedImages)
    {
        imageDict[img.Id] = new
        {
            id = img.Id,
            name = img.Name,
            dataUri = img.DataUri,
            widthPx = img.WidthPx,
            heightPx = img.HeightPx
        };
    }

    // Build device dictionary with references
    var deviceDict = new Dictionary<int, object>();
    var templateOptionsDict = new Dictionary<int, object[]>();

    foreach (var device in devices)
    {
        var matchedTemplate = templateMatches[device.Id].Template;
        var matchedTemplateRef = $"tpl_{matchedTemplate.Id}";

        // Find network for this device
        var network = networks.FirstOrDefault(n => n.NetworkId == device.NetworkId);
        var networkRef = network != null ? $"net_{network.Id}" : null;

        deviceDict[device.Id] = new
        {
            id = device.Id,
            name = device.Name,
            serial = device.Serial,
            model = device.Model,
            productType = device.ProductType,
            networkId = device.NetworkId,
            connectionId = device.ConnectionId,
            qrCode = device.QRCodeDataUri,
            networkRef = networkRef,
            matchedTemplateRef = matchedTemplateRef
        };

        // Build template options for this device
        if (!string.IsNullOrEmpty(device.ProductType) &&
            templatesByProductType.TryGetValue(device.ProductType, out var filterResult))
        {
            var options = new List<object>();

            // Add recommended if different from matched
            if (filterResult.RecommendedTemplate != null &&
                filterResult.RecommendedTemplate.Id != matchedTemplate.Id)
            {
                options.Add(new
                {
                    templateRef = $"tpl_{filterResult.RecommendedTemplate.Id}",
                    category = "recommended",
                    compatibilityNote = "Default template for this device type"
                });
            }

            // Add compatible templates
            if (filterResult.CompatibleTemplates != null)
            {
                foreach (var t in filterResult.CompatibleTemplates)
                {
                    if (t.Id == matchedTemplate.Id) continue;
                    options.Add(new
                    {
                        templateRef = $"tpl_{t.Id}",
                        category = "compatible",
                        compatibilityNote = "Compatible with this device type"
                    });
                }
            }

            // Add incompatible templates
            if (filterResult.IncompatibleTemplates != null)
            {
                foreach (var t in filterResult.IncompatibleTemplates)
                {
                    options.Add(new
                    {
                        templateRef = $"tpl_{t.Id}",
                        category = "incompatible",
                        compatibilityNote = "Not designed for this device type"
                    });
                }
            }

            if (options.Count > 0)
            {
                templateOptionsDict[device.Id] = options.ToArray();
            }
        }
    }

    // Convert template dict to use "tpl_{id}" keys for consistency
    var templateDictWithRefs = templateDict.ToDictionary(
        kvp => $"tpl_{kvp.Key}",
        kvp => kvp.Value
    );

    return new
    {
        devices = deviceDict,
        templates = templateDictWithRefs,
        networks = networkDict,
        organizations = orgDict,
        connection = new
        {
            id = connection.Id,
            displayName = connection.DisplayName,
            type = connection.GetType().Name
        },
        globalVariables = globalVariables,
        uploadedImages = imageDict,
        templateOptions = templateOptionsDict
    };
}

/// <summary>
/// Adds a template to the dictionary if it doesn't already exist
/// </summary>
static void AddTemplateIfNotExists(Dictionary<int, object> dict, StickerTemplate? template)
{
    if (template != null && !dict.ContainsKey(template.Id))
    {
        dict[template.Id] = new
        {
            id = template.Id,
            name = template.Name,
            templateJson = template.TemplateJson,
            pageWidth = template.PageWidth,
            pageHeight = template.PageHeight
        };
    }
}

app.Run();