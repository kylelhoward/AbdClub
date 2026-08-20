using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AbdClub.Models;
using AbdClub.Data;

namespace AbdClub.Pages.EmailLogPages;

public class EditModel : PageModel
{
    private readonly AbdContext _context;

    public EditModel(AbdContext context)
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
        EmailLog = emaillog;
        return Page();
    }

    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see https://aka.ms/RazorPagesCRUD.
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        _context.Attach(EmailLog).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!EmailLogExists(EmailLog.Id))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        return RedirectToPage("./Index");
    }

    private bool EmailLogExists(int id)
    {
        return _context.EmailLogs.Any(e => e.Id == id);
    }
}
