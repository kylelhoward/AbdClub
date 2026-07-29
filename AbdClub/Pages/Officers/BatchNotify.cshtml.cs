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

    [BindProperty]
    public BroadcastInputDto NotificationData { get; set; } = new();

    public int TotalActiveMembersCount { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        bool isAuthorizedOfficer = User.IsInRole("Officer") &&
                                  User.FindFirst("OfficerRole")?.Value == "Tech Sergeant Chen";
        if (!isAuthorizedOfficer) return Forbid();

        TotalActiveMembersCount = await _context.Members.CountAsync(m => m.IsActive);

        // Fetch the 10 most recent announcements with the related officer info
        PastAnnouncements = await _context.BroadcastAuditLogs
            .Include(a => a.SentByOfficer)
            .OrderByDescending(a => a.SentAt)
            .Take(10)
            .ToListAsync();

        return Page();
    }

    public async Task<IActionResult> OnPostSendBroadcastAsync()
    {
        bool isAuthorizedOfficer = User.IsInRole("Officer") &&
                                  User.FindFirst("OfficerRole")?.Value == "Tech Sergeant Chen";
        if (!isAuthorizedOfficer) return Forbid();

        if (!ModelState.IsValid)
        {
            TotalActiveMembersCount = await _context.Members.CountAsync(m => m.IsActive);
            return Page();
        }
        //  Resolve the sending Officer's primary database key from their custom cookie claim
        var officerIdClaim = User.FindFirst("MemberId")?.Value;
        if (string.IsNullOrEmpty(officerIdClaim) || !int.TryParse(officerIdClaim, out int loggedInOfficerId))
        {
            ModelState.AddModelError("", "Your login session context is invalid. Please sign out and sign back in.");
            TotalActiveMembersCount = await _context.Members.CountAsync(m => m.IsActive);
            return Page();
        }

        //  Fetch active targets directly into memory
        var targets = await _context.Members
            .Where(m => m.IsActive)
            .Select(m => new { m.Email, m.FullName })
            .ToListAsync();

        if (!targets.Any())
        {
            ModelState.AddModelError("", "There are no active club members to notify.");
            return Page();
        }
        //  AUDIT INSIGHT PIPELINE: Create and persist the audit record BEFORE starting the background thread
        var auditEntry = new BroadcastAuditLog
        {
            SentByOfficerId = loggedInOfficerId,
            Subject = NotificationData.Subject,
            MessageContent = NotificationData.MessageContent,
            RecipientCount = targets.Count,
            SentAt = DateTime.UtcNow
        };

        _context.BroadcastAuditLogs.Add(auditEntry);
        await _context.SaveChangesAsync(); // Generates a permanent database row
        //  DISPATCH BACKGROUND QUEUE: Offloads the intensive SMTP loop from the web runner thread
        // We capture references safely via lexical scope variables
        var subject = NotificationData.Subject;
        var messageContent = NotificationData.MessageContent;

        _ = Task.Run(async () =>
        {
            _logger.LogInformation("Officer initiated batch transmission queue for {Count} recipients.", targets.Count);

            foreach (var recipient in targets)
            {
                try
                {
                    await _emailService.SendBroadcastEmailAsync(recipient.Email, recipient.FullName, subject, messageContent);
                    // Add a tiny throttle delay to stay inside local smtp4dev connection safety bounds
                    await Task.Delay(150);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed background batch dispatch transmission block targeting {Email}", recipient.Email);
                }
            }

            _logger.LogInformation("Batch notification pipeline finalized successfully.");
        });

        TempData["GlobalSuccessNotice"] = $"Successfully queued broadcast notifications for {targets.Count} active members in the background.";
        return RedirectToPage();
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
