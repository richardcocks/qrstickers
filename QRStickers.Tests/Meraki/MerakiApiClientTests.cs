using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using QRStickers.Meraki;
using System.Net;

namespace QRStickers.Tests.Meraki;

public class MerakiApiClientTests
{
    private readonly IConfiguration _config;
    private readonly Mock<ILogger<MerakiApiClient>> _mockLogger;

    public MerakiApiClientTests()
    {
        _mockLogger = new Mock<ILogger<MerakiApiClient>>();

        // Build real configuration with in-memory values
        var configValues = new Dictionary<string, string>
        {
            { "Meraki:ApiVersion", "1.0-test" },
            { "Meraki:VendorName", "QRStickers" },
            { "meraki_client_id", "test_client_id" },
            { "meraki_client_secret", "test_client_secret" }
        };

        _config = new ConfigurationBuilder()
            .AddInMemoryCollection(configValues!)
            .Build();
    }

    [Fact]
    public void Constructor_SetsUserAgentHeader()
    {
        // Arrange
        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(mockHttpMessageHandler.Object);

        // Act
        var client = new MerakiApiClient(httpClient, _config, _mockLogger.Object);

        // Assert
        Assert.NotEmpty(httpClient.DefaultRequestHeaders.UserAgent);
        var userAgentString = httpClient.DefaultRequestHeaders.UserAgent.ToString();
        Assert.Contains("QRStickers/1.0-test", userAgentString);
        Assert.Contains("QRStickers", userAgentString);
    }

    [Fact]
    public async Task GetOrganizationsAsync_WithSinglePage_ReturnsAllOrganizations()
    {
        // Arrange
        var mockResponse = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("[{\"id\":\"org1\",\"name\":\"Test Org\",\"url\":\"https://meraki.com\"}]")
        };
        // No Link header means single page

        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(mockResponse);

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        var client = new MerakiApiClient(httpClient, _config, _mockLogger.Object);

        // Act
        var result = await client.GetOrganizationsAsync("test_token");

        // Assert
        Assert.NotNull(result);
        Assert.Single(result);
        Assert.Equal("org1", result[0].Id);
        Assert.Equal("Test Org", result[0].Name);
    }

    [Fact]
    public async Task GetOrganizationsAsync_WithMultiplePages_AutoPaginates()
    {
        // Arrange
        var page1Response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("[{\"id\":\"org1\",\"name\":\"Org 1\",\"url\":\"https://meraki.com\"}]")
        };
        page1Response.Headers.Add("Link", "<https://api.meraki.com/api/v1/organizations?perPage=1&startingAfter=org1>; rel=\"next\"");

        var page2Response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("[{\"id\":\"org2\",\"name\":\"Org 2\",\"url\":\"https://meraki.com\"}]")
        };
        // No Link header on last page

        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        var callCount = 0;

        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(() =>
            {
                callCount++;
                return callCount == 1 ? page1Response : page2Response;
            });

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        var client = new MerakiApiClient(httpClient, _config, _mockLogger.Object);

        // Act
        var result = await client.GetOrganizationsAsync("test_token");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("org1", result[0].Id);
        Assert.Equal("org2", result[1].Id);
        Assert.Equal(2, callCount); // Verify it made 2 API calls
    }

    [Fact]
    public async Task GetNetworksAsync_WithMultiplePages_AutoPaginates()
    {
        // Arrange
        var page1Response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("[{\"id\":\"net1\",\"name\":\"Network 1\",\"organizationId\":\"org1\",\"url\":\"https://meraki.com\"}]")
        };
        page1Response.Headers.Add("Link", "<https://api.meraki.com/api/v1/organizations/org1/networks?perPage=1&startingAfter=net1>; rel=\"next\"");

        var page2Response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("[{\"id\":\"net2\",\"name\":\"Network 2\",\"organizationId\":\"org1\",\"url\":\"https://meraki.com\"}]")
        };

        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        var callCount = 0;

        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(() =>
            {
                callCount++;
                return callCount == 1 ? page1Response : page2Response;
            });

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        var client = new MerakiApiClient(httpClient, _config, _mockLogger.Object);

        // Act
        var result = await client.GetNetworksAsync("test_token", "org1");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("net1", result[0].Id);
        Assert.Equal("net2", result[1].Id);
    }

    [Fact]
    public async Task GetOrganizationDevicesAsync_WithMultiplePages_AutoPaginates()
    {
        // Arrange
        var page1Response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("[{\"serial\":\"dev1\",\"name\":\"Device 1\",\"model\":\"MS220\",\"networkId\":\"net1\"}]")
        };
        page1Response.Headers.Add("Link", "<https://api.meraki.com/api/v1/organizations/org1/devices?perPage=1&startingAfter=dev1>; rel=\"next\"");

        var page2Response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("[{\"serial\":\"dev2\",\"name\":\"Device 2\",\"model\":\"MR42\",\"networkId\":\"net1\"}]")
        };

        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        var callCount = 0;

        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(() =>
            {
                callCount++;
                return callCount == 1 ? page1Response : page2Response;
            });

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        var client = new MerakiApiClient(httpClient, _config, _mockLogger.Object);

        // Act
        var result = await client.GetOrganizationDevicesAsync("test_token", "org1");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal("dev1", result[0].Serial);
        Assert.Equal("dev2", result[1].Serial);
    }

    [Fact]
    public async Task GetOrganizationsAsync_WhenApiReturnsError_ReturnsNull()
    {
        // Arrange
        var mockResponse = new HttpResponseMessage(HttpStatusCode.Unauthorized);

        var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
        mockHttpMessageHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(mockResponse);

        var httpClient = new HttpClient(mockHttpMessageHandler.Object);
        var client = new MerakiApiClient(httpClient, _config, _mockLogger.Object);

        // Act
        var result = await client.GetOrganizationsAsync("invalid_token");

        // Assert
        Assert.Null(result);
    }
}
