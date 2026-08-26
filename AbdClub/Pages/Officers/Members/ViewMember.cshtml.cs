using AbdClub.Data;
using AbdClub.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace AbdClub.Pages.Officers.Members;

[Authorize(Policy = "isOfficer")]
public class DetailsModel : PageModel
{
    private readonly AbdContext _db;
    public DetailsModel(AbdContext db) => _db = db;

    public Member Member { get; set; } = default!;
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Member = await _db.Members
            .Include(m => m.Payments)
            .FirstOrDefaultAsync(m => m.Id == id);

        if (Member == null)
        {
            ErrorMessage = "Member not found.";
            return Page();
        }

        return Page();
    }
}
