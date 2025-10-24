using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;
using QRStickers.Meraki.Pagination;

namespace QRStickers.Meraki;

/// <summary>
/// Low-level HTTP client for Meraki OAuth and API operations with pagination support
/// </summary>
public class MerakiApiClient
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;
    private readonly ILogger<MerakiApiClient> _logger;
    private const string TokenEndpoint = "https://as.meraki.com/oauth/token";
    private const string ApiBaseUrl = "https://api.meraki.com/api/v1";

    public MerakiApiClient(HttpClient httpClient, IConfiguration config, ILogger<MerakiApiClient> logger)
    {
        _httpClient = httpClient;
        _config = config;
        _logger = logger;

        // Set User-Agent header per Meraki API best practices
        // Format: "ApplicationName/<version> VendorName"
        var apiVersion = _config.GetValue<string>("Meraki:ApiVersion") ?? "1.0";
        var vendorName = _config.GetValue<string>("Meraki:VendorName") ?? "QRStickers";
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd($"QRStickers/{apiVersion} {vendorName}");
    }

    /// <summary>
    /// Exchange authorization code for access token
    /// </summary>
    public async Task<(string AccessToken, string RefreshToken, int ExpiresIn)?> ExchangeCodeForTokenAsync(string code, string redirectUri)
    {
        var clientId = _config.GetValue<string>("meraki_client_id") ?? "";
        var clientSecret = _config.GetValue<string>("meraki_client_secret") ?? "";

        // Create Basic Authentication header
        var credentials = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{clientId}:{clientSecret}"));

        var request = new HttpRequestMessage(HttpMethod.Post, TokenEndpoint)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "grant_type", "authorization_code" },
                { "code", code },
                { "redirect_uri", redirectUri },
                { "scope", "dashboard:general:config:read" }
            })
        };

        // Set Basic Authentication header (required by Meraki OAuth)
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);

        try
        {
            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("OAuth token exchange failed. Status: {StatusCode}", response.StatusCode);
                return null;
            }

            var rawJson = await response.Content.ReadAsStringAsync();
            var json = System.Text.Json.JsonSerializer.Deserialize<TokenResponse>(rawJson);

            if (json == null)
            {
                _logger.LogError("Failed to deserialize token response");
                return null;
            }

            return (json.access_token, json.refresh_token, json.expires_in)!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during OAuth token exchange");
            return null;
        }
    }

    /// <summary>
    /// Refresh an expired access token
    /// </summary>
    public async Task<(string AccessToken, string RefreshToken, int ExpiresIn)?> RefreshAccessTokenAsync(string refreshToken)
    {
        var clientId = _config.GetValue<string>("meraki_client_id") ?? "";
        var clientSecret = _config.GetValue<string>("meraki_client_secret") ?? "";

        // Create Basic Authentication header
        var credentials = Convert.ToBase64String(System.Text.Encoding.ASCII.GetBytes($"{clientId}:{clientSecret}"));

        var request = new HttpRequestMessage(HttpMethod.Post, TokenEndpoint)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                { "grant_type", "refresh_token" },
                { "refresh_token", refreshToken }
            })
        };

        // Set Basic Authentication header (required by Meraki OAuth)
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", credentials);

        try
        {
            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("OAuth token refresh failed. Status: {StatusCode}", response.StatusCode);
                return null;
            }

            var rawJson = await response.Content.ReadAsStringAsync();
            var json = System.Text.Json.JsonSerializer.Deserialize<TokenResponse>(rawJson);

            if (json == null)
            {
                _logger.LogError("Failed to deserialize refresh token response");
                return null;
            }

            return (json.access_token, json.refresh_token, json.expires_in)!;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during OAuth token refresh");
            return null;
        }
    }

    /// <summary>
    /// Get all organizations for the authenticated user (auto-paginates through all results)
    /// </summary>
    public async Task<List<Organization>?> GetOrganizationsAsync(string accessToken)
    {
        var allOrgs = new List<Organization>();
        string? nextPageUrl = $"{ApiBaseUrl}/organizations?perPage=1000";

        while (!string.IsNullOrWhiteSpace(nextPageUrl))
        {
            var page = await GetOrganizationsPageAsync(accessToken, nextPageUrl);
            if (page == null)
            {
                _logger.LogError("Failed to fetch organizations page");
                return null;
            }

            allOrgs.AddRange(page.Items);

            // Check if there are more pages
            if (page.PageInfo.HasNextPage)
            {
                nextPageUrl = page.PageInfo.Next;
                _logger.LogDebug("Fetching next page of organizations: {NextPageUrl}", nextPageUrl);
            }
            else
            {
                break;
            }
        }

        _logger.LogInformation("Fetched {Count} organizations across {Pages} pages", allOrgs.Count, (allOrgs.Count / 1000) + 1);
        return allOrgs;
    }

    /// <summary>
    /// Get a single page of organizations
    /// </summary>
    private async Task<PaginatedResponse<Organization>?> GetOrganizationsPageAsync(string accessToken, string url)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        try
        {
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            // Parse Link header for pagination
            var linkHeader = response.Headers.TryGetValues("Link", out var linkValues)
                ? linkValues.FirstOrDefault()
                : null;
            var pageInfo = LinkHeaderParser.Parse(linkHeader);

            // Parse response body
            var organizations = await response.Content.ReadFromJsonAsync<List<Organization>>();

            return new PaginatedResponse<Organization>(organizations ?? new List<Organization>(), pageInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while retrieving Meraki organizations");
            return null;
        }
    }

    /// <summary>
    /// Get all networks for a specific organization (auto-paginates through all results)
    /// </summary>
    public async Task<List<Network>?> GetNetworksAsync(string accessToken, string organizationId)
    {
        var allNetworks = new List<Network>();
        string? nextPageUrl = $"{ApiBaseUrl}/organizations/{organizationId}/networks?perPage=1000";

        while (!string.IsNullOrWhiteSpace(nextPageUrl))
        {
            var page = await GetNetworksPageAsync(accessToken, nextPageUrl);
            if (page == null)
            {
                _logger.LogError("Failed to fetch networks page for organization {OrganizationId}", organizationId);
                return null;
            }

            allNetworks.AddRange(page.Items);

            // Check if there are more pages
            if (page.PageInfo.HasNextPage)
            {
                nextPageUrl = page.PageInfo.Next;
                _logger.LogDebug("Fetching next page of networks: {NextPageUrl}", nextPageUrl);
            }
            else
            {
                break;
            }
        }

        _logger.LogInformation("Fetched {Count} networks for organization {OrganizationId} across {Pages} pages",
            allNetworks.Count, organizationId, (allNetworks.Count / 1000) + 1);
        return allNetworks;
    }

    /// <summary>
    /// Get a single page of networks
    /// </summary>
    private async Task<PaginatedResponse<Network>?> GetNetworksPageAsync(string accessToken, string url)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        try
        {
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            // Parse Link header for pagination
            var linkHeader = response.Headers.TryGetValues("Link", out var linkValues)
                ? linkValues.FirstOrDefault()
                : null;
            var pageInfo = LinkHeaderParser.Parse(linkHeader);

            // Parse response body
            var networks = await response.Content.ReadFromJsonAsync<List<Network>>();

            return new PaginatedResponse<Network>(networks ?? new List<Network>(), pageInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while retrieving Meraki networks");
            return null;
        }
    }

    /// <summary>
    /// Get all devices for a specific organization (auto-paginates through all results)
    /// </summary>
    public async Task<List<Device>?> GetOrganizationDevicesAsync(string accessToken, string organizationId)
    {
        var allDevices = new List<Device>();
        string? nextPageUrl = $"{ApiBaseUrl}/organizations/{organizationId}/devices?perPage=1000";

        while (!string.IsNullOrWhiteSpace(nextPageUrl))
        {
            var page = await GetOrganizationDevicesPageAsync(accessToken, nextPageUrl);
            if (page == null)
            {
                _logger.LogError("Failed to fetch devices page for organization {OrganizationId}", organizationId);
                return null;
            }

            allDevices.AddRange(page.Items);

            // Check if there are more pages
            if (page.PageInfo.HasNextPage)
            {
                nextPageUrl = page.PageInfo.Next;
                _logger.LogDebug("Fetching next page of devices: {NextPageUrl}", nextPageUrl);
            }
            else
            {
                break;
            }
        }

        _logger.LogInformation("Fetched {Count} devices for organization {OrganizationId} across {Pages} pages",
            allDevices.Count, organizationId, (allDevices.Count / 1000) + 1);
        return allDevices;
    }

    /// <summary>
    /// Get a single page of devices
    /// </summary>
    private async Task<PaginatedResponse<Device>?> GetOrganizationDevicesPageAsync(string accessToken, string url)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        try
        {
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            // Parse Link header for pagination
            var linkHeader = response.Headers.TryGetValues("Link", out var linkValues)
                ? linkValues.FirstOrDefault()
                : null;
            var pageInfo = LinkHeaderParser.Parse(linkHeader);

            // Parse response body
            var devices = await response.Content.ReadFromJsonAsync<List<Device>>();

            return new PaginatedResponse<Device>(devices ?? new List<Device>(), pageInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while retrieving Meraki devices");
            return null;
        }
    }

    private class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string access_token { get; set; } = null!;

        [JsonPropertyName("refresh_token")]
        public string? refresh_token { get; set; }

        [JsonPropertyName("expires_in")]
        public int expires_in { get; set; }
    }
}
