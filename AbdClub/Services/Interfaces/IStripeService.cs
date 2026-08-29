using AbdClub.Models;

namespace AbdClub.Services.Interfaces;

public interface IStripeService
{
    Task<string> CreateCheckoutSessionAsync(
        MembershipCheckoutRequest request,
        string successUrl,
        string cancelUrl);
    Task<bool> HandleWebhookAsync(string json, string stripeSignature);
    Task<string> CreateEventTicketCheckoutSessionAsync(
        EventTicketCheckoutRequest request,
        string successUrl,
        string cancelUrl);
    Task RefundEventTicketAsync(int ticketId);
}
