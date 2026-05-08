using AbdClub.Data;
using AbdClub.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AbdClub.Pages.Officers;

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
            "active" => query.Where(m => m.IsActive && m.ExpiryDate >= DateTime.UtcNow),
            "expiring" => query.Where(m => m.ExpiryDate <= DateTime.UtcNow.AddDays(60)
                                        && m.ExpiryDate >= DateTime.UtcNow),
            "expired" => query.Where(m => m.ExpiryDate < DateTime.UtcNow),
            "officers" => query.Where(m => m.IsOfficer),
            _ => query
        };

        Members = await query
            .OrderBy(m => m.FullName)
            .ToListAsync();
    }
}