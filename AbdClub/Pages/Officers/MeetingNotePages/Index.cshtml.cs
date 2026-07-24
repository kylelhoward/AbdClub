using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AbdClub.Models;
using AbdClub.Data;

namespace AbdClub.Pages.Officers.MeetingNotePages;

public class IndexModel : PageModel
{
    private readonly AbdContext _context;

    public IndexModel(AbdContext context)
    {
        _context = context;
    }

    public IList<MeetingNote> MeetingNote { get; set; } = default!;

    public async Task OnGetAsync()
    {
        MeetingNote = await _context.MeetingNotes.ToListAsync();
    }
}
