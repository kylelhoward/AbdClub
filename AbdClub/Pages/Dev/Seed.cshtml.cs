using AbdClub.Data;
using AbdClub.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

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
    public List<Location> LocationsList { get; set; } = new();
    public List<Dance> DancesList { get; set; } = new();

    public List<MasterDJ> Djs { get; set; } = new();
    public List<MasterHost> Hosts { get; set; } = new();
    public List<MasterInstructor> Instructors { get; set; } = new();
    public List<MasterVolunteer> Volunteers { get; set; } = new();
    public int DjsCount { get; set; }
    public int HostCount { get; set; }
    public int InstCount { get; set; }
    public int VolCount { get; set; }

    public async Task OnGetAsync()
    {
        await LoadDashboardMetricsAsync();
    }

    private async Task LoadDashboardMetricsAsync()
    {
        await LoadMembersAsync();
        await LoadNewsLetterSubscribersAsync();
        await LoadRegistryPersonsAsync();
        await LoadLocationListAsync();
        await LoadLocationListAsync();
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
            await LoadMembersAsync();
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
        await LoadMembersAsync();
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
        await LoadMembersAsync();
        return Page();
    }

    private async Task LoadMembersAsync()
    {
        Members = await _db.Members
            .OrderBy(m => m.ExpiryDate)
            .ToListAsync();
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
        await LoadNewsLetterSubscribersAsync();
        return Page();
    }

    private async Task LoadNewsLetterSubscribersAsync()
    {
        Subs = await _db.NewsletterSubscribers
            .OrderBy(m => m.SubscribedAt)
            .ToListAsync();
        SubCount = Subs.Count;
    }
    private async Task LoadLocationListAsync()
    {
        // Hydrate Locations for the new Sandbox accordion panes
        LocationsList = await _db.Locations.OrderBy(l => l.VenueName).ToListAsync();
    }
    private async Task LoadDanceListAsync()
    {
        // Hydrate Dances for the new Sandbox accordion panes
        DancesList = await _db.Events.OfType<Dance>().Include(d => d.Location).OrderBy(d => d.Date).ToListAsync();
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
            await LoadMembersAsync();
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
        await LoadMembersAsync();
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
        await LoadRegistryPersonsAsync();
        return RedirectToPage();
    }
    private async Task LoadRegistryPersonsAsync()
    {
        // 🌟 ASYNC TASK MATRIX: Concurrently query each independent table thread non-blockingly
        Djs = await _db.MasterDjs.OrderBy(m => m.Name).ToListAsync();
        DjsCount = Djs.Count;

        Hosts = await _db.MasterHosts.OrderBy(m => m.Name).ToListAsync();
        HostCount = Hosts.Count;

        Instructors = await _db.MasterInstructors.OrderBy(m => m.Name).ToListAsync();
        InstCount = Instructors.Count;

        Volunteers = await _db.MasterVolunteers.OrderBy(m => m.Name).ToListAsync();
        VolCount = Volunteers.Count;
    }



    // 3. Paste the Locations Seeder Handler Method:
    public async Task<IActionResult> OnPostSeedLocationsAsync()
    {
        bool isAuthorized = User.IsInRole("TechAdmin");
        if (!isAuthorized) return Forbid();

        var testVenues = new List<Location>
    {
        new() { VenueName = "Go Dance South", Address = "4477 S Lamar Blvd, Austin, TX 78745", Description = "Main ballroom entrance. Door code: #4242", GoogleMapsUrl = "https://google.com", PhotoUrl = "/images/venues/godance.jpg" },
        new() { VenueName = "Go Dance North", Address = "2525 W Anderson Ln., Austin, TX 78757", Description = "Studio 2 corridor entry setup.", GoogleMapsUrl = "https://google.com", PhotoUrl = "/images/venues/fallback.jpg" },
        new() { VenueName = "Fedora Club Hall", Address = "1200 San Jacinto Blvd, Austin, TX 78701", Description = "Street parking requires city meters. Back door loading ramp rules active.", GoogleMapsUrl = "https://google.com", PhotoUrl = "/images/venues/fallback.jpg" }
    };

        int addedCount = 0;
        int skippedCount = 0;

        foreach (var loc in testVenues)
        {
            if (!await _db.Locations.AnyAsync(l => l.VenueName == loc.VenueName))
            {
                _db.Locations.Add(loc);
                addedCount++;
            }
            else
            {
                skippedCount++;
            }
        }

        await _db.SaveChangesAsync();
        Message = $"Seeded {addedCount} test venue locations ({skippedCount} skipped — already exist).";

        await LoadLocationListAsync();
        return Page();
    }

    // 4. Paste the Relational Dances Seeder Handler Method:
    public async Task<IActionResult> OnPostSeedDancesAsync()
    {
        bool isAuthorized = User.IsInRole("TechAdmin");
        if (!isAuthorized) return Forbid();

        // Fetch an active location from the database to map our foreign key dependencies
        var primaryVenue = await _db.Locations.FirstOrDefaultAsync(l => l.VenueName == "Go Dance South");
        var backupVenue = await _db.Locations.FirstOrDefaultAsync(l => l.VenueName == "Go Dance North")
                          ?? primaryVenue;

        if (primaryVenue == null)
        {
            Message = "Error: Cannot seed dances. No location profiles exist inside database registries. Run Locations seeder first.";
            await LoadDanceListAsync();
            return Page();
        }

        var baseDate = DateOnly.FromDateTime(DateTime.Today);
        var mockDances = new List<Dance>
    {
        new() { Title = "Summer Retro Swing Gala", Description = "Join us for vintage big band sounds and high-energy social dancing!", ContactEmail = "admin@hillcountrywebco.com", Date = baseDate.AddDays(7), StartTime = new TimeOnly(19, 0), EndTime = new TimeOnly(22, 0), LocationId = primaryVenue.Id },
        new() { Title = "Salsa & Bachata Fusion Night", Description = "Introductory latin training layers followed by continuous club mixes.", ContactEmail = "admin@hillcountrywebco.com", Date = baseDate.AddDays(14), StartTime = new TimeOnly(19, 30), EndTime = new TimeOnly(22, 30), LocationId = backupVenue.Id },
        new() { Title = "Rockabilly Jive Social", Description = "A high-octane jump blues social session open to all experience ranks.", ContactEmail = "admin@hillcountrywebco.com", Date = baseDate.AddDays(21), StartTime = new TimeOnly(20, 0), EndTime = new TimeOnly(23, 0), LocationId = primaryVenue.Id }
    };

        int addedCount = 0;
        int skippedCount = 0;

        foreach (var dance in mockDances)
        {
            if (!await _db.Events.OfType<Dance>().AnyAsync(d => d.Title == dance.Title && d.Date == dance.Date))
            {
                _db.Events.Add(dance);
                addedCount++;
            }
            else
            {
                skippedCount++;
            }
        }

        await _db.SaveChangesAsync();
        Message = $"Seeded {addedCount} upcoming relational dance entries ({skippedCount} skipped — already exist).";

        await LoadDanceListAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostSeedHomepageAsync()
    {
        // Authorization layer check to verify administrator identities
        bool isAuthorized = User.IsInRole("TechAdmin") || User.IsInRole("Admin");
        if (!isAuthorized) return Forbid();

        // 1. SEED HOMEPAGE MARKETING TEXT COPIES
        var existingContent = await _db.HomepageContents.FindAsync(1);
        if (existingContent == null)
        {
            _db.HomepageContents.Add(new HomepageContent
            {
                Id = 1,
                MarketingHeader = "Welcome to Austin Ballroom Dancers",
                MarketingSubtitle = "Join us for weekly social dances, foundational technique lessons, and seasonal showcases open to all experience levels across Central Texas!"
            });
        }
        else
        {
            existingContent.MarketingHeader = "Welcome to Austin Ballroom Dancers";
            existingContent.MarketingSubtitle = "Join us for weekly social dances, foundational technique lessons, and seasonal showcases open to all experience levels across Central Texas!";
        }

        // 2. SEED DEFAULT PHOTO CAROUSEL SLIDES
        // Clear out existing mockup rows first to prevent duplicate structural keys crashes
        var existingSlides = await _db.CarouselSlides.ToListAsync();
        if (existingSlides.Any())
        {
            _db.CarouselSlides.RemoveRange(existingSlides);
        }

        var defaultSlides = new List<CarouselSlide>
        {
            new() { Title = "Our Annual Summer Gala", Subtitle = "Dancers gathering from across the state for our premier ballroom showcase event.", PhotoUrl = "/images/dances/abd_great_waltz.webp", DisplayOrder = 1 },
            new() { Title = "Friday Night Swing Socials", Subtitle = "Weekly high-energy drop-in sessions open to the public with no partner required.", PhotoUrl = "/images/dances/ball.webp", DisplayOrder = 2 },
            new() { Title = "Community Showcase Recitals", Subtitle = "Celebrating club membership accomplishments through coordinated team choreography.", PhotoUrl = "/images/dances/abd_party_hancock.webp", DisplayOrder = 3 }
        };

        _db.CarouselSlides.AddRange(defaultSlides);

        // Save changes to database layout tracking tables
        await _db.SaveChangesAsync();

        // Set dashboard response tracking notification string
        // Assuming your page model class uses a public 'Message' or 'StatusNotice' bind string variable
        TempData["StatusNotice"] = "Success: Seeded homepage copy defaults and initialized 3 carousel gallery rows.";

        // Reload metric structures as usual to refresh the dev dashboard interface state
        await LoadDashboardMetricsAsync();
        return Page();
    }

}