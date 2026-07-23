using AbdClub.Data;
using AbdClub.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AbdClub.Pages.Officers;

[Authorize(Roles = "Officer")]
public class AddMemberModel : PageModel
{
    private readonly AbdContext _db;

    public AddMemberModel(AbdContext db) => _db = db;

    [BindProperty]
    public Member Member { get; set; } = new()
    {
        JoinDate = DateTime.UtcNow,
        ExpiryDate = DateTime.UtcNow.AddYears(1),
        IsActive = true
    };

    public string? ErrorMessage { get; set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync(bool recordPayment = false)
    {
        // Remove navigation property validation errors
        ModelState.Remove("Member.Payments");
        ModelState.Remove("Member.EmailLogs");

        if (!ModelState.IsValid)
            return Page();

        // Check for duplicate email
        var existing = _db.Members
            .FirstOrDefault(m => m.Email == Member.Email.Trim().ToLower());

        if (existing != null)
        {
            ErrorMessage = $"A member with email {Member.Email} already exists.";
            return Page();
        }

        // Normalize email
        Member.Email = Member.Email.Trim().ToLower();
        Member.CreatedAt = DateTime.UtcNow;

        // Ensure dates are UTC for PostgreSQL
        Member.JoinDate = DateTime.SpecifyKind(Member.JoinDate, DateTimeKind.Utc);

        if (Member.ExpiryDate.HasValue)
            Member.ExpiryDate = DateTime.SpecifyKind(
                Member.ExpiryDate.Value, DateTimeKind.Utc);

        // Clear officer role if not an officer
        if (!Member.IsOfficer)
            Member.OfficerRole = null;

        _db.Members.Add(Member);
        await _db.SaveChangesAsync();

        // Optionally record a manual payment (cash/check)
        if (recordPayment)
        {
            _db.Payments.Add(new Payment
            {
                MemberId = Member.Id,
                Amount = 50.00m,
                PaymentDate = DateTime.UtcNow,
                PeriodStart = Member.JoinDate,
                PeriodEnd = Member.ExpiryDate ?? Member.JoinDate.AddYears(1),
                TransactionId = "manual_" + Guid.NewGuid().ToString()[..8],
                Status = "Completed"
            });

            await _db.SaveChangesAsync();
        }

        return RedirectToPage("./Members");
    }
}
