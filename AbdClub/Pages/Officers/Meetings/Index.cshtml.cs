using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AbdClub.Models;
using AbdClub.Data;
using Microsoft.AspNetCore.Authorization;

namespace AbdClub.Pages.Officers.Meetings;

[Authorize(Policy = "isOfficer")]

public class IndexModel(AbdContext context, IAuthorizationService authorizationService) : PageModel
{
    private readonly AbdContext _context = context;
    private readonly IAuthorizationService _authorizationService = authorizationService;

    public IList<MeetingNote> MeetingNote { get; set; } = default!;

    public async Task OnGetAsync()
    {
        MeetingNote = await _context.MeetingNotes.ToListAsync();
    }
}
