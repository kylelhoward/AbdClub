namespace AbdClub.Models;

public class Dance : Event
{
    // 🌟 THE CLEAN 1:1 UPGRADE: Replaces the old ICollection<Lesson>
    public int? LessonId { get; set; }
    public Lesson? AssignedLesson { get; set; }
    public ICollection<Member> AttendingOfficers { get; set; } = new List<Member>();

    // Single DJ assigned from lookup registry
    public int? AssignedDjId { get; set; }
    public MasterDJ? AssignedDj { get; set; }

    // Reusable Many-to-Many schedules linking to lookups
    public ICollection<MasterHost> AssignedHosts { get; set; } = new List<MasterHost>();
    public ICollection<MasterVolunteer> AssignedVolunteers { get; set; } = new List<MasterVolunteer>();

}
