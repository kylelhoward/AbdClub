using AbdClub.Data;
using Microsoft.EntityFrameworkCore;

namespace AbdClub.Services;

// Runs in the background — wakes up daily, checks expiry dates, fires emails
public class ReminderService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<ReminderService> _logger;

    public ReminderService(IServiceProvider services, ILogger<ReminderService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogInformation("ReminderService running at {Time}", DateTime.UtcNow);
            await CheckAndSendReminders();

            // Wait until next run — check once per day
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }

    private async Task CheckAndSendReminders()
    {
        // Must create a scope — DbContext is scoped, BackgroundService is singleton
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AbdContext>();
        var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

        var today = DateTime.UtcNow.Date;
        var checkDays = new[] { 60, 30, 7 };

        foreach (var days in checkDays)
        {
            var targetDate = today.AddDays(days);
            var emailType = $"Reminder{days}";

            // Find members expiring on exactly this target date
            // who haven't already received this reminder
            var members = await db.Members
                .Where(m =>
                    m.IsActive &&
                    m.ExpiryDate.Date == targetDate &&
                    !db.EmailLogs.Any(e =>
                        e.MemberId == m.Id &&
                        e.EmailType == emailType &&
                        e.SentAt.Date == today))
                .ToListAsync();

            foreach (var member in members)
            {
                await emailService.SendReminderAsync(member, emailType);

                // Log it so we don't send twice
                db.EmailLogs.Add(new Models.EmailLog
                {
                    MemberId = member.Id,
                    EmailType = emailType,
                    Subject = $"Membership reminder — {days} days",
                    SentAt = DateTime.UtcNow
                });
            }
        }

        await db.SaveChangesAsync();
    }
}