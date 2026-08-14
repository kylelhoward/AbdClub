using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AbdClub.Data;
using AbdClub.Models;

namespace AbdClub.Pages.Members.Dances;

public class IndexModel : PageModel
{
    private readonly AbdContext _context;

    public IndexModel(AbdContext context) => _context = context;

    public List<Dance> ActiveSchedule { get; set; } = new();
    public string CurrentMemberName { get; set; } = string.Empty;

    [TempData]
    public string? VolunteerNotice { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        // Verify your backend EF Core collection query loop contains these explicit Includes:
        ActiveSchedule = await _context.Events
            .OfType<Dance>()
            .Include(d => d.AssignedDj) // Crucial to prevent DJ errors
            .Include(d => d.Lessons)
                .ThenInclude(l => l.Instructor) // Crucial to prevent instructor name errors
            .Include(d => d.AssignedVolunteers)
            .Include(loc=>loc.Location)
            .Where(d => d.Date >= DateOnly.FromDateTime(DateTime.Today))
            .OrderBy(d => d.Date)
            .ToListAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostVolunteerAsync(int danceId, string dutyType)
    {
        // 1. Resolve browsing member context key identifier out of cookie claims
        var currentMemberId = User.FindFirst("MemberId")?.Value;
        if (string.IsNullOrEmpty(currentMemberId) || !int.TryParse(currentMemberId, out int memberId))
        {
            return Forbid();
        }

        // Grab their core profile metrics from your primary Members table
        var currentMember = await _context.Members.FindAsync(memberId);
        if (currentMember == null) return Forbid();

        // 2. Fetch the target dance along with its assigned volunteers collection loop
        var dance = await _context.Events.OfType<Dance>()
            .Include(d => d.AssignedVolunteers)
            .FirstOrDefaultAsync(d => d.Id == danceId);

        if (dance == null) return NotFound();

        // 3. MASTER REGISTRY PATTERN SAFETY CHECK:
        // Locate or dynamically spin up a permanent Master Volunteer profile row using their email
        var memberEmail = currentMember.Email?.Trim().ToLowerInvariant() ?? string.Empty;

        var masterVolunteer = await _context.MasterVolunteers
            .FirstOrDefaultAsync(v => v.Email!.ToLower() == memberEmail);

        if (masterVolunteer == null)
        {
            // If they have never volunteered before, save them permanently into the registry directory
            masterVolunteer = new MasterVolunteer
            {
                Name = $"{currentMember.FullName}".Trim(),
                Email = memberEmail,
                Notes = "Automatically registered profile via Member Portal self-service."
            };

            _context.MasterVolunteers.Add(masterVolunteer);
            await _context.SaveChangesAsync(); // Saves to MasterVolunteers table first
        }

        // 4. DUPLICATION PROTECTION: Check if they are already assigned to this specific dance
        bool alreadyAssigned = dance.AssignedVolunteers.Any(v => v.Id == masterVolunteer.Id);
        if (alreadyAssigned)
        {
            VolunteerNotice = "Info: You are already assigned to the helper roster for this dance event.";
            return RedirectToPage();
        }

        // 5. SECURE MANY-TO-MANY LINK INJECTION:
        // Simply add the master profile object directly into the dance's navigational array
        dance.AssignedVolunteers.Add(masterVolunteer);

        // Notes Field update optional: Store what shift they took
        masterVolunteer.Notes = $"Last active duty role shift taken: {dutyType}.";
        _context.MasterVolunteers.Update(masterVolunteer);

        // EF Core naturally generates the linking record row inside your 'DanceAssignedVolunteers' junction table
        await _context.SaveChangesAsync();

        VolunteerNotice = $"Success: Thank you! You have been successfully scheduled for the '{dutyType}' assignment role position.";
        return RedirectToPage();
    }

    //
}
