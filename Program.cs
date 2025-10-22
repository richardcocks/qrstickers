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

// ===================== APPLICATION SETUP =====================

var builder = WebApplication.CreateBuilder(args);

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
                    connectionId = exportData.Device.ConnectionId
                },
                network = exportData.Network != null ? new
                {
                    id = exportData.Network.Id,
                    networkId = exportData.Network.NetworkId,
                    name = exportData.Network.Name,
                    organizationId = exportData.Network.OrganizationId
                } : null,
                connection = new
                {
                    id = exportData.Connection.Id,
                    displayName = exportData.Connection.DisplayName,
                    type = exportData.Connection.GetType().Name
                },
                globalVariables = exportData.GlobalVariables,
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

// ===================== RAZOR PAGES =====================

// Map Razor Pages (will handle all page routes including Index, Meraki pages, and Identity pages)
app.MapRazorPages();

// ===================== SIGNALR HUBS =====================

// Map SignalR hub for real-time sync status updates
app.MapHub<SyncStatusHub>("/syncStatusHub");

app.Run();