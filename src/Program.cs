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

// Add Controllers for API endpoints
builder.Services.AddControllers();

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

// ===================== RAZOR PAGES =====================

// Map Razor Pages (will handle all page routes including Index, Meraki pages, and Identity pages)
app.MapRazorPages();

// ===================== API CONTROLLERS =====================

// Map API controllers (replaces inline endpoints for better organization)
app.MapControllers();

// ===================== SIGNALR HUBS =====================

// Map SignalR hub for real-time sync status updates
app.MapHub<SyncStatusHub>("/syncStatusHub");

app.Run();