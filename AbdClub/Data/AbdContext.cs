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
    public DbSet<DJ> Djs { get; set; } = null!;
    public DbSet<Volunteer> Volunteers { get; set; } = null!;

    public DbSet<MagicLink> MagicLinks { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // TPH discriminator for Event hierarchy
        modelBuilder.Entity<Event>()
            .HasDiscriminator<string>("EventType")
            .HasValue<Event>("Event")
            .HasValue<Dance>("Dance");

        // One-to-many: Dance -> Lessons
        modelBuilder.Entity<Lesson>()
            .HasOne(l => l.Dance)
            .WithMany(d => d.Lessons)
            .HasForeignKey(l => l.DanceId)
            .OnDelete(DeleteBehavior.Cascade);

        // One-to-one: DJ -> Dance
        modelBuilder.Entity<DJ>()
            .HasOne(j => j.Dance)
            .WithOne(d => d.Dj)
            .HasForeignKey<DJ>(j => j.DanceId)
            .OnDelete(DeleteBehavior.Cascade);

        // One-to-many: Dance -> Volunteers
        modelBuilder.Entity<Volunteer>()
            .HasOne(v => v.Dance)
            .WithMany(d => d.Volunteers)
            .HasForeignKey(v => v.DanceId)
            .OnDelete(DeleteBehavior.Cascade);

        // Many-to-many: Dance <-> Member (AttendingOfficers)
        modelBuilder.Entity<Dance>()
            .HasMany(d => d.AttendingOfficers)
            .WithMany()
            .UsingEntity(join => join.ToTable("DanceAttendingOfficers"));
    }
}
