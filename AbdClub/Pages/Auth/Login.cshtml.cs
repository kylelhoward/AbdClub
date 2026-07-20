using AbdClub.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AbdClub.Pages.Auth;

public class LoginModel : PageModel
{
    private readonly IMagicLinkService _magicLink;

    public LoginModel(IMagicLinkService magicLink)
    {
        _magicLink = magicLink;
    }

    public bool NotAMember { get; set; }
    public bool LinkSent { get; set; }
    public bool LinkExpired { get; set; }
    public string EmailSent { get; set; } = string.Empty;

    public void OnGet()
    {
        NotAMember = Request.Query.ContainsKey("notamember");
        LinkExpired = Request.Query.ContainsKey("expired");
    }

    // Google OAuth handler
    public IActionResult OnGetGoogleLogin()
    {
        var redirectUrl = Url.Page("/Auth/Callback");
        var props = new Microsoft.AspNetCore.Authentication.AuthenticationProperties
        {
            RedirectUri = redirectUrl,
            Parameters = { { "prompt", "select_account" } }
        };
        return Challenge(props, "Google");
    }

    // Magic link handler
    public async Task<IActionResult> OnPostAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            ModelState.AddModelError("", "Please enter your email address.");
            return Page();
        }

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        await _magicLink.SendMagicLinkAsync(email.Trim().ToLower(), baseUrl);

        // Always show success — don't reveal if email exists
        LinkSent = true;
        EmailSent = email;
        return Page();
    }
}