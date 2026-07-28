using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using AbdClub.Data;

namespace AbdClub.Pages.Newsletter
{
    public class UnsubscribeModel : PageModel
    {
        private readonly AbdContext _context;
        private readonly ILogger<UnsubscribeModel> _logger;

        public UnsubscribeModel(AbdContext context, ILogger<UnsubscribeModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Binds the incoming token safely across both GET and POST requests
        [BindProperty(SupportsGet = true)]
        public Guid? Token { get; set; }

        public string SubscriberEmail { get; set; } = string.Empty;
        public bool IsValidToken { get; set; }

        [TempData]
        public string? StatusMessage { get; set; }

        // Step A: Render the confirmation screen (Safe for automated spam bots)
        public async Task<IActionResult> OnGetAsync()
        {
            if (Token == null || Token == Guid.Empty)
            {
                StatusMessage = "Invalid or missing unsubscription link.";
                return Page();
            }

            // Verify the subscriber exists before rendering the button
            var subscriber = await _context.NewsletterSubscribers
                .FirstOrDefaultAsync(s => s.UnsubscribeToken == Token);

            if (subscriber == null)
            {
                // Masking the message prevents email enumeration vulnerability vectors
                StatusMessage = "You have been successfully removed from our mailing list.";
                return Page();
            }

            // Expose the masked email to the user so they know who is being opted out
            SubscriberEmail = MaskEmail(subscriber.Email);
            IsValidToken = true;
            return Page();
        }

        // Step B: Execute the actual database purge (Only triggered by real human interaction)
        public async Task<IActionResult> OnPostConfirmAsync()
        {
            if (Token == null || Token == Guid.Empty)
            {
                StatusMessage = "Invalid request security context.";
                return Page();
            }

            var subscriber = await _context.NewsletterSubscribers
                .FirstOrDefaultAsync(s => s.UnsubscribeToken == Token);

            if (subscriber != null)
            {
                _context.NewsletterSubscribers.Remove(subscriber);
                await _context.SaveChangesAsync();
                _logger.LogInformation("Human-confirmed unsubscribe executed for subscriber ID {Id}", subscriber.Id);
            }

            StatusMessage = "You have been successfully removed from our mailing list. We are sorry to see you go!";
            IsValidToken = false; // Hides the form interface components on refresh

            return Page();
        }

        private string MaskEmail(string email)
        {
            var parts = email.Split('@');
            if (parts.Length != 2 || parts[0].Length < 3) return email;
            return $"{parts[0][..2]}***@{parts[1]}";
        }
    }
}
