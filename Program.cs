using System.Net.Mime;
using QRCoder;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton(new QRCodeGenerator());

var app = builder.Build();

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
});

app.Run();