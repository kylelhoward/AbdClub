using AbdClub.Data;
using AbdClub.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AbdClub.Pages.Dev;

public class SeedModel : PageModel
{
    private readonly AbdContext _db;
    public SeedModel(AbdContext db) => _db = db;

    public string Message { get; set; } = string.Empty;
    public List<Member> Members { get; set; } = new();
    public int MemberCount { get; set; }

    public void OnGet()
    {
        LoadMembers();
    }

    public async Task<IActionResult> OnPostAsync(
        string fullName, string email, string? phone,
        DateTime expiryDate, string memberType, string? officerRole)
    {
        if (!IsDev()) return NotFound();

        var existing = _db.Members.FirstOrDefault(m => m.Email == email);
        if (existing != null)
        {
            Message = $"Member with email {email} already exists.";
            LoadMembers();
            return Page();
        }

        _db.Members.Add(new Member
        {
            FullName = fullName,
            Email = email,
            Phone = phone,
            JoinDate = DateTime.UtcNow,
            ExpiryDate = expiryDate,
            IsOfficer = memberType == "officer",
            OfficerRole = memberType == "officer" ? officerRole : null,
            IsActive = true
        });

        await _db.SaveChangesAsync();
        Message = $"Added: {fullName} ({email}) — expires {expiryDate:yyyy-MM-dd}";
        LoadMembers();
        return Page();
    }

    // One-click seed of 10 realistic test members
    public async Task<IActionResult> OnPostSeedAllAsync()
    {
        if (!IsDev()) return NotFound();

        var today = DateTime.UtcNow;
        var testMembers = new List<Member>
        {
            // Active members
            new() { FullName = "Alice Johnson",   Email = "alice.johnson.test@gmail.com",
                    JoinDate = today, ExpiryDate = today.AddYears(1),  IsActive = true },
            new() { FullName = "Bob Martinez",    Email = "bob.martinez.test@gmail.com",
                    JoinDate = today, ExpiryDate = today.AddMonths(8), IsActive = true },
            new() { FullName = "Carol Williams",  Email = "carol.williams.test@gmail.com",
                    JoinDate = today, ExpiryDate = today.AddMonths(6), IsActive = true },

            // Expiring soon
            new() { FullName = "David Chen",      Email = "david.chen.test@gmail.com",
                    JoinDate = today, ExpiryDate = today.AddDays(55),  IsActive = true },
            new() { FullName = "Eve Thompson",    Email = "eve.thompson.test@gmail.com",
                    JoinDate = today, ExpiryDate = today.AddDays(28),  IsActive = true },
            new() { FullName = "Frank Garcia",    Email = "frank.garcia.test@gmail.com",
                    JoinDate = today, ExpiryDate = today.AddDays(5),   IsActive = true },

            // Expired
            new() { FullName = "Grace Lee",       Email = "grace.lee.test@gmail.com",
                    JoinDate = today.AddYears(-1), ExpiryDate = today.AddDays(-10),
                    IsActive = true },
            new() { FullName = "Henry Wilson",    Email = "henry.wilson.test@gmail.com",
                    JoinDate = today.AddYears(-1), ExpiryDate = today.AddDays(-45),
                    IsActive = true },

            // Officers
            new() { FullName = "Isabel Cruz",     Email = "isabel.cruz.test@gmail.com",
                    JoinDate = today, ExpiryDate = today.AddYears(1),
                    IsActive = true, IsOfficer = true, OfficerRole = "President" },
            new() { FullName = "James Park",      Email = "james.park.test@gmail.com",
                    JoinDate = today, ExpiryDate = today.AddYears(1),
                    IsActive = true, IsOfficer = true, OfficerRole = "Treasurer" },
        };

        // Skip any emails already in the database
        var existingEmails = _db.Members
            .Select(m => m.Email)
            .ToHashSet();

        var toAdd = testMembers
            .Where(m => !existingEmails.Contains(m.Email))
            .ToList();

        _db.Members.AddRange(toAdd);
        await _db.SaveChangesAsync();

        Message = $"Seeded {toAdd.Count} test members " +
                  $"({testMembers.Count - toAdd.Count} skipped — already exist).";
        LoadMembers();
        return Page();
    }

    private void LoadMembers()
    {
        Members = _db.Members
            .OrderBy(m => m.ExpiryDate)
            .ToList();
        MemberCount = Members.Count;
    }

    // In SeedModel — simulate a Stripe payment for testing
    public async Task<IActionResult> OnPostSimulatePaymentAsync(
        string fullName, string email, string? phone)
    {
        if (!IsDev()) return NotFound();

        var existing = _db.Members
            .FirstOrDefault(m => m.Email == email);

        if (existing != null)
        {
            Message = $"{email} is already a member.";
            LoadMembers();
            return Page();
        }

        var member = new Member
        {
            FullName = fullName,
            Email = email,
            Phone = phone,
            JoinDate = DateTime.UtcNow,
            ExpiryDate = DateTime.UtcNow.AddYears(1),
            IsActive = true
        };

        _db.Members.Add(member);
        await _db.SaveChangesAsync();

        _db.Payments.Add(new Payment
        {
            MemberId = member.Id,
            Amount = 50.00m,
            PaymentDate = DateTime.UtcNow,
            PeriodStart = DateTime.UtcNow,
            PeriodEnd = member.ExpiryDate!.Value,
            TransactionId = "dev_simulated_" + Guid.NewGuid().ToString()[..8],
            Status = "Completed"
        });

        await _db.SaveChangesAsync();
        Message = $"Simulated payment — member {fullName} ({email}) created.";
        LoadMembers();
        return Page();
    }

    private bool IsDev() => HttpContext.RequestServices
        .GetRequiredService<IWebHostEnvironment>()
        .IsDevelopment();
}