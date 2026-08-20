using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AbdClub.Models;
using AbdClub.Data;

namespace AbdClub.Pages.EmailLogPages;

public class DeleteModel : PageModel
{
    private readonly AbdContext _context;

    public DeleteModel(AbdContext context)
    {
        _context = context;
    }

    [BindProperty]
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

    public async Task<IActionResult> OnPostAsync(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var emaillog = await _context.EmailLogs.FindAsync(id);
        if (emaillog != null)
        {
            EmailLog = emaillog;
            _context.EmailLogs.Remove(EmailLog);
            await _context.SaveChangesAsync();
        }

        return RedirectToPage("./Index");
    }
}
