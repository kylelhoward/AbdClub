using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AbdClub.Data;
using AbdClub.Models;

namespace AbdClub.Pages.Officers.Locations;

public class DeleteModel : PageModel
{
    private readonly AbdContext _context;

    public DeleteModel(AbdContext context)
    {
        _context = context;
    }

    [BindProperty]
    public Location Location { get; set; } = default!;

    [TempData]
    public string? ErrorMessage { get; set; }

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id == null) return NotFound();

        var location = await _context.Locations.FirstOrDefaultAsync(m => m.Id == id);
        if (location == null) return NotFound();

        Location = location;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(int? id)
    {
        if (id == null) return NotFound();

        // 🌟 SAFETY AUDIT CHECK: Verify if any future dances are scheduled here
        var today = DateOnly.FromDateTime(DateTime.Today);
        bool hasActiveDances = await _context.Events
            .AnyAsync(e => e.LocationId == id && e.Date >= today);

        if (hasActiveDances)
        {
            // Abort the deletion, cache the message, and reload the warning display layout
            ErrorMessage = "CRITICAL BLOCK: This venue cannot be deleted. It has active or upcoming dances currently linked to its calendar index.";
            return RedirectToPage("./Index");
        }

        var location = await _context.Locations.FindAsync(id);
        if (location != null)
        {
            Location = location;
            _context.Locations.Remove(Location);
            await _context.SaveChangesAsync();
        }

        return RedirectToPage("./Index");
    }
}
