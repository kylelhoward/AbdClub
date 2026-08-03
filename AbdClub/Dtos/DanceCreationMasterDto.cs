using AbdClub.Pages.Officers.Dances;

namespace AbdClub.Dtos;

// Master DTO payload pattern structures
public class DanceCreationMasterDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ContactEmail { get; set; }
    public string Location { get; set; } = string.Empty;
    public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public TimeOnly StartTime { get; set; } = new TimeOnly(19, 0);
    public TimeOnly EndTime { get; set; } = new TimeOnly(22, 0);

    public List<int> SelectedOfficerIds { get; set; } = new();

    public string? DjName { get; set; }
    public string? DjEmail { get; set; }

    public List<LessonCreationItem> Lessons { get; set; } = new();
}
