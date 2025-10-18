using System.Net.Mime;
using QRCoder;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QRStickers;

// ===================== APPLICATION SETUP =====================

var builder = WebApplication.CreateBuilder(args);

// Configure logging explicitly for Azure
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();
builder.Logging.AddEventSourceLogger();

// Configure SQLite database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? "Data Source=qrstickers.db";
builder.Services.AddDbContext<QRStickersDbContext>(options =>
    options.UseSqlite(connectionString));

// Register services
builder.Services.AddSingleton(new QRCodeGenerator());
builder.Services.AddHttpClient<MerakiApiClient>();

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
    await dbContext.Database.EnsureCreatedAsync();
}

app.UseRateLimiter();

// Helper method to get or create unique anonymous user ID
string GetOrCreateAnonymousUserId(HttpContext httpContext)
{
    const string cookieName = "AnonymousUserId";

    // Check if cookie already exists
    if (httpContext.Request.Cookies.TryGetValue(cookieName, out var existingId) && !string.IsNullOrEmpty(existingId))
    {
        return existingId;
    }

    // Generate new unique ID
    var newId = Guid.NewGuid().ToString();

    // Set cookie with security options
    httpContext.Response.Cookies.Append(cookieName, newId, new CookieOptions
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Expires = DateTimeOffset.UtcNow.AddYears(1)
    });

    return newId;
}

app.MapGet("/login", (HttpContext httpContext) =>
{
    string client_id = builder.Configuration.GetValue<string>("meraki_client_id") ?? "";
    string redirect_url = System.Text.Encodings.Web.UrlEncoder.Default.Encode(@"https://qrstickers-htbteydbgjh0b9c4.uksouth-01.azurewebsites.net/oauth/redirect");

    string scopes = System.Text.Encodings.Web.UrlEncoder.Default.Encode(@"sdwan:config:read dashboard:general:config:read");
    httpContext.Response.Redirect($"https://as.meraki.com/oauth/authorize?response_type=code&client_id={client_id}&redirect_uri={redirect_url}&scope={scopes}&state=test");

});

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

