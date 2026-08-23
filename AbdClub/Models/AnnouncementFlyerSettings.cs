using System.ComponentModel.DataAnnotations;

namespace AbdClub.Models;

/// <summary>
/// The single, current set of editable announcement-flyer settings.
/// This is intentionally not an archive of prior flyers.
/// </summary>
public class AnnouncementFlyerSettings
{
    public const int CurrentSettingsId = 1;

    public int Id { get; set; } = CurrentSettingsId;

    [Required, StringLength(200)]
    public string Greeting { get; set; } = "Thank you for coming tonight!";

    [Required, Url, StringLength(1024)]
    public string MembershipUrl { get; set; } =
        "https://www.danceatx.org/store/annual-membership-1";

    [Required, Url, StringLength(1024)]
    public string WebsiteUrl { get; set; } = "https://www.danceatx.org/";

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<FlyerAnnouncementItem> Announcements { get; set; } =
        new List<FlyerAnnouncementItem>();

    public ICollection<HelpWantedItem> HelpWantedItems { get; set; } =
        new List<HelpWantedItem>();
}

public class FlyerAnnouncementItem
{
    public int Id { get; set; }

    public int AnnouncementFlyerSettingsId { get; set; }
    public AnnouncementFlyerSettings AnnouncementFlyerSettings { get; set; } = null!;

    [Required, StringLength(500)]
    public string Text { get; set; } = string.Empty;

    public int DisplayOrder { get; set; }
}

public class HelpWantedItem
{
    public int Id { get; set; }

    public int AnnouncementFlyerSettingsId { get; set; }
    public AnnouncementFlyerSettings AnnouncementFlyerSettings { get; set; } = null!;

    [Required, StringLength(120)]
    public string Title { get; set; } = string.Empty;

    [StringLength(300)]
    public string? Description { get; set; }

    public int DisplayOrder { get; set; }
}
