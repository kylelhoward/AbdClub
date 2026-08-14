using System.ComponentModel.DataAnnotations;

namespace AbdClub.Pages.Officers.Dances;

// Unified DTO Form update container structures matching your advanced front-end view
public class DanceFormUpdateDto
{
    // NEW METRIC METRIC FIELDS
    [Required(ErrorMessage = "The dance event title cannot be empty.")]
    [StringLength(100, ErrorMessage = "Title must be under 100 characters.")]

    public string? ContactEmail { get; set; }

    [Required(ErrorMessage = "Event title is strictly required.")]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    [Required(ErrorMessage = "Please establish a scheduled date.")]
    public DateOnly Date { get; set; }

    [Required(ErrorMessage = "Please establish an operating start time.")]
    public TimeOnly StartTime { get; set; }

    [Required(ErrorMessage = "Please establish a scheduled wrap-up time.")]
    public TimeOnly EndTime { get; set; }
    
    // 🌟 ADD THIS KEY: Tracks the updated relational venue ID
    [Required(ErrorMessage = "You must assign an active location to this event.")]
    public int SelectedLocationId { get; set; }

    // Pre-existing structure mappings
    public int? SelectedDjId { get; set; }
    public List<int> SelectedOfficerIds { get; set; } = new();
    public List<int> SelectedHostIds { get; set; } = new();
    public List<int> SelectedInstructorIds { get; set; } = new();
    public List<int> SelectedVolunteerIds { get; set; } = new();
    public List<LessonInputItem> Lessons { get; set; } = new();
}
