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

            // Step A: Save the data row first. This is preserved regardless of email connection status
            var newSubscriber = new NewsletterSubscriber
            {
                FirstName = SubscriberData.FirstName,
                Email = SubscriberData.Email,
                SubscribedAt = DateTime.UtcNow
            };

            _context.NewsletterSubscribers.Add(newSubscriber);
            await _context.SaveChangesAsync();

            // Step B: Call the service. The internal service handles errors gracefully
            await _emailService.SendNewsletterWelcomeEmailAsync(newSubscriber.Email, newSubscriber.FirstName);

            // Step C: The user is redirected to the confirmation screen successfully
            TempData["SignupMessage"] = $"Thank you, {SubscriberData.FirstName}! You are now on our list.";
            return RedirectToPage();
        }


    }
}
