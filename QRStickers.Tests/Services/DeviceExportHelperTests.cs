using Microsoft.Extensions.Logging;
using Moq;
using QRStickers.Meraki;
using QRStickers.Services;
using QRStickers.Tests.Helpers;
using System.Text.Json;

namespace QRStickers.Tests.Services;

/// <summary>
/// Comprehensive test suite for DeviceExportHelper
/// Tests all public methods and edge cases
/// </summary>
public class DeviceExportHelperTests : IDisposable
{
    private readonly QRStickersDbContext _dbContext;
    private readonly DeviceExportHelper _helper;
    private readonly Mock<ILogger<DeviceExportHelper>> _mockLogger;

    public DeviceExportHelperTests()
    {
        _dbContext = InMemoryDbContextFactory.Create();
        _mockLogger = new Mock<ILogger<DeviceExportHelper>>();
        _helper = new DeviceExportHelper(_dbContext, _mockLogger.Object);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    /// <summary>
    /// Helper method to convert anonymous objects to JsonElement for easier assertions
    /// </summary>
    private static JsonElement ToJsonElement(object obj)
    {
        var json = JsonSerializer.Serialize(obj);
        return JsonDocument.Parse(json).RootElement;
    }

    // ===================== GetDeviceExportDataAsync Tests =====================

    [Fact]
    public async Task GetDeviceExportDataAsync_HappyPath_ReturnsCompleteContext()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser();
        var connection = TestDataBuilder.CreateMerakiConnection(id: 1, userId: user.Id);
        var network = TestDataBuilder.CreateNetwork(id: 1, connectionId: connection.Id);
        var organization = TestDataBuilder.CreateOrganization(id: 1, connectionId: connection.Id);
        var device = TestDataBuilder.CreateDevice(id: 1, connectionId: connection.Id, networkId: network.NetworkId);
        var globalVar = TestDataBuilder.CreateGlobalVariable(id: 1, connectionId: connection.Id);
        var uploadedImage = TestDataBuilder.CreateUploadedImage(id: 1, connectionId: connection.Id);

        _dbContext.Users.Add(user);
        _dbContext.Connections.Add(connection);
        _dbContext.CachedNetworks.Add(network);
        _dbContext.CachedOrganizations.Add(organization);
        _dbContext.CachedDevices.Add(device);
        _dbContext.GlobalVariables.Add(globalVar);
        _dbContext.UploadedImages.Add(uploadedImage);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _helper.GetDeviceExportDataAsync(device.Id, connection.Id, user);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(device.Id, result.Device.Id);
        Assert.Equal(connection.Id, result.Connection.Id);
        Assert.Equal(network.NetworkId, result.Network?.NetworkId);
        Assert.Equal(organization.OrganizationId, result.Organization?.OrganizationId);
        Assert.Single(result.GlobalVariables);
        Assert.Contains("company_name", result.GlobalVariables.Keys);
        Assert.Single(result.UploadedImages);
    }

    [Fact]
    public async Task GetDeviceExportDataAsync_DeviceNotFound_ThrowsArgumentException()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser();
        var connection = TestDataBuilder.CreateMerakiConnection(id: 1, userId: user.Id);

