using AbdClub.Data;
using AbdClub.Dtos;
using AbdClub.Models;
using AbdClub.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using QuestPDF.Infrastructure;
using static System.Runtime.InteropServices.JavaScript.JSType;
namespace AbdClub.Pages.Officers.Dances;

public class IndexModel : PageModel
{
    private readonly IAuthorizationService _authorizationService;
    public List<Location> AvailableLocations { get; set; } = new();
    private readonly AbdContext _context;
    private readonly ILogger<AttendingOfficersModel> _logger;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _config; // Inject configuration layer
    public IndexModel(
        AbdContext context,
        ILogger<AttendingOfficersModel> logger,
        IEmailService emailService,
        IAuthorizationService authorizationService,
        IConfiguration config)
    {
        _context = context;
        _logger = logger;
        QuestPDF.Settings.License = LicenseType.Community;
        _emailService = emailService;
        _authorizationService = authorizationService;
        _config = config;


    }
    [TempData]
    public string? UpdateFeedback { get; set; }
    public List<Dance> UpcomingDances { get; set; } = new();
    public List<Member> AvailableOfficers { get; set; } = new();
    // Master data selection properties
    public List<MasterDJ> RegistryDjs { get; set; } = new();
    public List<MasterHost> RegistryHosts { get; set; } = new();
    public List<MasterInstructor> RegistryInstructors { get; set; } = new();
    public List<MasterVolunteer> RegistryVolunteers { get; set; } = new();

