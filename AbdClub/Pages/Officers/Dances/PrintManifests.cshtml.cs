using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AbdClub.Data;
using AbdClub.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AbdClub.Pages.Officers.Dances;

[Authorize(Policy = "isOfficer")]
public class PrintManifestsModel : PageModel
{
    private readonly AbdContext _context;

    public PrintManifestsModel(AbdContext context)
    {
        _context = context;
    }

    public Dance? TargetDance { get; set; }
    public List<Member> ActiveRoster { get; set; } = new();
    public List<Member> InactiveRoster { get; set; } = new();

    // Configuration Parameter Flags
    public List<string> SelectedForms { get; set; } = new();
    public bool IsGeneric { get; set; }

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
            .FirstOrDefaultAsync(d => d.Id == id);

        if (TargetDance == null && !IsGeneric) return NotFound();

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

