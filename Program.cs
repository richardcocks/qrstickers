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
}

// Enable static files (CSS, JS, images from wwwroot)
app.UseStaticFiles();

// Add authentication and authorization middleware
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

// ===================== API ENDPOINTS =====================

// QR Code generation API endpoint (keep as minimal API for programmatic access)
app.MapGet("/qrcode", (HttpContext httpContext, string q, QRCodeGenerator qrGenerator) =>
{
    if (q.Length > 512)
    {
        return Results.BadRequest("Maximum string length is 512");
    }
    using QRCodeData qrCodeData = qrGenerator.CreateQrCode(q, QRCodeGenerator.ECCLevel.Q);
    using PngByteQRCode qrCode = new(qrCodeData);
    byte[] qrCodeImage = qrCode.GetGraphic(20, false);
    httpContext.Response.ContentType = MediaTypeNames.Image.Png;
    return Results.File(qrCodeImage, MediaTypeNames.Image.Png);
}).RequireRateLimiting("tokenBucket");

// Device export API endpoints (Phase 5)
app.MapGet("/api/export/device/{deviceId}", async (
    int deviceId,
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

        // Get template match
        var templateMatch = await templateMatcher.FindTemplateForDeviceAsync(exportData.Device, user);
        exportData.MatchedTemplate = templateMatch.Template;

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
                }
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

app.Run();