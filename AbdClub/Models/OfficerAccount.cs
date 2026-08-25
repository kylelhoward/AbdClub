using System.ComponentModel.DataAnnotations;

namespace AbdClub.Models;

public enum OfficerAccessLevel
{
    Officer = 1,
    Admin = 2,
    TechAdmin = 3
}

public class OfficerAccount
{
    public int Id { get; set; }

    [Required, EmailAddress, StringLength(254)]
    public string Email { get; set; } = string.Empty;

    [StringLength(255)]
    public string? GoogleSubId { get; set; }

    public OfficerAccessLevel AccessLevel { get; set; } = OfficerAccessLevel.Officer;

    [StringLength(100)]
    public string? OfficerTitle { get; set; }

    public bool IsEnabled { get; set; } = true;
    public int? MemberId { get; set; }
    public Member? Member { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
