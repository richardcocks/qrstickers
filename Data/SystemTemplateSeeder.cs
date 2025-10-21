using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace QRStickers.Data;

/// <summary>
/// Seeds the database with default system sticker templates
/// </summary>
public static class SystemTemplateSeeder
{
    /// <summary>
    /// Seeds system templates if they don't already exist
    /// </summary>
    public static async Task SeedTemplatesAsync(QRStickersDbContext db)
    {
        // Check if system templates already exist
        if (await db.StickerTemplates.AnyAsync(t => t.IsSystemTemplate))
        {
            return; // Already seeded
        }

        var templates = new List<StickerTemplate>
        {
            CreateRackMountTemplate(),
            CreateCeilingWallTemplate()
        };

        db.StickerTemplates.AddRange(templates);
        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Creates the rack-mount template (100mm x 50mm) for switches and appliances
    /// </summary>
    private static StickerTemplate CreateRackMountTemplate()
    {
        var templateJson = new
        {
            version = "1.0",
            fabricVersion = "5.3.0",
            pageSize = new
            {
                width = 100,
                height = 50,
                unit = "mm"
            },
            objects = new object[]
            {
                new
                {
                    type = "qrcode",
                    id = "qr-primary",
                    left = 5,
                    top = 5,
                    width = 40,
                    height = 40,
                    scaleX = 1,
                    scaleY = 1,
                    angle = 0,
                    properties = new
                    {
                        dataSource = "device.Serial",
                        eccLevel = "Q",
                        quietZone = 2
                    }
                },
                new
                {
                    type = "text",
                    id = "device-name",
                    left = 50,
                    top = 8,
                    width = 45,
                    height = 10,
                    text = "{{device.Name}}",
                    fontFamily = "Arial",
                    fontSize = 12,
                    fontWeight = "bold",
                    fill = "#000000",
                    properties = new
                    {
                        dataSource = "device.Name",
                        maxLength = 50,
                        overflow = "truncate"
                    }
                },
                new
                {
                    type = "text",
                    id = "serial-number",
                    left = 50,
                    top = 20,
                    width = 45,
                    height = 8,
                    text = "SN: {{device.Serial}}",
                    fontFamily = "Courier New",
                    fontSize = 8,
                    fill = "#333333",
                    properties = new
                    {
                        dataSource = "device.Serial",
                        format = "uppercase"
                    }
                },
                new
                {
                    type = "text",
                    id = "model-number",
                    left = 50,
                    top = 30,
                    width = 45,
                    height = 8,
                    text = "{{device.Model}}",
                    fontFamily = "Arial",
                    fontSize = 7,
                    fill = "#666666",
                    properties = new
                    {
                        dataSource = "device.Model"
                    }
                },
                new
                {
                    type = "image",
                    id = "company-logo",
                    left = 70,
                    top = 38,
                    width = 25,
                    height = 10,
                    src = "{{connection.CompanyLogoUrl}}",
                    properties = new
                    {
                        dataSource = "connection.CompanyLogoUrl",
                        placeholder = true,
                        aspectRatio = "contain"
                    }
                }
            }
        };

        return new StickerTemplate
        {
            Name = "Rack Mount Default",
            Description = "Default template for rack-mounted devices (switches, appliances)",
            ProductTypeFilter = null, // Matches all product types
            IsRackMount = true,
            IsDefault = false,
            IsSystemTemplate = true,
            PageWidth = 100.0,
            PageHeight = 50.0,
            TemplateJson = JsonSerializer.Serialize(templateJson, new JsonSerializerOptions
            {
                WriteIndented = true
            }),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates the ceiling/wall-mount template (60mm x 60mm) for APs, cameras, and sensors
    /// </summary>
    private static StickerTemplate CreateCeilingWallTemplate()
    {
        var templateJson = new
        {
            version = "1.0",
            fabricVersion = "5.3.0",
            pageSize = new
            {
                width = 60,
                height = 60,
                unit = "mm"
            },
            objects = new object[]
            {
                new
                {
                    type = "qrcode",
                    id = "qr-primary",
                    left = 15,
                    top = 5,
                    width = 30,
                    height = 30,
                    scaleX = 1,
                    scaleY = 1,
                    angle = 0,
                    properties = new
                    {
                        dataSource = "device.Serial",
                        eccLevel = "Q",
                        quietZone = 2
                    }
                },
                new
                {
                    type = "text",
                    id = "device-name",
                    left = 5,
                    top = 37,
                    width = 50,
                    height = 8,
                    text = "{{device.Name}}",
                    fontFamily = "Arial",
                    fontSize = 9,
                    fontWeight = "bold",
                    fill = "#000000",
                    textAlign = "center",
                    properties = new
                    {
                        dataSource = "device.Name",
                        maxLength = 40,
                        overflow = "truncate"
                    }
                },
                new
                {
                    type = "text",
                    id = "serial-number",
                    left = 5,
                    top = 47,
                    width = 50,
                    height = 6,
                    text = "{{device.Serial}}",
                    fontFamily = "Courier New",
                    fontSize = 7,
                    fill = "#333333",
                    textAlign = "center",
                    properties = new
                    {
                        dataSource = "device.Serial",
                        format = "uppercase"
                    }
                },
                new
                {
                    type = "image",
                    id = "company-logo",
                    left = 45,
                    top = 5,
                    width = 12,
                    height = 8,
                    src = "{{connection.CompanyLogoUrl}}",
                    properties = new
                    {
                        dataSource = "connection.CompanyLogoUrl",
                        placeholder = true,
                        aspectRatio = "contain"
                    }
                }
            }
        };

        return new StickerTemplate
        {
            Name = "Ceiling/Wall Mount Default",
            Description = "Default template for ceiling/wall-mounted devices (APs, cameras, sensors)",
            ProductTypeFilter = null, // Matches all product types
            IsRackMount = false,
            IsDefault = true, // This is the global fallback
            IsSystemTemplate = true,
            PageWidth = 60.0,
            PageHeight = 60.0,
            TemplateJson = JsonSerializer.Serialize(templateJson, new JsonSerializerOptions
            {
                WriteIndented = true
            }),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }
}
