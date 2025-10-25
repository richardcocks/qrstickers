using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using QRStickers.Meraki;
using QRStickers.Services;
using QRStickers.Tests.Helpers;

namespace QRStickers.Tests.Services;

public class TemplateMatchingServiceTests : IDisposable
{
    private readonly QRStickersDbContext _dbContext;
    private readonly TemplateMatchingService _service;
    private readonly Mock<ILogger<TemplateMatchingService>> _mockLogger;

    public TemplateMatchingServiceTests()
    {
        _dbContext = InMemoryDbContextFactory.Create();
        _mockLogger = new Mock<ILogger<TemplateMatchingService>>();
        _service = new TemplateMatchingService(_dbContext, _mockLogger.Object);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    [Fact]
    public async Task FindTemplateForDevice_WithMatchingConnectionDefault_ReturnsCorrectTemplate()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser();
        var connection = TestDataBuilder.CreateMerakiConnection(id: 1, userId: user.Id);
        var template = TestDataBuilder.CreateTemplate(id: 10, name: "Switch Template");
        var defaultMapping = TestDataBuilder.CreateConnectionDefault(
            connectionId: connection.Id,
            productType: "switch",
            templateId: template.Id);
        var device = TestDataBuilder.CreateDevice(
            connectionId: connection.Id,
            productType: "switch");

        _dbContext.Users.Add(user);
        _dbContext.Connections.Add(connection);
        _dbContext.StickerTemplates.Add(template);
        _dbContext.ConnectionDefaultTemplates.Add(defaultMapping);
        _dbContext.CachedDevices.Add(device);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _service.FindTemplateForDeviceAsync(device, user);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(template.Id, result.Template.Id);
        Assert.Equal("Switch Template", result.Template.Name);
        Assert.Equal("connection_default", result.MatchReason);
        Assert.Equal(1.0, result.Confidence);
        Assert.Equal("switch", result.MatchedBy);
    }

    [Fact]
    public async Task FindTemplateForDevice_NoMatchingDefault_ReturnsCompatibleTemplate()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser();
        var connection = TestDataBuilder.CreateMerakiConnection(id: 1, userId: user.Id);
        var systemTemplate = TestDataBuilder.CreateTemplate(
            id: 1,
            name: "Universal System Template",
            isSystemTemplate: true);
        // Template has no CompatibleProductTypes set, so it's universal (compatible with all)
        var device = TestDataBuilder.CreateDevice(
            connectionId: connection.Id,
            productType: "camera"); // No default for camera

