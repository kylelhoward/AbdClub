using AbdClub.Data;
using AbdClub.Models;
using AbdClub.Services.Interfaces;
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
        _logger.LogInformation(
             "Initiating Stripe Checkout Session request parameters. ClientEmail: {CustomerEmail}",
             email);

        try
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
        catch (StripeException ex)
        {
            _logger.LogCritical(ex,
                "Stripe API Handshake Failure: Failed to generate a payment gateway Session checkout lane for email {CustomerEmail}.",
                email);
            throw; // Re-throw so page-models can show clear error prompts to browsers
        }
    }

    public async Task<bool> HandleWebhookAsync(string json, string stripeSignature)
    {
        _logger.LogInformation("Incoming Stripe webhook payload package detected at route boundary.");

        try
        {
            var webhookSecret = _config["Stripe:WebhookSecret"]!;
            if (string.IsNullOrEmpty(webhookSecret))
            {
                _logger.LogCritical("System Configuration Error: 'Stripe:WebhookSecret' tracking string variable is unassigned or missing in appsettings.");
                return false;
            }
            var stripeEvent = EventUtility.ConstructEvent(
                json, stripeSignature, webhookSecret);

            _logger.LogInformation(
                "Stripe signature verified cleanly. Processing event classification token: {StripeEventType}",
                stripeEvent.Type);

            if (stripeEvent.Type == EventTypes.CheckoutSessionCompleted)
            {
                var session = stripeEvent.Data.Object as Session;
                if (session == null)
                {
                    _logger.LogError("Webhook Parse Failure: Extraction cast of Session object yielded null data blocks.");
                    return false;
                }

                _logger.LogInformation("Checkout completed event confirmed. Dispatched to fulfillment engine. SessionId: {SessionId}", session.Id);
                await FulfillMembershipAsync(session);
            }
            else
            {
                _logger.LogDebug("Skipping unhandled webhook event type signature allocation: {StripeEventType}", stripeEvent.Type);
            }

            return true;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe signature validation exception encountered during payload extraction handshake.");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled generic application fault occurred during webhook packet resolution process.");
            return false;
        }
    }

    private async Task FulfillMembershipAsync(Session session)
    {
        _logger.LogInformation("Fulfillment execution thread open for Stripe Session: {SessionId}", session.Id);

        // Pull member info from metadata
        session.Metadata.TryGetValue("fullName", out var fullName);
        session.Metadata.TryGetValue("email", out var email);
        session.Metadata.TryGetValue("phone", out var phone);

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(fullName))
        {
            _logger.LogCritical(
                "Fulfillment Aborted: Critical metadata descriptors are empty or missing inside Session {SessionId}. Cannot resolve target account mapping.",
                session.Id);
            return;
        }

        var cleanEmail = email.Trim().ToLower();

        try
        {
            // Check for existing member (renewal case)
            Member? existing = await _db.Members
                .FirstOrDefaultAsync(m => m.Email != null && m.Email.ToLower() == cleanEmail);

            if (existing != null)
            {
                _logger.LogInformation(
                    "Matched database record index. Executing Membership RENEWAL tracking loop for MemberId: {MemberId}, TargetEmail: {CustomerEmail}",
                    existing.Id, cleanEmail);

                // Renewal — extend from current expiry or from today if completely lapsed
                var baseDate = existing.ExpiryDate.HasValue && existing.ExpiryDate.Value > DateTime.UtcNow
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
                    TransactionId = session.PaymentIntentId ?? session.Id, // Fallback if intent string resolves unhydrated
                    Status = "Completed"
                });

                await _db.SaveChangesAsync();

                _logger.LogInformation(
                    "Database transaction success: Membership subscription renewed for MemberId: {MemberId} ({CustomerEmail}). New Expiration Window: {NewExpiryDate}",
                    existing.Id, cleanEmail, existing.ExpiryDate);

                // 🌟 EMAIL FAILURE PROTECTION BLOCK:
                // Isolate email dispatches within safe local try-catches so SMTP exceptions never block/rollback valid DB mutations
                try
                {
                    _logger.LogInformation("Dispatching renewal notifications via SMTP configuration for MemberId: {MemberId}", existing.Id);
                    await _email.SendReminderAsync(existing, "Welcome");
                }
                catch (Exception mailEx)
                {
                    _logger.LogError(mailEx, "Mailing Pipeline Exception: Database saved cleanly, but automated renewal notice failed to dispatch for email {CustomerEmail}.", cleanEmail);
                }
            }
            else
            {
                _logger.LogInformation(
                    "No matching identity profile found in data ledger records. Executing NEW MEMBER profile registration for account: {CustomerEmail}",
                    cleanEmail);

                // New member configuration allocation
                var member = new Member
                {
                    FullName = fullName.Trim(),
                    Email = cleanEmail,
                    Phone = string.IsNullOrEmpty(phone) ? null : phone.Trim(),
                    JoinDate = DateTime.UtcNow,
                    ExpiryDate = DateTime.UtcNow.AddYears(1),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    SelfRegistered = true   // paid online via Stripe 
                };

                _db.Members.Add(member);
                await _db.SaveChangesAsync(); // Commit first to capture the generated database Primary Key Sequence Id

                _db.Payments.Add(new Payment
                {
                    MemberId = member.Id,
                    Amount = 50.00m,
                    PaymentDate = DateTime.UtcNow,
                    PeriodStart = DateTime.UtcNow,
                    PeriodEnd = member.ExpiryDate.Value,
                    TransactionId = session.PaymentIntentId ?? session.Id,
                    Status = "Completed"
                });

                await _db.SaveChangesAsync();

                _logger.LogInformation(
                    "Database transaction success: Fresh club profile and primary transaction invoice logged for new MemberId: {MemberId} ({CustomerEmail}). Expiry: {NewExpiryDate}",
                    member.Id, cleanEmail, member.ExpiryDate);

                try
                {
                    _logger.LogInformation("Dispatching welcome notifications via SMTP configuration for new MemberId: {MemberId}", member.Id);
                    await _email.SendReminderAsync(member, "Welcome");
                }
                catch (Exception mailEx)
                {
                    _logger.LogError(mailEx, "Mailing Pipeline Exception: Database saved cleanly, but welcome notification failed to dispatch for email {CustomerEmail}.", cleanEmail);
                }
            }
        }
        catch (DbUpdateException dbEx)
        {
            _logger.LogCritical(dbEx,
                "Fulfillment Pipeline Database Crash: Fatal relational commit failure occurred while attempting to write invoice parameters for target client {CustomerEmail}. SessionId: {SessionId}",
                cleanEmail, session.Id);
            throw; // Force error state transparency to bubble up into audit logs
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unexpected failure encountered inside fulfillment sequence layer for email {CustomerEmail}.",
                cleanEmail);
            throw;
        }
    }



}
