namespace AbdClub.Models;

public class EmailLog
{
    public int Id { get; set; }
    public int MemberId { get; set; }
    public string EmailType { get; set; } = string.Empty;
    // EmailType values: Welcome, Reminder60, Reminder30, Reminder7, Expired, Broadcast
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public bool Opened { get; set; } = false;
    public string Subject { get; set; } = string.Empty;

    public Member Member { get; set; } = null!;
}