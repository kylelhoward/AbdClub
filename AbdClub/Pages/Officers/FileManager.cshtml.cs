using AbdClub.Enums;
using AbdClub.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using YourNamespace.Models;

namespace YourNamespace.Pages
{
    public class FileManagerModel : PageModel
    {
        private readonly MyDbContext _context;
        private readonly string _storageFolder;

        // Define allowed extensions in a whitelist
        private readonly string[] _allowedExtensions = { ".pdf", ".docx" };

        public FileManagerModel(MyDbContext context, IWebHostEnvironment environment)
        {
            _context = context;
            _storageFolder = Path.Combine(environment.ContentRootPath, "UploadedFiles");

            if (!Directory.Exists(_storageFolder)) Directory.CreateDirectory(_storageFolder);
        }

        [BindProperty]
        public ClubFilePostDto UploadData { get; set; } = new();

        public List<ClubFile> SavedClubFiles { get; set; } = new();

        public async Task OnGetAsync()
        {
            SavedClubFiles = await _context.ClubFiles
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

            // Validation Logic: Extract and check file extension
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

            var clubFile = new ClubFile
            {
                UploadedByMemberId = UploadData.UploadedByMemberId,
                FileName = rawFileName,
                FilePath = uniqueFileName,
                Category = UploadData.Category!.Value, // Explicitly typed as FileCategory Enum
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
            var dbFile = await _context.ClubFiles.FindAsync(id);
            if (dbFile == null) return NotFound();

            // Part A: Safely remove the file from physical disk storage
            var physicalPath = Path.Combine(_storageFolder, dbFile.FilePath);
            if (System.IO.File.Exists(physicalPath))
            {
                System.IO.File.Delete(physicalPath);
            }

            // Part B: Purge its metadata row from the database
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
