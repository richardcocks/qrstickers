using Microsoft.Extensions.Logging;
using Moq;
using QRCoder;
using QRStickers.Services;

namespace QRStickers.Tests.Services;

public class QRCodeGenerationServiceTests
{
    private readonly QRCodeGenerationService _service;
    private readonly Mock<ILogger<QRCodeGenerationService>> _mockLogger;

    public QRCodeGenerationServiceTests()
    {
        var qrGenerator = new QRCodeGenerator();
        _mockLogger = new Mock<ILogger<QRCodeGenerationService>>();
        _service = new QRCodeGenerationService(qrGenerator, _mockLogger.Object);
    }

    [Fact]
    public void GenerateQRCodeDataUri_ValidContent_ReturnsValidDataUri()
    {
        // Arrange
        var content = "https://example.com/device/12345";

        // Act
        var result = _service.GenerateQRCodeDataUri(content);

        // Assert
        Assert.NotNull(result);
        Assert.StartsWith("data:image/png;base64,", result);

        // Verify base64 portion is valid
        var base64Part = result.Substring("data:image/png;base64,".Length);
        Assert.NotEmpty(base64Part);

        // Verify it's valid base64
        var bytes = Convert.FromBase64String(base64Part);
        Assert.NotEmpty(bytes);
    }

    [Fact]
    public void GenerateQRCodeDataUri_NullContent_ReturnsNull()
    {
        // Act
        var result = _service.GenerateQRCodeDataUri(null!);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GenerateQRCodeDataUri_EmptyString_ReturnsNull()
    {
        // Act
        var result = _service.GenerateQRCodeDataUri("");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GenerateQRCodeDataUri_WhitespaceOnly_ReturnsNull()
    {
        // Act
        var result = _service.GenerateQRCodeDataUri("   ");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GenerateQRCodeDataUri_CustomSize_GeneratesWithSpecifiedSize()
    {
        // Arrange
        var content = "Test123";
        var customSize = 800;

        // Act
        var result = _service.GenerateQRCodeDataUri(content, customSize);

        // Assert
        Assert.NotNull(result);
        Assert.StartsWith("data:image/png;base64,", result);

        // Verify we got a result (can't easily verify exact pixel size without decoding PNG)
        var base64Part = result.Substring("data:image/png;base64,".Length);
        var bytes = Convert.FromBase64String(base64Part);
        Assert.NotEmpty(bytes);
    }

    [Fact]
    public void GenerateQRCodeDataUri_LongUrlContent_GeneratesSuccessfully()
    {
        // Arrange
        var longUrl = "https://dashboard.meraki.com/o/123456/manage/nodes/new_wired_status/000000000000?timespan=86400&key=very-long-key-parameter-with-lots-of-characters";

        // Act
        var result = _service.GenerateQRCodeDataUri(longUrl);

        // Assert
        Assert.NotNull(result);
        Assert.StartsWith("data:image/png;base64,", result);

        var base64Part = result.Substring("data:image/png;base64,".Length);
        var bytes = Convert.FromBase64String(base64Part);
        Assert.NotEmpty(bytes);
    }

    [Fact]
    public void GenerateQRCodeDataUri_ShortSerialNumber_GeneratesSuccessfully()
    {
        // Arrange
        var serial = "Q2XX-YYYY-ZZZZ";

        // Act
        var result = _service.GenerateQRCodeDataUri(serial);

        // Assert
        Assert.NotNull(result);
        Assert.StartsWith("data:image/png;base64,", result);

        var base64Part = result.Substring("data:image/png;base64,".Length);
        var bytes = Convert.FromBase64String(base64Part);
        Assert.NotEmpty(bytes);
    }

    [Fact]
    public void ShouldRegenerateQRCode_NullToContent_ReturnsTrue()
    {
        // Act
        var result = _service.ShouldRegenerateQRCode(null, "new-content");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ShouldRegenerateQRCode_ContentToNull_ReturnsTrue()
    {
        // Act
        var result = _service.ShouldRegenerateQRCode("old-content", null);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ShouldRegenerateQRCode_DifferentContent_ReturnsTrue()
    {
        // Act
        var result = _service.ShouldRegenerateQRCode("old-content", "new-content");

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void ShouldRegenerateQRCode_SameContent_ReturnsFalse()
    {
        // Arrange
        var content = "same-content";

        // Act
        var result = _service.ShouldRegenerateQRCode(content, content);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void ShouldRegenerateQRCode_BothNull_ReturnsFalse()
    {
        // Act
        var result = _service.ShouldRegenerateQRCode(null, null);

        // Assert
        Assert.False(result);
    }
}
