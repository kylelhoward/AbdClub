using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AbdClub.Data;
using AbdClub.Models;

namespace AbdClub.Pages.Officers.Newsletter;

public class IndexModel : PageModel
{
    private readonly AbdContext _context;
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(AbdContext context, ILogger<IndexModel> logger)
    {
        _context = context;
        _logger = logger;
    }

    public List<NewsletterSubscriber> Subscribers { get; set; } = new();
    public int TotalSubscribersCount { get; set; }
    public bool IsAuthorizedOfficer { get; set; }

    [TempData]
    public string? StatusMessage { get; set; }

    // 1. Initial Load: Fetch Lists & Metadata Statistics
    public async Task<IActionResult> OnGetAsync()
    {
        // Explicit Security Identity Check Gate
        IsAuthorizedOfficer = User.IsInRole("Officer") &&
                              User.FindFirst("OfficerRole")?.Value == "Tech Sergeant Chen";

        if (!IsAuthorizedOfficer)
        {
            return Forbid();
        }

        await LoadDashboardDataAsync();
        return Page();
    }

    // 2. Action Handler: Administrative Unsubscribe execution
    public async Task<IActionResult> OnPostUnsubscribeAsync(int id)
    {
        // Server-Side Defense in Depth Verification
        bool isAuthorized = User.IsInRole("Officer") &&
                            User.FindFirst("OfficerRole")?.Value == "Tech Sergeant Chen";

        if (!isAuthorized) return Forbid();

        var subscriber = await _context.NewsletterSubscribers.FindAsync(id);
        if (subscriber == null)
        {
            StatusMessage = "Error: Subscriber record could not be found.";
            return RedirectToPage();
        }

        // Execute row purging deletion tracking patterns
        _context.NewsletterSubscribers.Remove(subscriber);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Officer 'Tech Sergeant Chen' manually unsubscribed {Email} (ID: {Id}).", subscriber.Email, id);

        StatusMessage = $"Successfully unsubscribed {subscriber.FirstName} ({subscriber.Email}) from the public newsletter list.";
        return RedirectToPage();
    }

    private async Task LoadDashboardDataAsync()
    {
        TotalSubscribersCount = await _context.NewsletterSubscribers.CountAsync();

        Subscribers = await _context.NewsletterSubscribers
            .OrderByDescending(s => s.SubscribedAt)
            .ToListAsync();
    }
}