app.MapGet("/oauth/redirect", async (HttpContext httpContext, [FromQuery] string code, [FromQuery] string? state, MerakiApiClient merakiClient, QRStickersDbContext db) =>
{
    try
    {
        if (string.IsNullOrEmpty(code))
        {
            app.Logger.LogWarning("OAuth redirect received with no code");
            return Results.BadRequest("Authorization code is missing");
        }

        // Build the redirect URI (should match what's configured in Meraki OAuth)
        var redirectUri = "https://qrstickers-htbteydbgjh0b9c4.uksouth-01.azurewebsites.net/oauth/redirect";

        // Exchange code for token
        var tokenResult = await merakiClient.ExchangeCodeForTokenAsync(code, redirectUri);

        if (tokenResult == null)
        {
            app.Logger.LogWarning("Failed to exchange authorization code for token");
            return Results.StatusCode(StatusCodes.Status500InternalServerError);
        }

        var (accessToken, refreshToken, expiresIn) = tokenResult.Value;

        // Get user identifier from Azure headers or generate unique anonymous ID
        var userId = httpContext.Request.Headers["X-MS-CLIENT-PRINCIPAL-NAME"].SingleOrDefault() ?? GetOrCreateAnonymousUserId(httpContext);

        // Store or update token in database
        var existingToken = await db.OAuthTokens.FirstOrDefaultAsync(t => t.UserId == userId);

        if (existingToken != null)
        {
            existingToken.AccessToken = accessToken;
            existingToken.RefreshToken = refreshToken;
            existingToken.ExpiresAt = DateTime.UtcNow.AddSeconds(expiresIn);
            existingToken.UpdatedAt = DateTime.UtcNow;
            db.OAuthTokens.Update(existingToken);
        }
        else
        {
            var newToken = new OAuthToken
            {
                UserId = userId,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                ExpiresAt = DateTime.UtcNow.AddSeconds(expiresIn),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            db.OAuthTokens.Add(newToken);
        }

        await db.SaveChangesAsync();
        app.Logger.LogInformation("OAuth token stored successfully for user {userId}", userId);

        return Results.Redirect("/");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Error in OAuth redirect handler");
        return Results.StatusCode(StatusCodes.Status500InternalServerError);
    }
});

app.MapGet("/", async (HttpContext httpContext, QRStickersDbContext db) =>
{
    string claim_name = httpContext.Request.Headers["X-MS-CLIENT-PRINCIPAL-NAME"].SingleOrDefault() ?? "";
    string claim_id = httpContext.Request.Headers["X-MS-CLIENT-PRINCIPAL-ID"].SingleOrDefault() ?? "";
    string userId = claim_name != "" ? claim_name : GetOrCreateAnonymousUserId(httpContext);

    // Check if user has an OAuth token
    var token = await db.OAuthTokens.FirstOrDefaultAsync(t => t.UserId == userId);
    var isAuthenticated = token != null;
    var displayName = claim_name != "" ? claim_name : "Anonymous User";
    string authSection = isAuthenticated
        ? $"""
            <div style="background: #e8f5e9; padding: 10px; border-radius: 5px; margin: 10px 0;">
                <h3>✓ Meraki Account Connected</h3>
                <p>Token expires: {token!.ExpiresAt:g}</p>
                <p><a href="/meraki/organizations">View your organizations</a></p>
                <p><a href="/oauth/logout">Disconnect Meraki account</a></p>
            </div>
            """
        : """
            <div style="background: #fff3e0; padding: 10px; border-radius: 5px; margin: 10px 0;">
                <p><a href="/login">Click to connect your Meraki account</a></p>
            </div>
            """;

    return Results.Content($$$"""
        <head>
            <title>QR Stickers Generator</title>
            <style>
                body {{ font-family: Arial, sans-serif; margin: 20px; }}
                a {{ color: #1976d2; text-decoration: none; }}
                a:hover {{ text-decoration: underline; }}
            </style>
        </head>
        <body>
            <h1>Welcome to QR Stickers</h1>
            <h2>Hello {{{displayName}}}.</h2>
            {{{authSection}}}
            <hr>
            <h3>QR Code Generator</h3>
            <p>Use the /qrcode endpoint to generate QR codes</p>
            <p>Example: <a href="/qrcode?q=https://example.com">/qrcode?q=https://example.com</a></p>
        </body>
    """, "text/html");
});

app.MapGet("/meraki/organizations", async (HttpContext httpContext, MerakiApiClient merakiClient, QRStickersDbContext db) =>
{
    try
    {
        var userId = httpContext.Request.Headers["X-MS-CLIENT-PRINCIPAL-NAME"].SingleOrDefault() ?? GetOrCreateAnonymousUserId(httpContext);
        var token = await db.OAuthTokens.FirstOrDefaultAsync(t => t.UserId == userId);

        if (token == null)
        {
            return Results.Redirect("/");
        }

        // Check if token is expired and refresh if needed
        if (DateTime.UtcNow > token.ExpiresAt && token.RefreshToken != null)
        {
            var refreshResult = await merakiClient.RefreshAccessTokenAsync(token.RefreshToken);
            if (refreshResult != null)
            {
                var (newAccessToken, newRefreshToken, newExpiresIn) = refreshResult.Value;
                token.AccessToken = newAccessToken;
                token.RefreshToken = newRefreshToken;
                token.ExpiresAt = DateTime.UtcNow.AddSeconds(newExpiresIn);
                token.UpdatedAt = DateTime.UtcNow;
                db.OAuthTokens.Update(token);
                await db.SaveChangesAsync();
                app.Logger.LogInformation("Token refreshed for user {userId}", userId);
            }
        }

        // Get organizations
        var organizations = await merakiClient.GetOrganizationsAsync(token.AccessToken);

        if (organizations == null || organizations.Count == 0)
        {
            return Results.Content($"""
                <html>
                <head><title>Meraki Organizations</title></head>
                <body>
                    <h1>Your Meraki Organizations</h1>
                    <p>No organizations found.</p>
                    <p><a href="/">Back to home</a></p>
                </body>
                </html>
                """, "text/html");
        }

        var orgsList = string.Join("\n", organizations.Select(o =>
            $"<li><strong>{System.Net.WebUtility.HtmlEncode(o.Name)}</strong> (ID: {o.Id})</li>"));

        return Results.Content($$$"""
            <html>
            <head>
                <title>Meraki Organizations</title>
                <style>
                    body {{ font-family: Arial, sans-serif; margin: 20px; }}
                    a {{ color: #1976d2; text-decoration: none; }}
                </style>
            </head>
            <body>
                <h1>Your Meraki Organizations</h1>
                <ul>
                    {{{orgsList}}}
                </ul>
                <p><a href="/">Back to home</a></p>
            </body>
            </html>
            """, "text/html");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Error retrieving organizations");
        return Results.StatusCode(StatusCodes.Status500InternalServerError);
    }
});

app.MapGet("/oauth/logout", async (HttpContext httpContext, QRStickersDbContext db) =>
{
    try
    {
        var userId = httpContext.Request.Headers["X-MS-CLIENT-PRINCIPAL-NAME"].SingleOrDefault() ?? GetOrCreateAnonymousUserId(httpContext);
        var token = await db.OAuthTokens.FirstOrDefaultAsync(t => t.UserId == userId);

        if (token != null)
        {
            db.OAuthTokens.Remove(token);
            await db.SaveChangesAsync();
            app.Logger.LogInformation("OAuth token removed for user {userId}", userId);
        }

        return Results.Redirect("/");
    }
    catch (Exception ex)
    {
        app.Logger.LogError(ex, "Error during logout");
        return Results.StatusCode(StatusCodes.Status500InternalServerError);
    }
});

app.Run();