using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AbdClub.Models;

public enum TicketOrderStatus
{
    Pending = 1,
    Paid = 2,
    PartiallyRefunded = 3,
    Refunded = 4,
    Cancelled = 5
}

public class EventTicketOrder
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public Event Event { get; set; } = null!;

    [Required, StringLength(150)]
    public string PurchaserName { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(254)]
    public string PurchaserEmail { get; set; } = string.Empty;

    [StringLength(30)]
    public string? PurchaserPhone { get; set; }

    [Column(TypeName = "numeric(10,2)")]
    public decimal Amount { get; set; }

    [Column(TypeName = "numeric(10,2)")]
    public decimal RefundedAmount { get; set; }

    public TicketOrderStatus Status { get; set; } = TicketOrderStatus.Pending;

    [Required, StringLength(30)]
    public string PaymentMethod { get; set; } = "Stripe";

    [StringLength(255)]
    public string? StripeCheckoutSessionId { get; set; }

    [StringLength(255)]
    public string? StripePaymentIntentId { get; set; }

    [StringLength(255)]
    public string? ManualTransactionId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public DateTime? PaidAt { get; set; }
    public DateTime? ConfirmationEmailSentAt { get; set; }

    public ICollection<EventTicket> Tickets { get; set; } = new List<EventTicket>();
}
