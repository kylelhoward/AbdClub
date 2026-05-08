using AbdClub.Models;

namespace AbdClub.Services;

public interface IEmailService
{
    Task SendReminderAsync(Member member, string emailType);
    Task SendBroadcastAsync(List<Member> recipients, string subject, string body);
}

public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendReminderAsync(Member member, string emailType)
    {
        var subject = emailType switch
        {
            "Reminder60" => "Your ABD membership expires in 60 days",
            "Reminder30" => "Your ABD membership expires in 30 days",
            "Reminder7" => "Your ABD membership expires in 7 days — renew now",
            "Expired" => "Your ABD membership has expired",
            "Welcome" => "Welcome to Awesome Ballroom Dancers!",
            _ => "A message from Awesome Ballroom Dancers"
        };

        var body = BuildReminderBody(member, emailType);

        // TODO: replace with your SMTP provider (SMTP2GO, SendGrid, etc.)
        _logger.LogInformation(
            "Sending {EmailType} to {Email} — Subject: {Subject}",
            emailType, member.Email, subject);

        await Task.CompletedTask; // swap with real SMTP send
    }

    public async Task SendBroadcastAsync(List<Member> recipients, string subject, string body)
    {
        foreach (var member in recipients)
        {
            _logger.LogInformation("Broadcast to {Email}", member.Email);
            // TODO: send via SMTP
        }
        await Task.CompletedTask;
    }

    private string BuildReminderBody(Member member, string emailType)
    {
        var renewUrl = "https://yourdomain.com/membership";
        var days = emailType switch
        {
            "Reminder60" => "60 days",
            "Reminder30" => "30 days",
            "Reminder7" => "7 days",
            _ => ""
        };

        return $"""
            Hi {member.FullName},

            Your Awesome Ballroom Dancers membership expires on
            {member.ExpiryDate:MMMM d, yyyy}.
            {(days != "" ? $"That's only {days} away!" : "")}

            Renew here: {renewUrl}

            See you on the dance floor!
            — The ABD Team
            """;
    }
}