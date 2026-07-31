using AbdClub.Data;
using AbdClub.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AbdClub.Models;
using AbdClub.Dtos;

namespace AbdClub.Pages.Officers;

public class BatchNotifyModel : PageModel
{
    private readonly AbdContext _context;
    private readonly IEmailService _emailService;
    private readonly ILogger<BatchNotifyModel> _logger;
    public List<BroadcastAuditLog> PastAnnouncements { get; set; } = new();
    public BatchNotifyModel(AbdContext context, IEmailService emailService, ILogger<BatchNotifyModel> logger)
    {
        _context = context;
        _emailService = emailService;
        _logger = logger;
    }
public int MembersCount { get; set; }
    public int OfficersCount { get; set; }
    public int EveryoneCount { get; set; }

    [BindProperty]
    public BroadcastInputDto NotificationData { get; set; } = new();

    public int TotalActiveMembersCount { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        bool isAuthorizedOfficer = User.IsInRole("Officer") &&
                                  User.FindFirst("OfficerRole")?.Value == "Tech Sergeant Chen";
        if (!isAuthorizedOfficer) return Forbid();

        await LoadAudienceCountsAndLedgerAsync();
        return Page();
    }
    private async Task LoadAudienceCountsAndLedgerAsync()
    {
        // Compute individual counts natively inside the database layer
        MembersCount = await _context.Members.CountAsync(m => m.IsActive);
        OfficersCount = await _context.Members.CountAsync(m => m.IsActive && m.IsOfficer);

        var uniquelySubscribedEmailsCount = await _context.NewsletterSubscribers
            .Select(s => s.Email.ToLower())
            .Distinct()
            .CountAsync();

        // Calculate deduplicated total for "Everyone"
        EveryoneCount = MembersCount + uniquelySubscribedEmailsCount;

        // Fetch recent history logs
        PastAnnouncements = await _context.BroadcastAuditLogs
            .Include(a => a.SentByOfficer)
            .OrderByDescending(a => a.SentAt)
            .Take(10)
            .ToListAsync();
    }
    public async Task<IActionResult> OnPostSendBroadcastAsync()
    {
        bool isAuthorizedOfficer = User.IsInRole("Officer") && 
                                  User.FindFirst("OfficerRole")?.Value == "Tech Sergeant Chen";
        if (!isAuthorizedOfficer) return Forbid();

        if (!ModelState.IsValid)
        {
            await RefreshPageDataAsync();
            return Page();
        }

        var officerIdClaim = User.FindFirst("MemberId")?.Value;
        if (string.IsNullOrEmpty(officerIdClaim) || !int.TryParse(officerIdClaim, out int loggedInOfficerId))
        {
            ModelState.AddModelError("", "Your login session context is invalid.");
            await RefreshPageDataAsync();
            return Page();
        }

        // 1. Build an internal container to collect unique email targets
        var targets = new List<RecipientDetails>();

        switch (NotificationData.TargetAudience)
        {
            case "MembersOnly":
                targets = await _context.Members
                    .Where(m => m.IsActive)
                    .Select(m => new RecipientDetails { Email = m.Email, FullName = m.FullName })
                    .ToListAsync();
                break;

            case "Everyone":
                // Fetch active members
                var members = await _context.Members
                    .Where(m => m.IsActive)
                    .Select(m => new RecipientDetails { Email = m.Email, FullName = m.FullName})
                    .ToListAsync();

                // Fetch newsletter subscribers
                var subscribers = await _context.NewsletterSubscribers
                    .Select(s => new RecipientDetails { Email = s.Email, FullName = s.FirstName })
                    .ToListAsync();

                // Merge and deduplicate by email address to ensure nobody gets double-emailed if they are in both tables
                targets = members.Concat(subscribers)
                    .GroupBy(t => t.Email.ToLowerInvariant())
                    .Select(g => g.First())
                    .ToList();
                break;

            case "OfficersOnly":
                targets = await _context.Members
                    .Where(m => m.IsActive && m.IsOfficer) // Filters explicitly for active officer flags
                    .Select(m => new RecipientDetails { Email = m.Email, FullName = m.FullName })
                    .ToListAsync();
                break;

            default:
                ModelState.AddModelError("NotificationData.TargetAudience", "Invalid recipient audience selection.");
                await RefreshPageDataAsync();
                return Page();
        }

        if (!targets.Any())
        {
            ModelState.AddModelError("", "The selected target audience contains zero valid recipients.");
            await RefreshPageDataAsync();
            return Page();
        }

        // 2. Persist the record inside your Database Ledger
        var auditEntry = new BroadcastAuditLog
        {
            SentByOfficerId = loggedInOfficerId,
            Subject = $"[{NotificationData.TargetAudience}] {NotificationData.Subject}", // Appends context tag
            MessageContent = NotificationData.MessageContent,
            RecipientCount = targets.Count,
            SentAt = DateTime.UtcNow
        };

        _context.BroadcastAuditLogs.Add(auditEntry);
        await _context.SaveChangesAsync();

        // 3. Dispatch the Background Task Loop
        var subject = NotificationData.Subject;
        var messageContent = NotificationData.MessageContent;

        _ = Task.Run(async () =>
        {
            foreach (var recipient in targets)
            {
                try
                {
                    await _emailService.SendBroadcastEmailAsync(recipient.Email, recipient.FullName, subject, messageContent);
                    await Task.Delay(150); 
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed background broadcast targeting {Email}", recipient.Email);
                }
            }
        });

        TempData["GlobalSuccessNotice"] = $"Successfully logged entry and queued broadcast to {targets.Count} recipients via the {NotificationData.TargetAudience} filter channel.";
        return RedirectToPage();
    }

    private async Task RefreshPageDataAsync()
    {
        TotalActiveMembersCount = await _context.Members.CountAsync(m => m.IsActive);
        PastAnnouncements = await _context.BroadcastAuditLogs
            .Include(a => a.SentByOfficer)
            .OrderByDescending(a => a.SentAt)
            .Take(10)
            .ToListAsync();
    }


      // NEW PREVIEW HANDLER PIPELINE (Invoked via AJAX)
    public IActionResult OnPostPreviewLayout([FromBody] PreviewRequestDto request)
    {
        // Enforce role-based access identity gate
        bool isAuthorizedOfficer = User.IsInRole("Officer") &&
                                  User.FindFirst("OfficerRole")?.Value == "Tech Sergeant Chen";
        if (!isAuthorizedOfficer) return Forbid();

        if (string.IsNullOrEmpty(request.MessageBody))
        {
            return Content("<p class='text-danger'>Message body content is empty. Type a note before previewing.</p>", "text/html");
        }

        // Generate the formatted layout template using a sample placeholder recipient
        string previewHtml = _emailService.GenerateBroadcastHtmlBody("John Doe (Sample Member)", request.MessageBody);

        // Return raw html text straight to the client pipeline
        return Content(previewHtml, "text/html");
    }
}

// Auxiliary structures
public class RecipientDetails
{
    public string Email { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
}
