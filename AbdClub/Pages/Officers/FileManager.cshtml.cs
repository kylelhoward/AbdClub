using AbdClub.Data;
using AbdClub.Enums;
using AbdClub.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AbdClub.Pages.Officers
{
    public class FileManagerModel : PageModel
    {
        private readonly AbdContext _context;
        private readonly string _storageFolder;

        // Define allowed extensions in a whitelist
        private readonly string[] _allowedExtensions = { ".pdf", ".docx", ".txt", ".xls", ".xlsx", ".pptx", ".html", ".csv" };

        public FileManagerModel(AbdContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _storageFolder = Path.Combine(environment.ContentRootPath, "UploadedFiles");

            if (!Directory.Exists(_storageFolder)) Directory.CreateDirectory(_storageFolder);
        }

        [BindProperty]
        public ClubFilePostDto UploadData { get; set; } = new();

        public List<ClubFile> SavedClubFiles { get; set; } = [];

        public async Task OnGetAsync()
        {
            SavedClubFiles = await _context.ClubFiles
                .Include(f => f.UploadedBy) // Fetches the related Member record
                .OrderByDescending(f => f.UploadedAt)
                .ToListAsync();
        }

        // 1. Validated Upload Feature
        public async Task<IActionResult> OnPostAsync(IFormFile formFile)
        {
            if (formFile == null || formFile.Length == 0)
            {
                ModelState.AddModelError("", "Please select a valid file.");
                await OnGetAsync();
                return Page();
            }

            // 1. Read your specific custom cookie claim string
            var currentUserIdClaim = User.FindFirst("MemberId")?.Value;

            // DEBUGGING ASSISTANCE: Check if the claim is missing entirely
            if (string.IsNullOrEmpty(currentUserIdClaim))
            {
                ModelState.AddModelError("", "Security Error: The 'MemberId' claim was not found in your login cookie. Please log out and log back in.");
                await OnGetAsync();
                return Page();
            }

            if (!int.TryParse(currentUserIdClaim, out int loggedInMemberId))
            {
                ModelState.AddModelError("", $"Security Error: The 'MemberId' claim value '{currentUserIdClaim}' is not a valid integer.");
                await OnGetAsync();
                return Page();
            }

            // 2. CRITICAL GATE: Query the database to see if this Member ID actually exists
            bool memberExists = await _context.Members.AnyAsync(m => m.Id == loggedInMemberId);
            if (!memberExists)
            {
                // This stops EF Core from trying to save an invalid ID, preventing the 23503 exception!
                ModelState.AddModelError("", $"Database Error: Your cookie has Member ID '{loggedInMemberId}', but no row with ID {loggedInMemberId} exists in the Members table.");
                await OnGetAsync();
                return Page();
            }

            // 3. File Validation Logic
            var fileExtension = Path.GetExtension(formFile.FileName).ToLowerInvariant();
            if (!_allowedExtensions.Contains(fileExtension))
            {
                ModelState.AddModelError("", $"Invalid file type. Only {string.Join(", ", _allowedExtensions)} files are allowed.");
                await OnGetAsync();
                return Page();
            }

            var rawFileName = Path.GetFileName(formFile.FileName);
            var uniqueFileName = $"{Guid.NewGuid()}_{rawFileName}";
            var fullPhysicalPath = Path.Combine(_storageFolder, uniqueFileName);

            using (var stream = new FileStream(fullPhysicalPath, FileMode.Create))
            {
                await formFile.CopyToAsync(stream);
            }

            // 4. Map the verified ID to your model
            var clubFile = new ClubFile
            {
                UploadedByMemberId = loggedInMemberId, // Safe and verified
                FileName = rawFileName,
                FilePath = uniqueFileName,
                Category = UploadData.Category!.Value.ToString(),
                UploadedAt = DateTime.UtcNow
            };

            _context.ClubFiles.Add(clubFile);

            // Line 96 - Safe from constraint violations now
            await _context.SaveChangesAsync();

            return RedirectToPage();
        }

        // 2. Download Feature
        public async Task<IActionResult> OnGetDownloadAsync(int id)
        {
            var dbFile = await _context.ClubFiles.FindAsync(id);
            if (dbFile == null) return NotFound();

            var physicalPath = Path.Combine(_storageFolder, dbFile.FilePath);
            if (!System.IO.File.Exists(physicalPath)) return NotFound();

            var fileBytes = await System.IO.File.ReadAllBytesAsync(physicalPath);
            return File(fileBytes, "application/octet-stream", dbFile.FileName);
        }

        // 3. Purging Deletion Feature
        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var dbFile = await _context.ClubFiles.FindAsync(id);
            if (dbFile == null) return NotFound();

            // 1. Get the current user's Member ID from their authentication claims
            // (Assuming your authentication cookie stores the database Member ID as a claim)
            var currentUserIdClaim = User.FindFirst("MemberId")?.Value;
            int.TryParse(currentUserIdClaim, out int currentMemberId);

            // 2. Evaluate your specific executive role condition
            bool isAuthorizedOfficer = User.IsInRole("Officer") &&
                                       User.FindFirst("OfficerRole")?.Value == "Tech Sergeant Chen";

            // 3. Evaluate if the current user is the owner who uploaded the file
            bool isFileOwner = dbFile.UploadedByMemberId == currentMemberId;

            // Security Gate: Reject the request if neither condition is satisfied
            if (!isFileOwner && !isAuthorizedOfficer)
            {
                // Return a 403 Forbidden status if they try to bypass the UI
                return Forbid();
            }

            // 4. Proceed with physical disk deletion
            var physicalPath = Path.Combine(_storageFolder, dbFile.FilePath);
            if (System.IO.File.Exists(physicalPath))
            {
                System.IO.File.Delete(physicalPath);
            }

            // 5. Proceed with database metadata purge
            _context.ClubFiles.Remove(dbFile);
            await _context.SaveChangesAsync();

            return RedirectToPage();
        }
    }

    public class ClubFilePostDto
    {
        public int UploadedByMemberId { get; set; }

        // Nullable ensures the user is forced to make a dropdown selection
        public FileCategory? Category { get; set; }
    }
}
