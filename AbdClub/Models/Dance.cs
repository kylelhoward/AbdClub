namespace AbdClub.Models;

public class Dance : Event
{
    // Inherits: Id, Title, Description, ContactEmail, Location, Date, StartTime, EndTime

    // Lessons associated with this dance
    public ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();

    // Single DJ record
    public DJ? Dj { get; set; }

    // Volunteers for the dance
    public ICollection<Volunteer> Volunteers { get; set; } = new List<Volunteer>();

    // Attending officers (many-to-many to Member)
    public ICollection<Member> AttendingOfficers { get; set; } = new List<Member>();
}
