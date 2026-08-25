using AbdClub.Data;
using AbdClub.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QRCoder;

namespace AbdClub.Pages;

public class AnnouncementsModel : PageModel
{
    private readonly AbdContext _context;

    public AnnouncementsModel(AbdContext context)
    {
        _context = context;
    }

    public AnnouncementFlyerSettings Flyer { get; private set; } = new();
    public List<Dance> UpcomingDances { get; private set; } = new();
    public List<Member> NewMembers { get; private set; } = new();
    public string MembershipQrSvg { get; private set; } = string.Empty;
    public string NewsletterQrSvg { get; private set; } = string.Empty;

    public async Task OnGetAsync()
    {
        Flyer = await _context.AnnouncementFlyerSettings
            .AsNoTracking()
            .Include(s => s.Announcements)
            .Include(s => s.HelpWantedItems)
            .SingleOrDefaultAsync(s => s.Id == AnnouncementFlyerSettings.CurrentSettingsId)
            ?? new AnnouncementFlyerSettings();

        var today = DateOnly.FromDateTime(DateTime.Today);
        UpcomingDances = await _context.Dances
            .AsNoTracking()
            .Include(d => d.Location)
            .Where(d => d.Date >= today)
            .OrderBy(d => d.Date)
            .ThenBy(d => d.StartTime)
            .Take(2)
            .ToListAsync();

        var newMemberCutoff = DateTime.UtcNow.AddDays(-30);
        NewMembers = await _context.Members
            .AsNoTracking()
            .Where(m => m.JoinDate >= newMemberCutoff && !m.IsSuspended)
            .OrderByDescending(m => m.JoinDate)
            .ThenBy(m => m.LastName)
            .Take(7)
            .ToListAsync();

        using var qrData = QRCodeGenerator.GenerateQrCode(
            Flyer.MembershipUrl,
            QRCodeGenerator.ECCLevel.Q);
        var qrCode = new SvgQRCode(qrData);
        MembershipQrSvg = qrCode.GetGraphic(5);

        using var newsletterQrData = QRCodeGenerator.GenerateQrCode(
            Flyer.NewsletterUrl,
            QRCodeGenerator.ECCLevel.Q);
        var newsletterQrCode = new SvgQRCode(newsletterQrData);
        NewsletterQrSvg = newsletterQrCode.GetGraphic(5);
    }
}
