using System.ComponentModel.DataAnnotations;
using AbdClub.Data;
using AbdClub.Models;
using AbdClub.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace AbdClub.Pages.Officers.Dances;

[Authorize(Policy = "isOfficer")]
public class TicketsModel(
    AbdContext context,
    IAuthorizationService authorization,
    IStripeService stripe,
    IEmailService email,
    ILogger<TicketsModel> logger) : PageModel
{
    public SpecialEvent TargetEvent { get; private set; } = null!;
    public List<EventTicketType> TicketTypes { get; private set; } = new();
    public List<EventTicketOrder> Orders { get; private set; } = new();
    public bool ShowTicketTypeModal { get; private set; }
    public bool ShowManualSaleModal { get; private set; }
    public decimal GrossSales => Orders.Sum(o => o.Amount);
    public decimal Refunds => Orders.Sum(o => o.RefundedAmount);
    public decimal NetSales => GrossSales - Refunds;
    public int ValidTicketCount => Orders.SelectMany(o => o.Tickets).Count(t => t.Status == EventTicketStatus.Valid);
    public int CheckedInCount => Orders.SelectMany(o => o.Tickets).Count(t => t.IsCheckedIn);

    [BindProperty] public TicketTypeInput TypeInput { get; set; } = new();
    [BindProperty] public ManualTicketInput ManualInput { get; set; } = new();

    [TempData] public string? StatusMessage { get; set; }
    [TempData] public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        if (!await LoadAsync(id))
            return NotFound();
        return Page();
    }

    public async Task<IActionResult> OnGetExportAsync(int id)
    {
        if (!await LoadAsync(id)) return NotFound();
        var csv = new StringBuilder();
        csv.AppendLine("Ticket Code,Attendee,Ticket Type,Price,Ticket Status,Checked In,Purchaser,Email,Payment Method,Order Status,Paid At");
        foreach (var order in Orders)
        {
            foreach (var ticket in order.Tickets.OrderBy(t => t.HolderName))
            {
                csv.AppendLine(string.Join(",", new[]
                {
                    Csv(ticket.TicketCode), Csv(ticket.HolderName), Csv(ticket.TicketTypeName),
                    ticket.PricePaid.ToString("0.00"), Csv(ticket.Status.ToString()),
                    ticket.IsCheckedIn ? "Yes" : "No", Csv(order.PurchaserName), Csv(order.PurchaserEmail),
                    Csv(order.PaymentMethod), Csv(order.Status.ToString()), Csv(order.PaidAt?.ToString("O") ?? string.Empty)
                }));
            }
        }
        return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", $"event-{id}-tickets.csv");
    }

    public async Task<IActionResult> OnPostSaveTypeAsync(int id)
    {
        if (!await IsAdminAsync()) return Forbid();
        if (!await LoadAsync(id)) return NotFound();

        // Both modal forms are bound on this page. Only validate the ticket-type
        // fields when the ticket-type handler was submitted.
        RemoveModelStatePrefix(nameof(ManualInput));

        if (TypeInput.Price < 0)
            ModelState.AddModelError("TypeInput.Price", "Price cannot be negative.");
        if (TypeInput.IsActive && !TypeInput.IsDoorPrice && TypeInput.Price < 0.50m)
            ModelState.AddModelError("TypeInput.Price", "Online Stripe tickets must cost at least $0.50. Use a door/manual type for complimentary admission.");
        if (TypeInput.QuantityAvailable is < 1)
            ModelState.AddModelError("TypeInput.QuantityAvailable", "Capacity must be blank or at least 1.");
        if (TypeInput.SalesStartLocal.HasValue && TypeInput.SalesEndLocal.HasValue &&
            TypeInput.SalesEndLocal <= TypeInput.SalesStartLocal)
            ModelState.AddModelError(string.Empty, "Sales must end after they begin.");

        if (!ModelState.IsValid)
        {
            ShowTicketTypeModal = true;
            return Page();
        }

        EventTicketType type;
        if (TypeInput.Id.HasValue)
        {
            type = await context.EventTicketTypes
                .SingleOrDefaultAsync(t => t.Id == TypeInput.Id && t.EventId == id)
                ?? throw new InvalidOperationException("Ticket type not found.");
        }
        else
        {
            type = new EventTicketType { EventId = id };
            context.EventTicketTypes.Add(type);
        }

        type.Name = TypeInput.Name.Trim();
        type.Price = TypeInput.Price;
        type.SalesStartAt = ToUtc(TypeInput.SalesStartLocal);
        type.SalesEndAt = ToUtc(TypeInput.SalesEndLocal);
        type.IsMemberOnly = TypeInput.IsMemberOnly;
        type.IsDoorPrice = TypeInput.IsDoorPrice;
        type.IsActive = TypeInput.IsActive;
        type.QuantityAvailable = TypeInput.QuantityAvailable;
        type.DisplayOrder = TypeInput.DisplayOrder;

        await context.SaveChangesAsync();
        StatusMessage = "Ticket type saved.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostDeleteTypeAsync(int id, int ticketTypeId)
    {
        if (!await IsAdminAsync()) return Forbid();
        var type = await context.EventTicketTypes
            .Include(t => t.Tickets)
            .SingleOrDefaultAsync(t => t.Id == ticketTypeId && t.EventId == id);
        if (type == null) return NotFound();
        if (type.Tickets.Any())
        {
            ErrorMessage = "A ticket type with orders cannot be deleted. Disable it instead.";
            return RedirectToPage(new { id });
        }

        context.EventTicketTypes.Remove(type);
        await context.SaveChangesAsync();
        StatusMessage = "Ticket type deleted.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostManualSaleAsync(int id)
    {
        if (!await IsAdminAsync()) return Forbid();
        if (!await LoadAsync(id)) return NotFound();

        // Ignore the empty ticket-type modal while validating a manual sale.
        RemoveModelStatePrefix(nameof(TypeInput));

        var type = TicketTypes.SingleOrDefault(t => t.Id == ManualInput.TicketTypeId && t.IsActive);
        if (type == null)
            ModelState.AddModelError("ManualInput.TicketTypeId", "Select an active ticket type.");
        var allowedPaymentMethods = new[] { "Cash", "Card", "Check", "Complimentary" };
        if (!allowedPaymentMethods.Contains(ManualInput.PaymentMethod))
            ModelState.AddModelError("ManualInput.PaymentMethod", "Select a valid payment method.");

        Member? member = null;
        if (type?.IsMemberOnly == true)
        {
            if (!ManualInput.MemberNumber.HasValue)
                ModelState.AddModelError("ManualInput.MemberNumber", "An active member number is required.");
            else
            {
                member = await context.Members.SingleOrDefaultAsync(m => m.MemberNumber == ManualInput.MemberNumber);
                if (member == null || !member.IsActive)
                    ModelState.AddModelError("ManualInput.MemberNumber", "That is not an active member number.");
                else if (NormalizeName(ManualInput.HolderName) != NormalizeName($"{member.FirstName} {member.LastName}"))
                    ModelState.AddModelError("ManualInput.HolderName", "The attendee name must match the member record.");
            }
        }

        if (type?.QuantityAvailable.HasValue == true)
        {
            var issued = await context.EventTickets.CountAsync(t =>
                t.TicketTypeId == type.Id &&
                (t.Status == EventTicketStatus.Valid ||
                 (t.Status == EventTicketStatus.Pending && t.Order.ExpiresAt > DateTime.UtcNow)));
            if (issued >= type.QuantityAvailable.Value)
                ModelState.AddModelError(string.Empty, "That ticket type is sold out.");
        }

        if (!ModelState.IsValid)
        {
            ShowManualSaleModal = true;
            return Page();
        }

        var now = DateTime.UtcNow;
        var amountPaid = ManualInput.PaymentMethod == "Complimentary" ? 0m : type!.Price;
        var order = new EventTicketOrder
        {
            EventId = id,
            PurchaserName = ManualInput.PurchaserName.Trim(),
            PurchaserEmail = ManualInput.PurchaserEmail.Trim().ToLowerInvariant(),
            PurchaserPhone = Clean(ManualInput.PurchaserPhone),
            Amount = amountPaid,
            PaymentMethod = ManualInput.PaymentMethod,
            ManualTransactionId = Clean(ManualInput.TransactionId) ?? $"manual_ticket_{Guid.NewGuid():N}",
            Status = TicketOrderStatus.Paid,
            CreatedAt = now,
            ExpiresAt = now,
            PaidAt = now
        };
        var ticketCode = $"ABD-E{id}-{Guid.NewGuid():N}";
        order.Tickets.Add(new EventTicket
        {
            TicketTypeId = type!.Id,
            HolderName = ManualInput.HolderName.Trim(),
            MemberId = member?.Id,
            TicketCode = ticketCode[..Math.Min(40, ticketCode.Length)],
            TicketTypeName = type.Name,
            PricePaid = amountPaid,
            Status = EventTicketStatus.Valid
        });

        context.EventTicketOrders.Add(order);
        await context.SaveChangesAsync();
        try
        {
            order.Event = await context.SpecialEvents.FindAsync(id) ?? TargetEvent;
            await email.SendEventTicketConfirmationAsync(order);
            order.ConfirmationEmailSentAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Manual ticket sale saved, but confirmation email failed for OrderId {OrderId}.", order.Id);
        }
        StatusMessage = $"Manual ticket recorded: {order.Tickets.Single().TicketCode}.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostCheckInAsync(int id, int ticketId)
    {
        var ticket = await context.EventTickets
            .Include(t => t.Order)
            .SingleOrDefaultAsync(t => t.Id == ticketId && t.Order.EventId == id);
        if (ticket == null) return NotFound();
        if (ticket.Status != EventTicketStatus.Valid)
        {
            ErrorMessage = "Only valid tickets can be checked in.";
            return RedirectToPage(new { id });
        }

        var officerId = await CurrentOfficerAccountIdAsync();
        if (!officerId.HasValue) return Forbid();

        ticket.IsCheckedIn = !ticket.IsCheckedIn;
        ticket.CheckedInAt = ticket.IsCheckedIn ? DateTime.UtcNow : null;
        ticket.CheckedInByOfficerAccountId = ticket.IsCheckedIn ? officerId : null;
        await context.SaveChangesAsync();
        StatusMessage = ticket.IsCheckedIn ? $"Checked in {ticket.HolderName}." : $"Reversed check-in for {ticket.HolderName}.";
        return RedirectToPage(new { id });
    }

    public async Task<IActionResult> OnPostRefundAsync(int id, int ticketId)
    {
        if (!await IsAdminAsync()) return Forbid();
        var ticket = await context.EventTickets
            .Include(t => t.Order).ThenInclude(o => o.Tickets)
            .SingleOrDefaultAsync(t => t.Id == ticketId && t.Order.EventId == id);
        if (ticket == null) return NotFound();

        try
        {
            if (ticket.Order.PaymentMethod == "Stripe")
            {
                await stripe.RefundEventTicketAsync(ticketId);
            }
            else
            {
                if (ticket.Status != EventTicketStatus.Valid || ticket.IsCheckedIn)
                    throw new InvalidOperationException("Only an unused, valid ticket can be refunded.");
                if (ticket.PricePaid <= 0)
                    throw new InvalidOperationException("Complimentary tickets do not have a payment to refund.");
                ticket.Status = EventTicketStatus.Refunded;
                ticket.RefundedAt = DateTime.UtcNow;
                ticket.Order.RefundedAmount += ticket.PricePaid;
                ticket.Order.Status = ticket.Order.Tickets.All(t => t.Status == EventTicketStatus.Refunded)
                    ? TicketOrderStatus.Refunded
                    : TicketOrderStatus.PartiallyRefunded;
                await context.SaveChangesAsync();
            }
            StatusMessage = $"Refund recorded for {ticket.HolderName}.";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ticket refund failed for TicketId {TicketId}.", ticketId);
            ErrorMessage = ex is InvalidOperationException ? ex.Message : "Stripe could not complete the refund.";
        }
        return RedirectToPage(new { id });
    }

    private async Task<bool> LoadAsync(int id)
    {
        TargetEvent = await context.SpecialEvents
            .AsNoTracking()
            .Include(e => e.Location)
            .SingleOrDefaultAsync(e => e.Id == id) ?? null!;
        if (TargetEvent == null) return false;

        TicketTypes = await context.EventTicketTypes
            .AsNoTracking()
            .Where(t => t.EventId == id)
            .OrderBy(t => t.DisplayOrder).ThenBy(t => t.Price)
            .ToListAsync();
        Orders = await context.EventTicketOrders
            .AsNoTracking()
            .Where(o => o.EventId == id &&
                o.Status != TicketOrderStatus.Pending &&
                o.Status != TicketOrderStatus.Cancelled)
            .Include(o => o.Tickets)
            .OrderByDescending(o => o.PaidAt)
            .ToListAsync();
        return true;
    }

    private async Task<bool> IsAdminAsync() =>
        (await authorization.AuthorizeAsync(User, null, "isAdmin")).Succeeded;

    private async Task<int?> CurrentOfficerAccountIdAsync()
    {
        var value = User.FindFirst("MemberId")?.Value;
        if (!int.TryParse(value, out var memberId)) return null;
        return await context.OfficerAccounts
            .Where(a => a.MemberId == memberId && a.IsEnabled)
            .Select(a => (int?)a.Id)
            .SingleOrDefaultAsync();
    }

    private static DateTime? ToUtc(DateTime? central)
    {
        if (!central.HasValue) return null;
        var zone = TimeZoneInfo.FindSystemTimeZoneById("America/Chicago");
        return TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(central.Value, DateTimeKind.Unspecified), zone);
    }

    public static DateTime? ToCentral(DateTime? utc)
    {
        if (!utc.HasValue) return null;
        var zone = TimeZoneInfo.FindSystemTimeZoneById("America/Chicago");
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc.Value, DateTimeKind.Utc), zone);
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static string Csv(string value) => $"\"{value.Replace("\"", "\"\"")}\"";
    private static string NormalizeName(string value) =>
        new(value.Where(char.IsLetterOrDigit).Select(char.ToUpperInvariant).ToArray());

    private void RemoveModelStatePrefix(string prefix)
    {
        foreach (var key in ModelState.Keys
            .Where(key => key.Equals(prefix, StringComparison.Ordinal) ||
                          key.StartsWith(prefix + ".", StringComparison.Ordinal))
            .ToList())
        {
            ModelState.Remove(key);
        }
    }

    public class TicketTypeInput
    {
        public int? Id { get; set; }
        [Required, StringLength(100)] public string Name { get; set; } = string.Empty;
        [Range(0, 10000)] public decimal Price { get; set; }
        public DateTime? SalesStartLocal { get; set; }
        public DateTime? SalesEndLocal { get; set; }
        public bool IsMemberOnly { get; set; }
        public bool IsDoorPrice { get; set; }
        public bool IsActive { get; set; } = true;
        public int? QuantityAvailable { get; set; }
        public int DisplayOrder { get; set; }
    }

    public class ManualTicketInput
    {
        [Range(1, int.MaxValue)] public int TicketTypeId { get; set; }
        [Required, StringLength(150)] public string HolderName { get; set; } = string.Empty;
        [Required, StringLength(150)] public string PurchaserName { get; set; } = string.Empty;
        [Required, EmailAddress, StringLength(254)] public string PurchaserEmail { get; set; } = string.Empty;
        [Phone, StringLength(30)] public string? PurchaserPhone { get; set; }
        public int? MemberNumber { get; set; }
        [Required, StringLength(30)] public string PaymentMethod { get; set; } = "Cash";
        [StringLength(255)] public string? TransactionId { get; set; }
    }
}
