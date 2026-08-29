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

    public Event TargetDance { get; set; } = null!;
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

        TargetDance = await _context.Events
            .Include(d => d.AttendingOfficers).ThenInclude(m => m.OfficerAccount)
            .Include(d => d.AssignedDj)
            .Include(d => d.AssignedVolunteers)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (TargetDance == null) return NotFound();

        if (TargetDance is Dance regularDance)
        {
            await _context.Entry(regularDance).Collection(d => d.AssignedHosts).LoadAsync();
            await _context.Entry(regularDance).Reference(d => d.AssignedLesson).LoadAsync();
        }

        FormInput.SelectedDjId = TargetDance.AssignedDjId;

        CurrentlyAssignedOfficerIds = TargetDance.AttendingOfficers.Select(o => o.Id).ToHashSet();
        FormInput.SelectedOfficerIds = CurrentlyAssignedOfficerIds.ToList();

        CurrentlyAssignedHostIds = TargetDance is Dance dance
            ? dance.AssignedHosts.Select(h => h.Id).ToHashSet()
            : new HashSet<int>();
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
if (TargetDance is Dance danceWithLesson && danceWithLesson.AssignedLesson != null)
        {
            FormInput.AssignedLesson = new LessonInputItem
            {
                InstructorId = danceWithLesson.AssignedLesson.InstructorId,
                Type = danceWithLesson.AssignedLesson.Type,
                StartTime = danceWithLesson.AssignedLesson.StartTime,
                EndTime = danceWithLesson.AssignedLesson.EndTime
            };
        }
        else
        {
            // Initialize as a blank default template instance so your HTML input elements don't throw null crashes
            FormInput.AssignedLesson = new LessonInputItem();
        }

        if (TargetDance is Outing outing)
        {
            FormInput.ExternalWebsiteUrl = outing.ExternalWebsiteUrl;
            FormInput.RegistrationInstructions = outing.RegistrationInstructions;
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

        var danceToUpdate = await _context.Events
            .Include(d => d.AttendingOfficers).ThenInclude(m => m.OfficerAccount)
            .Include(d => d.AssignedVolunteers)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (danceToUpdate == null) return NotFound();

        if (danceToUpdate is Dance regularDance)
        {
            await _context.Entry(regularDance).Collection(d => d.AssignedHosts).LoadAsync();
            await _context.Entry(regularDance).Reference(d => d.AssignedLesson).LoadAsync();
        }
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
        danceToUpdate.AssignedDjId = danceToUpdate is Outing
            ? null
            : FormInput.SelectedDjId > 0 ? FormInput.SelectedDjId : null;

        if (danceToUpdate is Outing outing)
        {
            outing.ExternalWebsiteUrl = FormInput.ExternalWebsiteUrl?.Trim();
            outing.RegistrationInstructions = FormInput.RegistrationInstructions?.Trim();
        }

        // Many-to-Many updates
        if (danceToUpdate is Dance regularDanceToUpdate)
        {
            regularDanceToUpdate.AssignedHosts.Clear();
            if (FormInput.SelectedHostIds.Any())
                regularDanceToUpdate.AssignedHosts = await _context.MasterHosts
                    .Where(h => FormInput.SelectedHostIds.Contains(h.Id)).ToListAsync();
        }

        danceToUpdate.AssignedVolunteers.Clear();
        if (FormInput.SelectedVolunteerIds.Any())
            danceToUpdate.AssignedVolunteers = await _context.MasterVolunteers.Where(v => FormInput.SelectedVolunteerIds.Contains(v.Id)).ToListAsync();

        // Water - tight 1:1 lesson hydration logic
        // Inside your OnPostUpdateRosterAsync() or OnPostEditAsync() handler method:

        // Check if the user filled out the basic required elements of the lesson form
        bool hasLessonInput = FormInput.AssignedLesson != null &&
                              !string.IsNullOrWhiteSpace(FormInput.AssignedLesson.Type) &&
                              FormInput.AssignedLesson.InstructorId.HasValue &&
                              FormInput.AssignedLesson.StartTime.HasValue &&
                              FormInput.AssignedLesson.EndTime.HasValue;

        if (danceToUpdate is Dance lessonDance && hasLessonInput)
        {
            if (lessonDance.AssignedLesson == null)
            {
                // CASE A: The event didn't have a lesson row yet -> Instantiate a clean entity tracker instance
                lessonDance.AssignedLesson = new Lesson();
            }

            // Connect the matching parent relationship ID
            lessonDance.AssignedLesson.DanceId = lessonDance.Id;

            // 🌟 THE UNPACKING FIX: Appending '.Value' extracts the inner data cleanly, solving the compilation error!
            lessonDance.AssignedLesson.InstructorId = FormInput.AssignedLesson!.InstructorId!.Value;
            lessonDance.AssignedLesson.Type = FormInput.AssignedLesson.Type!.Trim();
            lessonDance.AssignedLesson.StartTime = FormInput.AssignedLesson.StartTime!.Value;
            lessonDance.AssignedLesson.EndTime = FormInput.AssignedLesson.EndTime!.Value;
        }
        else if (danceToUpdate is Dance danceWithoutLessonInput)
        {
            // CASE B: If the form is left blank, check if a lesson row currently exists on disk
            if (danceWithoutLessonInput.AssignedLesson != null)
            {
                // 🌟 NATIVE CASCADE CLEANUP: Telling EF Core to remove the tracked lesson row entity completely.
                // This ensures that clearing out the fields on the Edit page actually drops the record from PostgreSQL!
                _context.Lessons.Remove(danceWithoutLessonInput.AssignedLesson);
            }

            danceWithoutLessonInput.AssignedLesson = null;
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
                await _emailService
                        .SendOfficerDutyNotificationAsync(
                            officer.OfficerAccount?.Email ?? officer.Email,
                            $"{officer.LastName}", 
                            danceToUpdate.Title, 
                            dateString, 
                            dropText,
                            officer.Id);
            }
        }

        if (idsToAdd.Any())
        {
            var officersToAdd = await _context.Members
                .Include(m => m.OfficerAccount)
                .Where(m => idsToAdd.Contains(m.Id) &&
                    m.OfficerAccount != null && m.OfficerAccount.IsEnabled)
                .ToListAsync();
            string addText = "<span style='color:#198754; font-weight:bold;'>ASSIGNED TO DUTY</span> via adjustments.";
            foreach (var officer in officersToAdd)
            {
                danceToUpdate.AttendingOfficers.Add(officer);
                await _emailService
                        .SendOfficerDutyNotificationAsync(
                            officer.OfficerAccount!.Email,
                            $"{officer.LastName}",
                            danceToUpdate.Title,
                            dateString, 
                            addText,
                            officer.Id);
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

        UpdateFeedback = "The event details and assignments were updated.";
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
            && m.OfficerAccount != null
            && m.OfficerAccount.IsEnabled)
            .Include(m => m.OfficerAccount)
            .OrderBy(m => m.LastName)
            .ToListAsync();
    }
}
