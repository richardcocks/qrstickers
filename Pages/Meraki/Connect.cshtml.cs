using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Text.Json;
using System.Text.Encodings.Web;

namespace QRStickers.Pages.Meraki;

[Authorize]
public class ConnectModel : PageModel
{
    private readonly IConfiguration _configuration;

    public ConnectModel(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public IActionResult OnGet(string? displayName)
    {
        var clientId = _configuration.GetValue<string>("meraki_client_id") ?? "";
        var redirectUrl = UrlEncoder.Default.Encode("https://qrstickers-htbteydbgjh0b9c4.uksouth-01.azurewebsites.net/Meraki/Callback");
        var scopes = UrlEncoder.Default.Encode("sdwan:config:read dashboard:general:config:read");

        // Encode displayName in state parameter so we can retrieve it in callback
        var state = JsonSerializer.Serialize(new { displayName = displayName ?? "My Meraki Connection" });
        var encodedState = UrlEncoder.Default.Encode(state);

        var authorizeUrl = $"https://as.meraki.com/oauth/authorize?response_type=code&client_id={clientId}&redirect_uri={redirectUrl}&scope={scopes}&state={encodedState}";

        return Redirect(authorizeUrl);
    }
}
