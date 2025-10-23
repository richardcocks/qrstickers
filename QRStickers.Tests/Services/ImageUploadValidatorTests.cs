using Microsoft.Extensions.Logging;
using Moq;
using QRStickers.Services;
using QRStickers.Tests.Helpers;

namespace QRStickers.Tests.Services;

public class ImageUploadValidatorTests : IDisposable
{
    private readonly QRStickersDbContext _dbContext;
    private readonly ImageUploadValidator _validator;
    private readonly Mock<ILogger<ImageUploadValidator>> _mockLogger;

    public ImageUploadValidatorTests()
    {
        _dbContext = InMemoryDbContextFactory.Create();
        _mockLogger = new Mock<ILogger<ImageUploadValidator>>();
        _validator = new ImageUploadValidator(_dbContext, _mockLogger.Object);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    [Fact]
    public async Task ValidateUpload_ValidPngImage_ReturnsSuccess()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser();
        var connection = TestDataBuilder.CreateMerakiConnection(userId: user.Id);
        _dbContext.Users.Add(user);
        _dbContext.Connections.Add(connection);
        await _dbContext.SaveChangesAsync();

        var dataUri = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==";

        // Act
        var result = await _validator.ValidateUploadAsync(
            connection.Id,
            "Test Image",
            dataUri,
            widthPx: 100,
            heightPx: 100,
            user.Id);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal("image/png", result.MimeType);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public async Task ValidateUpload_ValidJpegImage_ReturnsSuccess()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser();
        var connection = TestDataBuilder.CreateMerakiConnection(userId: user.Id);
        _dbContext.Users.Add(user);
        _dbContext.Connections.Add(connection);
        await _dbContext.SaveChangesAsync();

        var dataUri = "data:image/jpeg;base64,/9j/4AAQSkZJRgABAQEASABIAAD/2wBDAAEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQH/2wBDAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQEBAQH/wAARCAABAAEDASIAAhEBAxEB/8QAFQABAQAAAAAAAAAAAAAAAAAAAAv/xAAUEAEAAAAAAAAAAAAAAAAAAAAA/8QAFQEBAQAAAAAAAAAAAAAAAAAAAAX/xAAUEQEAAAAAAAAAAAAAAAAAAAAA/9oADAMBAAIRAxEAPwA/8H//2Q==";

        // Act
        var result = await _validator.ValidateUploadAsync(
            connection.Id,
            "Test JPEG",
            dataUri,
            widthPx: 100,
            heightPx: 100,
            user.Id);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal("image/jpeg", result.MimeType);
    }

    [Fact]
    public async Task ValidateUpload_ConnectionNotOwned_ReturnsFail()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser();
        var otherUser = TestDataBuilder.CreateUser(id: "other-user");
        var connection = TestDataBuilder.CreateMerakiConnection(userId: otherUser.Id);
        _dbContext.Users.Add(user);
        _dbContext.Users.Add(otherUser);
        _dbContext.Connections.Add(connection);
        await _dbContext.SaveChangesAsync();

        var dataUri = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==";

