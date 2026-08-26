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

    public StripeService(IConfiguration config, ILogger<StripeService> logger, AbdContext db, IEmailService email)
    {
        _config = config;
        _logger = logger;
        _db = db;
        _email = email;
        StripeConfiguration.ApiKey = config["Stripe:SecretKey"];
    }

    public async Task<string> CreateCheckoutSessionAsync(
        MembershipCheckoutRequest request,
        string successUrl,
        string cancelUrl)
    {
        var isCouple = request.Plan == MembershipPlan.Couple;
        var amount = isCouple
            ? _config.GetValue("ClubPricing:CoupleMembershipFee", 90.00m)
            : _config.GetValue("ClubPricing:MembershipRenewalFee", 50.00m);

        var metadata = new Dictionary<string, string>
        {
            ["plan"] = request.Plan.ToString(),
            ["firstName1"] = request.FirstName1,
            ["lastName1"] = request.LastName1,
            ["email1"] = request.Email1,
            ["phone1"] = request.Phone1 ?? string.Empty
        };
        if (isCouple)
        {
            metadata["firstName2"] = request.FirstName2 ?? string.Empty;
            metadata["lastName2"] = request.LastName2 ?? string.Empty;
            metadata["email2"] = request.Email2 ?? request.Email1;
            metadata["phone2"] = request.Phone2 ?? string.Empty;
            metadata["sharedEmail"] = request.SharedEmail.ToString();
        }

        var options = new SessionCreateOptions
        {
            PaymentMethodTypes = new List<string> { "card" },
            LineItems = new List<SessionLineItemOptions>
            {
                new()
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "usd",
                        UnitAmount = decimal.ToInt64(amount * 100m),
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = isCouple ? "Two Annual Memberships" : "Annual Membership",
                            Description = isCouple
                                ? "Austin Ballroom Dancers — two one-year memberships"
                                : "Austin Ballroom Dancers — one-year membership"
                        }
                    },
                    Quantity = 1
                }
            },
            Mode = "payment",
            SuccessUrl = successUrl,
            CancelUrl = cancelUrl,
            CustomerEmail = request.Email1,
            Metadata = metadata
        };

        var session = await new SessionService().CreateAsync(options);
        _logger.LogInformation("Created {Plan} Stripe checkout {SessionId} for {Email}", request.Plan, session.Id, request.Email1);
        return session.Url;
    }

    public async Task<bool> HandleWebhookAsync(string json, string stripeSignature)
    {
        try
        {
            var webhookSecret = _config["Stripe:WebhookSecret"];
            if (string.IsNullOrWhiteSpace(webhookSecret))
            {
                _logger.LogCritical("Stripe webhook secret is not configured.");
                return false;
            }

            var stripeEvent = EventUtility.ConstructEvent(json, stripeSignature, webhookSecret);
            if (stripeEvent.Type == EventTypes.CheckoutSessionCompleted && stripeEvent.Data.Object is Session session)
                await FulfillMembershipAsync(session);

            return true;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe webhook signature validation failed.");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Stripe webhook processing failed.");
            return false;
        }
    }

    private async Task FulfillMembershipAsync(Session session)
    {
        if (!string.Equals(session.PaymentStatus, "paid", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Ignoring unpaid Stripe checkout session {SessionId}.", session.Id);
            return;
        }

        var transactionId = session.PaymentIntentId ?? session.Id;
        if (await _db.Payments.AnyAsync(p => p.TransactionId == transactionId))
        {
            _logger.LogInformation("Stripe checkout {TransactionId} was already fulfilled.", transactionId);
            return;
        }

        var plan = session.Metadata.TryGetValue("plan", out var planValue) &&
                   Enum.TryParse<MembershipPlan>(planValue, out var parsedPlan)
            ? parsedPlan
            : MembershipPlan.Individual;

        var people = new List<MemberDetails>
        {
            ReadPerson(session.Metadata, 1)
        };
        if (plan == MembershipPlan.Couple)
            people.Add(ReadPerson(session.Metadata, 2));

        if (people.Any(p => string.IsNullOrWhiteSpace(p.FirstName) || string.IsNullOrWhiteSpace(p.LastName) || string.IsNullOrWhiteSpace(p.Email)))
            throw new InvalidOperationException($"Stripe session {session.Id} is missing required member metadata.");

        var configuredTotal = plan == MembershipPlan.Couple
            ? _config.GetValue("ClubPricing:CoupleMembershipFee", 90.00m)
            : _config.GetValue("ClubPricing:MembershipRenewalFee", 50.00m);
        var totalPaid = session.AmountTotal.HasValue ? session.AmountTotal.Value / 100m : configuredTotal;
        var amountPerMember = decimal.Round(totalPaid / people.Count, 2);
        var fulfilledMembers = new List<Member>();

        await using var dbTransaction = await _db.Database.BeginTransactionAsync();
        for (var personIndex = 0; personIndex < people.Count; personIndex++)
        {
            var person = people[personIndex];
            var email = person.Email.Trim().ToLowerInvariant();
            var firstName = person.FirstName.Trim();
            var lastName = person.LastName.Trim();
            var member = await _db.Members.FirstOrDefaultAsync(m =>
                m.Email.ToLower() == email &&
                m.FirstName.ToLower() == firstName.ToLower() &&
                m.LastName.ToLower() == lastName.ToLower());

            var periodStart = DateTime.UtcNow;
            if (member == null)
            {
                member = new Member
                {
                    FirstName = firstName,
                    LastName = lastName,
                    Email = email,
                    Phone = Clean(person.Phone),
                    JoinDate = periodStart,
                    ExpiryDate = periodStart.AddYears(1),
                    CreatedAt = periodStart,
                    SelfRegistered = true
                };
                _db.Members.Add(member);
                await _db.SaveChangesAsync();
            }
            else
            {
                periodStart = member.ExpiryDate.HasValue && member.ExpiryDate.Value > DateTime.UtcNow
                    ? member.ExpiryDate.Value
                    : DateTime.UtcNow;
                member.ExpiryDate = periodStart.AddYears(1);
                member.Phone = Clean(person.Phone) ?? member.Phone;
            }

            _db.Payments.Add(new Payment
            {
                MemberId = member.Id,
                Amount = personIndex == people.Count - 1
                    ? totalPaid - (amountPerMember * (people.Count - 1))
                    : amountPerMember,
                PaymentDate = DateTime.UtcNow,
                PeriodStart = periodStart,
                PeriodEnd = member.ExpiryDate!.Value,
                TransactionId = transactionId,
                Status = "Completed"
            });
            fulfilledMembers.Add(member);
        }

        await _db.SaveChangesAsync();
        await dbTransaction.CommitAsync();

        foreach (var member in fulfilledMembers)
        {
            try
            {
                await _email.SendReminderAsync(member, "Welcome");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Membership saved, but welcome email failed for MemberId {MemberId}.", member.Id);
            }
        }
    }

    private static MemberDetails ReadPerson(IReadOnlyDictionary<string, string> metadata, int number)
    {
        metadata.TryGetValue($"firstName{number}", out var firstName);
        metadata.TryGetValue($"lastName{number}", out var lastName);
        metadata.TryGetValue($"email{number}", out var email);
        metadata.TryGetValue($"phone{number}", out var phone);
        return new MemberDetails(firstName ?? string.Empty, lastName ?? string.Empty, email ?? string.Empty, phone);
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private sealed record MemberDetails(string FirstName, string LastName, string Email, string? Phone);
}
