using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace QRStickers.Pages.Meraki;

[Authorize]
public class ConnectModel : PageModel
{
    private readonly IConfiguration _configuration;

    public ConnectModel(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public IActionResult OnGet()
    {
        var clientId = _configuration.GetValue<string>("meraki_client_id") ?? "";
        var redirectUrl = System.Text.Encodings.Web.UrlEncoder.Default.Encode("https://qrstickers-htbteydbgjh0b9c4.uksouth-01.azurewebsites.net/Meraki/Callback");
        var scopes = System.Text.Encodings.Web.UrlEncoder.Default.Encode("sdwan:config:read dashboard:general:config:read");

        var authorizeUrl = $"https://as.meraki.com/oauth/authorize?response_type=code&client_id={clientId}&redirect_uri={redirectUrl}&scope={scopes}&state=test";

        return Redirect(authorizeUrl);
    }
}
