using AbdClub.Models;
using Microsoft.EntityFrameworkCore;

namespace AbdClub.Data;

public class AbdContext : DbContext
{
    public AbdContext(DbContextOptions<AbdContext> options) : base(options) { }

    public DbSet<Member> Members { get; set; } = null!;
    public DbSet<Payment> Payments { get; set; } = null!;
    public DbSet<EmailLog> EmailLogs { get; set; } = null!;
    public DbSet<MeetingNote> MeetingNotes { get; set; } = null!;
    public DbSet<ClubFile> ClubFiles { get; set; } = null!;
    public DbSet<Event> Events { get; set; } = null!;
    public DbSet<Dance> Dances { get; set; } = null!;
    public DbSet<Lesson> Lessons { get; set; } = null!;
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

        // TPH discriminator for Event hierarchy
        modelBuilder.Entity<Event>()
            .HasDiscriminator<string>("EventType")
            .HasValue<Event>("Event")
            .HasValue<Dance>("Dance");

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