        // Act
        var result = await _validator.ValidateUploadAsync(
            connection.Id,
            "Test Image",
            dataUri,
            widthPx: 100,
            heightPx: 100,
            user.Id); // User doesn't own the connection

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("not found or access denied", result.ErrorMessage);
    }

    [Fact]
    public async Task ValidateUpload_InvalidDataUriPrefix_ReturnsFail()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser();
        var connection = TestDataBuilder.CreateMerakiConnection(userId: user.Id);
        _dbContext.Users.Add(user);
        _dbContext.Connections.Add(connection);
        await _dbContext.SaveChangesAsync();

        var dataUri = "invalid-prefix;base64,abc123";

        // Act
        var result = await _validator.ValidateUploadAsync(
            connection.Id,
            "Test Image",
            dataUri,
            widthPx: 100,
            heightPx: 100,
            user.Id);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("must start with 'data:image/'", result.ErrorMessage);
    }

    [Fact]
    public async Task ValidateUpload_UnsupportedMimeType_ReturnsFail()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser();
        var connection = TestDataBuilder.CreateMerakiConnection(userId: user.Id);
        _dbContext.Users.Add(user);
        _dbContext.Connections.Add(connection);
        await _dbContext.SaveChangesAsync();

        var dataUri = "data:image/gif;base64,R0lGODlhAQABAAAAACH5BAEKAAEALAAAAAABAAEAAAICTAEAOw=="; // GIF not allowed

        // Act
        var result = await _validator.ValidateUploadAsync(
            connection.Id,
            "Test GIF",
            dataUri,
            widthPx: 100,
            heightPx: 100,
            user.Id);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("Unsupported MIME type", result.ErrorMessage);
        Assert.Contains("image/gif", result.ErrorMessage);
    }

    [Theory]
    [InlineData(901, 100)] // Width too large
    [InlineData(100, 901)] // Height too large
    [InlineData(1000, 1000)] // Both too large
    public async Task ValidateUpload_DimensionsTooLarge_ReturnsFail(int width, int height)
    {
        // Arrange
        var user = TestDataBuilder.CreateUser();
        var connection = TestDataBuilder.CreateMerakiConnection(userId: user.Id);
        _dbContext.Users.Add(user);
        _dbContext.Connections.Add(connection);
        await _dbContext.SaveChangesAsync();

        var dataUri = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==";

        // Act
        var result = await _validator.ValidateUploadAsync(
            connection.Id,
            "Test Image",
            dataUri,
            widthPx: width,
            heightPx: height,
            user.Id);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("dimensions too large", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("900", result.ErrorMessage); // Max dimension
    }

    [Theory]
    [InlineData(0, 100)] // Zero width
    [InlineData(100, 0)] // Zero height
    [InlineData(-1, 100)] // Negative width
    [InlineData(100, -1)] // Negative height
    public async Task ValidateUpload_InvalidDimensions_ReturnsFail(int width, int height)
    {
        // Arrange
        var user = TestDataBuilder.CreateUser();
        var connection = TestDataBuilder.CreateMerakiConnection(userId: user.Id);
        _dbContext.Users.Add(user);
        _dbContext.Connections.Add(connection);
        await _dbContext.SaveChangesAsync();

        var dataUri = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==";

        // Act
        var result = await _validator.ValidateUploadAsync(
            connection.Id,
            "Test Image",
            dataUri,
            widthPx: width,
            heightPx: height,
            user.Id);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("Invalid dimensions", result.ErrorMessage);
    }

    [Theory]
    [InlineData(null)] // Null name
    [InlineData("")] // Empty name
    [InlineData("   ")] // Whitespace only
    public async Task ValidateUpload_InvalidName_ReturnsFail(string? name)
    {
        // Arrange
        var user = TestDataBuilder.CreateUser();
        var connection = TestDataBuilder.CreateMerakiConnection(userId: user.Id);
        _dbContext.Users.Add(user);
        _dbContext.Connections.Add(connection);
        await _dbContext.SaveChangesAsync();

        var dataUri = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==";

        // Act
        var result = await _validator.ValidateUploadAsync(
            connection.Id,
            name!,
            dataUri,
            widthPx: 100,
            heightPx: 100,
            user.Id);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("name is required", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ValidateUpload_NameTooLong_ReturnsFail()
    {
        // Arrange
        var user = TestDataBuilder.CreateUser();
        var connection = TestDataBuilder.CreateMerakiConnection(userId: user.Id);
        _dbContext.Users.Add(user);
        _dbContext.Connections.Add(connection);
        await _dbContext.SaveChangesAsync();

        var dataUri = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==";
        var longName = new string('a', 201); // 201 characters

        // Act
        var result = await _validator.ValidateUploadAsync(
            connection.Id,
            longName,
            dataUri,
            widthPx: 100,
            heightPx: 100,
            user.Id);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("name too long", result.ErrorMessage, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("200", result.ErrorMessage);
    }

    [Fact]
    public async Task GetQuotaInfo_NoImages_ReturnsZeroUsage()
    {
        // Arrange
        var connection = TestDataBuilder.CreateMerakiConnection();
        _dbContext.Connections.Add(connection);
        await _dbContext.SaveChangesAsync();

        // Act
        var quota = await _validator.GetQuotaInfoAsync(connection.Id);

        // Assert
        Assert.Equal(0, quota.ImagesUsed);
        Assert.Equal(25, quota.ImagesLimit);
        Assert.Equal(0, quota.StorageUsed);
        Assert.Equal(20_000_000, quota.StorageLimit);
    }
}
