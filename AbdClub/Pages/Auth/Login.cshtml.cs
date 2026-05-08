using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AbdClub.Pages.Auth;

public class LoginModel : PageModel
{
    public bool NotAMember { get; set; }

    public void OnGet()
    {
        NotAMember = Request.Query.ContainsKey("notamember");
    }

    // This handler triggers the Google OAuth redirect
    public IActionResult OnGetGoogleLogin()
    {
        var redirectUrl = Url.Page("/Auth/Callback");
        var props = new Microsoft.AspNetCore.Authentication.AuthenticationProperties
        {
            RedirectUri = redirectUrl
        };
        return Challenge(props, "Google");
    }
}