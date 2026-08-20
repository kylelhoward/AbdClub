using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AbdClub.Data;
using AbdClub.Models;

namespace AbdClub.Pages.Admin.EmailLogs;

[Authorize(Roles = "Admin,TechAdmin")]
public class DetailsModel : PageModel
{
    private readonly AbdContext _db;

    public DetailsModel(AbdContext db)
    {
        _db = db;
    }

    public EmailLog? EmailRecord { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        EmailRecord = await _db.EmailLogs
            .Include(e => e.Member)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id);

        if (EmailRecord == null)
        {
            return NotFound();
        }

        return Page();
    }
}

