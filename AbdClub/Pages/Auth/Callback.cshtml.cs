using AbdClub.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

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
            .FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;

        if (email == null)
            return RedirectToPage("/Auth/Login");

        // Check if this email exists in our Members table
        var member = await _db.Members
            .FirstOrDefaultAsync(m => m.Email == email);

        if (member == null)
        {
            // Not a registered member — send them to login with a message
            await HttpContext.SignOutAsync("Cookies");
            return RedirectToPage("/Auth/Login", new { notamember = true });
        }

        if (!member.IsActive || member.ExpiryDate < DateTime.UtcNow)
        {
            // Lapsed member — let them in but redirect to membership page
            return RedirectToPage("/Membership", new { expired = true });
        }

        // All good — send officers to officer dashboard, members to member dashboard
        if (member.IsOfficer)
            return RedirectToPage("/Officers/Dashboard");

        return RedirectToPage("/Members/Dashboard");
    }
}