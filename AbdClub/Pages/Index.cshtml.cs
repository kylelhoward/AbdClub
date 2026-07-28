using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AbdClub.Data;
using AbdClub.Models;
using AbdClub.Services.Interfaces;

namespace AbdClub.Pages
{
    public class IndexModel : PageModel
    {
        private readonly AbdContext _context;
        private readonly IEmailService _emailService; // Injecting the dependency interface

        public IndexModel(AbdContext context, IEmailService emailService)
        {
            _context = context;
            _emailService = emailService;
        }

        [BindProperty]
        public SubscriberInputDto SubscriberData { get; set; } = new();

        public void OnGet() { }

        public async Task<IActionResult> OnPostSubscribeAsync()
        {
            if (!ModelState.IsValid) return Page();

            bool emailExists = await _context.NewsletterSubscribers
                .AnyAsync(s => s.Email.ToLower() == SubscriberData.Email.ToLower());

            if (emailExists)
            {
                ModelState.AddModelError("SubscriberData.Email", "This email address is already subscribed.");
                return Page();
            }

            var newSubscriber = new NewsletterSubscriber
            {
                FirstName = SubscriberData.FirstName,
                Email = SubscriberData.Email,
                SubscribedAt = DateTime.UtcNow
            };

            _context.NewsletterSubscribers.Add(newSubscriber);
            await _context.SaveChangesAsync();

            // CLEAN PATTERN CALL: Simple, readable, single line abstract method invocation
            await _emailService.SendNewsletterWelcomeEmailAsync(newSubscriber.Email, newSubscriber.FirstName);

            TempData["SignupMessage"] = $"Thank you, {SubscriberData.FirstName}! You are now on our list.";
            return RedirectToPage();
        }
    }
}
