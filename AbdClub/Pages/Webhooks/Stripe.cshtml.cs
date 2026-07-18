using AbdClub.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AbdClub.Pages.Webhooks;

[IgnoreAntiforgeryToken]
public class StripeModel : PageModel
{
    private readonly IStripeService _stripe;
    private readonly ILogger<StripeModel> _logger;

    public StripeModel(IStripeService stripe, ILogger<StripeModel> logger)
    {
        _stripe = stripe;
        _logger = logger;
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var json = await new StreamReader(Request.Body).ReadToEndAsync();
        var signature = Request.Headers["Stripe-Signature"].ToString();

        if (string.IsNullOrEmpty(signature))
        {
            _logger.LogWarning("Stripe webhook received with no signature");
            return BadRequest();
        }

        var success = await _stripe.HandleWebhookAsync(json, signature);
        return success
            ? new OkResult()
            : new BadRequestResult();
    }
}
