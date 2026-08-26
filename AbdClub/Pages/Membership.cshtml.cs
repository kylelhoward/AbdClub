using System.ComponentModel.DataAnnotations;
using AbdClub.Models;
using AbdClub.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AbdClub.Pages;

public class MembershipModel : PageModel
{
    private readonly IStripeService _stripe;
    private readonly ILogger<MembershipModel> _logger;

    public MembershipModel(IStripeService stripe, ILogger<MembershipModel> logger, IConfiguration config)
    {
        _stripe = stripe;
        _logger = logger;
        IndividualPrice = config.GetValue("ClubPricing:MembershipRenewalFee", 50.00m);
        CouplePrice = config.GetValue("ClubPricing:CoupleMembershipFee", 90.00m);
    }

    public bool PaymentCancelled { get; private set; }
    public bool PaymentSuccess { get; private set; }
    public decimal IndividualPrice { get; }
    public decimal CouplePrice { get; }

    [BindProperty]
    public MembershipInput Input { get; set; } = new();

    public void OnGet()
    {
        PaymentCancelled = Request.Query.ContainsKey("cancelled");
        PaymentSuccess = Request.Query.ContainsKey("success");
    }

    public async Task<IActionResult> OnPostAsync()
    {
        ValidateConditionalFields();
        if (!ModelState.IsValid)
            return Page();

        var request = new MembershipCheckoutRequest
        {
            Plan = Input.Plan,
            FirstName1 = Input.FirstName1.Trim(),
            LastName1 = Input.LastName1.Trim(),
            Email1 = Input.Email1.Trim().ToLowerInvariant(),
            Phone1 = Clean(Input.Phone1),
            FirstName2 = Clean(Input.FirstName2),
            LastName2 = Clean(Input.LastName2),
            Email2 = Input.Plan == MembershipPlan.Couple
                ? (Input.SharedEmail ? Input.Email1.Trim().ToLowerInvariant() : Clean(Input.Email2)?.ToLowerInvariant())
                : null,
            Phone2 = Clean(Input.Phone2),
            SharedEmail = Input.SharedEmail
        };

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
            return Redirect(await _stripe.CreateCheckoutSessionAsync(request, successUrl, cancelUrl));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unable to create Stripe membership checkout session.");
            ModelState.AddModelError(string.Empty, "Unable to start payment. Please try again.");
            return Page();
        }
    }

    private void ValidateConditionalFields()
    {
        if (Input.Plan != MembershipPlan.Couple)
        {
            ModelState.Remove("Input.FirstName2");
            ModelState.Remove("Input.LastName2");
            ModelState.Remove("Input.Email2");
            ModelState.Remove("Input.Phone2");
            return;
        }

        if (string.IsNullOrWhiteSpace(Input.FirstName2))
            ModelState.AddModelError("Input.FirstName2", "The second member’s first name is required.");
        if (string.IsNullOrWhiteSpace(Input.LastName2))
            ModelState.AddModelError("Input.LastName2", "The second member’s last name is required.");
        if (Input.SharedEmail)
            ModelState.Remove("Input.Email2");
        else if (string.IsNullOrWhiteSpace(Input.Email2))
            ModelState.AddModelError("Input.Email2", "Enter the second member’s email or select shared email.");
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public class MembershipInput
    {
        public MembershipPlan Plan { get; set; } = MembershipPlan.Individual;
        [Required, StringLength(50), Display(Name = "First name")] public string FirstName1 { get; set; } = string.Empty;
        [Required, StringLength(50), Display(Name = "Last name")] public string LastName1 { get; set; } = string.Empty;
        [Required, EmailAddress, StringLength(254), Display(Name = "Email address")] public string Email1 { get; set; } = string.Empty;
        [Phone, StringLength(30), Display(Name = "Phone")] public string? Phone1 { get; set; }
        [StringLength(50), Display(Name = "First name")] public string? FirstName2 { get; set; }
        [StringLength(50), Display(Name = "Last name")] public string? LastName2 { get; set; }
        [EmailAddress, StringLength(254), Display(Name = "Email address")] public string? Email2 { get; set; }
        [Phone, StringLength(30), Display(Name = "Phone")] public string? Phone2 { get; set; }
        public bool SharedEmail { get; set; } = true;
    }
}
