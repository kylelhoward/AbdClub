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
            .Include(d => d.AssignedInstructors)
            .Include(d => d.AssignedVolunteers)
            .Include(d => d.Lessons) // Eagerly loads updated Lesson collections
            .FirstOrDefaultAsync(d => d.Id == id);

        if (TargetDance == null) return NotFound();

        FormInput.SelectedDjId = TargetDance.AssignedDjId;

        CurrentlyAssignedOfficerIds = TargetDance.AttendingOfficers.Select(o => o.Id).ToHashSet();
        FormInput.SelectedOfficerIds = CurrentlyAssignedOfficerIds.ToList();

        CurrentlyAssignedHostIds = TargetDance.AssignedHosts.Select(h => h.Id).ToHashSet();
        FormInput.SelectedHostIds = CurrentlyAssignedHostIds.ToList();

        CurrentlyAssignedInstructorIds = TargetDance.AssignedInstructors.Select(i => i.Id).ToHashSet();
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
        // REFACTORED INITIALIZATION MAP: Selects InstructorId integer indices
        FormInput.Lessons = TargetDance.Lessons.Select(l => new LessonInputItem
        {
            InstructorId = l.InstructorId,
            Type = l.Type,
            StartTime = l.StartTime,
            EndTime = l.EndTime
        }).ToList();

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
            .Include(d => d.AssignedInstructors)
            .Include(d => d.AssignedVolunteers)
            .Include(d => d.Lessons)
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

        danceToUpdate.AssignedInstructors.Clear();
        if (FormInput.SelectedInstructorIds.Any())
            danceToUpdate.AssignedInstructors = await _context.MasterInstructors.Where(i => FormInput.SelectedInstructorIds.Contains(i.Id)).ToListAsync();

        danceToUpdate.AssignedVolunteers.Clear();
        if (FormInput.SelectedVolunteerIds.Any())
            danceToUpdate.AssignedVolunteers = await _context.MasterVolunteers.Where(v => FormInput.SelectedVolunteerIds.Contains(v.Id)).ToListAsync();

        // REFACTORED PERSISTENCE METHOD: Saves InstructorId references cleanly
        _context.RemoveRange(danceToUpdate.Lessons);
        danceToUpdate.Lessons.Clear();

        if (FormInput.Lessons != null && FormInput.Lessons.Any())
        {
            foreach (var item in FormInput.Lessons.Where(l => l.InstructorId > 0))
            {
                danceToUpdate.Lessons.Add(new Lesson
                {
                    DanceId = id,
                    InstructorId = item.InstructorId,
                    Type = item.Type.Trim(),
                    StartTime = item.StartTime,
                    EndTime = item.EndTime
                });
            }
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
                await _emailService.SendOfficerDutyNotificationAsync(officer.Email, $"{officer.FullName}", danceToUpdate.Title, dateString, dropText);
            }
        }

        if (idsToAdd.Any())
        {
            var officersToAdd = await _context.Members.Where(m => idsToAdd.Contains(m.Id)).ToListAsync();
            string addText = "<span style='color:#198754; font-weight:bold;'>ASSIGNED TO DUTY</span> via adjustments.";
            foreach (var officer in officersToAdd)
            {
                danceToUpdate.AttendingOfficers.Add(officer);
                await _emailService.SendOfficerDutyNotificationAsync(officer.Email, $"{officer.FullName}", danceToUpdate.Title, dateString, addText);
            }
        }

        await _context.SaveChangesAsync();

        UpdateFeedback = "Success: Event data rosters, lookups, and relational lesson instructor profiles updated.";
        return RedirectToPage(new { id });
    }

    private async Task LoadMasterRegistriesAsync()
    {
        RegistryDjs = await _context.MasterDjs.OrderBy(d => d.Name).ToListAsync();
        RegistryHosts = await _context.MasterHosts.OrderBy(h => h.Name).ToListAsync();
        RegistryInstructors = await _context.MasterInstructors.OrderBy(i => i.Name).ToListAsync();
        RegistryVolunteers = await _context.MasterVolunteers.OrderBy(v => v.Name).ToListAsync();
        AllActiveOfficers = await _context.Members.Where(m => m.IsActive && m.IsOfficer).OrderBy(m => m.FullName).ToListAsync();
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
