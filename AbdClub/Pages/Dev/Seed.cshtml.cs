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
    public List<NewsletterSubscriber> Subs { get; set; } = new();
    public int SubCount { get; set; }

    public List<MasterDJ> Djs { get; set; } = new();
    public List<MasterHost> Hosts { get; set; } = new();
    public List<MasterInstructor> Instructors { get; set; } = new();
    public List<MasterVolunteer> Volunteers { get; set; } = new();
    public int DjsCount { get; set; }
    public int HostCount { get; set; }
    public int InstCount { get; set; }
    public int VolCount { get; set; }

    public void OnGet()
    {
        LoadMembers();
        LoadNewsLetterSubscribers();
        LoadRegristryPersons();
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
    public async Task<IActionResult> OnPostSeedMembersAsync()
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

    // One-click seed of 10 realistic test members
    public async Task<IActionResult> OnPostNewsLetterSubscribersAsync()
    {
        if (!IsDev()) return NotFound();

        var today = DateTime.UtcNow;
        var testSubs = new List<NewsletterSubscriber>
        {
            // Active members
           new() { FirstName = "Galadriel", Email = "galadriel.sub.test@gmail.com", SubscribedAt = today },
new() { FirstName = "Frodo", Email = "frodo.sub.test@gmail.com", SubscribedAt = today },
new() { FirstName = "SamGamgee", Email = "samGamgee.sub.test@gmail.com", SubscribedAt = today },
new() { FirstName = "Aragorn", Email = "aragorn.sub.test@gmail.com", SubscribedAt = today },
new() { FirstName = "Legolas", Email = "legolas.sub.test@gmail.com", SubscribedAt = today },
new() { FirstName = "Gimli", Email = "gimli.sub.test@gmail.com", SubscribedAt = today },
new() { FirstName = "Gandalf", Email = "gandalf.sub.test@gmail.com", SubscribedAt = today },
new() { FirstName = "Boromir", Email = "boromir.sub.test@gmail.com", SubscribedAt = today },
new() { FirstName = "Merry", Email = "merry.sub.test@gmail.com", SubscribedAt = today },
new() { FirstName = "Pippin", Email = "pippin.sub.test@gmail.com", SubscribedAt = today },

                    };

        // Skip any emails already in the database
        var existingEmails = _db.NewsletterSubscribers
            .Select(m => m.Email)
            .ToHashSet();

        var toAdd = testSubs
            .Where(m => !existingEmails.Contains(m.Email))
            .ToList();

        _db.NewsletterSubscribers.AddRange(toAdd);
        await _db.SaveChangesAsync();

        Message = $"Seeded {toAdd.Count} test news letter subscribers " +
                  $"({testSubs.Count - toAdd.Count} skipped — already exist).";
        LoadNewsLetterSubscribers();
        return Page();
    }

    private void LoadNewsLetterSubscribers()
    {
        Subs = _db.NewsletterSubscribers
            .OrderBy(m => m.SubscribedAt)
            .ToList();
        SubCount = Subs.Count;
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




    // (Make sure to populate these lists using a basic await _db.MasterDjs.ToListAsync() inside your OnGetAsync method)

    // 2. Paste this Handler into Pages/Dev/Index.cshtml.cs
    public async Task<IActionResult> OnPostSeedStaffRegistriesAsync()
    {
        // Safety authorization block match
        bool isAuthorized = User.IsInRole("TechAdmin");
        if (!isAuthorized) return Forbid();

        // Clear existing mock entries first to avoid unique key index duplication crashes
        _db.MasterDjs.RemoveRange(_db.MasterDjs);
        _db.MasterHosts.RemoveRange(_db.MasterHosts);
        _db.MasterInstructors.RemoveRange(_db.MasterInstructors);
        _db.MasterVolunteers.RemoveRange(_db.MasterVolunteers);
        await _db.SaveChangesAsync();

        

        // SEED DJs
        _db.MasterDjs.AddRange(new List<MasterDJ> {
        new() { Name = "DJ Swing Kid", Email = "swingkid.test@gmail.com", Phone = "555-0101", Notes = "Lindy Hop specialist." },
        new() { Name = "DJ Shuffle Cat", Email = "shufflecat.test@gmail.com", Phone = "555-0102", Notes = "West Coast modern tracking." },
        new() { Name = "DJ Retro Spin", Email = "retrospin.test@gmail.com", Phone = "555-0103", Notes = "Old school jazz vinyl sets." }
    });

        // SEED HOSTS
        _db.MasterHosts.AddRange(new List<MasterHost> {
        new() { Name = "Aragorn Ranger", Email = "aragorn.host.test@gmail.com", Phone = "555-0201", Notes = "Experienced floor manager." },
        new() { Name = "Boromir Gondor", Email = "boromir.host.test@gmail.com", Phone = "555-0202", Notes = "Front gate monitor security." },
        new() { Name = "Faramir Ithilien", Email = "faramir.host.test@gmail.com", Phone = "555-0203", Notes = "Greeter team leader." }
    });

        // SEED INSTRUCTORS
        _db.MasterInstructors.AddRange(new List<MasterInstructor> {
        new() { Name = "Galadriel Lorien", Email = "galadriel.instructor.test@gmail.com", Phone = "555-0301", Notes = "Advanced Ballroom Waltz tech." },
        new() { Name = "Elrond Rivendell", Email = "elrond.instructor.test@gmail.com", Phone = "555-0302", Notes = "Salsa & Bachata sequence flow." },
        new() { Name = "Celeborn Caras", Email = "celeborn.instructor.test@gmail.com", Phone = "555-0303", Notes = "Foxtrot & Tango introduction tracks." }
    });

        // SEED VOLUNTEERS
        _db.MasterVolunteers.AddRange(new List<MasterVolunteer> {
        new() { Name = "Frodo Baggins", Email = "frodo.vol.test@gmail.com", Phone = "555-0401", Notes = "Front Desk check-in backup operations." },
        new() { Name = "Samwise Gamgee", Email = "samwise.vol.test@gmail.com", Phone = "555-0402", Notes = "Setup / Teardown equipment team." },
        new() { Name = "Peregrin Took", Email = "pippin.vol.test@gmail.com", Phone = "555-0403", Notes = "Refreshment tables and hydration manager." }
    });

        await _db.SaveChangesAsync();
        LoadRegristryPersons();
        return RedirectToPage();
    }
    private void LoadRegristryPersons()
    {
        Djs = [.. _db.MasterDjs.OrderBy(m => m.Name)];
        DjsCount = Djs.Count;
        Hosts = [.. _db.MasterHosts.OrderBy(m => m.Name)];
        HostCount = Hosts.Count;
        Instructors = [.. _db.MasterInstructors.OrderBy(m => m.Name)];
        InstCount = Instructors.Count;
        Volunteers = [.. _db.MasterVolunteers.OrderBy(m => m.Name)];
        VolCount = Volunteers.Count;
    }

}