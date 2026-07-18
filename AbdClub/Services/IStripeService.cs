namespace AbdClub.Services;

public interface IStripeService
{
    Task<string> CreateCheckoutSessionAsync(
        string fullName, string email, string? phone,
        string successUrl, string cancelUrl);
    Task<bool> HandleWebhookAsync(string json, string stripeSignature);
}
