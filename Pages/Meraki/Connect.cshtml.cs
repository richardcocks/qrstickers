using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Caching.Memory;
using System.Text.Json;
using System.Text.Encodings.Web;

namespace QRStickers.Pages.Meraki;

[Authorize]
public class ConnectModel : PageModel
{
    private readonly IConfiguration _configuration;
    private readonly IMemoryCache _cache;

    public ConnectModel(IConfiguration configuration, IMemoryCache cache)
    {
        _configuration = configuration;
        _cache = cache;
    }

    public IActionResult OnGet(string? displayName)
    {
        var clientId = _configuration.GetValue<string>("meraki_client_id") ?? "";
        var redirectUrl = UrlEncoder.Default.Encode("https://qrstickers-htbteydbgjh0b9c4.uksouth-01.azurewebsites.net/Meraki/Callback");
        var scopes = UrlEncoder.Default.Encode("sdwan:config:read dashboard:general:config:read");

        // Generate cryptographic nonce for CSRF protection
        var nonce = Guid.NewGuid().ToString("N"); // 32-char hex string

        // Store nonce in cache with 10-minute expiration
        _cache.Set($"oauth_nonce_{nonce}", true, TimeSpan.FromMinutes(10));

        // Embed nonce in state parameter for reliable CSRF validation
        var state = JsonSerializer.Serialize(new {
            displayName = displayName ?? "My Meraki Connection",
            nonce
        });
        var encodedState = UrlEncoder.Default.Encode(state);

        // Also send nonce as dedicated parameter to test if Meraki echoes it back
        var authorizeUrl = $"https://as.meraki.com/oauth/authorize?response_type=code&client_id={clientId}&redirect_uri={redirectUrl}&scope={scopes}&state={encodedState}&nonce={nonce}";

        return Redirect(authorizeUrl);
    }
}
