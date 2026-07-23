using AbdClub.Data;
using AbdClub.Models;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;

namespace AbdClub.Services;

public class StripeService : IStripeService
{
    private readonly IConfiguration _config;
    private readonly ILogger<StripeService> _logger;
    private readonly AbdContext _db;
    private readonly IEmailService _email;

    public StripeService(
        IConfiguration config,
        ILogger<StripeService> logger,
        AbdContext db,
        IEmailService email)
    {
        _config = config;
        _logger = logger;
        _db = db;
        _email = email;

        StripeConfiguration.ApiKey = config["Stripe:SecretKey"];
    }

    public async Task<string> CreateCheckoutSessionAsync(
        string fullName, string email, string? phone,
        string successUrl, string cancelUrl)
    {
        var options = new SessionCreateOptions
        {
            PaymentMethodTypes = new List<string> { "card" },
            LineItems = new List<SessionLineItemOptions>
            {
                new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "usd",
                        UnitAmount = 5000,  // $50.00 in cents
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = "Annual Membership",
                            Description = "Austin Ballroom Dancers — 1 year membership"
                        }
                    },
                    Quantity = 1
                }
            },
            Mode = "payment",
            SuccessUrl = successUrl,
            CancelUrl = cancelUrl,
            CustomerEmail = email,

            // Pass member info as metadata — echoed back in webhook
            Metadata = new Dictionary<string, string>
            {
                { "fullName", fullName },
                { "email",    email },
                { "phone",    phone ?? "" }
            }
        };

        var service = new SessionService();
        var session = await service.CreateAsync(options);

        _logger.LogInformation(
            "Stripe checkout session created: {SessionId} for {Email}",
            session.Id, email);

        return session.Url;
    }

    public async Task<bool> HandleWebhookAsync(string json, string stripeSignature)
    {
        try
        {
            var webhookSecret = _config["Stripe:WebhookSecret"]!;

            var stripeEvent = EventUtility.ConstructEvent(
                json, stripeSignature, webhookSecret);

            _logger.LogInformation(
                "Stripe webhook received: {EventType}", stripeEvent.Type);

            if (stripeEvent.Type == EventTypes.CheckoutSessionCompleted)
            {
                var session = stripeEvent.Data.Object as Session;
                if (session == null)
                {
                    _logger.LogWarning("Webhook: session object was null");
                    return false;
                }

                await FulfillMembershipAsync(session);
            }

            return true;
        }
        catch (StripeException ex)
        {
            _logger.LogError("Stripe webhook error: {Message}", ex.Message);
            return false;
        }
    }

    private async Task FulfillMembershipAsync(Session session)
    {
        // Pull member info from metadata
        session.Metadata.TryGetValue("fullName", out var fullName);
        session.Metadata.TryGetValue("email", out var email);
        session.Metadata.TryGetValue("phone", out var phone);

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(fullName))
        {
            _logger.LogWarning(
                "Webhook: missing member info in session {Id}", session.Id);
            return;
        }

        // Check for existing member (renewal case)
        Member? existing = await _db.Members
            .FirstOrDefaultAsync(
                m => m.Email.Equals(email.Trim(),
                StringComparison.OrdinalIgnoreCase
                )
            );

        if (existing != null)
        {
            // Renewal — extend from current expiry or from today if lapsed
            var baseDate = existing.ExpiryDate.HasValue &&
                           existing.ExpiryDate.Value > DateTime.UtcNow
                ? existing.ExpiryDate.Value
                : DateTime.UtcNow;

            existing.ExpiryDate = baseDate.AddYears(1);
            existing.IsActive = true;
            existing.FullName = fullName.Trim();
            existing.Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();

            _db.Payments.Add(new Payment
            {
                MemberId = existing.Id,
                Amount = 50.00m,
                PaymentDate = DateTime.UtcNow,
                PeriodStart = DateTime.UtcNow,
                PeriodEnd = existing.ExpiryDate.Value,
                TransactionId = session.PaymentIntentId,
                Status = "Completed"
            });

            await _db.SaveChangesAsync();

            _logger.LogInformation(
                "Membership renewed for {Email} until {Expiry}",
                email, existing.ExpiryDate);

            await _email.SendReminderAsync(existing, "Welcome");
        }
        else
        {
            // New member
            var member = new Member
            {
                FullName = fullName,
                Email = email.Trim().ToLower(),
                Phone = string.IsNullOrEmpty(phone) ? null : phone,
                JoinDate = DateTime.UtcNow,
                ExpiryDate = DateTime.UtcNow.AddYears(1),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                SelfRegistered = true   // ← paid online via Stripe
            };

            _db.Members.Add(member);
            await _db.SaveChangesAsync();

            _db.Payments.Add(new Payment
            {
                MemberId = member.Id,
                Amount = 50.00m,
                PaymentDate = DateTime.UtcNow,
                PeriodStart = DateTime.UtcNow,
                PeriodEnd = member.ExpiryDate!.Value,
                TransactionId = session.PaymentIntentId,
                Status = "Completed"
            });

            await _db.SaveChangesAsync();

            _logger.LogInformation(
                "New member created: {Email} expires {Expiry}",
                email, member.ExpiryDate);

            await _email.SendReminderAsync(member, "Welcome");
        }
    }
}
