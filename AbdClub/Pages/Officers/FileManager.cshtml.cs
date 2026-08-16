using AbdClub.Data;
using AbdClub.Enums;
using AbdClub.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AbdClub.Pages.Officers
{
    [Authorize(Policy = "isOfficer")]
    public class FileManagerModel : PageModel
    {
        private readonly AbdContext _context;
        private readonly string _storageFolder;
        private readonly IAuthorizationService _authorizationService;
        // Define allowed extensions in a whitelist
        private readonly string[] _allowedExtensions = 
            { ".pdf", ".docx", ".txt", ".xls", ".xlsx", ".pptx", ".html", ".csv" };

        public FileManagerModel(AbdContext context,
            IWebHostEnvironment environment,
            IAuthorizationService authorizationService)
        {
            _context = context;
            _storageFolder = Path.Combine(environment.ContentRootPath, "UploadedFiles");
            _authorizationService = authorizationService;
            if (!Directory.Exists(_storageFolder)) Directory.CreateDirectory(_storageFolder);
            _authorizationService = authorizationService;
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
        public async Task<IActionResult> OnPostAsync(IFormFile? formFile) // 🌟 FIXED: Changed to Nullable parameter to stop automatic framework binder drops!
        {
            // 1. COOKIE IDENTITY VERIFICATION GATEWAY
            var currentUserIdClaim = User.FindFirst("MemberId")?.Value;

            if (string.IsNullOrEmpty(currentUserIdClaim) || !int.TryParse(currentUserIdClaim, out int loggedInMemberId))
            {
                ModelState.AddModelError("", "Security Error: Your member tracking identity session is invalid.");
                await OnGetAsync();
                return Page();
            }

          
            // 3. DATABASE PRIVILEGES CORROBORATION CHECK
            bool memberExists = await _context.Members.AnyAsync(m => m.Id == loggedInMemberId);
            if (!memberExists)
            {
                ModelState.AddModelError("", $"Database Error: Your cookie has Member ID '{loggedInMemberId}', but no row with ID {loggedInMemberId} exists in the Members table.");
                await OnGetAsync();
                return Page();
            }
            // Moving this below your manual checks allows you to safely process remaining fields (like Category)
            if (!ModelState.IsValid)
            {
                await OnGetAsync();
                return Page();
            }
            // Initialize tracking variables with safe data fallbacks
            var rawFileName = "No file attached";
            var uniqueFileName = string.Empty;

            // 4. OPTIONAL FILE PROCESSING PIPELINE
            if (formFile != null) // 🌟 RUNS ONLY IF A FILE IS PRESENT IN THE CELL PICKER
            {
                if (formFile.Length == 0)
                {
                    ModelState.AddModelError("", "The selected file is empty or corrupted. Please pick an active file asset.");
                    await OnGetAsync();
                    return Page();
                }

                // File Extension Validation Rules Guard Check
                var fileExtension = Path.GetExtension(formFile.FileName).ToLowerInvariant();
                if (!_allowedExtensions.Contains(fileExtension))
                {
                    ModelState.AddModelError("", $"Invalid file type. Only {string.Join(", ", _allowedExtensions)} files are allowed.");
                    await OnGetAsync();
                    return Page();
                }

                // Extract and formulate unique file stream naming allocations
                rawFileName = Path.GetFileName(formFile.FileName);
                uniqueFileName = $"{Guid.NewGuid()}_{rawFileName}";
                var fullPhysicalPath = Path.Combine(_storageFolder, uniqueFileName);

                // Pipe file bytes directly onto physical storage allocations
                using (var stream = new FileStream(fullPhysicalPath, FileMode.Create))
                {
                    await formFile.CopyToAsync(stream);
                }
            }
            else
            {
                // 🌟 SUCCESS BOUNDARY: Picker was left empty by choice! 
                // Code execution automatically drops through here without any error states added.
            }

            // 5. SECURELY POPULATE DATABASE DATA RECORD PROFILES
            var clubFile = new ClubFile
            {
                UploadedByMemberId = loggedInMemberId, // 🌟 Safe, verified, and completely derived from the secure server cookie!
                FileName = rawFileName,
                FilePath = uniqueFileName,
                Category = UploadData.Category!.Value.ToString(),
                UploadedAt = DateTime.UtcNow
            };

            _context.ClubFiles.Add(clubFile);
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
            // 🌟 EVALUATE THE CENTRALIZED "isAdmin" POLICY DIRECTLY
            var authResult = await _authorizationService.AuthorizeAsync(User, null, "isAdmin");

            var dbFile = await _context.ClubFiles.FindAsync(id);
            if (dbFile == null) return NotFound();

            // 1. Get the current user's Member ID from their authentication claims
            // (Assuming your authentication cookie stores the database Member ID as a claim)
            var currentUserIdClaim = User.FindFirst("MemberId")?.Value;
            int.TryParse(currentUserIdClaim, out int currentMemberId);

            // 2. Evaluate your specific executive role condition
            bool isAuthorizedOfficer =
            authResult.Succeeded;

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
        // 🌟 THE FIX: Changing to int? clears the automatic "0 is invalid" error
        public int? UploadedByMemberId { get; set; }

        public FileCategory? Category { get; set; }

        public IFormFile? FormFile { get; set; }
    }

}
