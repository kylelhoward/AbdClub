using AbdClub.Data;
using AbdClub.Dtos;
using AbdClub.Models;
using AbdClub.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Infrastructure;
using Microsoft.Extensions.Configuration;
namespace AbdClub.Pages.Officers.Dances;

public class IndexModel : PageModel
{
    private readonly AbdContext _context;
    private readonly ILogger<AttendingOfficersModel> _logger;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _config; // Inject configuration layer
    public IndexModel(
        AbdContext context,
        ILogger<AttendingOfficersModel> logger,
        IEmailService emailService,
        IConfiguration config)
    {
        _context = context;
        _logger = logger;
        QuestPDF.Settings.License = LicenseType.Community;
        QuestPDF.Settings.License = LicenseType.Community;
        _emailService = emailService;
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
    public DanceRegistryCreationDto NewDance { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        bool isAuthorized = User.IsInRole("Officer") && User.FindFirst("OfficerRole")?.Value == "Tech Sergeant Chen";
        if (!isAuthorized) return Forbid();
        // HYDRATE FORM CONFIGURATION DEFAULTS FROM APPSETTINGS.JSON
        NewDance.Location = _config["DanceDefaults:Location"] ?? "Go Dance";
        NewDance.ContactEmail = _config["DanceDefaults:ContactEmail"] ?? "management@abdclub.com";
        NewDance.Date = DateOnly.FromDateTime(DateTime.Today); // Sensible operational default parameter

        // Parse time elements out safely into type-safe TimeOnly objects
        if (TimeOnly.TryParse(_config["DanceDefaults:StartTime"], out var startTime))
            NewDance.StartTime = startTime;
        else
            NewDance.StartTime = new TimeOnly(19, 0); // Hard fallback safety

        if (TimeOnly.TryParse(_config["DanceDefaults:EndTime"], out var endTime))
            NewDance.EndTime = endTime;
        else
            NewDance.EndTime = new TimeOnly(22, 0);
        // Load your lookup registries
        RegistryDjs = await _context.MasterDjs.OrderBy(d => d.Name).ToListAsync();
        RegistryHosts = await _context.MasterHosts.OrderBy(h => h.Name).ToListAsync();
        RegistryInstructors = await _context.MasterInstructors.OrderBy(i => i.Name).ToListAsync();
        RegistryVolunteers = await _context.MasterVolunteers.OrderBy(v => v.Name).ToListAsync();
        AvailableOfficers = await _context.Members.Where(m => m.IsActive && m.IsOfficer).OrderBy(m => m.FullName).ToListAsync();

        UpcomingDances = await _context.Events.OfType<Dance>().Include(d => d.AssignedDj).OrderBy(d => d.Date).ToListAsync();
        return Page();
    }
    public async Task<IActionResult> OnPostCreateDanceAsync()
    {
        bool isAuthorized = User.IsInRole("Officer") && User.FindFirst("OfficerRole")?.Value == "Tech Sergeant Chen";
        if (!isAuthorized) return Forbid();

        if (!ModelState.IsValid) return Page();

        var dance = new Dance
        {
            Title = NewDance.Title,
            Location = NewDance.Location,
            Description = NewDance.Description,
            ContactEmail = NewDance.ContactEmail,
            Date = NewDance.Date,
            StartTime = NewDance.StartTime,
            EndTime = NewDance.EndTime,
            AssignedDjId = NewDance.SelectedDjId > 0 ? NewDance.SelectedDjId : null
        };

        // Attach Many-to-Many entities directly from lookups using ID mappings
        if (NewDance.SelectedHostIds.Any())
            dance.AssignedHosts = await _context.MasterHosts.Where(h => NewDance.SelectedHostIds.Contains(h.Id)).ToListAsync();

        if (NewDance.SelectedInstructorIds.Any())
            dance.AssignedInstructors = await _context.MasterInstructors.Where(i => NewDance.SelectedInstructorIds.Contains(i.Id)).ToListAsync();

        if (NewDance.SelectedVolunteerIds.Any())
            dance.AssignedVolunteers = await _context.MasterVolunteers.Where(v => NewDance.SelectedVolunteerIds.Contains(v.Id)).ToListAsync();

        if (NewDance.SelectedOfficerIds.Any())
            dance.AttendingOfficers = await _context.Members.Where(m => NewDance.SelectedOfficerIds.Contains(m.Id)).ToListAsync();

        _context.Events.Add(dance);
        await _context.SaveChangesAsync();
        // REFACTORED PERSISTENCE METHOD: Saves InstructorId references cleanly
        _context.RemoveRange(dance.Lessons);
        dance.Lessons.Clear();

        if (NewDance.Lessons != null && NewDance.Lessons.Any())
        {
            foreach (var item in NewDance.Lessons.Where(l => l.InstructorId > 0))
            {
                dance.Lessons.Add(new Lesson
                {
                    DanceId = dance.Id,
                    InstructorId = item.InstructorId,
                    Type = item.Type.Trim(),
                    StartTime = item.StartTime,
                    EndTime = item.EndTime
                });
            }
        }
         await _context.SaveChangesAsync();

        UpdateFeedback = "Success: Event saved.";
        return RedirectToPage();
    }
}