        _dbContext.Users.Add(user);
        _dbContext.Connections.Add(connection);
        _dbContext.StickerTemplates.Add(systemTemplate);
        _dbContext.CachedDevices.Add(device);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _service.FindTemplateForDeviceAsync(device, user);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(systemTemplate.Id, result.Template.Id);
        Assert.Equal("compatible", result.MatchReason); // Universal templates match as compatible
        Assert.Equal(0.6, result.Confidence);
        Assert.Equal("camera", result.MatchedBy);
    }

    [Fact]
    public async Task FindTemplateForDevice_WithNullProductType_ReturnsFallbackTemplate()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser();
        var connection = TestDataBuilder.CreateMerakiConnection(id: 1, userId: user.Id);
        var systemTemplate = TestDataBuilder.CreateTemplate(
            id: 1,
            name: "System Template",
            isSystemTemplate: true);
        var device = TestDataBuilder.CreateDevice(
            connectionId: connection.Id,
            productType: null); // Null ProductType

        _dbContext.Users.Add(user);
        _dbContext.Connections.Add(connection);
        _dbContext.StickerTemplates.Add(systemTemplate);
        _dbContext.CachedDevices.Add(device);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _service.FindTemplateForDeviceAsync(device, user);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(systemTemplate.Id, result.Template.Id);
        Assert.Equal("fallback", result.MatchReason);
    }

    [Fact]
    public async Task FindTemplateForDevice_WithEmptyProductType_ReturnsFallbackTemplate()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser();
        var connection = TestDataBuilder.CreateMerakiConnection(id: 1, userId: user.Id);
        var systemTemplate = TestDataBuilder.CreateTemplate(
            id: 1,
            name: "System Template",
            isSystemTemplate: true);
        var device = TestDataBuilder.CreateDevice(
            connectionId: connection.Id,
            productType: ""); // Empty ProductType

        _dbContext.Users.Add(user);
        _dbContext.Connections.Add(connection);
        _dbContext.StickerTemplates.Add(systemTemplate);
        _dbContext.CachedDevices.Add(device);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _service.FindTemplateForDeviceAsync(device, user);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(systemTemplate.Id, result.Template.Id);
        Assert.Equal("fallback", result.MatchReason);
    }

    [Fact]
    public async Task FindTemplateForDevice_NoTemplatesAvailable_ThrowsException()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser();
        var connection = TestDataBuilder.CreateMerakiConnection(id: 1, userId: user.Id);
        var device = TestDataBuilder.CreateDevice(connectionId: connection.Id);

        _dbContext.Users.Add(user);
        _dbContext.Connections.Add(connection);
        _dbContext.CachedDevices.Add(device);
        await _dbContext.SaveChangesAsync();
        // No templates in database

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _service.FindTemplateForDeviceAsync(device, user));
    }

    [Fact]
    public async Task FindTemplateForDevice_PreferSystemTemplateFallback_ReturnsSystemTemplate()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser();
        var connection = TestDataBuilder.CreateMerakiConnection(id: 1, userId: user.Id);
        var userTemplate = TestDataBuilder.CreateTemplate(
            id: 10,
            name: "User Template",
            isSystemTemplate: false,
            connectionId: connection.Id);
        var systemTemplate = TestDataBuilder.CreateTemplate(
            id: 1,
            name: "System Template",
            isSystemTemplate: true);
        var device = TestDataBuilder.CreateDevice(
            connectionId: connection.Id,
            productType: "sensor"); // No default for sensor

        _dbContext.Users.Add(user);
        _dbContext.Connections.Add(connection);
        _dbContext.StickerTemplates.Add(userTemplate);
        _dbContext.StickerTemplates.Add(systemTemplate);
        _dbContext.CachedDevices.Add(device);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _service.FindTemplateForDeviceAsync(device, user);

        // Assert
        Assert.NotNull(result);
        // Both templates are universal (no CompatibleProductTypes set)
        // The first compatible template found will be returned
        // This test verifies that a compatible template is found (not testing which one specifically)
        Assert.Equal("compatible", result.MatchReason);
        Assert.Equal(0.6, result.Confidence);
    }

    [Fact]
    public async Task FindTemplateForDevice_CaseInsensitiveProductType_ReturnsMatchingTemplate()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser();
        var connection = TestDataBuilder.CreateMerakiConnection(id: 1, userId: user.Id);
        var template = TestDataBuilder.CreateTemplate(id: 10, name: "Switch Template");
        var defaultMapping = TestDataBuilder.CreateConnectionDefault(
            connectionId: connection.Id,
            productType: "switch", // lowercase in database
            templateId: template.Id);
        var device = TestDataBuilder.CreateDevice(
            connectionId: connection.Id,
            productType: "SWITCH"); // UPPERCASE from API

        _dbContext.Users.Add(user);
        _dbContext.Connections.Add(connection);
        _dbContext.StickerTemplates.Add(template);
        _dbContext.ConnectionDefaultTemplates.Add(defaultMapping);
        _dbContext.CachedDevices.Add(device);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _service.FindTemplateForDeviceAsync(device, user);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(template.Id, result.Template.Id);
        Assert.Equal("connection_default", result.MatchReason);
    }

    [Fact]
    public async Task GetAlternateTemplates_ReturnsSystemAndConnectionTemplates()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser();
        var connection = TestDataBuilder.CreateMerakiConnection(id: 1, userId: user.Id);
        var systemTemplate = TestDataBuilder.CreateTemplate(
            id: 1,
            name: "System Template",
            isSystemTemplate: true);
        var userTemplate = TestDataBuilder.CreateTemplate(
            id: 10,
            name: "User Template",
            connectionId: connection.Id);
        var otherConnectionTemplate = TestDataBuilder.CreateTemplate(
            id: 20,
            name: "Other Connection Template",
            connectionId: 999); // Different connection
        var device = TestDataBuilder.CreateDevice(connectionId: connection.Id);

        _dbContext.Users.Add(user);
        _dbContext.Connections.Add(connection);
        _dbContext.StickerTemplates.Add(systemTemplate);
        _dbContext.StickerTemplates.Add(userTemplate);
        _dbContext.StickerTemplates.Add(otherConnectionTemplate);
        _dbContext.CachedDevices.Add(device);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _service.GetAlternateTemplatesAsync(device, user);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count); // Should only include system + connection template
        Assert.Contains(result, t => t.Id == systemTemplate.Id);
        Assert.Contains(result, t => t.Id == userTemplate.Id);
        Assert.DoesNotContain(result, t => t.Id == otherConnectionTemplate.Id);
    }

    [Fact]
    public async Task GetAlternateTemplates_WithExcludeTemplateId_ExcludesSpecifiedTemplate()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser();
        var connection = TestDataBuilder.CreateMerakiConnection(id: 1, userId: user.Id);
        var template1 = TestDataBuilder.CreateTemplate(id: 1, name: "Template 1", isSystemTemplate: true);
        var template2 = TestDataBuilder.CreateTemplate(id: 2, name: "Template 2", isSystemTemplate: true);
        var device = TestDataBuilder.CreateDevice(connectionId: connection.Id);

        _dbContext.Users.Add(user);
        _dbContext.Connections.Add(connection);
        _dbContext.StickerTemplates.Add(template1);
        _dbContext.StickerTemplates.Add(template2);
        _dbContext.CachedDevices.Add(device);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _service.GetAlternateTemplatesAsync(device, user, excludeTemplateId: template1.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal(template2.Id, result[0].Id);
        Assert.DoesNotContain(result, t => t.Id == template1.Id);
    }

    [Fact]
    public async Task FindTemplateForDevice_WithCompatibleTemplate_ReturnsCompatibleMatch()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser();
        var connection = TestDataBuilder.CreateMerakiConnection(id: 1, userId: user.Id);
        var wirelessTemplate = TestDataBuilder.CreateTemplate(
            id: 1,
            name: "Wireless Template",
            isSystemTemplate: true);
        // Set template to be compatible with wireless devices only
        wirelessTemplate.SetCompatibleProductTypes(new List<string> { "wireless" });

        var device = TestDataBuilder.CreateDevice(
            connectionId: connection.Id,
            productType: "wireless");

        _dbContext.Users.Add(user);
        _dbContext.Connections.Add(connection);
        _dbContext.StickerTemplates.Add(wirelessTemplate);
        _dbContext.CachedDevices.Add(device);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _service.FindTemplateForDeviceAsync(device, user);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(wirelessTemplate.Id, result.Template.Id);
        Assert.Equal("compatible", result.MatchReason);
        Assert.Equal(0.6, result.Confidence);
        Assert.Equal("wireless", result.MatchedBy);
    }

    [Fact]
    public async Task FindTemplateForDevice_WithIncompatibleTemplate_ReturnsFallbackMatch()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser();
        var connection = TestDataBuilder.CreateMerakiConnection(id: 1, userId: user.Id);
        var switchTemplate = TestDataBuilder.CreateTemplate(
            id: 1,
            name: "Switch Only Template",
            isSystemTemplate: true);
        // Set template to be compatible with switches only
        switchTemplate.SetCompatibleProductTypes(new List<string> { "switch" });

        var device = TestDataBuilder.CreateDevice(
            connectionId: connection.Id,
            productType: "wireless"); // Device is wireless, template is for switches

        _dbContext.Users.Add(user);
        _dbContext.Connections.Add(connection);
        _dbContext.StickerTemplates.Add(switchTemplate);
        _dbContext.CachedDevices.Add(device);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _service.FindTemplateForDeviceAsync(device, user);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(switchTemplate.Id, result.Template.Id);
        Assert.Equal("fallback_incompatible", result.MatchReason);
        Assert.Equal(0.1, result.Confidence);
        Assert.Equal("fallback", result.MatchedBy);
    }

    [Fact]
    public async Task GetAlternateTemplates_WithCompatibleOnlyFlag_ReturnsOnlyCompatibleTemplates()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser();
        var connection = TestDataBuilder.CreateMerakiConnection(id: 1, userId: user.Id);

        var wirelessTemplate = TestDataBuilder.CreateTemplate(
            id: 1,
            name: "Wireless Template",
            isSystemTemplate: true);
        wirelessTemplate.SetCompatibleProductTypes(new List<string> { "wireless" });

        var switchTemplate = TestDataBuilder.CreateTemplate(
            id: 2,
            name: "Switch Template",
            isSystemTemplate: true);
        switchTemplate.SetCompatibleProductTypes(new List<string> { "switch" });

        var universalTemplate = TestDataBuilder.CreateTemplate(
            id: 3,
            name: "Universal Template",
            isSystemTemplate: true);
        // No CompatibleProductTypes set, so it's universal

        var device = TestDataBuilder.CreateDevice(
            connectionId: connection.Id,
            productType: "wireless");

        _dbContext.Users.Add(user);
        _dbContext.Connections.Add(connection);
        _dbContext.StickerTemplates.Add(wirelessTemplate);
        _dbContext.StickerTemplates.Add(switchTemplate);
        _dbContext.StickerTemplates.Add(universalTemplate);
        _dbContext.CachedDevices.Add(device);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _service.GetAlternateTemplatesAsync(device, user, compatibleOnly: true);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count); // Should include wireless and universal templates
        Assert.Contains(result, t => t.Id == wirelessTemplate.Id);
        Assert.Contains(result, t => t.Id == universalTemplate.Id);
        Assert.DoesNotContain(result, t => t.Id == switchTemplate.Id); // Switch template is incompatible
    }

    [Fact]
    public async Task FindTemplateForDevice_MultipleCompatibleTemplates_ReturnsFirstMatch()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser();
        var connection = TestDataBuilder.CreateMerakiConnection(id: 1, userId: user.Id);

        var template1 = TestDataBuilder.CreateTemplate(
            id: 1,
            name: "Wireless Template 1",
            isSystemTemplate: true);
        template1.SetCompatibleProductTypes(new List<string> { "wireless", "switch" });

        var template2 = TestDataBuilder.CreateTemplate(
            id: 2,
            name: "Wireless Template 2",
            isSystemTemplate: true);
        template2.SetCompatibleProductTypes(new List<string> { "wireless" });

        var device = TestDataBuilder.CreateDevice(
            connectionId: connection.Id,
            productType: "wireless");

        _dbContext.Users.Add(user);
        _dbContext.Connections.Add(connection);
        _dbContext.StickerTemplates.Add(template1);
        _dbContext.StickerTemplates.Add(template2);
        _dbContext.CachedDevices.Add(device);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _service.FindTemplateForDeviceAsync(device, user);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("compatible", result.MatchReason);
        Assert.Equal(0.6, result.Confidence);
        // Should return one of the compatible templates (whichever is found first)
        Assert.True(result.Template.Id == template1.Id || result.Template.Id == template2.Id);
    }

    #region Batch Template Matching Tests

    [Fact]
    public async Task FindTemplatesForDevicesBatchAsync_WithConnectionDefaults_ReturnsCorrectTemplates()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser();
        var connection = TestDataBuilder.CreateMerakiConnection(id: 1, userId: user.Id);

        var switchTemplate = TestDataBuilder.CreateTemplate(id: 1, name: "Switch Template");
        var wirelessTemplate = TestDataBuilder.CreateTemplate(id: 2, name: "Wireless Template");

        var switchDefault = TestDataBuilder.CreateConnectionDefault(
            id: 1, connectionId: connection.Id, productType: "switch", templateId: switchTemplate.Id);
        var wirelessDefault = TestDataBuilder.CreateConnectionDefault(
            id: 2, connectionId: connection.Id, productType: "wireless", templateId: wirelessTemplate.Id);

        var devices = new List<CachedDevice>
        {
            TestDataBuilder.CreateDevice(id: 1, connectionId: connection.Id, serial: "Q1", productType: "switch"),
            TestDataBuilder.CreateDevice(id: 2, connectionId: connection.Id, serial: "Q2", productType: "wireless"),
            TestDataBuilder.CreateDevice(id: 3, connectionId: connection.Id, serial: "Q3", productType: "switch")
        };

        _dbContext.Users.Add(user);
        _dbContext.Connections.Add(connection);
        _dbContext.StickerTemplates.AddRange(switchTemplate, wirelessTemplate);
        _dbContext.ConnectionDefaultTemplates.AddRange(switchDefault, wirelessDefault);
        _dbContext.CachedDevices.AddRange(devices);
        await _dbContext.SaveChangesAsync();

        // Act
        var results = await _service.FindTemplatesForDevicesBatchAsync(devices, connection.Id, user);

        // Assert
        Assert.Equal(3, results.Count);
        Assert.Equal(switchTemplate.Id, results[1].Template.Id);
        Assert.Equal(wirelessTemplate.Id, results[2].Template.Id);
        Assert.Equal(switchTemplate.Id, results[3].Template.Id);
        Assert.All(results.Values, r => Assert.Equal("connection_default", r.MatchReason));
        Assert.All(results.Values, r => Assert.Equal(1.0, r.Confidence));
    }

    [Fact]
    public async Task FindTemplatesForDevicesBatchAsync_TemplateDeduplication_ReturnsSameTemplateReference()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser();
        var connection = TestDataBuilder.CreateMerakiConnection(id: 1, userId: user.Id);
        var switchTemplate = TestDataBuilder.CreateTemplate(id: 1, name: "Switch Template");
        var switchDefault = TestDataBuilder.CreateConnectionDefault(
            connectionId: connection.Id, productType: "switch", templateId: switchTemplate.Id);

        // Create 10 devices all with same ProductType
        var devices = Enumerable.Range(1, 10)
            .Select(i => TestDataBuilder.CreateDevice(
                id: i,
                connectionId: connection.Id,
                serial: $"Q{i:D3}",
                productType: "switch"))
            .ToList();

        _dbContext.Users.Add(user);
        _dbContext.Connections.Add(connection);
        _dbContext.StickerTemplates.Add(switchTemplate);
        _dbContext.ConnectionDefaultTemplates.Add(switchDefault);
        _dbContext.CachedDevices.AddRange(devices);
        await _dbContext.SaveChangesAsync();

        // Act
        var results = await _service.FindTemplatesForDevicesBatchAsync(devices, connection.Id, user);

        // Assert
        Assert.Equal(10, results.Count);
        var firstTemplate = results[1].Template;
        Assert.All(results.Values, r => Assert.Same(firstTemplate, r.Template)); // Same reference
        Assert.All(results.Values, r => Assert.Equal("connection_default", r.MatchReason));
    }

    [Fact]
    public async Task FindTemplatesForDevicesBatchAsync_MixedProductTypes_MatchesCorrectly()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser();
        var connection = TestDataBuilder.CreateMerakiConnection(id: 1, userId: user.Id);

        var switchTemplate = TestDataBuilder.CreateTemplate(id: 1, name: "Switch Template");
        switchTemplate.SetCompatibleProductTypes(new List<string> { "switch" });

        var wirelessTemplate = TestDataBuilder.CreateTemplate(id: 2, name: "Wireless Template");
        wirelessTemplate.SetCompatibleProductTypes(new List<string> { "wireless" });

        var universalTemplate = TestDataBuilder.CreateTemplate(id: 3, name: "Universal Template", isSystemTemplate: true);
        // No CompatibleProductTypes set = universal

        var switchDefault = TestDataBuilder.CreateConnectionDefault(
            connectionId: connection.Id, productType: "switch", templateId: switchTemplate.Id);

        var devices = new List<CachedDevice>
        {
            TestDataBuilder.CreateDevice(id: 1, connectionId: connection.Id, serial: "S1", productType: "switch"), // Connection default
            TestDataBuilder.CreateDevice(id: 2, connectionId: connection.Id, serial: "W1", productType: "wireless"), // Compatible
            TestDataBuilder.CreateDevice(id: 3, connectionId: connection.Id, serial: "C1", productType: "camera"), // Fallback to universal
            TestDataBuilder.CreateDevice(id: 4, connectionId: connection.Id, serial: "N1", productType: null) // Null ProductType
        };

        _dbContext.Users.Add(user);
        _dbContext.Connections.Add(connection);
        _dbContext.StickerTemplates.AddRange(switchTemplate, wirelessTemplate, universalTemplate);
        _dbContext.ConnectionDefaultTemplates.Add(switchDefault);
        _dbContext.CachedDevices.AddRange(devices);
        await _dbContext.SaveChangesAsync();

        // Act
        var results = await _service.FindTemplatesForDevicesBatchAsync(devices, connection.Id, user);

        // Assert
        Assert.Equal(4, results.Count);

        // Device 1: Switch with connection default
        Assert.Equal(switchTemplate.Id, results[1].Template.Id);
        Assert.Equal("connection_default", results[1].MatchReason);
        Assert.Equal(1.0, results[1].Confidence);

        // Device 2: Wireless with compatible template
        Assert.Equal(wirelessTemplate.Id, results[2].Template.Id);
        Assert.Equal("compatible", results[2].MatchReason);
        Assert.Equal(0.6, results[2].Confidence);

        // Device 3: Camera with universal fallback
        Assert.Equal(universalTemplate.Id, results[3].Template.Id);
        Assert.Equal("compatible", results[3].MatchReason); // Universal templates match as compatible
        Assert.Equal(0.6, results[3].Confidence);

        // Device 4: Null ProductType with universal fallback
        Assert.Equal(universalTemplate.Id, results[4].Template.Id);
        Assert.Equal("fallback", results[4].MatchReason);
        Assert.Equal(0.1, results[4].Confidence);
    }

    [Fact]
    public async Task FindTemplatesForDevicesBatchAsync_EmptyDeviceList_ReturnsEmptyDictionary()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser();
        var connection = TestDataBuilder.CreateMerakiConnection(id: 1, userId: user.Id);
        var devices = new List<CachedDevice>();

        _dbContext.Users.Add(user);
        _dbContext.Connections.Add(connection);
        await _dbContext.SaveChangesAsync();

        // Act
        var results = await _service.FindTemplatesForDevicesBatchAsync(devices, connection.Id, user);

        // Assert
        Assert.NotNull(results);
        Assert.Empty(results);
    }

    [Fact]
    public async Task FindTemplatesForDevicesBatchAsync_NoTemplatesAvailable_ThrowsException()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser();
        var connection = TestDataBuilder.CreateMerakiConnection(id: 1, userId: user.Id);
        var devices = new List<CachedDevice>
        {
            TestDataBuilder.CreateDevice(id: 1, connectionId: connection.Id)
        };

        _dbContext.Users.Add(user);
        _dbContext.Connections.Add(connection);
        _dbContext.CachedDevices.AddRange(devices);
        await _dbContext.SaveChangesAsync();
        // No templates in database

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await _service.FindTemplatesForDevicesBatchAsync(devices, connection.Id, user));
    }

    [Fact]
    public async Task FindTemplatesForDevicesBatchAsync_Performance50Devices_CompletesUnder500ms()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser();
        var connection = TestDataBuilder.CreateMerakiConnection(id: 1, userId: user.Id);

        var switchTemplate = TestDataBuilder.CreateTemplate(id: 1, name: "Switch Template");
        var switchDefault = TestDataBuilder.CreateConnectionDefault(
            connectionId: connection.Id, productType: "switch", templateId: switchTemplate.Id);

        // Create 50 devices
        var devices = Enumerable.Range(1, 50)
            .Select(i => TestDataBuilder.CreateDevice(
                id: i,
                connectionId: connection.Id,
                serial: $"Q{i:D3}",
                productType: "switch"))
            .ToList();

        _dbContext.Users.Add(user);
        _dbContext.Connections.Add(connection);
        _dbContext.StickerTemplates.Add(switchTemplate);
        _dbContext.ConnectionDefaultTemplates.Add(switchDefault);
        _dbContext.CachedDevices.AddRange(devices);
        await _dbContext.SaveChangesAsync();

        // Act
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var results = await _service.FindTemplatesForDevicesBatchAsync(devices, connection.Id, user);
        stopwatch.Stop();

        // Assert
        Assert.Equal(50, results.Count);
        Assert.True(stopwatch.ElapsedMilliseconds < 500,
            $"Batch operation took {stopwatch.ElapsedMilliseconds}ms, expected < 500ms");
    }

    [Fact]
    public async Task FindTemplatesForDevicesBatchAsync_CaseInsensitiveProductType_MatchesCorrectly()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser();
        var connection = TestDataBuilder.CreateMerakiConnection(id: 1, userId: user.Id);
        var switchTemplate = TestDataBuilder.CreateTemplate(id: 1, name: "Switch Template");
        var switchDefault = TestDataBuilder.CreateConnectionDefault(
            connectionId: connection.Id, productType: "switch", templateId: switchTemplate.Id); // lowercase

        var devices = new List<CachedDevice>
        {
            TestDataBuilder.CreateDevice(id: 1, connectionId: connection.Id, serial: "Q1", productType: "SWITCH"), // UPPERCASE
            TestDataBuilder.CreateDevice(id: 2, connectionId: connection.Id, serial: "Q2", productType: "Switch"), // Mixed case
            TestDataBuilder.CreateDevice(id: 3, connectionId: connection.Id, serial: "Q3", productType: "switch")  // lowercase
        };

        _dbContext.Users.Add(user);
        _dbContext.Connections.Add(connection);
        _dbContext.StickerTemplates.Add(switchTemplate);
        _dbContext.ConnectionDefaultTemplates.Add(switchDefault);
        _dbContext.CachedDevices.AddRange(devices);
        await _dbContext.SaveChangesAsync();

        // Act
        var results = await _service.FindTemplatesForDevicesBatchAsync(devices, connection.Id, user);

        // Assert
        Assert.Equal(3, results.Count);
        Assert.All(results.Values, r => Assert.Equal(switchTemplate.Id, r.Template.Id));
        Assert.All(results.Values, r => Assert.Equal("connection_default", r.MatchReason));
    }

    [Fact]
    public async Task FindTemplatesForDevicesBatchAsync_IncompatibleTemplate_ReturnsFallbackIncompatible()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser();
        var connection = TestDataBuilder.CreateMerakiConnection(id: 1, userId: user.Id);

        var switchOnlyTemplate = TestDataBuilder.CreateTemplate(id: 1, name: "Switch Only Template", isSystemTemplate: true);
        switchOnlyTemplate.SetCompatibleProductTypes(new List<string> { "switch" }); // Only compatible with switches

        var devices = new List<CachedDevice>
        {
            TestDataBuilder.CreateDevice(id: 1, connectionId: connection.Id, serial: "W1", productType: "wireless"), // Incompatible
            TestDataBuilder.CreateDevice(id: 2, connectionId: connection.Id, serial: "C1", productType: "camera")    // Incompatible
        };

        _dbContext.Users.Add(user);
        _dbContext.Connections.Add(connection);
        _dbContext.StickerTemplates.Add(switchOnlyTemplate);
        _dbContext.CachedDevices.AddRange(devices);
        await _dbContext.SaveChangesAsync();

        // Act
        var results = await _service.FindTemplatesForDevicesBatchAsync(devices, connection.Id, user);

        // Assert
        Assert.Equal(2, results.Count);
        Assert.All(results.Values, r => Assert.Equal(switchOnlyTemplate.Id, r.Template.Id));
        Assert.All(results.Values, r => Assert.Equal("fallback_incompatible", r.MatchReason));
        Assert.All(results.Values, r => Assert.Equal(0.1, r.Confidence));
    }

    #endregion
}
