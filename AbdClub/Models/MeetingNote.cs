namespace AbdClub.Models;

public class MeetingNote
{
    public int Id { get; set; }
    public int UploadedByMemberId { get; set; }
    public DateTime MeetingDate { get; set; }
    public string Title { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public Member UploadedBy { get; set; } = null!;
}