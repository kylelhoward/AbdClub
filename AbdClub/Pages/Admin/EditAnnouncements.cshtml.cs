using System.ComponentModel.DataAnnotations;
using AbdClub.Data;
using AbdClub.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AbdClub.Pages.Admin;

[Authorize(Policy = "isAdmin")]
public class EditAnnouncementsModel : PageModel
{
    private readonly AbdContext _context;

    public EditAnnouncementsModel(AbdContext context)
    {
        _context = context;
    }

    [BindProperty]
    public FlyerInput Input { get; set; } = new();

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task OnGetAsync()
    {
        var settings = await _context.AnnouncementFlyerSettings
            .AsNoTracking()
            .Include(s => s.Announcements)
            .Include(s => s.HelpWantedItems)
            .SingleOrDefaultAsync(s => s.Id == AnnouncementFlyerSettings.CurrentSettingsId)
            ?? new AnnouncementFlyerSettings();

        Input = FlyerInput.FromSettings(settings);
    }

    public async Task<IActionResult> OnPostAsync()
    {
        Input.Announcements ??= new();
        Input.HelpWantedItems ??= new();

        if (!ModelState.IsValid)
            return Page();

        var settings = await _context.AnnouncementFlyerSettings
            .Include(s => s.Announcements)
            .Include(s => s.HelpWantedItems)
            .SingleOrDefaultAsync(s => s.Id == AnnouncementFlyerSettings.CurrentSettingsId);

        if (settings == null)
        {
            settings = new AnnouncementFlyerSettings
            {
                Id = AnnouncementFlyerSettings.CurrentSettingsId
            };
            _context.AnnouncementFlyerSettings.Add(settings);
        }

        settings.Greeting = Input.Greeting.Trim();
        settings.MembershipUrl = Input.MembershipUrl.Trim();
        settings.WebsiteUrl = Input.WebsiteUrl.Trim();
        settings.UpdatedAt = DateTime.UtcNow;

        _context.FlyerAnnouncementItems.RemoveRange(settings.Announcements);
        _context.HelpWantedItems.RemoveRange(settings.HelpWantedItems);

        settings.Announcements = Input.Announcements
            .Where(i => !string.IsNullOrWhiteSpace(i.Text))
            .Select((i, index) => new FlyerAnnouncementItem
            {
                Text = i.Text.Trim(),
                DisplayOrder = index
            })
            .ToList();

        settings.HelpWantedItems = Input.HelpWantedItems
            .Where(i => !string.IsNullOrWhiteSpace(i.Title))
            .Select((i, index) => new HelpWantedItem
            {
                Title = i.Title.Trim(),
                Description = string.IsNullOrWhiteSpace(i.Description)
                    ? null
                    : i.Description.Trim(),
                DisplayOrder = index
            })
            .ToList();

        await _context.SaveChangesAsync();

        StatusMessage = "The announcement flyer was updated.";
        return RedirectToPage();
    }

    public class FlyerInput
    {
        [Required, StringLength(200)]
        [Display(Name = "Greeting")]
        public string Greeting { get; set; } = "Thank you for coming tonight!";

        [Required, Url, StringLength(1024)]
        [Display(Name = "Membership sign-up URL")]
        public string MembershipUrl { get; set; } =
            "https://www.danceatx.org/store/annual-membership-1";

        [Required, Url, StringLength(1024)]
        [Display(Name = "ABD website URL")]
        public string WebsiteUrl { get; set; } = "https://www.danceatx.org/";

        public List<AnnouncementInput> Announcements { get; set; } = new();
        public List<HelpWantedInput> HelpWantedItems { get; set; } = new();

        public static FlyerInput FromSettings(AnnouncementFlyerSettings settings) => new()
        {
            Greeting = settings.Greeting,
            MembershipUrl = settings.MembershipUrl,
            WebsiteUrl = settings.WebsiteUrl,
            Announcements = settings.Announcements
                .OrderBy(i => i.DisplayOrder)
                .Select(i => new AnnouncementInput { Text = i.Text })
                .ToList(),
            HelpWantedItems = settings.HelpWantedItems
                .OrderBy(i => i.DisplayOrder)
                .Select(i => new HelpWantedInput
                {
                    Title = i.Title,
                    Description = i.Description
                })
                .ToList()
        };
    }

    public class AnnouncementInput
    {
        [StringLength(500)]
        public string? Text { get; set; }
    }

    public class HelpWantedInput
    {
        [StringLength(120)]
        public string? Title { get; set; }

        [StringLength(300)]
        public string? Description { get; set; }
    }
}
