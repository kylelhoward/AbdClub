using System.ComponentModel.DataAnnotations;
using AbdClub.Data;
using AbdClub.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AbdClub.Pages.Officers.SpecialAnnouncement;

[Authorize(Policy = "isOfficer")]
[RequestSizeLimit(MaximumRequestSize)]
public class EditModel(AbdContext context) : PageModel
{
    public const long MaximumFileSize = 10 * 1024 * 1024;
    public const long MaximumRequestSize = MaximumFileSize + (1024 * 1024);

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public bool HasExistingFile { get; private set; }
    public string? ExistingFileName { get; private set; }
    public DateTime? ExistingUploadedAt { get; private set; }

    [TempData]
    public string? StatusMessage { get; set; }

    public async Task OnGetAsync() => await LoadExistingAsync();

    public async Task<IActionResult> OnPostAsync()
    {
        var existing = await context.SpecialAnnouncements
            .SingleOrDefaultAsync(a => a.Id == Models.SpecialAnnouncement.CurrentAnnouncementId);

        if (Input.File == null && existing == null)
            ModelState.AddModelError("Input.File", "Select a PDF or image to upload.");

        byte[]? fileData = null;
        string? contentType = null;
        string? fileName = null;

        if (Input.File != null)
        {
            if (Input.File.Length == 0)
                ModelState.AddModelError("Input.File", "The selected file is empty.");
            else if (Input.File.Length > MaximumFileSize)
                ModelState.AddModelError("Input.File", "The file must be 10 MB or smaller.");
            else
            {
                await using var stream = new MemoryStream();
                await Input.File.CopyToAsync(stream);
                fileData = stream.ToArray();
                contentType = DetectContentType(fileData);

                if (contentType == null)
                    ModelState.AddModelError("Input.File", "Only genuine PDF, JPG, PNG, or WebP files are allowed.");
                else
                    fileName = SafeFileName(Input.File.FileName, contentType);
            }
        }

        if (!ModelState.IsValid)
        {
            SetExisting(existing);
            return Page();
        }

        var memberIdValue = User.FindFirst("MemberId")?.Value;
        if (!int.TryParse(memberIdValue, out var memberId))
            return Forbid();

        var officerAccountId = await context.OfficerAccounts
            .Where(a => a.MemberId == memberId && a.IsEnabled)
            .Select(a => (int?)a.Id)
            .SingleOrDefaultAsync();

        if (!officerAccountId.HasValue)
            return Forbid();

        if (existing == null)
        {
            existing = new Models.SpecialAnnouncement
            {
                Id = Models.SpecialAnnouncement.CurrentAnnouncementId
            };
            context.SpecialAnnouncements.Add(existing);
        }

        if (fileData != null)
        {
            existing.FileData = fileData;
            existing.ContentType = contentType!;
            existing.OriginalFileName = fileName!;
            existing.UploadedAt = DateTime.UtcNow;
            existing.UploadedByOfficerAccountId = officerAccountId.Value;
        }

        existing.Title = string.IsNullOrWhiteSpace(Input.Title) ? null : Input.Title.Trim();
        existing.IsPublished = Input.IsPublished;

        await context.SaveChangesAsync();
        StatusMessage = existing.IsPublished
            ? "The special announcement was saved and published."
            : "The special announcement was saved but is not published.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRemoveAsync()
    {
        var existing = await context.SpecialAnnouncements
            .SingleOrDefaultAsync(a => a.Id == Models.SpecialAnnouncement.CurrentAnnouncementId);

        if (existing != null)
        {
            context.SpecialAnnouncements.Remove(existing);
            await context.SaveChangesAsync();
        }

        StatusMessage = "The special announcement was removed.";
        return RedirectToPage();
    }

    private async Task LoadExistingAsync()
    {
        var existing = await context.SpecialAnnouncements
            .AsNoTracking()
            .SingleOrDefaultAsync(a => a.Id == Models.SpecialAnnouncement.CurrentAnnouncementId);

        if (existing != null)
        {
            Input.Title = existing.Title;
            Input.IsPublished = existing.IsPublished;
        }

        SetExisting(existing);
    }

    private void SetExisting(Models.SpecialAnnouncement? existing)
    {
        HasExistingFile = existing != null;
        ExistingFileName = existing?.OriginalFileName;
        ExistingUploadedAt = existing?.UploadedAt;
    }

    private static string? DetectContentType(byte[] data)
    {
        if (data.Length >= 5 && data.AsSpan(0, 5).SequenceEqual("%PDF-"u8))
            return "application/pdf";
        if (data.Length >= 3 && data[0] == 0xff && data[1] == 0xd8 && data[2] == 0xff)
            return "image/jpeg";
        if (data.Length >= 8 && data.AsSpan(0, 8).SequenceEqual(new byte[] { 0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a }))
            return "image/png";
        if (data.Length >= 12 && data.AsSpan(0, 4).SequenceEqual("RIFF"u8) &&
            data.AsSpan(8, 4).SequenceEqual("WEBP"u8))
            return "image/webp";
        return null;
    }

    private static string SafeFileName(string suppliedName, string contentType)
    {
        var baseName = Path.GetFileNameWithoutExtension(Path.GetFileName(suppliedName));
        if (string.IsNullOrWhiteSpace(baseName))
            baseName = "special-announcement";

        baseName = string.Concat(baseName.Where(c => char.IsLetterOrDigit(c) || c is '-' or '_' or ' ')).Trim();
        if (baseName.Length > 220)
            baseName = baseName[..220];

        var extension = contentType switch
        {
            "application/pdf" => ".pdf",
            "image/jpeg" => ".jpg",
            "image/png" => ".png",
            "image/webp" => ".webp",
            _ => string.Empty
        };
        return (string.IsNullOrWhiteSpace(baseName) ? "special-announcement" : baseName) + extension;
    }

    public class InputModel
    {
        [StringLength(150)]
        public string? Title { get; set; }

        [Display(Name = "PDF or image")]
        public IFormFile? File { get; set; }

        [Display(Name = "Publish this announcement")]
        public bool IsPublished { get; set; }
    }
}
