using AbdClub.Data;
using AbdClub.Models;
using AbdClub.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore; // 🌟 CRITICAL MISSING IMPORT LINE
using System.ComponentModel.DataAnnotations;

namespace AbdClub.Pages.Officers.Dances;

public class EditModel(
    AbdContext context,
    IAuthorizationService authorizationService,
    IEmailService emailService,
    ILogger<EditModel> logger) : PageModel
{
    private readonly AbdContext _context = context;
    private readonly IEmailService _emailService = emailService;
    private readonly ILogger<EditModel> _logger = logger;
    private readonly IAuthorizationService _authorizationService = authorizationService;

    public Dance TargetDance { get; set; } = null!;
    public List<Member> AllActiveOfficers { get; set; } = new();
    public List<MasterDJ> RegistryDjs { get; set; } = new();
    public List<MasterHost> RegistryHosts { get; set; } = new();
    public List<MasterInstructor> RegistryInstructors { get; set; } = new();
    public List<MasterVolunteer> RegistryVolunteers { get; set; } = new();

    // Declare the options array collection at the top of your PageModel class:
    public List<Location> AvailableVenues { get; set; } = new();

    public HashSet<int> CurrentlyAssignedOfficerIds { get; set; } = new();
    public HashSet<int> CurrentlyAssignedHostIds { get; set; } = new();
    public HashSet<int> CurrentlyAssignedInstructorIds { get; set; } = new();
    public HashSet<int> CurrentlyAssignedVolunteerIds { get; set; } = new();

    [BindProperty]
    public DanceFormUpdateDto FormInput { get; set; } = new();

    [TempData]
    public string? UpdateFeedback { get; set; }

    public async Task<IActionResult> OnGetAsync(int id)
    {
        var authResult = await _authorizationService.AuthorizeAsync(User, null, "isOfficer");
        if (!authResult.Succeeded)
        {
            return Forbid(); // Blocks lower-level officers automatically
        }

        TargetDance = await _context.Events.OfType<Dance>()
            .Include(d => d.AttendingOfficers)
            .Include(d => d.AssignedDj)
            .Include(d => d.AssignedHosts)
            .Include(d => d.AssignedVolunteers)
            .Include(d => d.AssignedLesson) // Eagerly loads updated Lesson collections
            .FirstOrDefaultAsync(d => d.Id == id);

        if (TargetDance == null) return NotFound();

        FormInput.SelectedDjId = TargetDance.AssignedDjId;

        CurrentlyAssignedOfficerIds = TargetDance.AttendingOfficers.Select(o => o.Id).ToHashSet();
        FormInput.SelectedOfficerIds = CurrentlyAssignedOfficerIds.ToList();

        CurrentlyAssignedHostIds = TargetDance.AssignedHosts.Select(h => h.Id).ToHashSet();
        FormInput.SelectedHostIds = CurrentlyAssignedHostIds.ToList();

        FormInput.SelectedInstructorIds = CurrentlyAssignedInstructorIds.ToList();

        CurrentlyAssignedVolunteerIds = TargetDance.AssignedVolunteers.Select(v => v.Id).ToHashSet();
        FormInput.SelectedVolunteerIds = CurrentlyAssignedVolunteerIds.ToList();
        // Hydrate the venue choices list
        AvailableVenues = await _context.Locations.OrderBy(l => l.VenueName).ToListAsync();

        FormInput.Title = TargetDance.Title;
        FormInput.Description = TargetDance.Description;
        FormInput.Date = TargetDance.Date;
        FormInput.StartTime = TargetDance.StartTime;
        FormInput.EndTime = TargetDance.EndTime;
        FormInput.ContactEmail = TargetDance.ContactEmail;
        FormInput.SelectedLocationId = TargetDance.LocationId;
        // 🌟 REFACTORED 1:1 INITIALIZATION MAP: Replaces your old multi-row collection list loops
if (TargetDance.AssignedLesson != null)
        {
            FormInput.AssignedLesson = new LessonInputItem
            {
                InstructorId = TargetDance.AssignedLesson.InstructorId,
                Type = TargetDance.AssignedLesson.Type,
                StartTime = TargetDance.AssignedLesson.StartTime,
                EndTime = TargetDance.AssignedLesson.EndTime
            };
        }
        else
        {
            // Initialize as a blank default template instance so your HTML input elements don't throw null crashes
            FormInput.AssignedLesson = new LessonInputItem();
        }

        await LoadMasterRegistriesAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostUpdateRosterAsync(int id)
    {
        var authResult = await _authorizationService.AuthorizeAsync(User, null, "isAdmin");
        if (!authResult.Succeeded)
        {
            return Forbid(); // Blocks lower-level officers automatically
        }

        var danceToUpdate = await _context.Events.OfType<Dance>()
            .Include(d => d.AttendingOfficers)
            .Include(d => d.AssignedHosts)
            .Include(d => d.AssignedVolunteers)
            .Include(d => d.AssignedLesson)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (danceToUpdate == null) return NotFound();
        danceToUpdate.Title = FormInput.Title.Trim();
        danceToUpdate.Description = FormInput.Description?.Trim();
        // 🌟 MAP THE SCHEDULE PARAMETER TRANSFORMS CLEANLY
        danceToUpdate.Date = FormInput.Date;
        danceToUpdate.StartTime = FormInput.StartTime;
        danceToUpdate.EndTime = FormInput.EndTime;
        danceToUpdate.ContactEmail = FormInput.ContactEmail;

        // 🌟 MAP THE STRATEGIC FORIEGN KEY SELECTION DIRECTLY
        danceToUpdate.LocationId = FormInput.SelectedLocationId;
        if (!ModelState.IsValid)
        {
            AvailableVenues = await _context.Locations.OrderBy(l => l.VenueName).ToListAsync();
            await LoadMasterRegistriesAsync();
            TargetDance = danceToUpdate;
            return Page();
        }
        danceToUpdate.AssignedDjId = FormInput.SelectedDjId > 0 ? FormInput.SelectedDjId : null;

        // Many-to-Many updates
        danceToUpdate.AssignedHosts.Clear();
        if (FormInput.SelectedHostIds.Any())
            danceToUpdate.AssignedHosts = await _context.MasterHosts.Where(h => FormInput.SelectedHostIds.Contains(h.Id)).ToListAsync();

        danceToUpdate.AssignedVolunteers.Clear();
        if (FormInput.SelectedVolunteerIds.Any())
            danceToUpdate.AssignedVolunteers = await _context.MasterVolunteers.Where(v => FormInput.SelectedVolunteerIds.Contains(v.Id)).ToListAsync();

       // Water - tight 1:1 lesson hydration logic
    if (FormInput.AssignedLesson != null)
        {
            if (danceToUpdate.AssignedLesson == null)
            {
                // The event didn't have a lesson row yet -> Instantiate a clean entity tracker instance
                danceToUpdate.AssignedLesson = new Lesson();
            }

            // 🌟 THE CRITICAL REALIGNMENT FIX: 
            // Manually push the dance primary key directly onto your lesson's relational properties.
            // This satisfies the physical database "FK_Lessons_Events_DanceId" constraint instantly!
            danceToUpdate.AssignedLesson.DanceId = danceToUpdate.Id;

            danceToUpdate.AssignedLesson.InstructorId = FormInput.AssignedLesson.InstructorId;
            danceToUpdate.AssignedLesson.Type = FormInput.AssignedLesson.Type?.Trim();
            danceToUpdate.AssignedLesson.StartTime = FormInput.AssignedLesson.StartTime;
            danceToUpdate.AssignedLesson.EndTime = FormInput.AssignedLesson.EndTime;
        }
        else
        {
            // If the user completely wiped the form input text boxes, clear the record from disk space
            danceToUpdate.AssignedLesson = null;
        }

        // Handle Officer Assignments and Notifications
        var originalOfficers = danceToUpdate.AttendingOfficers.ToList();
        var originalIds = originalOfficers.Select(o => o.Id).ToHashSet();
        var incomingIds = FormInput.SelectedOfficerIds ?? new List<int>();

        var idsToAdd = incomingIds.Where(oid => !originalIds.Contains(oid)).ToList();
        var idsToRemove = originalIds.Where(oid => !incomingIds.Contains(oid)).ToList();
        string dateString = danceToUpdate.Date.ToString("MMMM dd, yyyy");

        if (idsToRemove.Any())
        {
            var officersToRemove = originalOfficers.Where(o => idsToRemove.Contains(o.Id)).ToList();
            string dropText = "<span style='color:#dc3545; font-weight:bold;'>REMOVED FROM DUTY</span> via adjustments.";
            foreach (var officer in officersToRemove)
            {
                danceToUpdate.AttendingOfficers.Remove(officer);
                await _emailService.SendOfficerDutyNotificationAsync(officer.Email, $"{officer.LastName}", danceToUpdate.Title, dateString, dropText);
            }
        }

        if (idsToAdd.Any())
        {
            var officersToAdd = await _context.Members.Where(m => idsToAdd.Contains(m.Id)).ToListAsync();
            string addText = "<span style='color:#198754; font-weight:bold;'>ASSIGNED TO DUTY</span> via adjustments.";
            foreach (var officer in officersToAdd)
            {
                danceToUpdate.AttendingOfficers.Add(officer);
                await _emailService.SendOfficerDutyNotificationAsync(officer.Email, $"{officer.LastName}", danceToUpdate.Title, dateString, addText);
            }
        }

        try
        {
            // Save your changes. Fully safe from constraint drops now!
            await _context.SaveChangesAsync(); // Line 203 will commit perfectly now!
        }
        catch (DbUpdateConcurrencyException)
        {
            var stillExists = await _context.Events.AnyAsync(e => e.Id == id);
            if (!stillExists) return NotFound();
            throw;
        }

        UpdateFeedback = "Success: Event data rosters, lookups, and relational lesson instructor profiles updated.";
        return RedirectToPage(new { id });
    }

    private async Task LoadMasterRegistriesAsync()
    {
        RegistryDjs = await _context.MasterDjs.OrderBy(d => d.Name).ToListAsync();
        RegistryHosts = await _context.MasterHosts.OrderBy(h => h.Name).ToListAsync();
        RegistryInstructors = await _context.MasterInstructors.OrderBy(i => i.Name).ToListAsync();
        RegistryVolunteers = await _context.MasterVolunteers.OrderBy(v => v.Name).ToListAsync();
        AllActiveOfficers = await _context.Members
            .Where(m => !m.IsSuspended &&
            m.ExpiryDate.HasValue 
            && m.ExpiryDate.Value >= DateTime.UtcNow
            && m.IsOfficer)
            .OrderBy(m => m.LastName)
            .ToListAsync();
    }
}

public class LessonInputItem
{
    [Required]
    public int InstructorId { get; set; }

    [Required]
    public string Type { get; set; } = string.Empty;
    public TimeOnly StartTime { get; set; } = new TimeOnly(19, 0);
    public TimeOnly EndTime { get; set; } = new TimeOnly(20, 0);
}
