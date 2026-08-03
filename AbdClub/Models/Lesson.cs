
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AbdClub.Models;

public class Lesson
{
    public int Id { get; set; }

    [Required]
    public string Type { get; set; } = string.Empty;

    public TimeOnly StartTime { get; set; }
    public TimeOnly EndTime { get; set; }

    // REFACTORED RELATIONSHIP: Links directly into your new Master Registry table
    [Required]
    public int InstructorId { get; set; }

    [ForeignKey("InstructorId")]
    public MasterInstructor Instructor { get; set; } = null!;

    // Event Timeline Constraints
    [Required]
    public int DanceId { get; set; }

    [ForeignKey("DanceId")]
    public Dance Dance { get; set; } = null!;
}

