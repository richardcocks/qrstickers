using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace QRStickers;

/// <summary>
/// Helper class for Meraki OAuth and API operations
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
    /// Get organizations for the authenticated user
    /// </summary>
    public async Task<List<Organization>?> GetOrganizationsAsync(string accessToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"{ApiBaseUrl}/organizations");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        try
        {
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var organizations = await response.Content.ReadFromJsonAsync<List<Organization>>();
            return organizations;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while retrieving Meraki organizations");
            return null;
        }
    }

    /// <summary>
    /// Get networks for a specific organization
    /// </summary>
    public async Task<List<Network>?> GetNetworksAsync(string accessToken, string organizationId)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"{ApiBaseUrl}/organizations/{organizationId}/networks");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        try
        {
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var networks = await response.Content.ReadFromJsonAsync<List<Network>>();
            return networks;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while retrieving Meraki networks for organization {OrganizationId}", organizationId);
            return null;
        }
    }

    /// <summary>
    /// Get all devices for a specific organization
    /// </summary>
    public async Task<List<Device>?> GetOrganizationDevicesAsync(string accessToken, string organizationId)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"{ApiBaseUrl}/organizations/{organizationId}/devices");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        try
        {
            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var devices = await response.Content.ReadFromJsonAsync<List<Device>>();
            return devices;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception while retrieving Meraki devices for organization {OrganizationId}", organizationId);
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