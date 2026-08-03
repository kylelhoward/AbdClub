namespace AbdClub.Dtos;

public class DanceInputDto
{
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ContactEmail { get; set; }
    public string Location { get; set; } = string.Empty;
    public DateOnly Date { get; set; } = DateOnly.FromDateTime(DateTime.Today);
    public TimeOnly StartTime { get; set; } = new TimeOnly(19, 0);
    public TimeOnly EndTime { get; set; } = new TimeOnly(22, 0);

    // Dynamic collection property to catch incoming checked IDs from the form submission
    public List<int> SelectedOfficerIds { get; set; } = new();
}