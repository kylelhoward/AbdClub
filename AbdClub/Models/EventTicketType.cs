using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AbdClub.Models;

public class EventTicketType
{
    public int Id { get; set; }
    public int EventId { get; set; }
    public Event Event { get; set; } = null!;

    [Required, StringLength(100)]
    public string Name { get; set; } = string.Empty;

    [Column(TypeName = "numeric(10,2)")]
    public decimal Price { get; set; }

    public DateTime? SalesStartAt { get; set; }
    public DateTime? SalesEndAt { get; set; }
    public bool IsMemberOnly { get; set; }
    public bool IsDoorPrice { get; set; }
    public bool IsActive { get; set; } = true;
    public int? QuantityAvailable { get; set; }
    public int DisplayOrder { get; set; }

    public ICollection<EventTicket> Tickets { get; set; } = new List<EventTicket>();
}
