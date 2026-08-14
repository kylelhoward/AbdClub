using AbdClub.Models;
using Microsoft.EntityFrameworkCore;

namespace AbdClub.Data;

public class AbdContext : DbContext
{
    public AbdContext(DbContextOptions<AbdContext> options) : base(options) { }

    public DbSet<CarouselSlide> CarouselSlides { get; set; } = null!;
    public DbSet<HomepageContent> HomepageContents { get; set; } = null!;
    public DbSet<Member> Members { get; set; } = null!;
    public DbSet<Payment> Payments { get; set; } = null!;
    public DbSet<EmailLog> EmailLogs { get; set; } = null!;
    public DbSet<MeetingNote> MeetingNotes { get; set; } = null!;
    public DbSet<ClubFile> ClubFiles { get; set; } = null!;
    public DbSet<Event> Events { get; set; } = null!;
    public DbSet<Dance> Dances { get; set; } = null!;
    public DbSet<Lesson> Lessons { get; set; } = null!;
    public DbSet<Location> Locations { get; set; } = null!;
    public DbSet<NewsletterSubscriber> NewsletterSubscribers { get; set; } = null!;
    public DbSet<BroadcastAuditLog> BroadcastAuditLogs { get; set; }
    public DbSet<MagicLink> MagicLinks { get; set; } = null!;
    public DbSet<MasterDJ> MasterDjs { get; set; }
    public DbSet<MasterHost> MasterHosts { get; set; }
    public DbSet<MasterInstructor> MasterInstructors { get; set; }
    public DbSet<MasterVolunteer> MasterVolunteers { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
       
      
        // Pre-existing TPH Hierarchical configurations
        modelBuilder.Entity<Event>()
            .HasDiscriminator<string>("EventType")
            .HasValue<Event>("Event")
            .HasValue<Dance>("Dance");

        // 🌟 ATTACH RELATIONAL LOCATION RULE TO THE BASE ENTITY:
        // This tells EF Core that any entry inside the Events table maps to a Location primary key
        modelBuilder.Entity<Event>()
            .HasOne(e => e.Location)
            .WithMany(l => l.Events)
            .HasForeignKey(e => e.LocationId)
            .OnDelete(DeleteBehavior.Restrict); // Prevent dropping a venue that has active matches


        // One-to-Many: MasterInstructor -> Lessons (An instructor can teach multiple lessons over time)
        modelBuilder.Entity<Lesson>()
            .HasOne(l => l.Instructor)
            .WithMany() // Leave empty if your MasterInstructor class doesn't have an explicit navigation collection list
            .HasForeignKey(l => l.InstructorId)
            .OnDelete(DeleteBehavior.Restrict); // Restrict stops accidental deletions of instructors with active classes


        // One-to-Many: DJ Lookup -> Dances
        modelBuilder.Entity<Dance>()
            .HasOne(d => d.AssignedDj)
            .WithMany()
            .HasForeignKey(d => d.AssignedDjId)
            .OnDelete(DeleteBehavior.SetNull);

        // Many-to-Many Junction table maps for reusable assignments
        modelBuilder.Entity<Dance>().HasMany(d => d.AssignedHosts).WithMany().UsingEntity(j => j.ToTable("DanceAssignedHosts"));
        modelBuilder.Entity<Dance>().HasMany(d => d.AssignedInstructors).WithMany().UsingEntity(j => j.ToTable("DanceAssignedInstructors"));
        modelBuilder.Entity<Dance>().HasMany(d => d.AssignedVolunteers).WithMany().UsingEntity(j => j.ToTable("DanceAssignedVolunteers"));


        modelBuilder.Entity<ClubFile>()
            .HasOne(f => f.UploadedBy)
            .WithMany() // or .WithMany(m => m.ClubFiles) if Member has a collection
            .HasForeignKey(f => f.UploadedByMemberId)
            .HasConstraintName("FK_ClubFiles_Members_UploadedById"); // Maps cleanly to your Postgres constraint

        modelBuilder.Entity<NewsletterSubscriber>()
            .HasIndex(n => n.Email)
            .IsUnique();
    }
}
