using System.ComponentModel.DataAnnotations;
using AbdClub.Data;
using AbdClub.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.RateLimiting;

namespace AbdClub.Pages;

[EnableRateLimiting("membership-status")]
public class MembershipStatusModel(AbdContext db, IEmailService emailService) : PageModel
{
    [BindProperty, Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    public bool RequestAccepted { get; set; }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        var normalizedEmail = Email.Trim().ToLowerInvariant();
        var members = await db.Members
            .AsNoTracking()
            .Where(m => m.Email.ToLower() == normalizedEmail)
            .OrderBy(m => m.LastName)
            .ThenBy(m => m.FirstName)
            .ToListAsync();

        // The browser response is intentionally identical whether records exist.
        // This prevents strangers from using the form to discover member emails.
        if (members.Count > 0)
            await emailService.SendMembershipStatusAsync(normalizedEmail, members);

        RequestAccepted = true;
        Email = string.Empty;
        ModelState.Clear();
        return Page();
    }
}
