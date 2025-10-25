using QRStickers.Meraki;
using QRStickers.Services;

namespace QRStickers.Tests.Helpers;

/// <summary>
/// Builder class for creating test data entities
/// Provides sensible defaults while allowing customization
/// </summary>
public static class TestDataBuilder
{
    /// <summary>
    /// Creates a test ApplicationUser with default values
    /// </summary>
    public static ApplicationUser CreateUser(
        string id = "test-user-id",
        string email = "test@example.com",
        string userName = "testuser")
    {
        return new ApplicationUser
        {
            Id = id,
            Email = email,
            UserName = userName,
            NormalizedEmail = email.ToUpperInvariant(),
            NormalizedUserName = userName.ToUpperInvariant(),
            EmailConfirmed = true
        };
    }

    /// <summary>
    /// Creates a test MerakiConnection
    /// </summary>
    public static MerakiConnection CreateMerakiConnection(
        int id = 1,
        string userId = "test-user-id",
        string displayName = "Test Connection",
        bool isActive = true)
    {
        return new MerakiConnection
        {
            Id = id,
            UserId = userId,
            DisplayName = displayName,
            IsActive = isActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a test StickerTemplate
    /// </summary>
    public static StickerTemplate CreateTemplate(
        int id = 1,
        string name = "Test Template",
        bool isSystemTemplate = false,
        int? connectionId = null,
        int pageWidth = 62,
        int pageHeight = 29)
    {
        return new StickerTemplate
        {
            Id = id,
            Name = name,
            IsSystemTemplate = isSystemTemplate,
            ConnectionId = connectionId,
            PageWidth = pageWidth,
            PageHeight = pageHeight,
            TemplateJson = "{}",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a test ConnectionDefaultTemplate
    /// </summary>
    public static ConnectionDefaultTemplate CreateConnectionDefault(
        int id = 1,
        int connectionId = 1,
        string productType = "switch",
        int? templateId = 1)
    {
        return new ConnectionDefaultTemplate
        {
            Id = id,
            ConnectionId = connectionId,
            ProductType = productType,
            TemplateId = templateId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a test CachedDevice
    /// </summary>
    public static CachedDevice CreateDevice(
        int id = 1,
        int connectionId = 1,
        string? networkId = "network-1",
        string serial = "Q2XX-XXXX-XXXX",
        string? name = "Test Device",
        string? model = "MS250-48",
        string? productType = "switch")
    {
        return new CachedDevice
        {
            Id = id,
            ConnectionId = connectionId,
            NetworkId = networkId,
            Serial = serial,
            Name = name,
            Model = model,
            ProductType = productType,
            CreatedAt = DateTime.UtcNow,
            LastSyncedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a test CachedNetwork
    /// </summary>
    public static CachedNetwork CreateNetwork(
        int id = 1,
        int connectionId = 1,
        string organizationId = "org-1",
        string networkId = "network-1",
        string name = "Test Network")
    {
        return new CachedNetwork
        {
            Id = id,
            ConnectionId = connectionId,
            OrganizationId = organizationId,
            NetworkId = networkId,
            Name = name,
            TimeZone = "America/Los_Angeles",
            ProductTypesJson = "[\"switch\",\"wireless\"]",
            CreatedAt = DateTime.UtcNow,
            LastSyncedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a test CachedOrganization
    /// </summary>
    public static CachedOrganization CreateOrganization(
        int id = 1,
        int connectionId = 1,
        string organizationId = "org-1",
        string name = "Test Organization")
    {
        return new CachedOrganization
        {
            Id = id,
            ConnectionId = connectionId,
            OrganizationId = organizationId,
            Name = name,
            CreatedAt = DateTime.UtcNow,
            LastSyncedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a test GlobalVariable
    /// </summary>
    public static GlobalVariable CreateGlobalVariable(
        int id = 1,
        int connectionId = 1,
        string variableName = "company_name",
        string variableValue = "Test Company")
    {
        return new GlobalVariable
        {
            Id = id,
            ConnectionId = connectionId,
            VariableName = variableName,
            VariableValue = variableValue,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a test UploadedImage
    /// </summary>
    public static UploadedImage CreateUploadedImage(
        int id = 1,
        int connectionId = 1,
        string name = "Test Image",
        string dataUri = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==",
        int widthPx = 100,
        int heightPx = 100,
        bool isDeleted = false)
    {
        return new UploadedImage
        {
            Id = id,
            ConnectionId = connectionId,
            Name = name,
            Description = "Test image description",
            DataUri = dataUri,
            WidthPx = widthPx,
            HeightPx = heightPx,
            MimeType = "image/png",
            FileSizeBytes = 1024,
            IsDeleted = isDeleted,
            UploadedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Creates a test TemplateMatchResult
    /// </summary>
    public static TemplateMatchResult CreateTemplateMatchResult(
        StickerTemplate? template = null,
        string matchReason = "connection_default",
        double confidence = 1.0,
        string matchedBy = "switch")
    {
        return new TemplateMatchResult
        {
            Template = template ?? CreateTemplate(),
            MatchReason = matchReason,
            Confidence = confidence,
            MatchedBy = matchedBy
        };
    }

    /// <summary>
    /// Creates a test TemplateFilterResult
    /// </summary>
    public static TemplateFilterResult CreateTemplateFilterResult(
        StickerTemplate? recommendedTemplate = null,
        List<StickerTemplate>? compatibleTemplates = null,
        List<StickerTemplate>? incompatibleTemplates = null)
    {
        return new TemplateFilterResult
        {
            RecommendedTemplate = recommendedTemplate,
            CompatibleTemplates = compatibleTemplates ?? new List<StickerTemplate>(),
            IncompatibleTemplates = incompatibleTemplates ?? new List<StickerTemplate>()
        };
    }
}
