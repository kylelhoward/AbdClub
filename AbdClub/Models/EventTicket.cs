using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AbdClub.Models;

public enum EventTicketStatus
{
    Pending = 1,
    Valid = 2,
    Refunded = 3,
    Cancelled = 4
}

public class EventTicket
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public EventTicketOrder Order { get; set; } = null!;
    public int TicketTypeId { get; set; }
    public EventTicketType TicketType { get; set; } = null!;

    [Required, StringLength(150)]
    public string HolderName { get; set; } = string.Empty;

    public int? MemberId { get; set; }
    public Member? Member { get; set; }

    [Required, StringLength(40)]
    public string TicketCode { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string TicketTypeName { get; set; } = string.Empty;

    [Column(TypeName = "numeric(10,2)")]
    public decimal PricePaid { get; set; }

    public EventTicketStatus Status { get; set; } = EventTicketStatus.Pending;
    public bool IsCheckedIn { get; set; }
    public DateTime? CheckedInAt { get; set; }
    public int? CheckedInByOfficerAccountId { get; set; }
    public OfficerAccount? CheckedInByOfficerAccount { get; set; }
    public DateTime? RefundedAt { get; set; }
    public string? StripeRefundId { get; set; }
}
