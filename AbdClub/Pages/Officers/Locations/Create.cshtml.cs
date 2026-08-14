using Microsoft.AspNetCore.Hosting; // 🌟 Required to locate wwwroot
using Microsoft.AspNetCore.Http;    // 🌟 Required for IFormFile
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AbdClub.Data;
using AbdClub.Models;
using System.IO;

namespace AbdClub.Pages.Officers.Locations;

public class CreateModel(
    AbdContext context,
    IWebHostEnvironment environment,
    ILogger<CreateModel> logger) : PageModel
{
    private readonly AbdContext _context = context;
    private readonly IWebHostEnvironment _environment = environment; // Injected hosting environment service
    private readonly ILogger<CreateModel> _logger = logger;

    [BindProperty]
    public Location Location { get; set; } = default!;

    // 🌟 BINDS THE INCOMING WEB FILE BLOCK: Name must match the form HTML input attribute exactly
    [BindProperty]
    public IFormFile? VenuePhotoFile { get; set; }

    public void OnGet() { }

    public async Task<IActionResult> OnPostAsync()
    {
        // 1. If basic string attributes fail validation, drop back out gracefully
        if (!ModelState.IsValid) return Page();

        // 2. CHECK IF A FILE WAS ACTUALLY DROPPED IN BY THE OFFICER
        if (VenuePhotoFile != null && VenuePhotoFile.Length > 0)
        {
            try
            {
                // Normalize the venue name to create a safe, web-friendly file name (lowercase, no spaces)
                string safeVenueName = Location.VenueName.Trim().ToLower()
                    .Replace(" ", "-")
                    .Replace("/", "-");

                // Extract the uploaded file extension safely (.jpg, .png, etc.)
                string fileExtension = Path.GetExtension(VenuePhotoFile.FileName).ToLower();
                string finalFileName = $"{safeVenueName}{fileExtension}";

                // Map the destination path point directly into your wwwroot directory structure
                string uploadFolder = Path.Combine(_environment.WebRootPath, "images", "venues");

                // Safety backup: Ensure the physical subfolders exist on the server host
                if (!Directory.Exists(uploadFolder))
                {
                    Directory.CreateDirectory(uploadFolder);
                }

                string fullPhysicalFilePath = Path.Combine(uploadFolder, finalFileName);

                // 3. FILE STREAM TRANSFER: Pipe the file directly onto the server disk storage
                using (var stream = new FileStream(fullPhysicalFilePath, FileMode.Create))
                {
                    await VenuePhotoFile.CopyToAsync(stream);
                }

                // 4. DATABASE SYNC: Save the clean relative web path string token into your model
                Location.PhotoUrl = $"/images/venues/{finalFileName}";

                _logger.LogInformation("File Upload Pipeline Success: Photo saved cleanly to physical path disk allocation: {FileName}", finalFileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "File System Boundary Exception: Failed to execute file save actions for venue {VenueName}", Location.VenueName);
                ModelState.AddModelError("Location.PhotoUrl", "System Error: The web server failed to save your uploaded file image to disk storage.");
                return Page();
            }
        }
        else
        {
            // If they didn't upload a picture, assign your standard default system image fallback token path
            Location.PhotoUrl = "/images/venues/fallback.jpg";
        }

        _context.Locations.Add(Location);
        await _context.SaveChangesAsync();

        return RedirectToPage("./Index");
    }
}
