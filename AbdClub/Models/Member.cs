using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
namespace AbdClub.Models;

public class Member
{
    public int Id { get; set; }
    public int MemberNumber { get; set; }

    [NotMapped]
    public string DisplayMemberNumber => $"ABD-{MemberNumber:D5}";
    // 🌟 THE CLEAN ARCHITECTURE UPGRADE
    [Required, StringLength(50)] public string FirstName { get; set; } = string.Empty;
    [StringLength(50)] public string? MiddleName { get; set; }
    [Required, StringLength(50)] public string LastName { get; set; } = string.Empty;
    // 🌟 COMPATIBILITY BRIDGE: Combines names automatically on the fly for your frontend views
    [NotMapped]
    public string FullName => string.IsNullOrWhiteSpace(MiddleName)
        ? $"{FirstName} {LastName}"
        : $"{FirstName} {MiddleName} {LastName}";
    public string Email { get; set; } = string.Empty;
    public string? Phone { get; set; }
    // Legacy authentication fields are retained temporarily so the migration can
    // copy existing officer access into OfficerAccounts. Runtime authorization no
    // longer reads these fields.
    public string? GoogleSubId { get; set; }
    public DateTime JoinDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public bool IsOfficer { get; set; } = false;
    public bool IsAdmin { get; set; } = false;
    public bool IsTechAdmin { get; set; } = false;
    public string? OfficerRole { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public bool SelfRegistered { get; set; } = false;
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public ICollection<EmailLog> EmailLogs { get; set; } = new List<EmailLog>();

// 🌟 THE ADMINISTRATIVE MANUAL OVERRIDE FLAG:
    // Keeps a physical column in the database table. Defaults to false.
    public bool IsSuspended { get; set; } = false;

    // 🌟 SMART CALCULATED PROPERTY: 
    // A member is only active if their date is valid AND they aren't manually suspended!
    [NotMapped]
    public bool IsActive => 
        !IsSuspended && 
        ExpiryDate.HasValue && 
        ExpiryDate.Value.Date >= DateTime.UtcNow.Date;

    // UI Helper properties adapt automatically
    [NotMapped]
    public bool IsExpired => !ExpiryDate.HasValue || ExpiryDate.Value.Date < DateTime.UtcNow.Date;

    [NotMapped]
    public bool IsExpiringSoon => 
        !IsSuspended &&
        ExpiryDate.HasValue && 
        ExpiryDate.Value.Date >= DateTime.UtcNow.Date && 
        ExpiryDate.Value.Date <= DateTime.UtcNow.AddDays(30).Date;

}
