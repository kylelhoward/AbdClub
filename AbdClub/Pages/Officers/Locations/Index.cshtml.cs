using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AbdClub.Models;
using AbdClub.Data;

namespace AbdClub.Pages.Officers.Locations;

public class IndexModel : PageModel
{
    private readonly AbdContext _context;
    [TempData]
    public string? ErrorMessage { get; set; }
    public IndexModel(AbdContext context)
    {
        _context = context;
    }

    public IList<Location> Location { get; set; } = default!;

    public async Task OnGetAsync()
    {
        Location = await _context.Locations.ToListAsync();
    }
}
