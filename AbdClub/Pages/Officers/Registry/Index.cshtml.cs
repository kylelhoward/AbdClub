using AbdClub.Data;
using AbdClub.Models;
using AbdClub.Models.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace AbdClub.Pages.Officers.Registry;

[Authorize(Policy = "isOfficer")]
public class IndexModel(
    AbdContext context,
    ILogger<IndexModel> logger,
    IAuthorizationService authorizationService) : PageModel
{
    private readonly AbdContext _context = context;
    private readonly ILogger<IndexModel> _logger = logger;
    private readonly IAuthorizationService _authorizationService = authorizationService;

    // Registry lists bound to the UI tabs
    public List<MasterDJ> Djs { get; set; } = new();
    public List<MasterHost> Hosts { get; set; } = new();
    public List<MasterInstructor> Instructors { get; set; } = new();
    public List<MasterVolunteer> Volunteers { get; set; } = new();

    [BindProperty]
    public RegistryFormInput FormInput { get; set; } = new();

    [TempData]
    public string? StatusNotice { get; set; }

    public async Task<IActionResult> OnGetAsync()
    {
        // 🌟 EVALUATE THE CENTRALIZED "isAdmin" POLICY DIRECTLY
        var authResult = await _authorizationService.AuthorizeAsync(User, null, "isOfficer");

        if (!authResult.Succeeded)
        {
            return Forbid(); // Blocks lower-level officers automatically
        }

        // Query all permanent data tiers concurrently
        Djs = await _context.MasterDjs.OrderBy(x => x.Name).ToListAsync();
        Hosts = await _context.MasterHosts.OrderBy(x => x.Name).ToListAsync();
        Instructors = await _context.MasterInstructors.OrderBy(x => x.Name).ToListAsync();
        Volunteers = await _context.MasterVolunteers.OrderBy(x => x.Name).ToListAsync();

        return Page();
    }

    // UNIFIED UPSERT HANDLER: Handles both New Additions and Profile Changes
    // Open Pages/Officers/Registry/Index.cshtml.cs and update this block:
    public async Task<IActionResult> OnPostSaveProfileAsync()
    {
        // 🌟 EVALUATE THE CENTRALIZED "isAdmin" POLICY DIRECTLY
        var authResult = await _authorizationService.AuthorizeAsync(User, null, "isOfficer");

        if (!authResult.Succeeded)
        {
            return Forbid(); // Blocks lower-level officers automatically
        }

        if (!ModelState.IsValid)
        {
            StatusNotice = "Error: Input validation failed. Please check form data styles.";
            return RedirectToPage();
        }

        // Convert incoming parameter to uppercase to ensure string compatibility checks bypass casing typos
        switch (FormInput.TargetType.ToUpperInvariant())
        {
            case "DJ": await ProcessUpsertAsync<MasterDJ>(); break;
            case "HOST": await ProcessUpsertAsync<MasterHost>(); break;
            case "INSTRUCTOR": await ProcessUpsertAsync<MasterInstructor>(); break;
            case "VOLUNTEER": await ProcessUpsertAsync<MasterVolunteer>(); break;
            default: return BadRequest($"Invalid classification type route. Received: {FormInput.TargetType}");
        }

        return RedirectToPage();
    }

    // UNIFIED DELETE HANDLER
    public async Task<IActionResult> OnPostDeleteProfileAsync(int id, string type)
    {
        // 🌟 EVALUATE THE CENTRALIZED "isAdmin" POLICY DIRECTLY
        var authResult = await _authorizationService.AuthorizeAsync(User, null, "isAdmin");

        if (!authResult.Succeeded)
        {
            return Forbid(); // Blocks lower-level officers automatically
        }
        switch (type)
        {
            case "DJ": await ProcessDeleteAsync<MasterDJ>(id); break;
            case "Host": await ProcessDeleteAsync<MasterHost>(id); break;
            case "Instructor": await ProcessDeleteAsync<MasterInstructor>(id); break;
            case "Volunteer": await ProcessDeleteAsync<MasterVolunteer>(id); break;
        }

        return RedirectToPage();
    }

    private async Task ProcessUpsertAsync<T>() where T : class, IRegistryPerson, new()
    {
        if (FormInput.RecordId == 0)
        {
            // Creation execution pathing
            var entity = new T
            {
                Name = FormInput.Name.Trim(),
                Email = FormInput.Email?.Trim().ToLower(),
                Phone = FormInput.Phone?.Trim(),
                Notes = FormInput.Notes?.Trim()
            };
            _context.Set<T>().Add(entity);
            StatusNotice = $"Success: Successfully added new {FormInput.TargetType} entry profile.";
        }
        else
        {
            // Edit modification execution pathing
            var existing = await _context.Set<T>().FindAsync(FormInput.RecordId);
            if (existing != null)
            {
                existing.Name = FormInput.Name.Trim();
                existing.Email = FormInput.Email?.Trim().ToLower();
                existing.Phone = FormInput.Phone?.Trim();
                existing.Notes = FormInput.Notes?.Trim();
                _context.Set<T>().Update(existing);
                StatusNotice = $"Success: Modified profile record for {FormInput.Name}.";
            }
        }
        await _context.SaveChangesAsync();
    }

    private async Task ProcessDeleteAsync<T>(int id) where T : class, IRegistryPerson
    {
        var record = await _context.Set<T>().FindAsync(id);
        if (record != null)
        {
            _context.Set<T>().Remove(record);
            await _context.SaveChangesAsync();
            StatusNotice = $"Success: Record purged cleanly from system registers.";
        }
    }
}

public class RegistryFormInput
{
    public int RecordId { get; set; } // 0 means new record, >0 means edit
    [Required] public string TargetType { get; set; } = string.Empty; // "DJ", "Host", "Instructor", "Volunteer"
    [Required(ErrorMessage = "Name field is mandatory.")] public string Name { get; set; } = string.Empty;
    [EmailAddress] public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Notes { get; set; }
}

