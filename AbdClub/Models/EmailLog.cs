using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AbdClub.Models;

public class EmailLog
{
    public int Id { get; set; }

    // Optional link if the email was targeted at an existing database member
    public int? MemberId { get; set; }
    public Member? Member { get; set; }

    [Required, StringLength(255)]
    public string RecipientEmail { get; set; } = string.Empty;

    [Required, StringLength(255)]
    public string Subject { get; set; } = string.Empty;

    // Stores full HTML / Text content for auditing
    [Required]
    public string Body { get; set; } = string.Empty;

    // E.g., "Reminder60", "MagicLink", "GeneralAnnouncement", "PaymentReceipt"
    [Required, StringLength(100)]
    public string EmailType { get; set; } = "General";

    // Tracks WHO or WHAT triggered this email: "System:ReminderService", "Officer:kyle@abdclub.org"
    [Required, StringLength(150)]
    public string TriggeredBy { get; set; } = "System";

    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    public bool IsSuccess { get; set; } = true;

    public string? ErrorMessage { get; set; }
}
