using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AbdClub.Models;
using AbdClub.Data;
using Microsoft.AspNetCore.Authorization;

namespace AbdClub.Pages.Officers.Meetings;

[Authorize(Policy = "isOfficer")]
public class DetailsModel : PageModel
{
    private readonly AbdContext _context;
    public DetailsModel(AbdContext context)
    {
        _context = context;
    }

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
}
