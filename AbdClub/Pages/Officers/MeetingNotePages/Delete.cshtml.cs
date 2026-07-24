using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AbdClub.Models;
using AbdClub.Data;

namespace AbdClub.Pages.Officers.MeetingNotePages;

public class DeleteModel : PageModel
{
    private readonly AbdContext _context;

    public DeleteModel(AbdContext context)
    {
        _context = context;
    }

    [BindProperty]
    public MeetingNote MeetingNote { get; set; } = default!;

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var meetingnote = await _context.MeetingNotes.FirstOrDefaultAsync(m => m.Id == id);
        if (meetingnote is null)
        {
            return NotFound();
        }
        else
        {
            MeetingNote = meetingnote;
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var meetingnote = await _context.MeetingNotes.FindAsync(id);
        if (meetingnote != null)
        {
            MeetingNote = meetingnote;
            _context.MeetingNotes.Remove(MeetingNote);
            await _context.SaveChangesAsync();
        }

        return RedirectToPage("./Index");
    }
}
