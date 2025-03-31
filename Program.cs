using System.Net.Mime;
using QRCoder;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton(new QRCodeGenerator());

builder.Services.AddRateLimiter(_ => _
    .AddTokenBucketLimiter(policyName: "tokenBucket", options =>
    {
        options.AutoReplenishment = true;
        options.TokenLimit = 5_000;
        options.ReplenishmentPeriod = TimeSpan.FromSeconds(1);
        options.TokensPerPeriod = 100;
        options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
        options.QueueLimit = 10;
    }));


var app = builder.Build();
app.UseRateLimiter();

app.MapGet("/login", (HttpContext httpContext) =>
{
    string client_id = @"jUNbTS9CoxpssAyMY7484yAEJeVda7J0yvWvGMuZfW4";
    string redirect_url = System.Text.Encodings.Web.UrlEncoder.Default.Encode(@"https://qrstickers.lemonmushroom-d2aba5e0.uksouth.azurecontainerapps.io/oauth/redirect");

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

app.MapGet("/oauth/redirect", (HttpContext httpContext, [FromQuery] string code) =>
{
    string? referer = httpContext.Request.Headers.Referer;




    app.Logger.LogInformation("Redirection incoming from {referer}: {code}", referer, code);

    httpContext.Response.Redirect("/");
});

app.MapGet("/", (HttpContext httpContext) =>
{

    string claim_name = httpContext.Request.Headers["X-MS-CLIENT-PRINCIPAL-NAME"].SingleOrDefault() ?? "";
    string claim_id = httpContext.Request.Headers["X-MS-CLIENT-PRINCIPAL-ID"].SingleOrDefault() ?? "";

    return Results.Content($"""
        <head>
            <title>QR Stickers Generator</title>
        </head>
        <body>
            <h1>Welcome to QR Stickers</h1>
            <div>
                <h2>Hello {claim_name}.
                </h2>
                <p>
                    <a href="/login">Click to connect your Meraki account</a>
                </p>
            </div>
        </body>
    """, "text/html");
});

app.Run();