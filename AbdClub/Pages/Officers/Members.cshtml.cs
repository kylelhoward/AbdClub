using AbdClub.Data;
using AbdClub.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;

namespace AbdClub.Pages.Officers;

[Authorize(Roles = "Officer")]
public class MembersModel : PageModel
{
    private readonly AbdContext _db;
    public MembersModel(AbdContext db) => _db = db;

    public List<Member> Members { get; set; } = new();
    public string Filter { get; set; } = "all";

    public async Task OnGetAsync(string filter = "all")
    {
        Filter = filter;
        var query = _db.Members.AsQueryable();

        query = filter switch
        {
            "active" => query.Where(m =>
                m.IsActive &&
                (
                    // treat null expiry as lifetime, or expiry in the future
                    m.ExpiryDate == null ||
                    m.ExpiryDate >= DateTime.UtcNow
                )
            ),
            "expiring" => query.Where(m =>
                m.ExpiryDate != null &&
                m.ExpiryDate <= DateTime.UtcNow.AddDays(60) &&
                m.ExpiryDate >= DateTime.UtcNow
            ),
            "expired" => query.Where(m =>
                m.ExpiryDate != null &&
                m.ExpiryDate < DateTime.UtcNow
            ),
            "officers" => query.Where(m => m.IsOfficer),
            _ => query
        };
        Members = await query
            .OrderBy(m => m.FullName)
            .ToListAsync();
    }
}