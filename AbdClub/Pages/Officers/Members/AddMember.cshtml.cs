using System.ComponentModel.DataAnnotations;
using AbdClub.Data;
using AbdClub.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AbdClub.Pages.Officers.Members;

[Authorize(Policy = "isAdmin")]
public class AddMemberModel(
    AbdContext db,
    IAuthorizationService authorizationService,
    IConfiguration configuration) : PageModel
{
    private readonly AbdContext _db = db;
    private readonly IAuthorizationService _authorizationService = authorizationService;

    [BindProperty] public MembershipPlan Plan { get; set; } = MembershipPlan.Individual;
    [BindProperty] public Member Member { get; set; } = NewMember();
    [BindProperty] public SecondMemberInput SecondMember { get; set; } = new();
    [BindProperty] public bool SharedEmail { get; set; } = true;
    [BindProperty] public bool RecordPayment { get; set; }
    [BindProperty] public string PaymentMethod { get; set; } = "Cash";

    [BindProperty, StringLength(500)]
    public string? PaymentNotes { get; set; }

    public decimal IndividualPrice { get; } =
        configuration.GetValue("ClubPricing:MembershipRenewalFee", 50.00m);
    public decimal CouplePrice { get; } =
        configuration.GetValue("ClubPricing:CoupleMembershipFee", 90.00m);
    public string? ErrorMessage { get; set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        var authResult = await _authorizationService.AuthorizeAsync(User, null, "isAdmin");
        if (!authResult.Succeeded)
            return Forbid();

        RemoveMemberNavigationValidation("Member");

        if (Plan is not MembershipPlan.Individual and not MembershipPlan.Couple)
            ModelState.AddModelError(nameof(Plan), "Select a valid membership type.");

        if (Plan == MembershipPlan.Couple)
        {
            if (string.IsNullOrWhiteSpace(SecondMember.FirstName))
                ModelState.AddModelError("SecondMember.FirstName", "First name is required.");
            if (string.IsNullOrWhiteSpace(SecondMember.LastName))
                ModelState.AddModelError("SecondMember.LastName", "Last name is required.");
            if (!SharedEmail && string.IsNullOrWhiteSpace(SecondMember.Email))
                ModelState.AddModelError("SecondMember.Email", "Email is required when the members do not share an email.");
        }

        if (string.IsNullOrWhiteSpace(Member.Email))
            ModelState.AddModelError("Member.Email", "Email is required.");

        var allowedPaymentMethods = new[] { "Cash", "Check", "Square/Card", "Other" };
        if (RecordPayment && !allowedPaymentMethods.Contains(PaymentMethod))
            ModelState.AddModelError(nameof(PaymentMethod), "Select a valid payment method.");

        if (!ModelState.IsValid)
            return Page();

        PrepareMember(Member);
        var members = new List<Member> { Member };

        if (Plan == MembershipPlan.Couple)
        {
            var second = new Member
            {
                FirstName = SecondMember.FirstName.Trim(),
                MiddleName = Clean(SecondMember.MiddleName),
                LastName = SecondMember.LastName.Trim(),
                Email = (SharedEmail ? Member.Email : SecondMember.Email!).Trim().ToLowerInvariant(),
                Phone = Clean(SecondMember.Phone),
                JoinDate = Member.JoinDate,
                ExpiryDate = Member.ExpiryDate
            };
            PrepareMember(second);
            members.Add(second);
        }

        await using var transaction = await _db.Database.BeginTransactionAsync();
        _db.Members.AddRange(members);
        await _db.SaveChangesAsync();

        if (RecordPayment)
        {
            var total = Plan == MembershipPlan.Couple ? CouplePrice : IndividualPrice;
            var allocation = decimal.Round(total / members.Count, 2);
            var transactionId = "manual_" + Guid.NewGuid().ToString("N")[..8];
            var paymentDate = DateTime.UtcNow;

            for (var index = 0; index < members.Count; index++)
            {
                var amount = index == members.Count - 1
                    ? total - (allocation * (members.Count - 1))
                    : allocation;

                _db.Payments.Add(new Payment
                {
                    MemberId = members[index].Id,
                    Amount = amount,
                    PaymentDate = paymentDate,
                    PeriodStart = members[index].JoinDate,
                    PeriodEnd = members[index].ExpiryDate ?? members[index].JoinDate.AddYears(1),
                    TransactionId = transactionId,
                    Status = "Completed",
                    PaymentMethod = PaymentMethod,
                    Notes = Clean(PaymentNotes)
                });
            }

            await _db.SaveChangesAsync();
        }

        await transaction.CommitAsync();
        return RedirectToPage("./MembersList");
    }

    private void RemoveMemberNavigationValidation(string prefix)
    {
        ModelState.Remove($"{prefix}.Payments");
        ModelState.Remove($"{prefix}.EmailLogs");
    }

    private static void PrepareMember(Member member)
    {
        member.FirstName = member.FirstName.Trim();
        member.MiddleName = Clean(member.MiddleName);
        member.LastName = member.LastName.Trim();
        member.Email = member.Email.Trim().ToLowerInvariant();
        member.Phone = Clean(member.Phone);
        member.CreatedAt = DateTime.UtcNow;
        member.JoinDate = DateTime.SpecifyKind(member.JoinDate, DateTimeKind.Utc);
        if (member.ExpiryDate.HasValue)
            member.ExpiryDate = DateTime.SpecifyKind(member.ExpiryDate.Value, DateTimeKind.Utc);

        member.IsOfficer = false;
        member.IsAdmin = false;
        member.IsTechAdmin = false;
        member.OfficerRole = null;
        member.GoogleSubId = null;
    }

    private static Member NewMember() => new()
    {
        JoinDate = DateTime.UtcNow.Date,
        ExpiryDate = DateTime.UtcNow.Date.AddYears(1)
    };

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public sealed class SecondMemberInput
    {
        public string FirstName { get; set; } = string.Empty;

        [StringLength(50)]
        public string? MiddleName { get; set; }

        public string LastName { get; set; } = string.Empty;

        [EmailAddress]
        public string? Email { get; set; }

        public string? Phone { get; set; }
    }
}
