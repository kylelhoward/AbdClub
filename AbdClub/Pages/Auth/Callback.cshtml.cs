using AbdClub.Data;
using AbdClub.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace AbdClub.Pages.Auth;

public class CallbackModel : PageModel
{
    private readonly AbdContext _db;

    public CallbackModel(AbdContext db) => _db = db;

    public async Task<IActionResult> OnGetAsync()
    {
        // Read the authentication result from the cookie
        var result = await HttpContext.AuthenticateAsync("Cookies");

        if (!result.Succeeded)
        {
            // Google auth failed for some reason
            return RedirectToPage("/Auth/Login");
        }

        // Get the email from the claims Google returned
        var email = result.Principal?
            .FindFirst(ClaimTypes.Email)?.Value;

        if (email == null)
            return RedirectToPage("/Auth/Login");

        var officer = await _db.OfficerAccounts
            .Include(a => a.Member)
            .FirstOrDefaultAsync(a => a.Email == email.ToLower() && a.IsEnabled);

        if (officer == null)
        {
            // Not a registered member — send them to login with a message
            await HttpContext.SignOutAsync("Cookies");
            return RedirectToPage("/Auth/Login", new { notamember = true });
        }

        var claims = OfficerClaimsFactory.Create(officer);

        var identity = new ClaimsIdentity(claims, "Cookies");
        await HttpContext.SignInAsync("Cookies", new ClaimsPrincipal(identity));

        return RedirectToPage("/Officers/Dashboard");
    }
}
