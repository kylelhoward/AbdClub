using AbdClub.Data;
using AbdClub.Models;
using AbdClub.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Stripe;
using Stripe.Checkout;
using System.Data;

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
            {
                if (session.Metadata.TryGetValue("kind", out var kind) && kind == "eventTickets")
                    await FulfillEventTicketOrderAsync(session);
                else
                    await FulfillMembershipAsync(session);
            }
            else if (stripeEvent.Type == EventTypes.CheckoutSessionExpired && stripeEvent.Data.Object is Session expiredSession)
            {
                await CancelExpiredTicketOrderAsync(expiredSession);
            }

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

    public async Task<string> CreateEventTicketCheckoutSessionAsync(
        EventTicketCheckoutRequest request,
        string successUrl,
        string cancelUrl)
    {
        if (request.Selections.Count == 0)
            throw new InvalidOperationException("Select at least one ticket.");

        var selectedTypeIds = request.Selections.Select(s => s.TicketTypeId).Distinct().ToList();
        if (selectedTypeIds.Count != request.Selections.Count)
            throw new InvalidOperationException("Each ticket type may only appear once per order.");
        await using var reservationTransaction = await _db.Database
            .BeginTransactionAsync(IsolationLevel.Serializable);
        var ticketTypes = await _db.EventTicketTypes
            .Include(t => t.Event)
            .Where(t => selectedTypeIds.Contains(t.Id) && t.EventId == request.EventId)
            .ToDictionaryAsync(t => t.Id);

        if (ticketTypes.Count != selectedTypeIds.Count || ticketTypes.Values.Any(t => !t.IsActive || t.IsDoorPrice))
            throw new InvalidOperationException("One or more selected ticket types are unavailable online.");

        var now = DateTime.UtcNow;
        if (ticketTypes.Values.Any(t =>
            (t.SalesStartAt.HasValue && t.SalesStartAt.Value > now) ||
            (t.SalesEndAt.HasValue && t.SalesEndAt.Value < now)))
            throw new InvalidOperationException("One or more selected ticket types are outside their sales window.");

        var totalTicketCount = request.Selections.Sum(s => s.HolderNames.Count);
        if (totalTicketCount is < 1 or > 12)
            throw new InvalidOperationException("An order must contain between 1 and 12 tickets.");

        var memberNumbers = request.Selections
            .SelectMany(s => s.MemberNumbers)
            .ToList();
        if (memberNumbers.Count != memberNumbers.Distinct().Count())
            throw new InvalidOperationException("A member number can only be used for one member-priced ticket per order.");
        memberNumbers = memberNumbers.Distinct().ToList();
        var membersByNumber = await _db.Members
            .Where(m => memberNumbers.Contains(m.MemberNumber))
            .ToDictionaryAsync(m => m.MemberNumber);

        foreach (var selection in request.Selections)
        {
            if (!ticketTypes.TryGetValue(selection.TicketTypeId, out var type))
                throw new InvalidOperationException("A selected ticket type no longer exists.");
            if (selection.HolderNames.Count == 0 || selection.HolderNames.Any(string.IsNullOrWhiteSpace))
                throw new InvalidOperationException($"Enter each attendee name for {type.Name}.");

            if (type.IsMemberOnly)
            {
                if (selection.MemberNumbers.Count != selection.HolderNames.Count)
                    throw new InvalidOperationException($"Enter one member number per {type.Name} ticket.");

                for (var memberIndex = 0; memberIndex < selection.MemberNumbers.Count; memberIndex++)
                {
                    var memberNumber = selection.MemberNumbers[memberIndex];
                    if (!membersByNumber.TryGetValue(memberNumber, out var member) || !member.IsActive)
                        throw new InvalidOperationException($"ABD-{memberNumber:D5} is not an active member number.");
                    var expectedName = NormalizePersonName($"{member.FirstName} {member.LastName}");
                    if (NormalizePersonName(selection.HolderNames[memberIndex]) != expectedName)
                        throw new InvalidOperationException($"The attendee name for ABD-{memberNumber:D5} must match the member record.");
                }
            }

            if (type.QuantityAvailable.HasValue)
            {
                var reserved = await _db.EventTickets.CountAsync(t =>
                    t.TicketTypeId == type.Id &&
                    (t.Status == EventTicketStatus.Valid ||
                     (t.Status == EventTicketStatus.Pending && t.Order.ExpiresAt > now)));
                if (reserved + selection.HolderNames.Count > type.QuantityAvailable.Value)
                    throw new InvalidOperationException($"There are not enough {type.Name} tickets remaining.");
            }
        }

        var order = new EventTicketOrder
        {
            EventId = request.EventId,
            PurchaserName = request.PurchaserName.Trim(),
            PurchaserEmail = request.PurchaserEmail.Trim().ToLowerInvariant(),
            PurchaserPhone = Clean(request.PurchaserPhone),
            PaymentMethod = "Stripe",
            Status = TicketOrderStatus.Pending,
            CreatedAt = now,
            ExpiresAt = now.AddHours(24)
        };

        foreach (var selection in request.Selections)
        {
            var type = ticketTypes[selection.TicketTypeId];
            for (var index = 0; index < selection.HolderNames.Count; index++)
            {
                int? memberId = null;
                if (type.IsMemberOnly)
                    memberId = membersByNumber[selection.MemberNumbers[index]].Id;

                var ticketCode = $"ABD-E{request.EventId}-{Guid.NewGuid():N}";
                order.Tickets.Add(new EventTicket
                {
                    TicketTypeId = type.Id,
                    HolderName = selection.HolderNames[index].Trim(),
                    MemberId = memberId,
                    TicketCode = ticketCode[..Math.Min(40, ticketCode.Length)],
                    TicketTypeName = type.Name,
                    PricePaid = type.Price,
                    Status = EventTicketStatus.Pending
                });
            }
        }
        order.Amount = order.Tickets.Sum(t => t.PricePaid);

        _db.EventTicketOrders.Add(order);
        await _db.SaveChangesAsync();
        await reservationTransaction.CommitAsync();

        Session session;
        try
        {
            var lineItems = request.Selections.Select(selection =>
            {
                var type = ticketTypes[selection.TicketTypeId];
                return new SessionLineItemOptions
                {
                    Quantity = selection.HolderNames.Count,
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = "usd",
                        UnitAmount = decimal.ToInt64(type.Price * 100m),
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = $"{type.Event.Title} — {type.Name}",
                            Description = type.IsMemberOnly ? "ABD member ticket" : "Event ticket"
                        }
                    }
                };
            }).ToList();

            session = await new SessionService().CreateAsync(new SessionCreateOptions
            {
                PaymentMethodTypes = new List<string> { "card" },
                LineItems = lineItems,
                Mode = "payment",
                SuccessUrl = successUrl,
                CancelUrl = cancelUrl,
                CustomerEmail = order.PurchaserEmail,
                Metadata = new Dictionary<string, string>
                {
                    ["kind"] = "eventTickets",
                    ["orderId"] = order.Id.ToString()
                }
            });

        }
        catch
        {
            _db.EventTicketOrders.Remove(order);
            await _db.SaveChangesAsync();
            throw;
        }

        order.StripeCheckoutSessionId = session.Id;
        await _db.SaveChangesAsync();
        return session.Url;
    }

    public async Task RefundEventTicketAsync(int ticketId)
    {
        var ticket = await _db.EventTickets
            .Include(t => t.Order).ThenInclude(o => o.Tickets)
            .SingleOrDefaultAsync(t => t.Id == ticketId)
            ?? throw new InvalidOperationException("Ticket not found.");

        if (ticket.Status != EventTicketStatus.Valid || ticket.IsCheckedIn)
            throw new InvalidOperationException("Only an unused, valid ticket can be refunded.");
        if (string.IsNullOrWhiteSpace(ticket.Order.StripePaymentIntentId))
            throw new InvalidOperationException("This ticket was not paid through Stripe.");

        var refund = await new RefundService().CreateAsync(
            new RefundCreateOptions
            {
                PaymentIntent = ticket.Order.StripePaymentIntentId,
                Amount = decimal.ToInt64(ticket.PricePaid * 100m),
                Metadata = new Dictionary<string, string>
                {
                    ["eventTicketId"] = ticket.Id.ToString(),
                    ["eventOrderId"] = ticket.OrderId.ToString()
                }
            },
            new RequestOptions { IdempotencyKey = $"event-ticket-refund-{ticket.Id}" });

        ticket.Status = EventTicketStatus.Refunded;
        ticket.RefundedAt = DateTime.UtcNow;
        ticket.StripeRefundId = refund.Id;
        ticket.Order.RefundedAmount += ticket.PricePaid;
        ticket.Order.Status = ticket.Order.Tickets.All(t => t.Id == ticket.Id || t.Status == EventTicketStatus.Refunded)
            ? TicketOrderStatus.Refunded
            : TicketOrderStatus.PartiallyRefunded;
        await _db.SaveChangesAsync();
    }

    private async Task FulfillEventTicketOrderAsync(Session session)
    {
        if (!string.Equals(session.PaymentStatus, "paid", StringComparison.OrdinalIgnoreCase))
            return;
        if (!session.Metadata.TryGetValue("orderId", out var orderIdValue) ||
            !int.TryParse(orderIdValue, out var orderId))
            throw new InvalidOperationException($"Stripe ticket session {session.Id} has no valid order ID.");

        var order = await _db.EventTicketOrders
            .Include(o => o.Event)
            .Include(o => o.Tickets)
            .SingleOrDefaultAsync(o => o.Id == orderId)
            ?? throw new InvalidOperationException($"Ticket order {orderId} was not found.");

        if (order.Status == TicketOrderStatus.Pending)
        {
            if (!string.IsNullOrWhiteSpace(order.StripeCheckoutSessionId) &&
                order.StripeCheckoutSessionId != session.Id)
                throw new InvalidOperationException($"Stripe session does not match ticket order {orderId}.");

            var amountPaid = (session.AmountTotal ?? 0) / 100m;
            if (amountPaid != order.Amount)
                throw new InvalidOperationException($"Stripe amount does not match ticket order {orderId}.");

            order.StripeCheckoutSessionId = session.Id;
            order.StripePaymentIntentId = session.PaymentIntentId;
            order.Status = TicketOrderStatus.Paid;
            order.PaidAt = DateTime.UtcNow;
            foreach (var ticket in order.Tickets)
                ticket.Status = EventTicketStatus.Valid;

            await _db.SaveChangesAsync();
            _logger.LogInformation("Fulfilled event ticket order {OrderId} from Stripe session {SessionId}.", order.Id, session.Id);
        }

        if (order.Status == TicketOrderStatus.Paid && !order.ConfirmationEmailSentAt.HasValue)
        {
            await _email.SendEventTicketConfirmationAsync(order);
            order.ConfirmationEmailSentAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
    }

    private async Task CancelExpiredTicketOrderAsync(Session session)
    {
        if (!session.Metadata.TryGetValue("kind", out var kind) || kind != "eventTickets" ||
            !session.Metadata.TryGetValue("orderId", out var orderIdValue) ||
            !int.TryParse(orderIdValue, out var orderId))
            return;

        var order = await _db.EventTicketOrders
            .Include(o => o.Tickets)
            .SingleOrDefaultAsync(o => o.Id == orderId);
        if (order == null || order.Status != TicketOrderStatus.Pending)
            return;

        order.Status = TicketOrderStatus.Cancelled;
        foreach (var ticket in order.Tickets)
            ticket.Status = EventTicketStatus.Cancelled;
        await _db.SaveChangesAsync();
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
                Status = "Completed",
                PaymentMethod = "Stripe"
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
    private static string NormalizePersonName(string value) =>
        new(value.Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray());
    private sealed record MemberDetails(string FirstName, string LastName, string Email, string? Phone);
}
