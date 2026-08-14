using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AbdClub.Models;
using AbdClub.Data;

namespace AbdClub.Pages.Officers.Locations;

public class EditModel(
    AbdContext context,
    IWebHostEnvironment environment,
    ILogger<EditModel> logger) : PageModel
{
    private readonly AbdContext _context = context;

    private readonly IWebHostEnvironment _environment = environment; // Injected hosting environment service
    private readonly ILogger<EditModel> _logger = logger;

    [BindProperty]
    public Location Location { get; set; } = default!;

    // 🌟 CRITICAL PROPERTY FOR WEB INPUT STREAM CAPTURE
    [BindProperty]
    public IFormFile? VenuePhotoFile { get; set; }

    public async Task<IActionResult> OnGetAsync(int? id)
    {
        if (id is null)
        {
            return NotFound();
        }

        var location = await _context.Locations.FirstOrDefaultAsync(m => m.Id == id);
        if (location is null)
        {
            return NotFound();
        }
        Location = location;
        return Page();
    }

    // To protect from overposting attacks, enable the specific properties you want to bind to.
    // For more details, see https://aka.ms/RazorPagesCRUD.
    public async Task<IActionResult> OnPostAsync()
    {
        if (!ModelState.IsValid)
        {
            return Page();
        }

        // 🌟 FILE UPLOAD LIFECYCLE: Check if a new image was uploaded from the browser picker
        if (VenuePhotoFile != null && VenuePhotoFile.Length > 0)
        {
            try
            {
                // Create a web-safe lowercase filename (e.g., "saengerrunde-hall.webp")
                string safeVenueName = Location.VenueName.Trim().ToLower()
                    .Replace(" ", "-")
                    .Replace("/", "-");

                string fileExtension = Path.GetExtension(VenuePhotoFile.FileName).ToLower();
                string finalFileName = $"{safeVenueName}{fileExtension}";

                // Map path straight to your physical wwwroot folder directory location
                string uploadFolder = Path.Combine(_environment.WebRootPath, "images", "venues");
                if (!Directory.Exists(uploadFolder))
                {
                    Directory.CreateDirectory(uploadFolder);
                }

                string fullPhysicalFilePath = Path.Combine(uploadFolder, finalFileName);

                // Pipe the file data onto the disk storage array
                using (var stream = new FileStream(fullPhysicalFilePath, FileMode.Create))
                {
                    await VenuePhotoFile.CopyToAsync(stream);
                }

                // Assign the new relative path string token directly into our bound entry model
                Location.PhotoUrl = $"/images/venues/{finalFileName}";
                _logger.LogInformation("Edit Upload Success: Updated photo written to disk as: {FileName}", finalFileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "File System Exception: Failed to rewrite venue asset parameters on save for ID {Id}", Location.Id);
                ModelState.AddModelError("Location.PhotoUrl", "System Error: Failed to save your new file image to disk storage.");
                return Page();
            }
        }
        else
        {
            // 🌟 SAFETY IMAGE PRESERVATION: If no new file is selected, fetch the existing record 
            // out of a separate tracking context query so we don't accidentally overwrite it with an empty string!
            var currentDbRecord = await _context.Locations.AsNoTracking()
                .FirstOrDefaultAsync(l => l.Id == Location.Id);

            if (currentDbRecord != null)
            {
                Location.PhotoUrl = currentDbRecord.PhotoUrl;
            }
        }

        // Attach our modified form inputs to the tracking entry layer context
        _context.Attach(Location).State = EntityState.Modified;

        try
        {
            await _context.SaveChangesAsync();
            _logger.LogInformation("Administrative Override: Venue record ID {Id} has been updated safely in the master directories.", Location.Id);
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!LocationExists(Location.Id))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        return RedirectToPage("./Index");
    }


    private bool LocationExists(int id)
    {
        return _context.Locations.Any(e => e.Id == id);
    }
}
