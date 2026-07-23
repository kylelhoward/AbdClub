using AbdClub.Data;
using AbdClub.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AbdClub.Pages;

public class MembershipModel : PageModel
{
    private readonly IStripeService _stripe;
    private readonly ILogger<MembershipModel> _logger;
    private readonly AbdContext _db;

    public MembershipModel(
        IStripeService stripe,
        ILogger<MembershipModel> logger,
        AbdContext db)
    {
        _stripe = stripe;
        _logger = logger;
        _db = db;
    }

    public bool PaymentCancelled { get; set; }
    public bool PaymentSuccess { get; set; }
    public bool IsRenewal { get; set; }

    [BindProperty]
    public string FullName { get; set; } = string.Empty;

    [BindProperty]
    public string Email { get; set; } = string.Empty;

    [BindProperty]
    public string? Phone { get; set; }

    public async Task OnGetAsync()
    {
        PaymentCancelled = Request.Query.ContainsKey("cancelled");
        PaymentSuccess = Request.Query.ContainsKey("success");

        var memberIdClaim = User.FindFirst("MemberId");
        if (!int.TryParse(memberIdClaim?.Value, out var memberId))
            return;

        var member = await _db.Members
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == memberId);

        if (member == null)
            return;

        IsRenewal = true;
        FullName = member.FullName;
        Email = member.Email;
        Phone = member.Phone;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var memberIdClaim = User.FindFirst("MemberId");
        if (int.TryParse(memberIdClaim?.Value, out var memberId))
        {
            var member = await _db.Members
                .AsNoTracking()
                .FirstOrDefaultAsync(m => m.Id == memberId);

            if (member != null)
            {
                // A renewal must remain associated with the signed-in account.
                // Name and phone may be updated, but email changes need a separate
                // account-management flow so payment cannot create a duplicate member.
                IsRenewal = true;
                Email = member.Email;
            }
        }

        if (string.IsNullOrWhiteSpace(FullName) ||
            string.IsNullOrWhiteSpace(Email))
        {
            ModelState.AddModelError("", "Name and email are required.");
            return Page();
        }

        var successUrl = Url.Page(
            "/Membership",
            pageHandler: null,
            values: new { success = true },
            protocol: Request.Scheme)!;

        var cancelUrl = Url.Page(
            "/Membership",
            pageHandler: null,
            values: new { cancelled = true },
            protocol: Request.Scheme)!;

        try
        {
            var checkoutUrl = await _stripe.CreateCheckoutSessionAsync(
                FullName, Email, Phone, successUrl, cancelUrl);

            return Redirect(checkoutUrl);
        }
        catch (Exception ex)
        {
            _logger.LogError("Stripe checkout error: {Message}", ex.Message);
            ModelState.AddModelError("",
                "Unable to start payment. Please try again.");
            return Page();
        }
    }
}
