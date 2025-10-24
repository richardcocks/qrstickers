using QRStickers.Meraki.Pagination;

namespace QRStickers.Tests.Meraki;

public class LinkHeaderParserTests
{
    [Fact]
    public void Parse_WithNullOrEmptyInput_ReturnsEmptyPageInfo()
    {
        // Arrange & Act
        var result1 = LinkHeaderParser.Parse(null);
        var result2 = LinkHeaderParser.Parse("");
        var result3 = LinkHeaderParser.Parse("   ");

        // Assert
        Assert.NotNull(result1);
        Assert.Null(result1.First);
        Assert.Null(result1.Last);
        Assert.Null(result1.Prev);
        Assert.Null(result1.Next);

        Assert.NotNull(result2);
        Assert.NotNull(result3);
    }

    [Fact]
    public void Parse_WithSingleLink_ParsesCorrectly()
    {
        // Arrange
        var linkHeader = "<https://api.meraki.com/api/v1/organizations?perPage=10&startingAfter=abc123>; rel=\"next\"";

        // Act
        var result = LinkHeaderParser.Parse(linkHeader);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("https://api.meraki.com/api/v1/organizations?perPage=10&startingAfter=abc123", result.Next);
        Assert.Null(result.First);
        Assert.Null(result.Last);
        Assert.Null(result.Prev);
    }

    [Fact]
    public void Parse_WithMultipleLinks_ParsesAllRelations()
    {
        // Arrange
        var linkHeader = "<https://api.meraki.com/api/v1/organizations?perPage=10>; rel=\"first\", " +
                        "<https://api.meraki.com/api/v1/organizations?perPage=10&startingAfter=xyz>; rel=\"prev\", " +
                        "<https://api.meraki.com/api/v1/organizations?perPage=10&startingAfter=abc>; rel=\"next\", " +
                        "<https://api.meraki.com/api/v1/organizations?perPage=10&endingBefore=zzz>; rel=\"last\"";

        // Act
        var result = LinkHeaderParser.Parse(linkHeader);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("https://api.meraki.com/api/v1/organizations?perPage=10", result.First);
        Assert.Equal("https://api.meraki.com/api/v1/organizations?perPage=10&startingAfter=xyz", result.Prev);
        Assert.Equal("https://api.meraki.com/api/v1/organizations?perPage=10&startingAfter=abc", result.Next);
        Assert.Equal("https://api.meraki.com/api/v1/organizations?perPage=10&endingBefore=zzz", result.Last);
    }

    [Fact]
    public void Parse_WithQuotedRelValues_ParsesCorrectly()
    {
        // Arrange
        var linkHeader = "<https://api.example.com/page1>; rel=\"first\", " +
                        "<https://api.example.com/page2>; rel=next"; // No quotes

        // Act
        var result = LinkHeaderParser.Parse(linkHeader);

        // Assert
        Assert.Equal("https://api.example.com/page1", result.First);
        Assert.Equal("https://api.example.com/page2", result.Next);
    }

    [Fact]
    public void Parse_WithCaseInsensitiveRel_ParsesCorrectly()
    {
        // Arrange
        var linkHeader = "<https://api.example.com/page1>; REL=\"NEXT\"";

        // Act
        var result = LinkHeaderParser.Parse(linkHeader);

        // Assert
        Assert.Equal("https://api.example.com/page1", result.Next);
    }

    [Fact]
    public void HasNextPage_WithNextUrl_ReturnsTrue()
    {
        // Arrange
        var pageInfo = new PageInfo { Next = "https://api.example.com/next" };

        // Act & Assert
        Assert.True(pageInfo.HasNextPage);
    }

    [Fact]
    public void HasNextPage_WithoutNextUrl_ReturnsFalse()
    {
        // Arrange
        var pageInfo = new PageInfo { Next = null };

        // Act & Assert
        Assert.False(pageInfo.HasNextPage);
    }

    [Fact]
    public void HasPrevPage_WithPrevUrl_ReturnsTrue()
    {
        // Arrange
        var pageInfo = new PageInfo { Prev = "https://api.example.com/prev" };

        // Act & Assert
        Assert.True(pageInfo.HasPrevPage);
    }

    [Fact]
    public void HasPrevPage_WithoutPrevUrl_ReturnsFalse()
    {
        // Arrange
        var pageInfo = new PageInfo { Prev = null };

        // Act & Assert
        Assert.False(pageInfo.HasPrevPage);
    }

    [Fact]
    public void ExtractStartingAfter_WithValidUrl_ExtractsToken()
    {
        // Arrange
        var url = "https://api.meraki.com/api/v1/organizations?perPage=10&startingAfter=abc123";

        // Act
        var result = LinkHeaderParser.ExtractStartingAfter(url);

        // Assert
        Assert.Equal("abc123", result);
    }

    [Fact]
    public void ExtractStartingAfter_WithoutParameter_ReturnsNull()
    {
        // Arrange
        var url = "https://api.meraki.com/api/v1/organizations?perPage=10";

        // Act
        var result = LinkHeaderParser.ExtractStartingAfter(url);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ExtractStartingAfter_WithNullUrl_ReturnsNull()
    {
        // Act
        var result = LinkHeaderParser.ExtractStartingAfter(null);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ExtractEndingBefore_WithValidUrl_ExtractsToken()
    {
        // Arrange
        var url = "https://api.meraki.com/api/v1/organizations?perPage=10&endingBefore=xyz789";

        // Act
        var result = LinkHeaderParser.ExtractEndingBefore(url);

        // Assert
        Assert.Equal("xyz789", result);
    }

    [Fact]
    public void ExtractEndingBefore_WithoutParameter_ReturnsNull()
    {
        // Arrange
        var url = "https://api.meraki.com/api/v1/organizations?perPage=10";

        // Act
        var result = LinkHeaderParser.ExtractEndingBefore(url);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void Parse_WithMalformedLink_IgnoresInvalidEntries()
    {
        // Arrange
        var linkHeader = "invalid-link, <https://api.example.com/next>; rel=\"next\"";

        // Act
        var result = LinkHeaderParser.Parse(linkHeader);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("https://api.example.com/next", result.Next);
        Assert.Null(result.First);
    }
}