    [BindProperty]
    public DanceRegistryCreationDto FormInput { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        var authResult = await _authorizationService.AuthorizeAsync(User, null, "isOfficer");
        if (!authResult.Succeeded)
        {
            return Forbid(); // Blocks lower-level officers automatically
        }

        // HYDRATE FORM CONFIGURATION DEFAULTS FROM APPSETTINGS.JSON
        //NewDance.Location = _config["DanceDefaults:Location"] ?? "Go Dance";
        FormInput.ContactEmail = _config["DanceDefaults:ContactEmail"] ?? "management@abdclub.com";
        FormInput.Date = DateOnly.FromDateTime(DateTime.Today); // Sensible operational default parameter
        // Hydrate your new venue dropdown collection array
    AvailableLocations = await _context.Locations.OrderBy(l => l.VenueName).ToListAsync();

        // Parse time elements out safely into type-safe TimeOnly objects
        if (TimeOnly.TryParse(_config["DanceDefaults:StartTime"], out var startTime))
            FormInput.StartTime = startTime;
        else
            FormInput.StartTime = new TimeOnly(19, 0); // Hard fallback safety

        if (TimeOnly.TryParse(_config["DanceDefaults:EndTime"], out var endTime))
            FormInput.EndTime = endTime;
        else
            FormInput.EndTime = new TimeOnly(22, 0);
        // Load your lookup registries
        RegistryDjs = await _context.MasterDjs.OrderBy(d => d.Name).ToListAsync();
        RegistryHosts = await _context.MasterHosts.OrderBy(h => h.Name).ToListAsync();
        RegistryInstructors = await _context.MasterInstructors.OrderBy(i => i.Name).ToListAsync();
        RegistryVolunteers = await _context.MasterVolunteers.OrderBy(v => v.Name).ToListAsync();
        // 🌟 FIXED SERVER-SIDE EVALUATION: Uses persistent table fields for water-tight SQL translation
        AvailableOfficers = await _context.Members
            .Where(m =>
                m.IsOfficer &&
                !m.IsSuspended &&
                m.ExpiryDate.HasValue &&
                m.ExpiryDate.Value >= DateTime.UtcNow)
            .OrderBy(m => m.LastName)
            .ThenBy(m => m.FirstName) // Added an extra sort pass to keep matching surnames alphabetized
            .ToListAsync();

        UpcomingDances = await _context.Events
            .OfType<Dance>()
            .Include(d => d.AssignedDj)
            .Include(l=>l.AssignedLesson)
            .Include(loc=>loc.Location)
            .OrderBy(d => d.Date)
            .ToListAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostCreateDanceAsync()
    {
        var authResult = await _authorizationService.AuthorizeAsync(User, null, "isOfficer");
        if (!authResult.Succeeded)
        {
            return Forbid(); // Blocks lower-level officers automatically
        }

        if (!ModelState.IsValid) return Page();

        var danceToUpdate = new Dance
        {
            Title = FormInput.Title,
            Description = FormInput.Description,
            ContactEmail = FormInput.ContactEmail,
            Date = FormInput.Date,
            StartTime = FormInput.StartTime,
            EndTime = FormInput.EndTime,
             // 🌟 MAP THE STRATEGIC FOREIGN KEY LINK DIRECTLY
            LocationId = FormInput.SelectedLocationId,
            AssignedDjId = FormInput.SelectedDjId > 0 ? FormInput.SelectedDjId : null
        };

        // Attach Many-to-Many entities directly from lookups using ID mappings
        if (FormInput.SelectedHostIds.Any())
            danceToUpdate.AssignedHosts = await _context.MasterHosts.Where(h => FormInput.SelectedHostIds.Contains(h.Id)).ToListAsync();

        if (FormInput.SelectedVolunteerIds.Any())
            danceToUpdate.AssignedVolunteers = await _context.MasterVolunteers.Where(v => FormInput.SelectedVolunteerIds.Contains(v.Id)).ToListAsync();

        if (FormInput.SelectedOfficerIds.Any())
            danceToUpdate.AttendingOfficers = await _context.Members.Where(m => FormInput.SelectedOfficerIds.Contains(m.Id)).ToListAsync();

        _context.Events.Add(danceToUpdate);
        await _context.SaveChangesAsync();
        // 🌟 DYNAMIC OPTIONAL UNPACKING GATES:
        // Only instantiate a database Lesson entry if the user filled out the form fields!
        if (FormInput.AssignedLesson != null &&
            !string.IsNullOrWhiteSpace(FormInput.AssignedLesson.Type) &&
            FormInput.AssignedLesson.InstructorId.HasValue &&
            FormInput.AssignedLesson.StartTime.HasValue &&
            FormInput.AssignedLesson.EndTime.HasValue)
        {
            var lesson = new Lesson
            {
                DanceId = danceToUpdate.Id,
                InstructorId = FormInput.AssignedLesson.InstructorId.Value, // Access underlying value cleanly
                Type = FormInput.AssignedLesson.Type.Trim(),
                StartTime = FormInput.AssignedLesson.StartTime.Value, // Access underlying value cleanly
                EndTime = FormInput.AssignedLesson.EndTime.Value     // Access underlying value cleanly
            };

            _context.Lessons.Add(lesson);
            await _context.SaveChangesAsync();
        }
        UpdateFeedback = "Success: Event saved.";
        return RedirectToPage();
    }


    public async Task<IActionResult> OnPostDeleteDanceAsync(int id)
    {
        if (!User.IsInRole("Admin") && !User.IsInRole("TechAdmin"))
        {
            return Forbid();
        }

        // 1. 🌟 CRITICAL: Eager-load the AttendingOfficers tracking collection graph!
        var danceToDelete = await _context.Events
            .OfType<Dance>()
            .Include(d => d.AttendingOfficers) // 👈 Prevents constraint collisions
            .FirstOrDefaultAsync(d => d.Id == id);

        if (danceToDelete == null)
        {
            TempData["ErrorMessage"] = "The targeted dance record could not be found.";
            return RedirectToPage("./Index");
        }

        try
        {
            // 2. 🌟 Clear out the junction references from memory
            // This instantly deletes rows inside 'DanceAttendingOfficers' safely, 
            // leaving your core permanent member account rows completely untouched!
            danceToDelete.AttendingOfficers.Clear();

            // 3. Queue the primary entity removal pass
            _context.Events.Remove(danceToDelete);

            // 4. Commit changes down to PostgreSQL
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Successfully deleted event: '{danceToDelete.Title}'.";
        }
        catch (Exception ex)
        {
            TempData["ErrorMessage"] = $"Database Deletion Error: {ex.Message}";
        }

        return RedirectToPage("./Index");
    }


}
