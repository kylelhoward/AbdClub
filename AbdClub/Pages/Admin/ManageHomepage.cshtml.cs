using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AbdClub.Data;
using AbdClub.Models;

namespace AbdClub.Pages.Admin;

[Authorize(Policy = "isAdmin")]
public class ManageHomepageModel : PageModel
{
    private readonly AbdContext _context;
    private readonly IWebHostEnvironment _env;

    public ManageHomepageModel(AbdContext context, IWebHostEnvironment env)
    {
        _context = context;
        _env = env;
    }

    [BindProperty] public HomepageContent ContentInput { get; set; } = default!;
    [BindProperty] public CarouselSlide NewSlide { get; set; } = new();
    [BindProperty] public IFormFile? SlideUploadFile { get; set; }

    public List<CarouselSlide> ExistingSlides { get; set; } = new();

    public async Task<IActionResult> OnGetAsync()
    {
        ExistingSlides = await _context.CarouselSlides.OrderBy(s => s.DisplayOrder).ToListAsync();
        ContentInput = await _context.HomepageContents.FirstOrDefaultAsync(c => c.Id == 1)
                       ?? new HomepageContent { Id = 1 };
        return Page();
    }

    // HANDLER 1: Update Marketing Text Fields
    public async Task<IActionResult> OnPostSaveTextAsync()
    {
        if (!ModelState.IsValid) return Page();

        var existing = await _context.HomepageContents.FindAsync(1);
        if (existing == null)
        {
            ContentInput.Id = 1;
            _context.HomepageContents.Add(ContentInput);
        }
        else
        {
            existing.MarketingHeader = ContentInput.MarketingHeader.Trim();
            existing.MarketingSubtitle = ContentInput.MarketingSubtitle.Trim();
        }

        await _context.SaveChangesAsync();
        return RedirectToPage();
    }

    // HANDLER 2: Upload and Register a Brand New Carousel Slide Entry
    public async Task<IActionResult> OnPostAddSlideAsync()
    {
        if (SlideUploadFile == null || SlideUploadFile.Length == 0)
        {
            ModelState.AddModelError("SlideUploadFile", "You must choose an image file to upload.");
            return await OnGetAsync() == null ? Page() : Page();
        }

        string fileName = $"slide_{Guid.NewGuid()}{Path.GetExtension(SlideUploadFile.FileName).ToLower()}";
        string targetFolder = Path.Combine(_env.WebRootPath, "images", "dances");

        if (!Directory.Exists(targetFolder)) Directory.CreateDirectory(targetFolder);

        using (var stream = new FileStream(Path.Combine(targetFolder, fileName), FileMode.Create))
        {
            await SlideUploadFile.CopyToAsync(stream);
        }

        NewSlide.PhotoUrl = $"/images/dances/{fileName}";
        _context.CarouselSlides.Add(NewSlide);
        await _context.SaveChangesAsync();

        return RedirectToPage();
    }

    // HANDLER 3: Delete an Existing Carousel Slide
    public async Task<IActionResult> OnPostDeleteSlideAsync(int id)
    {
        var slide = await _context.CarouselSlides.FindAsync(id);
        if (slide != null)
        {
            _context.CarouselSlides.Remove(slide);
            await _context.SaveChangesAsync();
        }
        return RedirectToPage();
    }
}

