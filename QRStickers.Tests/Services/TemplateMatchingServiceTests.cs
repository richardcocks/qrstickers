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
    public async Task FindTemplateForDevice_NoMatchingDefault_ReturnsFallbackTemplate()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser();
        var connection = TestDataBuilder.CreateMerakiConnection(id: 1, userId: user.Id);
        var systemTemplate = TestDataBuilder.CreateTemplate(
            id: 1,
            name: "Fallback System Template",
            isSystemTemplate: true);
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
        Assert.Equal("fallback", result.MatchReason);
        Assert.Equal(0.1, result.Confidence);
        Assert.Equal("fallback", result.MatchedBy);
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
        Assert.Equal(systemTemplate.Id, result.Template.Id); // Should prefer system template
        Assert.True(result.Template.IsSystemTemplate);
        Assert.Equal("fallback", result.MatchReason);
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
}
