namespace AbdClub.Models;

public class Member
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? GoogleSubId { get; set; }
    public DateTime JoinDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public bool IsOfficer { get; set; } = false;
    public string? OfficerRole { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool SelfRegistered { get; set; } = false;
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public ICollection<EmailLog> EmailLogs { get; set; } = new List<EmailLog>();
}