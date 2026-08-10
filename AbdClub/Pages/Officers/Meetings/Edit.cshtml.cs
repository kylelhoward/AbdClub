using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AbdClub.Models;
using AbdClub.Data;
using Microsoft.AspNetCore.Authorization;

namespace AbdClub.Pages.Officers.Meetings;

[Authorize(Policy = "isAdmin")]
public class EditModel : PageModel
{
    private readonly AbdContext _context;

    public EditModel(AbdContext context)
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
        MeetingNote = meetingnote;
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

        _context.Attach(MeetingNote).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!MeetingNoteExists(MeetingNote.Id))
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

    private bool MeetingNoteExists(int id)
    {
        return _context.MeetingNotes.Any(e => e.Id == id);
    }
}
