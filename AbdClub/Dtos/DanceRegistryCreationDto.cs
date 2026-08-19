using System.ComponentModel.DataAnnotations;
using AbdClub.Pages.Officers.Dances;

namespace AbdClub.Dtos;

public class DanceRegistryCreationDto
{
    [Required(ErrorMessage = "Please provide a title for this social dance.")]
    public string Title { get; set; } = string.Empty;

    // 🌟 REFACTORED TRACKING FIELD: Captures the chosen primary key from the Master Locations table
    [Required(ErrorMessage = "Please select a venue from the lookup registry.")]
    [Range(1, int.MaxValue, ErrorMessage = "Please select a valid venue location.")]
    public int SelectedLocationId { get; set; }
    public LessonInputItem? AssignedLesson { get; set; }

    public string? Description { get; set; }
    public string? ContactEmail { get; set; }

    public DateOnly Date { get; set; }
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }

    public int? SelectedDjId { get; set; }
    public List<int> SelectedHostIds { get; set; } = new();
    public List<int> SelectedInstructorIds { get; set; } = new();
    public List<int> SelectedVolunteerIds { get; set; } = new();
    public List<int> SelectedOfficerIds { get; set; } = new();
}

