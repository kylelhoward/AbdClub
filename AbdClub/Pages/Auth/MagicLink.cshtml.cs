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

        var member = await _magicLink.ValidateTokenAsync(token);

        if (member == null)
        {
            IsValid = false;
            return Page();
        }

        IsValid = true;

        // Build the same claims as Google login
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name,  member.FullName),
            new(ClaimTypes.Email, member.Email),
            new("MemberId",       member.Id.ToString()),
            new("IsOfficer",      member.IsOfficer.ToString().ToLower()),
            new("ExpiryDate",     member.ExpiryDate.HasValue
                                  ? member.ExpiryDate.Value.ToString("O")
                                  : ""),
        };

        if (member.OfficerRole != null)
            claims.Add(new("OfficerRole", member.OfficerRole));

        if (member.IsOfficer)
            claims.Add(new(ClaimTypes.Role, "Officer"));

        var identity = new ClaimsIdentity(claims, "MagicLink");
        var principal = new ClaimsPrincipal(identity);

        await HttpContext.SignInAsync("Cookies", principal,
            new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = DateTimeOffset.UtcNow.AddDays(30)
            });

        _logger.LogInformation(
            "Magic link login successful for {Email}", member.Email);

        // Redirect based on role — same logic as Callback.cshtml.cs
        if (member.IsOfficer)
            return RedirectToPage("/Officers/Dashboard");

        return RedirectToPage("/Members/Dashboard");
    }
}
