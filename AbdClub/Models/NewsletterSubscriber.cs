using System.ComponentModel.DataAnnotations;

namespace AbdClub.Models;

public class NewsletterSubscriber
{
    public int Id { get; set; }

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string FirstName { get; set; } = string.Empty;

    public DateTime SubscribedAt { get; set; } = DateTime.UtcNow;

    // Secure, unguessable key for unsubscription paths
    [Required]
    public Guid UnsubscribeToken { get; set; } = Guid.NewGuid();
}
