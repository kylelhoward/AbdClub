namespace AbdClub.Models;

public class Volunteer
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Email { get; set; }

    // FK
    public int DanceId { get; set; }
    public Dance Dance { get; set; } = null!;
}
