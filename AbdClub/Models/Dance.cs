namespace AbdClub.Models;

public class Dance : Event
{
    public ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
    public ICollection<Member> AttendingOfficers { get; set; } = new List<Member>();

    // Single DJ assigned from lookup registry
    public int? AssignedDjId { get; set; }
    public MasterDJ? AssignedDj { get; set; }

    // Reusable Many-to-Many schedules linking to lookups
    public ICollection<MasterHost> AssignedHosts { get; set; } = new List<MasterHost>();
    public ICollection<MasterInstructor> AssignedInstructors { get; set; } = new List<MasterInstructor>();
    public ICollection<MasterVolunteer> AssignedVolunteers { get; set; } = new List<MasterVolunteer>();

}
