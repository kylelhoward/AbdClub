using System.ComponentModel.DataAnnotations;
using AbdClub.Data;
using AbdClub.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AbdClub.Pages.Admin.OfficerAccounts;

public class EditModel(AbdContext db) : PageModel
{
    [BindProperty]
    public InputModel Input { get; set; } = new();
    public List<MemberOption> Members { get; private set; } = new();

    public sealed record MemberOption(int Id, string DisplayName, string Email);

    public class InputModel
    {
        public int? Id { get; set; }
        [Required, EmailAddress, StringLength(254)] public string Email { get; set; } = string.Empty;
        public int? MemberId { get; set; }
        [Required] public OfficerAccessLevel AccessLevel { get; set; } = OfficerAccessLevel.Officer;
        [StringLength(100)] public string? OfficerTitle { get; set; }
        public bool IsEnabled { get; set; } = true;
    }

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id.HasValue)
        {
            var account = await db.OfficerAccounts.FindAsync(id.Value);
            if (account == null) return NotFound();
            Input = new InputModel { Id = account.Id, Email = account.Email, MemberId = account.MemberId, AccessLevel = account.AccessLevel, OfficerTitle = account.OfficerTitle, IsEnabled = account.IsEnabled };
        }
        await LoadMembersAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var email = Input.Email.Trim().ToLowerInvariant();
        if (await db.OfficerAccounts.AnyAsync(a => a.Email == email && a.Id != Input.Id))
            ModelState.AddModelError("Input.Email", "That email already has an officer account.");
        if (Input.MemberId.HasValue && await db.OfficerAccounts.AnyAsync(a => a.MemberId == Input.MemberId && a.Id != Input.Id))
            ModelState.AddModelError("Input.MemberId", "That member is already linked to another officer account.");

        if (!ModelState.IsValid)
        {
            await LoadMembersAsync();
            return Page();
        }

        var account = Input.Id.HasValue
            ? await db.OfficerAccounts.FindAsync(Input.Id.Value)
            : new OfficerAccount();
        if (account == null) return NotFound();

        account.Email = email;
        account.MemberId = Input.MemberId;
        account.AccessLevel = Input.AccessLevel;
        account.OfficerTitle = string.IsNullOrWhiteSpace(Input.OfficerTitle) ? null : Input.OfficerTitle.Trim();
        account.IsEnabled = Input.IsEnabled;
        if (!Input.Id.HasValue) db.OfficerAccounts.Add(account);
        await db.SaveChangesAsync();
        return RedirectToPage("Index");
    }

    private async Task LoadMembersAsync()
    {
        var members = await db.Members.AsNoTracking().OrderBy(m => m.LastName).ThenBy(m => m.FirstName).ToListAsync();
        Members = members
            .Select(m => new MemberOption(
                m.Id,
                $"{m.DisplayMemberNumber} — {m.FullName}",
                m.Email))
            .ToList();
    }
}
