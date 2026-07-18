using AbdClub.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AbdClub.Pages;

public class MembershipModel : PageModel
{
    private readonly IStripeService _stripe;
    private readonly ILogger<MembershipModel> _logger;

    public MembershipModel(IStripeService stripe, ILogger<MembershipModel> logger)
    {
        _stripe = stripe;
        _logger = logger;
    }

    public bool PaymentCancelled { get; set; }
    public bool PaymentSuccess { get; set; }

    public void OnGet()
    {
        PaymentCancelled = Request.Query.ContainsKey("cancelled");
        PaymentSuccess = Request.Query.ContainsKey("success");
    }

    public async Task<IActionResult> OnPostAsync(
        string fullName, string email, string? phone)
    {
        if (string.IsNullOrWhiteSpace(fullName) ||
            string.IsNullOrWhiteSpace(email))
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
                fullName, email, phone, successUrl, cancelUrl);

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