using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AbdClub.Models;

public abstract class Event
{
    public int Id { get; set; }

    [Required]
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ContactEmail { get; set; }

    public DateOnly Date { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }

    // 🌟 UNIFIED TPH RELATIONSHIP ATTACHMENT
    [Required]
    public int LocationId { get; set; }

    [ForeignKey("LocationId")]
    public Location Location { get; set; } = null!;

    // Staffing shared by regular dances, special events, and outings.
    public ICollection<Member> AttendingOfficers { get; set; } = new List<Member>();
    public ICollection<MasterVolunteer> AssignedVolunteers { get; set; } = new List<MasterVolunteer>();

    // Entertainment is optional. The existing MasterDJ table now represents DJs and bands.
    public int? AssignedDjId { get; set; }
    public MasterDJ? AssignedDj { get; set; }

    public ICollection<EventTicketType> TicketTypes { get; set; } = new List<EventTicketType>();
    public ICollection<EventTicketOrder> TicketOrders { get; set; } = new List<EventTicketOrder>();

    [NotMapped]
    public string EventTypeLabel => this switch
    {
        Dance => "Regular Dance",
        SpecialEvent => "Special Event",
        Outing => "Outing",
        _ => "Event"
    };
}
