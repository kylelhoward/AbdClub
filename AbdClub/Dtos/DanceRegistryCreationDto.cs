namespace AbdClub.Dtos;

public class DanceRegistryCreationDto
{
    public string Title { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
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
