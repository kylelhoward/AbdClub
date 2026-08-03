namespace AbdClub.Pages.Officers.Dances;

// Unified DTO Form update container structures matching your advanced front-end view
public class DanceFormUpdateDto
{
    public int? SelectedDjId { get; set; }
    public List<int> SelectedOfficerIds { get; set; } = new();
    public List<int> SelectedHostIds { get; set; } = new();
    public List<int> SelectedInstructorIds { get; set; } = new();
    public List<int> SelectedVolunteerIds { get; set; } = new();
    public List<LessonInputItem> Lessons { get; set; } = new();
}
