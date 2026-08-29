using AbdClub.Data;
using AbdClub.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace AbdClub.Pages.Officers.Members;

[Authorize(Policy = "isOfficer")]
public class MembersListModel(AbdContext db, IAuthorizationService authorizationService) : PageModel
{
    private readonly AbdContext _db = db;
    private readonly IAuthorizationService _authorizationService = authorizationService;

    public List<Member> Members { get; set; } = new();
    public string Filter { get; set; } = "all";

    public async Task OnGetAsync(string filter = "all", string? q = null)
    {
        Filter = filter;
        var query = _db.Members.AsQueryable();

        // Apply search first
        if (!string.IsNullOrWhiteSpace(q))
        {
            var searchText = q.Trim();
            var normalizedText = searchText.ToLower();

            var memberNumberText = searchText.StartsWith(
                "ABD-",
                StringComparison.OrdinalIgnoreCase)
                    ? searchText[3..]
                    : searchText;

            var isMemberNumber =
                int.TryParse(memberNumberText, out var memberNumber);

            query = query.Where(m =>
              m.LastName.ToLower().Contains(normalizedText) ||
              m.FirstName.ToLower().Contains(normalizedText) ||
              (m.Email != null &&
               m.Email.ToLower().Contains(normalizedText)) ||
              (isMemberNumber && m.MemberNumber == memberNumber));
        }

        // 🌟 SERVER-SIDE REALIGNMENT: Bypasses the unmapped IsActive parameter natively
        query = filter switch
        {
            // A member is genuinely active if they are not administratively suspended and their date is in the future
            "suspended" => query.Where(m =>
                m.IsSuspended),

            // A member is genuinely active if they are not administratively suspended and their date is in the future
            "active" => query.Where(m =>
                !m.IsSuspended &&
                m.ExpiryDate.HasValue &&
                m.ExpiryDate.Value >= DateTime.UtcNow),

            // Expiring within a 60-day warning horizon window
            "expiring" => query.Where(m =>
     !m.IsSuspended &&
     m.ExpiryDate.HasValue &&
     m.ExpiryDate.Value.Date >= DateTime.UtcNow.Date &&
     m.ExpiryDate.Value.Date <= DateTime.UtcNow.Date.AddDays(60)),

            // Lapsed or physically expired chronologically
            "expired" => query.Where(m =>
                m.ExpiryDate.HasValue &&
                m.ExpiryDate.Value < DateTime.UtcNow),

            _ => query
        };

        // This query execution will now compile and fetch cleanly into PostgreSQL without any translation errors!
        Members = await query
            .OrderBy(m => m.LastName)
            .ToListAsync();

    }
}
