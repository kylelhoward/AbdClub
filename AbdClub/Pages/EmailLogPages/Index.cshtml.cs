using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AbdClub.Models;
using AbdClub.Data;

namespace AbdClub.Pages.EmailLogPages;

public class IndexModel : PageModel
{
    private readonly AbdContext _context;

    public IndexModel(AbdContext context)
    {
        _context = context;
    }

    public IList<EmailLog> EmailLog { get; set; } = default!;

    public async Task OnGetAsync()
    {
        EmailLog = await _context.EmailLogs.ToListAsync();
    }
}
