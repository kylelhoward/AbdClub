using AbdClub.Data;
using AbdClub.Models;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AbdClub.Pages.Admin.OfficerAccounts;

public class IndexModel(AbdContext db) : PageModel
{
    public List<OfficerAccount> Accounts { get; private set; } = new();

    public async Task OnGetAsync() => Accounts = await db.OfficerAccounts
        .AsNoTracking()
        .Include(a => a.Member)
        .OrderBy(a => a.Email)
        .ToListAsync();
}
