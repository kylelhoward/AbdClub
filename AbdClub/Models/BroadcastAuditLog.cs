using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AbdClub.Models
{
    public class BroadcastAuditLog
    {
        public int Id { get; set; }

        [Required]
        public int SentByOfficerId { get; set; }

        [Required]
        [StringLength(100)]
        public string Subject { get; set; } = string.Empty;

        [Required]
        [StringLength(4000)]
        public string MessageContent { get; set; } = string.Empty;

        [Required]
        public int RecipientCount { get; set; }

        [Required]
        public DateTime SentAt { get; set; } = DateTime.UtcNow;

        // Navigation property to map back to the primary Member details
        [ForeignKey("SentByOfficerId")]
        public Member SentByOfficer { get; set; } = null!;
    }
}

