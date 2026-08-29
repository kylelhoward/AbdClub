using System.ComponentModel.DataAnnotations;
using AbdClub.Data;
using AbdClub.Models;
using AbdClub.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AbdClub.Pages;

public class EventTicketsModel(
    AbdContext context,
    IStripeService stripe,
    ILogger<EventTicketsModel> logger) : PageModel
{
    public Event Event { get; private set; } = null!;
    public List<TicketTypeView> TicketTypes { get; private set; } = new();
    public EventTicketOrder? CompletedOrder { get; private set; }
    public bool PaymentCancelled { get; private set; }
    public bool PaymentPending { get; private set; }

    [BindProperty]
    public PurchaseInput Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(int eventId, string? session_id = null)
    {
        if (!await LoadEventAsync(eventId))
            return NotFound();

        PaymentCancelled = Request.Query.ContainsKey("cancelled");
        if (!string.IsNullOrWhiteSpace(session_id))
        {
            CompletedOrder = await context.EventTicketOrders
                .AsNoTracking()
                .Include(o => o.Tickets)
                .SingleOrDefaultAsync(o => o.EventId == eventId && o.StripeCheckoutSessionId == session_id);
            PaymentPending = CompletedOrder == null || CompletedOrder.Status == TicketOrderStatus.Pending;
        }

        Input.Selections = TicketTypes
            .Select(t => new TicketSelectionInput { TicketTypeId = t.Id })
            .ToList();
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int eventId)
    {
        if (!await LoadEventAsync(eventId))
            return NotFound();

        Input.Selections ??= new();
        var availableIds = TicketTypes.Select(t => t.Id).ToHashSet();
        var selections = new List<EventTicketSelection>();

        foreach (var selection in Input.Selections.Where(s => s.Quantity > 0))
        {
            var type = TicketTypes.SingleOrDefault(t => t.Id == selection.TicketTypeId);
            if (type == null || !availableIds.Contains(selection.TicketTypeId))
            {
                ModelState.AddModelError(string.Empty, "A selected ticket type is no longer available.");
                continue;
            }
            if (selection.Quantity > 12)
            {
                ModelState.AddModelError(string.Empty, "No more than 12 tickets may be purchased in one order.");
                continue;
            }

            var names = Lines(selection.HolderNames);
            if (names.Count != selection.Quantity)
                ModelState.AddModelError(string.Empty, $"Enter exactly {selection.Quantity} attendee name(s) for {type.Name}, one per line.");

            var memberNumbers = new List<int>();
            if (type.IsMemberOnly)
            {
                foreach (var value in Lines(selection.MemberNumbers))
                {
                    var normalized = value.Trim().ToUpperInvariant().Replace("ABD-", string.Empty);
                    if (int.TryParse(normalized, out var number))
                        memberNumbers.Add(number);
                    else
                        ModelState.AddModelError(string.Empty, $"'{value}' is not a valid ABD member number.");
                }
                if (memberNumbers.Count != selection.Quantity)
                    ModelState.AddModelError(string.Empty, $"Enter exactly {selection.Quantity} member number(s) for {type.Name}, one per line.");
            }

            selections.Add(new EventTicketSelection
            {
                TicketTypeId = selection.TicketTypeId,
                HolderNames = names,
                MemberNumbers = memberNumbers
            });
        }

        if (selections.Count == 0)
            ModelState.AddModelError(string.Empty, "Select at least one ticket.");
        if (selections.Sum(s => s.HolderNames.Count) > 12)
            ModelState.AddModelError(string.Empty, "No more than 12 tickets may be purchased in one order.");

        if (!ModelState.IsValid)
            return Page();

        var request = new EventTicketCheckoutRequest
        {
            EventId = eventId,
            PurchaserName = Input.PurchaserName.Trim(),
            PurchaserEmail = Input.PurchaserEmail.Trim().ToLowerInvariant(),
            PurchaserPhone = Clean(Input.PurchaserPhone),
            Selections = selections
        };

        var successUrl = Url.Page(
            "/EventTickets",
            pageHandler: null,
            values: new { eventId, success = true },
            protocol: Request.Scheme)! + "&session_id={CHECKOUT_SESSION_ID}";
        var cancelUrl = Url.Page(
            "/EventTickets",
            pageHandler: null,
            values: new { eventId, cancelled = true },
            protocol: Request.Scheme)!;

        try
        {
            return Redirect(await stripe.CreateEventTicketCheckoutSessionAsync(request, successUrl, cancelUrl));
        }
        catch (InvalidOperationException ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return Page();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unable to create ticket checkout for EventId {EventId}.", eventId);
            ModelState.AddModelError(string.Empty, "Unable to start payment. Please try again.");
            return Page();
        }
    }

    private async Task<bool> LoadEventAsync(int eventId)
    {
        Event = await context.Events
            .AsNoTracking()
            .Include(e => e.Location)
            .SingleOrDefaultAsync(e => e.Id == eventId) ?? null!;
        if (Event == null)
            return false;

        var now = DateTime.UtcNow;
        var types = await context.EventTicketTypes
            .AsNoTracking()
            .Where(t => t.EventId == eventId && t.IsActive && !t.IsDoorPrice &&
                (!t.SalesStartAt.HasValue || t.SalesStartAt <= now) &&
                (!t.SalesEndAt.HasValue || t.SalesEndAt >= now))
            .OrderBy(t => t.DisplayOrder)
            .ThenBy(t => t.Price)
            .ToListAsync();

        foreach (var type in types)
        {
            var reserved = await context.EventTickets.CountAsync(t =>
                t.TicketTypeId == type.Id &&
                (t.Status == EventTicketStatus.Valid ||
                 (t.Status == EventTicketStatus.Pending && t.Order.ExpiresAt > now)));
            TicketTypes.Add(new TicketTypeView(
                type.Id,
                type.Name,
                type.Price,
                type.IsMemberOnly,
                type.QuantityAvailable.HasValue ? Math.Max(0, type.QuantityAvailable.Value - reserved) : null));
        }
        return true;
    }

    private static List<string> Lines(string? value) =>
        (value ?? string.Empty)
            .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    public sealed record TicketTypeView(int Id, string Name, decimal Price, bool IsMemberOnly, int? Remaining);

    public class PurchaseInput
    {
        [Required, StringLength(150), Display(Name = "Purchaser name")]
        public string PurchaserName { get; set; } = string.Empty;

        [Required, EmailAddress, StringLength(254), Display(Name = "Email address")]
        public string PurchaserEmail { get; set; } = string.Empty;

        [Phone, StringLength(30), Display(Name = "Phone")]
        public string? PurchaserPhone { get; set; }

        public List<TicketSelectionInput> Selections { get; set; } = new();
    }

    public class TicketSelectionInput
    {
        public int TicketTypeId { get; set; }
        [Range(0, 12)] public int Quantity { get; set; }
        public string? HolderNames { get; set; }
        public string? MemberNumbers { get; set; }
    }
}
