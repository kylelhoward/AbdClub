using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AbdClub.Data;
using AbdClub.Models;
namespace AbdClub.Pages.Officers.Dances;

[Authorize(Policy = "isOfficer")]
public class PrintManifestsModel : PageModel
{
    private readonly AbdContext _context;
    private readonly IConfiguration _config; // Tracking field handle allocation

    // Inject the configuration dependency channel straight into the class constructor
    public PrintManifestsModel(AbdContext context, IConfiguration config)
    {
        _context = context;
        _config = config;
    }

    public Dance? TargetDance { get; set; }
    public List<Member> ActiveRoster { get; set; } = new();
    public List<Member> InactiveRoster { get; set; } = new();

    // Configuration Parameter Flags
    public List<string> SelectedForms { get; set; } = new();
    public bool IsGeneric { get; set; }
    // 🌟 STRATEGIC INTERFACE PROPERTY READOUT BOUNDS
    public string ConfiguredAdmissionFee { get; set; } = "$10.00";
    public string ConfiguredRenewalFee { get; set; } = "$50.00";
    public string ConfiguredNonMemberFee{ get; set; } = "$15.00";
    public string ConfiguredStudentNonMemberFee{ get; set; } = "$5.00";
    
    public async Task<IActionResult> OnGetAsync(int id, string forms, bool generic)
    {
        IsGeneric = generic;

        if (!string.IsNullOrEmpty(forms))
        {
            SelectedForms = forms.Split(',').Select(f => f.Trim().ToLower()).ToList();
        }

        // 1. Fetch Dance profile data context along with its linked relational elements
        TargetDance = await _context.Events
            .OfType<Dance>()
            .Include(d => d.Location)
            // 🌟 EAGER-LOAD EVERYTHING FOR MANIFEST 3 DUITY LOGS
            .Include(d => d.AttendingOfficers).ThenInclude(m => m.OfficerAccount)
            .Include(d => d.AssignedDj)
            .Include(d => d.AssignedLesson).ThenInclude(l => l.Instructor)
            .Include(d => d.AssignedHosts)
            .Include(d => d.AssignedVolunteers)
            .FirstOrDefaultAsync(d => d.Id == id);

        if (TargetDance == null && !IsGeneric) return NotFound();
       
        // 🌟 READ CONFIG VALUES NATIVELY: Pull decimal metrics and format as clean currency tokens
        ConfiguredAdmissionFee = 
            _config
            .GetValue(
                "ClubPricing:AdmissionFee",
                10.00m).ToString("C"); // Outputs format: $10.00
        ConfiguredRenewalFee = _config.GetValue(
            "ClubPricing:MembershipRenewalFee",
            50.00m).ToString("C");     // Outputs format: $50.00
        ConfiguredNonMemberFee = _config.GetValue(
            "ClubPricing:NonMemberFee",
            15.00m).ToString("C");     // Outputs format: $15.00
        ConfiguredStudentNonMemberFee = _config.GetValue(
            "ClubPricing:StudentNonMemberFee",
            5.00m).ToString("C");     // Outputs format: $5.00

        // 2. Fetch records only if a member check-in layout sheet was checked
        if (!IsGeneric)
        {
            if (SelectedForms.Contains("active-members"))
            {
                // 🌟 CLEAN COMPREHENSIVE NATIVE SORT: Runs instantly on the database engine server side
                ActiveRoster = await _context.Members
                    .Where(m => !m.IsSuspended && m.ExpiryDate.HasValue && m.ExpiryDate.Value >= DateTime.UtcNow && m.Email != null)
                    .OrderBy(m => m.LastName) // 👈 Beautiful, indexed native server sorting!
                    .ThenBy(m => m.FirstName)
                    .ToListAsync();

            }

            // Inside Pages/Officers/Dances/PrintManifests.cshtml.cs inside OnGetAsync():

            if (SelectedForms.Contains("inactive-members"))
            {
                // 🌟 FIXED SERVER-SIDE EVALUATION: Removed the unmapped IsActive parameter entirely
                InactiveRoster = await _context.Members
                    .Where(m => (m.IsSuspended || !m.ExpiryDate.HasValue || m.ExpiryDate.Value < DateTime.UtcNow) && m.Email != null)
                    .OrderBy(m => m.LastName)
                    .ThenBy(m => m.FirstName) // Keeps matching surnames perfectly alphabetized on paper
                    .ToListAsync();
            }

        }

        return Page();
    }
}
