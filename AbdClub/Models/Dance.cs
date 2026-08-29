namespace AbdClub.Models;

public class Dance : Event
{
    // 🌟 THE CLEAN 1:1 UPGRADE: Replaces the old ICollection<Lesson>
    public int? LessonId { get; set; }
    public Lesson? AssignedLesson { get; set; }
    // Dance hosts only apply to regular dances.
    public ICollection<MasterHost> AssignedHosts { get; set; } = new List<MasterHost>();
}
