using AbdClub.Services.Interfaces;
using AbdClub.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace AbdClub.Pages.Auth;

public class MagicLinkModel : PageModel
{
    private readonly IMagicLinkService _magicLink;
    private readonly ILogger<MagicLinkModel> _logger;

    public MagicLinkModel(
        IMagicLinkService magicLink,
        ILogger<MagicLinkModel> logger)
    {
        _magicLink = magicLink;
        _logger = logger;
    }

    public bool IsValid { get; set; }

    public async Task<IActionResult> OnGetAsync(string token)
    {
        if (string.IsNullOrEmpty(token))
            return RedirectToPage("/Auth/Login", new { expired = true });

        var officer = await _magicLink.ValidateTokenAsync(token);

        if (officer == null)
        {
            IsValid = false;
            return Page();
        }

        IsValid = true;

        // Build the same claims as Google login
        var claimsToAdd = OfficerClaimsFactory.Create(officer);
        var identity = new ClaimsIdentity(claimsToAdd, "MagicLink");
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync("Cookies", principal,
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30)
            });

        _logger.LogInformation(
            "Magic link login successful for {Email}", officer.Email);

        return RedirectToPage("/Officers/Dashboard");
    }
}
