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
}