        _dbContext.Users.Add(user);
        _dbContext.Connections.Add(connection);
        await _dbContext.SaveChangesAsync();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => _helper.GetDeviceExportDataAsync(999, connection.Id, user));
        Assert.Contains("Device 999 not found", exception.Message);
    }

    [Fact]
    public async Task GetDeviceExportDataAsync_UnauthorizedUser_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var owner = TestDataBuilder.CreateUser(id: "owner-id", email: "owner@example.com");
        var otherUser = TestDataBuilder.CreateUser(id: "other-id", email: "other@example.com");
        var connection = TestDataBuilder.CreateMerakiConnection(id: 1, userId: owner.Id);
        var device = TestDataBuilder.CreateDevice(id: 1, connectionId: connection.Id);

        _dbContext.Users.AddRange(owner, otherUser);
        _dbContext.Connections.Add(connection);
        _dbContext.CachedDevices.Add(device);
        await _dbContext.SaveChangesAsync();

        // Act & Assert
        var exception = await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _helper.GetDeviceExportDataAsync(device.Id, connection.Id, otherUser));
        Assert.Contains("don't have permission", exception.Message);
    }

    [Fact]
    public async Task GetDeviceExportDataAsync_ConnectionNotFound_ThrowsArgumentException()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser();
        var connection = TestDataBuilder.CreateMerakiConnection(id: 1, userId: user.Id);
        var device = TestDataBuilder.CreateDevice(id: 1, connectionId: connection.Id);

        _dbContext.Users.Add(user);
        _dbContext.Connections.Add(connection);
        _dbContext.CachedDevices.Add(device);
        await _dbContext.SaveChangesAsync();

        // Act & Assert - Try to use wrong connection ID
        await Assert.ThrowsAsync<ArgumentException>(
            () => _helper.GetDeviceExportDataAsync(device.Id, 999, user));
    }

    [Fact]
    public async Task GetDeviceExportDataAsync_DeviceWithoutNetwork_ReturnsNullNetwork()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser();
        var connection = TestDataBuilder.CreateMerakiConnection(id: 1, userId: user.Id);
        var device = TestDataBuilder.CreateDevice(id: 1, connectionId: connection.Id, networkId: null);

        _dbContext.Users.Add(user);
        _dbContext.Connections.Add(connection);
        _dbContext.CachedDevices.Add(device);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _helper.GetDeviceExportDataAsync(device.Id, connection.Id, user);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.Network);
        Assert.Null(result.Organization);
    }

    [Fact]
    public async Task GetDeviceExportDataAsync_FiltersDeletedImages()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser();
        var connection = TestDataBuilder.CreateMerakiConnection(id: 1, userId: user.Id);
        var device = TestDataBuilder.CreateDevice(id: 1, connectionId: connection.Id);
        var activeImage = TestDataBuilder.CreateUploadedImage(id: 1, connectionId: connection.Id, isDeleted: false);
        var deletedImage = TestDataBuilder.CreateUploadedImage(id: 2, connectionId: connection.Id, isDeleted: true);

        _dbContext.Users.Add(user);
        _dbContext.Connections.Add(connection);
        _dbContext.CachedDevices.Add(device);
        _dbContext.UploadedImages.AddRange(activeImage, deletedImage);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _helper.GetDeviceExportDataAsync(device.Id, connection.Id, user);

        // Assert
        Assert.Single(result.UploadedImages);
        Assert.Equal(activeImage.Id, result.UploadedImages[0].Id);
    }

    [Fact]
    public async Task GetDeviceExportDataAsync_IncludesGlobalVariables()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser();
        var connection = TestDataBuilder.CreateMerakiConnection(id: 1, userId: user.Id);
        var device = TestDataBuilder.CreateDevice(id: 1, connectionId: connection.Id);
        var var1 = TestDataBuilder.CreateGlobalVariable(id: 1, connectionId: connection.Id, variableName: "var1", variableValue: "value1");
        var var2 = TestDataBuilder.CreateGlobalVariable(id: 2, connectionId: connection.Id, variableName: "var2", variableValue: "value2");

        _dbContext.Users.Add(user);
        _dbContext.Connections.Add(connection);
        _dbContext.CachedDevices.Add(device);
        _dbContext.GlobalVariables.AddRange(var1, var2);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _helper.GetDeviceExportDataAsync(device.Id, connection.Id, user);

        // Assert
        Assert.Equal(2, result.GlobalVariables.Count);
        Assert.Equal("value1", result.GlobalVariables["var1"]);
        Assert.Equal("value2", result.GlobalVariables["var2"]);
    }

    [Fact]
    public async Task GetDeviceExportDataAsync_WithNetworkAndOrganization_ReturnsComplete()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser();
        var connection = TestDataBuilder.CreateMerakiConnection(id: 1, userId: user.Id);
        var organization = TestDataBuilder.CreateOrganization(id: 1, connectionId: connection.Id, organizationId: "org-123");
        var network = TestDataBuilder.CreateNetwork(id: 1, connectionId: connection.Id, organizationId: organization.OrganizationId, networkId: "net-456");
        var device = TestDataBuilder.CreateDevice(id: 1, connectionId: connection.Id, networkId: network.NetworkId);

        _dbContext.Users.Add(user);
        _dbContext.Connections.Add(connection);
        _dbContext.CachedOrganizations.Add(organization);
        _dbContext.CachedNetworks.Add(network);
        _dbContext.CachedDevices.Add(device);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _helper.GetDeviceExportDataAsync(device.Id, connection.Id, user);

        // Assert
        Assert.NotNull(result.Network);
        Assert.Equal("net-456", result.Network.NetworkId);
        Assert.NotNull(result.Organization);
        Assert.Equal("org-123", result.Organization.OrganizationId);
    }

    // ===================== GetGlobalVariablesAsync Tests =====================

    [Fact]
    public async Task GetGlobalVariablesAsync_ReturnsDictionary()
    {
        // Arrange
        var connection = TestDataBuilder.CreateMerakiConnection(id: 1);
        var var1 = TestDataBuilder.CreateGlobalVariable(id: 1, connectionId: connection.Id, variableName: "key1", variableValue: "val1");
        var var2 = TestDataBuilder.CreateGlobalVariable(id: 2, connectionId: connection.Id, variableName: "key2", variableValue: "val2");

        _dbContext.Connections.Add(connection);
        _dbContext.GlobalVariables.AddRange(var1, var2);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _helper.GetGlobalVariablesAsync(connection.Id);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("val1", result["key1"]);
        Assert.Equal("val2", result["key2"]);
    }

    [Fact]
    public async Task GetGlobalVariablesAsync_NoVariables_ReturnsEmptyDictionary()
    {
        // Arrange
        var connection = TestDataBuilder.CreateMerakiConnection(id: 1);
        _dbContext.Connections.Add(connection);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _helper.GetGlobalVariablesAsync(connection.Id);

        // Assert
        Assert.Empty(result);
    }

    // ===================== TrackImageUsageAsync Tests =====================

    [Fact]
    public async Task TrackImageUsageAsync_UpdatesLastUsedAt()
    {
        // Arrange
        var connection = TestDataBuilder.CreateMerakiConnection(id: 1);
        var image1 = TestDataBuilder.CreateUploadedImage(id: 1, connectionId: connection.Id);
        var image2 = TestDataBuilder.CreateUploadedImage(id: 2, connectionId: connection.Id);
        image1.LastUsedAt = null;
        image2.LastUsedAt = null;

        _dbContext.Connections.Add(connection);
        _dbContext.UploadedImages.AddRange(image1, image2);
        await _dbContext.SaveChangesAsync();

        var beforeUpdate = DateTime.UtcNow;

        // Act
        await _helper.TrackImageUsageAsync(new[] { 1, 2 });

        // Assert
        var updatedImages = _dbContext.UploadedImages.Where(i => i.Id <= 2).ToList();
        Assert.All(updatedImages, img =>
        {
            Assert.NotNull(img.LastUsedAt);
            Assert.True(img.LastUsedAt >= beforeUpdate);
        });
    }

    [Fact]
    public async Task TrackImageUsageAsync_EmptyArray_NoOp()
    {
        // Act & Assert - Should not throw
        await _helper.TrackImageUsageAsync(Array.Empty<int>());
    }

    [Fact]
    public async Task TrackImageUsageAsync_NullArray_NoOp()
    {
        // Act & Assert - Should not throw
        await _helper.TrackImageUsageAsync(null!);
    }

    // ===================== GetBulkDeviceExportDataAsync Tests =====================

    [Fact]
    public async Task GetBulkDeviceExportDataAsync_HappyPath_ReturnsListOfContexts()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser();
        var connection = TestDataBuilder.CreateMerakiConnection(id: 1, userId: user.Id);
        var device1 = TestDataBuilder.CreateDevice(id: 1, connectionId: connection.Id);
        var device2 = TestDataBuilder.CreateDevice(id: 2, connectionId: connection.Id);
        var globalVar = TestDataBuilder.CreateGlobalVariable(id: 1, connectionId: connection.Id);

        _dbContext.Users.Add(user);
        _dbContext.Connections.Add(connection);
        _dbContext.CachedDevices.AddRange(device1, device2);
        _dbContext.GlobalVariables.Add(globalVar);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _helper.GetBulkDeviceExportDataAsync(new[] { 1, 2 }, connection.Id, user);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, ctx => ctx.Device.Id == 1);
        Assert.Contains(result, ctx => ctx.Device.Id == 2);
        // Global variables should be shared (same instance)
        Assert.Equal(result[0].GlobalVariables, result[1].GlobalVariables);
    }

    [Fact]
    public async Task GetBulkDeviceExportDataAsync_ConnectionNotFound_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser();
        _dbContext.Users.Add(user);
        await _dbContext.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _helper.GetBulkDeviceExportDataAsync(new[] { 1 }, 999, user));
    }

    [Fact]
    public async Task GetBulkDeviceExportDataAsync_NoDevicesFound_ThrowsArgumentException()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser();
        var connection = TestDataBuilder.CreateMerakiConnection(id: 1, userId: user.Id);

        _dbContext.Users.Add(user);
        _dbContext.Connections.Add(connection);
        await _dbContext.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => _helper.GetBulkDeviceExportDataAsync(new[] { 999 }, connection.Id, user));
    }

    [Fact]
    public async Task GetBulkDeviceExportDataAsync_UnauthorizedDevices_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var owner = TestDataBuilder.CreateUser(id: "owner-id");
        var otherUser = TestDataBuilder.CreateUser(id: "other-id", email: "other@example.com");
        var connection = TestDataBuilder.CreateMerakiConnection(id: 1, userId: owner.Id);
        var device = TestDataBuilder.CreateDevice(id: 1, connectionId: connection.Id);

        _dbContext.Users.AddRange(owner, otherUser);
        _dbContext.Connections.Add(connection);
        _dbContext.CachedDevices.Add(device);
        await _dbContext.SaveChangesAsync();

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => _helper.GetBulkDeviceExportDataAsync(new[] { 1 }, connection.Id, otherUser));
    }

    [Fact]
    public async Task GetBulkDeviceExportDataAsync_SharesDataAcrossDevices()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser();
        var connection = TestDataBuilder.CreateMerakiConnection(id: 1, userId: user.Id);
        var device1 = TestDataBuilder.CreateDevice(id: 1, connectionId: connection.Id);
        var device2 = TestDataBuilder.CreateDevice(id: 2, connectionId: connection.Id);
        var globalVar = TestDataBuilder.CreateGlobalVariable(id: 1, connectionId: connection.Id);
        var image = TestDataBuilder.CreateUploadedImage(id: 1, connectionId: connection.Id);

        _dbContext.Users.Add(user);
        _dbContext.Connections.Add(connection);
        _dbContext.CachedDevices.AddRange(device1, device2);
        _dbContext.GlobalVariables.Add(globalVar);
        _dbContext.UploadedImages.Add(image);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _helper.GetBulkDeviceExportDataAsync(new[] { 1, 2 }, connection.Id, user);

        // Assert - Data should be shared (optimization)
        Assert.Equal(result[0].GlobalVariables, result[1].GlobalVariables);
        Assert.Equal(result[0].UploadedImages, result[1].UploadedImages);
        Assert.Equal(result[0].Connection, result[1].Connection);
    }

    // ===================== BuildTemplateOptionsArray Tests (Static) =====================

    [Fact]
    public void BuildTemplateOptionsArray_WithRecommendedTemplate_IncludesIt()
    {
        // Arrange
        var recommendedTemplate = TestDataBuilder.CreateTemplate(id: 1, name: "Recommended");
        var matchedTemplate = TestDataBuilder.CreateTemplate(id: 2, name: "Matched");
        var filterResult = TestDataBuilder.CreateTemplateFilterResult(recommendedTemplate: recommendedTemplate);

        // Act
        var result = DeviceExportHelper.BuildTemplateOptionsArray(filterResult, matchedTemplate.Id);

        // Assert
        Assert.Single(result);
        var firstOption = ToJsonElement(result[0]);
        Assert.Equal("recommended", firstOption.GetProperty("category").GetString());
        Assert.True(firstOption.GetProperty("isRecommended").GetBoolean());
        Assert.True(firstOption.GetProperty("isCompatible").GetBoolean());
    }

    [Fact]
    public void BuildTemplateOptionsArray_RecommendedSameAsMatched_SkipsIt()
    {
        // Arrange
        var template = TestDataBuilder.CreateTemplate(id: 1, name: "Same Template");
        var filterResult = TestDataBuilder.CreateTemplateFilterResult(recommendedTemplate: template);

        // Act
        var result = DeviceExportHelper.BuildTemplateOptionsArray(filterResult, template.Id);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void BuildTemplateOptionsArray_WithCompatibleTemplates_IncludesThem()
    {
        // Arrange
        var compatible1 = TestDataBuilder.CreateTemplate(id: 1, name: "Compatible 1");
        var compatible2 = TestDataBuilder.CreateTemplate(id: 2, name: "Compatible 2");
        var matched = TestDataBuilder.CreateTemplate(id: 3, name: "Matched");
        var filterResult = TestDataBuilder.CreateTemplateFilterResult(
            compatibleTemplates: new List<StickerTemplate> { compatible1, compatible2 });

        // Act
        var result = DeviceExportHelper.BuildTemplateOptionsArray(filterResult, matched.Id);

        // Assert
        Assert.Equal(2, result.Length);
        Assert.All(result, option =>
        {
            var opt = ToJsonElement(option);
            Assert.Equal("compatible", opt.GetProperty("category").GetString());
            Assert.False(opt.GetProperty("isRecommended").GetBoolean());
            Assert.True(opt.GetProperty("isCompatible").GetBoolean());
        });
    }

    [Fact]
    public void BuildTemplateOptionsArray_WithIncompatibleTemplates_IncludesThem()
    {
        // Arrange
        var incompatible1 = TestDataBuilder.CreateTemplate(id: 1, name: "Incompatible 1");
        var matched = TestDataBuilder.CreateTemplate(id: 2, name: "Matched");
        var filterResult = TestDataBuilder.CreateTemplateFilterResult(
            incompatibleTemplates: new List<StickerTemplate> { incompatible1 });

        // Act
        var result = DeviceExportHelper.BuildTemplateOptionsArray(filterResult, matched.Id);

        // Assert
        Assert.Single(result);
        var firstOption = ToJsonElement(result[0]);
        Assert.Equal("incompatible", firstOption.GetProperty("category").GetString());
        Assert.False(firstOption.GetProperty("isRecommended").GetBoolean());
        Assert.False(firstOption.GetProperty("isCompatible").GetBoolean());
    }

    [Fact]
    public void BuildTemplateOptionsArray_SkipsMatchedTemplateInCompatibleList()
    {
        // Arrange
        var matched = TestDataBuilder.CreateTemplate(id: 1, name: "Matched");
        var other = TestDataBuilder.CreateTemplate(id: 2, name: "Other");
        var filterResult = TestDataBuilder.CreateTemplateFilterResult(
            compatibleTemplates: new List<StickerTemplate> { matched, other });

        // Act
        var result = DeviceExportHelper.BuildTemplateOptionsArray(filterResult, matched.Id);

        // Assert
        Assert.Single(result); // Only 'other' should be included
        var firstOption = ToJsonElement(result[0]);
        Assert.Equal(2, firstOption.GetProperty("template").GetProperty("id").GetInt32()); // Should be 'other'
    }

    [Fact]
    public void BuildTemplateOptionsArray_EmptyFilterResult_ReturnsEmptyArray()
    {
        // Arrange
        var filterResult = TestDataBuilder.CreateTemplateFilterResult();

        // Act
        var result = DeviceExportHelper.BuildTemplateOptionsArray(filterResult, 1);

        // Assert
        Assert.Empty(result);
    }

    // ===================== BuildBulkExportResponse Tests (Static) =====================

    [Fact]
    public void BuildBulkExportResponse_DeduplicatesTemplates()
    {
        // Arrange
        var template1 = TestDataBuilder.CreateTemplate(id: 1, name: "Template 1");
        var template2 = TestDataBuilder.CreateTemplate(id: 2, name: "Template 2");
        var device1 = TestDataBuilder.CreateDevice(id: 1);
        var device2 = TestDataBuilder.CreateDevice(id: 2);
        var connection = TestDataBuilder.CreateMerakiConnection(id: 1);

        var templateMatches = new Dictionary<int, TemplateMatchResult>
        {
            { 1, TestDataBuilder.CreateTemplateMatchResult(template: template1) },
            { 2, TestDataBuilder.CreateTemplateMatchResult(template: template1) } // Same template
        };

        var devices = new List<CachedDevice> { device1, device2 };

        // Act
        var resultObj = DeviceExportHelper.BuildBulkExportResponse(
            devices,
            templateMatches,
            new Dictionary<string, TemplateFilterResult>(),
            new List<CachedNetwork>(),
            new List<CachedOrganization>(),
            connection,
            new Dictionary<string, string>(),
            new List<UploadedImage>());
        var result = ToJsonElement(resultObj);

        // Assert
        var templates = result.GetProperty("templates");
        Assert.Single(templates.EnumerateObject()); // Only one template despite two devices using it
        Assert.True(templates.TryGetProperty("tpl_1", out _));
    }

    [Fact]
    public void BuildBulkExportResponse_BuildsNetworkDictionary()
    {
        // Arrange
        var device = TestDataBuilder.CreateDevice(id: 1, networkId: "net-1");
        var network = TestDataBuilder.CreateNetwork(id: 1, networkId: "net-1");
        var connection = TestDataBuilder.CreateMerakiConnection(id: 1);
        var template = TestDataBuilder.CreateTemplate(id: 1);
        var templateMatches = new Dictionary<int, TemplateMatchResult>
        {
            { 1, TestDataBuilder.CreateTemplateMatchResult(template: template) }
        };

        // Act
        var resultObj = DeviceExportHelper.BuildBulkExportResponse(
            new List<CachedDevice> { device },
            templateMatches,
            new Dictionary<string, TemplateFilterResult>(),
            new List<CachedNetwork> { network },
            new List<CachedOrganization>(),
            connection,
            new Dictionary<string, string>(),
            new List<UploadedImage>());
        var result = ToJsonElement(resultObj);

        // Assert
        var networks = result.GetProperty("networks");
        Assert.Single(networks.EnumerateObject());
        Assert.True(networks.TryGetProperty("net_1", out _));
    }

    [Fact]
    public void BuildBulkExportResponse_BuildsDeviceDictionary()
    {
        // Arrange
        var device1 = TestDataBuilder.CreateDevice(id: 1, serial: "ABC-123");
        var device2 = TestDataBuilder.CreateDevice(id: 2, serial: "DEF-456");
        var connection = TestDataBuilder.CreateMerakiConnection(id: 1);
        var template = TestDataBuilder.CreateTemplate(id: 1);
        var templateMatches = new Dictionary<int, TemplateMatchResult>
        {
            { 1, TestDataBuilder.CreateTemplateMatchResult(template: template) },
            { 2, TestDataBuilder.CreateTemplateMatchResult(template: template) }
        };

        // Act
        var resultObj = DeviceExportHelper.BuildBulkExportResponse(
            new List<CachedDevice> { device1, device2 },
            templateMatches,
            new Dictionary<string, TemplateFilterResult>(),
            new List<CachedNetwork>(),
            new List<CachedOrganization>(),
            connection,
            new Dictionary<string, string>(),
            new List<UploadedImage>());
        var result = ToJsonElement(resultObj);

        // Assert
        var devices = result.GetProperty("devices");
        Assert.Equal(2, devices.EnumerateObject().Count());
        Assert.True(devices.TryGetProperty("1", out _));
        Assert.True(devices.TryGetProperty("2", out _));
    }

    [Fact]
    public void BuildBulkExportResponse_CreatesDeviceReferences()
    {
        // Arrange
        var device = TestDataBuilder.CreateDevice(id: 1, networkId: "net-1");
        var network = TestDataBuilder.CreateNetwork(id: 1, networkId: "net-1");
        var connection = TestDataBuilder.CreateMerakiConnection(id: 1);
        var template = TestDataBuilder.CreateTemplate(id: 5);
        var templateMatches = new Dictionary<int, TemplateMatchResult>
        {
            { 1, TestDataBuilder.CreateTemplateMatchResult(template: template) }
        };

        // Act
        var resultObj = DeviceExportHelper.BuildBulkExportResponse(
            new List<CachedDevice> { device },
            templateMatches,
            new Dictionary<string, TemplateFilterResult>(),
            new List<CachedNetwork> { network },
            new List<CachedOrganization>(),
            connection,
            new Dictionary<string, string>(),
            new List<UploadedImage>());
        var result = ToJsonElement(resultObj);

        // Assert
        var deviceData = result.GetProperty("devices").GetProperty("1");
        Assert.Equal("net_1", deviceData.GetProperty("networkRef").GetString());
        Assert.Equal("tpl_5", deviceData.GetProperty("matchedTemplateRef").GetString());
    }

    [Fact]
    public void BuildBulkExportResponse_HandlesDevicesWithoutNetwork()
    {
        // Arrange
        var device = TestDataBuilder.CreateDevice(id: 1, networkId: null);
        var connection = TestDataBuilder.CreateMerakiConnection(id: 1);
        var template = TestDataBuilder.CreateTemplate(id: 1);
        var templateMatches = new Dictionary<int, TemplateMatchResult>
        {
            { 1, TestDataBuilder.CreateTemplateMatchResult(template: template) }
        };

        // Act
        var resultObj = DeviceExportHelper.BuildBulkExportResponse(
            new List<CachedDevice> { device },
            templateMatches,
            new Dictionary<string, TemplateFilterResult>(),
            new List<CachedNetwork>(),
            new List<CachedOrganization>(),
            connection,
            new Dictionary<string, string>(),
            new List<UploadedImage>());
        var result = ToJsonElement(resultObj);

        // Assert
        var deviceData = result.GetProperty("devices").GetProperty("1");
        Assert.Equal(JsonValueKind.Null, deviceData.GetProperty("networkRef").ValueKind);
    }

    // ===================== DeviceExportContext.ToDeviceDataMap Tests =====================

    [Fact]
    public void ToDeviceDataMap_WithCompleteData_ReturnsMappedObject()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser();
        var connection = TestDataBuilder.CreateMerakiConnection(id: 1, userId: user.Id);
        var network = TestDataBuilder.CreateNetwork(id: 1, networkId: "net-1", name: "Test Network");
        var device = TestDataBuilder.CreateDevice(id: 1, serial: "Q2XX-AAAA-BBBB", name: "Switch01", model: "MS250-48");

        var context = new DeviceExportContext
        {
            Device = device,
            Network = network,
            Organization = null,
            Connection = connection,
            GlobalVariables = new Dictionary<string, string> { { "key1", "value1" } },
            UploadedImages = new List<UploadedImage>()
        };

        // Act
        var resultObj = context.ToDeviceDataMap();
        var result = ToJsonElement(resultObj);

        // Assert
        Assert.Equal("Q2XX-AAAA-BBBB", result.GetProperty("device").GetProperty("serial").GetString());
        Assert.Equal("Switch01", result.GetProperty("device").GetProperty("name").GetString());
        Assert.Equal("switch", result.GetProperty("device").GetProperty("type").GetString()); // MS = switch
        Assert.Equal(JsonValueKind.Object, result.GetProperty("network").ValueKind);
        Assert.Equal("Test Network", result.GetProperty("network").GetProperty("name").GetString());
        Assert.Equal(JsonValueKind.Object, result.GetProperty("global").ValueKind);
        Assert.Equal("value1", result.GetProperty("global").GetProperty("key1").GetString());
    }

    [Fact]
    public void ToDeviceDataMap_WithoutNetwork_ReturnsNullNetwork()
    {
        // Arrange
        var connection = TestDataBuilder.CreateMerakiConnection(id: 1);
        var device = TestDataBuilder.CreateDevice(id: 1);

        var context = new DeviceExportContext
        {
            Device = device,
            Network = null,
            Organization = null,
            Connection = connection,
            GlobalVariables = new Dictionary<string, string>(),
            UploadedImages = new List<UploadedImage>()
        };

        // Act
        var resultObj = context.ToDeviceDataMap();
        var result = ToJsonElement(resultObj);

        // Assert
        Assert.Equal(JsonValueKind.Null, result.GetProperty("network").ValueKind);
    }

    [Fact]
    public void ToDeviceDataMap_WithMerakiConnection_IncludesCompanyLogoUrl()
    {
        // Arrange
        var merakiConnection = new MerakiConnection
        {
            Id = 1,
            UserId = "user-1",
            DisplayName = "Test Connection",
            CompanyLogoUrl = "https://example.com/logo.png",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        var device = TestDataBuilder.CreateDevice(id: 1);

        var context = new DeviceExportContext
        {
            Device = device,
            Network = null,
            Organization = null,
            Connection = merakiConnection,
            GlobalVariables = new Dictionary<string, string>(),
            UploadedImages = new List<UploadedImage>()
        };

        // Act
        var resultObj = context.ToDeviceDataMap();
        var result = ToJsonElement(resultObj);

        // Assert
        Assert.Equal("https://example.com/logo.png", result.GetProperty("connection").GetProperty("companyLogoUrl").GetString());
    }

    [Fact]
    public void ToDeviceDataMap_DeviceTypeDetermination_CorrectlyClassifiesModels()
    {
        // Test cases for device type determination
        var testCases = new[]
        {
            ("MS250-48", "switch"),
            ("C9300-48P", "switch"),
            ("MR45", "ap"),
            ("MX84", "gateway"),
            ("Z3C", "appliance"),
            ("MV12", "camera"),
            ("MT20", "sensor"),
            ("MC74", "cellular"),
            ("Unknown", "unknown"),
            (null, "unknown")
        };

        foreach (var (model, expectedType) in testCases)
        {
            // Arrange
            var connection = TestDataBuilder.CreateMerakiConnection(id: 1);
            var device = TestDataBuilder.CreateDevice(id: 1, model: model);

            var context = new DeviceExportContext
            {
                Device = device,
                Network = null,
                Organization = null,
                Connection = connection,
                GlobalVariables = new Dictionary<string, string>(),
                UploadedImages = new List<UploadedImage>()
            };

            // Act
            var resultObj = context.ToDeviceDataMap();
            var result = ToJsonElement(resultObj);

            // Assert
            Assert.Equal(expectedType, result.GetProperty("device").GetProperty("type").GetString());
        }
    }
}
