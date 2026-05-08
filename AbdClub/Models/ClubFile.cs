namespace AbdClub.Models;

public class ClubFile
{
    public int Id { get; set; }
    public int UploadedByMemberId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    // Category values: Bylaws, Forms, Flyers, Other
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public Member UploadedBy { get; set; } = null!;
}