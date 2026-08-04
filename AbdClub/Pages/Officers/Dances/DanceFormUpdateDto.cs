using System.ComponentModel.DataAnnotations;

namespace AbdClub.Pages.Officers.Dances;

// Unified DTO Form update container structures matching your advanced front-end view
public class DanceFormUpdateDto
{
    // NEW METRIC METRIC FIELDS
    [Required(ErrorMessage = "The dance event title cannot be empty.")]
    [StringLength(100, ErrorMessage = "Title must be under 100 characters.")]
    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    // Pre-existing structure mappings
    public int? SelectedDjId { get; set; }
    public List<int> SelectedOfficerIds { get; set; } = new();
    public List<int> SelectedHostIds { get; set; } = new();
    public List<int> SelectedInstructorIds { get; set; } = new();
    public List<int> SelectedVolunteerIds { get; set; } = new();
    public List<LessonInputItem> Lessons { get; set; } = new();
}
