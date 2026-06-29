using AbdClub.Data;
using AbdClub.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;

namespace AbdClub.Pages.Officers;


[Authorize(Roles = "Officer")]
public class EditMemberModel : PageModel
{
    private readonly AbdContext _db;
    public EditMemberModel(AbdContext db) => _db = db;

    [BindProperty]
    public Member Member { get; set; } = default!;
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        Member = await _db.Members.FindAsync(id);
        if (Member == null)
        {
            ErrorMessage = "Member not found.";
            return Page();
        }
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
            return Page();

        var memberInDb = await _db.Members.FindAsync(Member.Id);
        if (memberInDb == null)
        {
            ErrorMessage = "Member not found.";
            return Page();
        }

        memberInDb.FullName = Member.FullName;
        memberInDb.Email = Member.Email;
        memberInDb.IsActive = Member.IsActive;
        memberInDb.ExpiryDate = Member.ExpiryDate.HasValue
            ? System.DateTime.SpecifyKind(Member.ExpiryDate.Value, System.DateTimeKind.Utc)
            : (DateTime?)null;
        memberInDb.IsOfficer = Member.IsOfficer;
        memberInDb.OfficerRole = Member.OfficerRole;


        await _db.SaveChangesAsync();
        return RedirectToPage("./Members");
    }
}
