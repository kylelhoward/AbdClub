namespace AbdClub.Models;

public class Lesson
{
    public int Id { get; set; }

    public string Instructor { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;

    // Use TimeOnly for times (consistent with Event)
    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }

    // FK
    public int DanceId { get; set; }
    public Dance Dance { get; set; } = null!;
}
