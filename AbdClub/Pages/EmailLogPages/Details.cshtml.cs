using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AbdClub.Models;
using AbdClub.Data;

namespace AbdClub.Pages.EmailLogPages;

public class DetailsModel : PageModel
{
    private readonly AbdContext _context;
    public DetailsModel(AbdContext context)
    {
        _context = context;
    }

    public EmailLog EmailLog { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var emaillog = await _context.EmailLogs.FirstOrDefaultAsync(m => m.Id == id);
        if (emaillog is null)
        {
            return NotFound();
        }
        else
        {
            EmailLog = emaillog;
        }

        return Page();
    }
}
