using System.ComponentModel.DataAnnotations;

namespace AbdClub.Models;

/// <summary>
/// Stores the single officer-managed announcement displayed on the public site.
/// The file is kept in PostgreSQL so it survives application deployments.
/// </summary>
public class SpecialAnnouncement
{
    public const int CurrentAnnouncementId = 1;

    public int Id { get; set; } = CurrentAnnouncementId;

    [StringLength(150)]
    public string? Title { get; set; }

    [Required, StringLength(255)]
    public string OriginalFileName { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string ContentType { get; set; } = string.Empty;

    [Required]
    public byte[] FileData { get; set; } = [];

    public bool IsPublished { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public int UploadedByOfficerAccountId { get; set; }
    public OfficerAccount UploadedByOfficerAccount { get; set; } = null!;
}
