using System.ComponentModel.DataAnnotations;

namespace AbdClub.Pages.Officers.Dances;

public class LessonCreationItem
{
    [Required]
    public int InstructorId { get; set; }

    [Required]
    public string Type { get; set; } = string.Empty;
    public TimeOnly StartTime { get; set; } = new TimeOnly(19, 0);
    public TimeOnly EndTime { get; set; } = new TimeOnly(20, 0);
}
