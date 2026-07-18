using AbdClub.Data;
using AbdClub.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AbdClub.Pages.Members;

public class DashboardModel : PageModel
{
    private readonly AbdContext _db;
    public DashboardModel(AbdContext db) => _db = db;

    public Member Member { get; set; } = null!;

    public bool IsExpiringSoon => Member.ExpiryDate.HasValue &&
        Member.ExpiryDate.Value <= DateTime.UtcNow.AddDays(30) &&
        Member.ExpiryDate.Value > DateTime.UtcNow;

    public bool IsExpired => Member.ExpiryDate.HasValue &&
        Member.ExpiryDate.Value < DateTime.UtcNow;

    public async Task<IActionResult> OnGetAsync()
    {
        var memberIdClaim = User.FindFirst("MemberId");

        if (memberIdClaim == null)
        {
            // Claim missing — session issue, send back to login
            return RedirectToPage("/Auth/Login");
        }

        var memberId = int.Parse(memberIdClaim.Value);
        var member = await _db.Members.FirstOrDefaultAsync(m => m.Id == memberId);

        if (member == null)
        {
            // Member deleted or mismatch — send back to login
            return RedirectToPage("/Auth/Login");
        }

        Member = member;
        return Page();
    }
}