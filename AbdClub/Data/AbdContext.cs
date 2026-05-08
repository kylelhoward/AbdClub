using AbdClub.Models;
using Microsoft.EntityFrameworkCore;

namespace AbdClub.Data;

public class AbdContext : DbContext
{
    public AbdContext(DbContextOptions<AbdContext> options) : base(options) { }

    public DbSet<Member> Members => Set<Member>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<EmailLog> EmailLogs => Set<EmailLog>();
    public DbSet<MeetingNote> MeetingNotes => Set<MeetingNote>();
    public DbSet<ClubFile> ClubFiles => Set<ClubFile>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Ensure email is unique
        modelBuilder.Entity<Member>()
            .HasIndex(m => m.Email)
            .IsUnique();

        // Ensure GoogleSubId is unique when set
        modelBuilder.Entity<Member>()
            .HasIndex(m => m.GoogleSubId)
            .IsUnique();

        // Payment precision for currency
        modelBuilder.Entity<Payment>()
            .Property(p => p.Amount)
            .HasPrecision(10, 2);
    }
}