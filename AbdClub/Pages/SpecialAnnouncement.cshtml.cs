using AbdClub.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AbdClub.Pages;

public class SpecialAnnouncementModel(AbdContext context) : PageModel
{
    public string? AnnouncementTitle { get; private set; }
    public string? ContentType { get; private set; }
    public bool HasAnnouncement { get; private set; }
    public bool IsPdf => string.Equals(ContentType, "application/pdf", StringComparison.OrdinalIgnoreCase);

    public async Task OnGetAsync()
    {
        var announcement = await context.SpecialAnnouncements
            .AsNoTracking()
            .Where(a => a.Id == Models.SpecialAnnouncement.CurrentAnnouncementId && a.IsPublished)
            .Select(a => new { a.Title, a.ContentType })
            .SingleOrDefaultAsync();

        HasAnnouncement = announcement != null;
        AnnouncementTitle = announcement?.Title;
        ContentType = announcement?.ContentType;
    }

    public async Task<IActionResult> OnGetFileAsync()
    {
        var announcement = await GetPublishedFileAsync();
        if (announcement == null)
            return NotFound();

        Response.Headers["Content-Disposition"] = "inline";
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        return File(announcement.FileData, announcement.ContentType);
    }

    public async Task<IActionResult> OnGetDownloadAsync()
    {
        var announcement = await GetPublishedFileAsync();
        if (announcement == null)
            return NotFound();

        Response.Headers["X-Content-Type-Options"] = "nosniff";
        return File(announcement.FileData, announcement.ContentType, announcement.OriginalFileName);
    }

    private Task<Models.SpecialAnnouncement?> GetPublishedFileAsync() =>
        context.SpecialAnnouncements
            .AsNoTracking()
            .SingleOrDefaultAsync(a =>
                a.Id == Models.SpecialAnnouncement.CurrentAnnouncementId && a.IsPublished);
}
