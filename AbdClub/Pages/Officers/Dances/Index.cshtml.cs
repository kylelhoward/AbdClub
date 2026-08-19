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


        _context.Events.Add(danceToUpdate);
      
            // Save your changes. Fully safe from constraint drops now!
            await _context.SaveChangesAsync(); // Line 203 will commit perfectly now!

        UpdateFeedback = "Success: Event saved.";
        return RedirectToPage();
    }
}
