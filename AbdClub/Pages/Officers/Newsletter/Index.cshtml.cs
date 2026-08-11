using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AbdClub.Data;
using AbdClub.Models;
using Microsoft.AspNetCore.Authorization;

namespace AbdClub.Pages.Officers.Newsletter;

[Authorize(Policy = "isOfficer")]
public class IndexModel(AbdContext context, ILogger<IndexModel> logger,IAuthorizationService authorizationService) : PageModel
{
    private readonly IAuthorizationService _authorizationService = authorizationService;
    private readonly AbdContext _context = context;
    private readonly ILogger<IndexModel> _logger = logger;

    public List<NewsletterSubscriber> Subscribers { get; set; } = new();
    public int TotalSubscribersCount { get; set; }
    public bool IsAuthorizedOfficer { get; set; }

    [TempData]
    public string? StatusMessage { get; set; }

    // 1. Initial Load: Fetch Lists & Metadata Statistics
    public async Task<IActionResult> OnGetAsync()
    {
        // 🌟 EVALUATE THE CENTRALIZED "isAdmin" POLICY DIRECTLY
        var authResult = await _authorizationService.AuthorizeAsync(User, null, "isOfficer");

        if (!authResult.Succeeded)
        {
            return Forbid(); // Blocks lower-level officers automatically
        }

        await LoadDashboardDataAsync();
        return Page();
    }

    // 2. Action Handler: Administrative Unsubscribe execution
    [Authorize(Policy = "isAdmin")]
    public async Task<IActionResult> OnPostUnsubscribeAsync(int id)
    {
        // 🌟 EVALUATE THE CENTRALIZED "isAdmin" POLICY DIRECTLY
        var authResult = await _authorizationService.AuthorizeAsync(User, null, "isAdmin");

        if (!authResult.Succeeded)
        {
            return Forbid(); // Blocks lower-level officers automatically
        }

        var subscriber = await _context.NewsletterSubscribers.FindAsync(id);
        if (subscriber == null)
        {
            StatusMessage = "Error: Subscriber record could not be found.";
            return RedirectToPage();
        }

        // Execute row purging deletion tracking patterns
        _context.NewsletterSubscribers.Remove(subscriber);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Officer 'Admin' manually unsubscribed {Email} (ID: {Id}).", subscriber.Email, id);

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
