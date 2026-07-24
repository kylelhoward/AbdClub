using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AbdClub.Models;
using AbdClub.Data;

namespace AbdClub.Pages.Officers.MeetingNotePages;

public class CreateModel : PageModel
{
    private readonly AbdContext _context;

    public CreateModel(AbdContext context)
    {
        _context = context;
    }

    public IActionResult OnGet()
    {
        return Page();
    }

    [BindProperty]
    public MeetingNote MeetingNote { get; set; } = default!;

    // To protect from overposting attacks, see https://aka.ms/RazorPagesCRUD.
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        _context.MeetingNotes.Add(MeetingNote);
        await _context.SaveChangesAsync();

        return RedirectToPage("./Index");
    }
}
