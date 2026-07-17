using AbdClub.Data;
using AbdClub.Models;
using System.Net;
using System.Net.Mail;

namespace AbdClub.Services;


public class EmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailService> _logger;

    public EmailService(IConfiguration config, ILogger<EmailService> logger)
    {
        _config = config;
        _logger = logger;
    }

    private SmtpClient GetSmtpClient()
    {
        var host = _config["Email:SmtpHost"]!;
        var port = int.Parse(_config["Email:SmtpPort"]!);
        var username = _config["Email:Username"]!;
        var password = _config["Email:Password"]!;

        return new SmtpClient(host, port)
        {
            Credentials = new NetworkCredential(username, password),
            EnableSsl = true
        };
    }

    private MailMessage BuildMessage(string toEmail, string toName,
        string subject, string body)
    {
        var fromAddress = _config["Email:FromAddress"]!;
        var fromName = _config["Email:FromName"]!;

        var message = new MailMessage
        {
            From = new MailAddress(fromAddress, fromName),
            Subject = subject,
            Body = body,
            IsBodyHtml = false
        };

        message.To.Add(new MailAddress(toEmail, toName));
        return message;
    }

    public async Task SendReminderAsync(Member member, string emailType)
    {
        var subject = emailType switch
        {
            "Reminder60" => "Your ABD membership expires in 60 days",
            "Reminder30" => "Your ABD membership expires in 30 days",
            "Reminder7" => "Action needed — ABD membership expires in 7 days",
            "Expired" => "Your ABD membership has expired",
            "Welcome" => "Welcome to Austin Ballroom Dancers!",
            _ => "A message from Austin Ballroom Dancers"
        };

        var body = BuildReminderBody(member, emailType);

        try
        {
            using var smtp = GetSmtpClient();
            using var message = BuildMessage(
                member.Email, member.FullName, subject, body);

            await smtp.SendMailAsync(message);

            _logger.LogInformation(
                "Email sent: {EmailType} to {Email}", emailType, member.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                "Failed to send {EmailType} to {Email}: {Error}",
                emailType, member.Email, ex.Message);
        }
    }

    public async Task SendBroadcastAsync(
        List<Member> recipients, string subject, string body)
    {
        using var smtp = GetSmtpClient();

        foreach (var member in recipients)
        {
            try
            {
                using var message = BuildMessage(
                    member.Email, member.FullName, subject, body);

                await smtp.SendMailAsync(message);

                _logger.LogInformation(
                    "Broadcast sent to {Email}", member.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    "Failed broadcast to {Email}: {Error}",
                    member.Email, ex.Message);
            }
        }
    }

    private string BuildReminderBody(Member member, string emailType)
    {
        var renewUrl = "https://danceatx.org/membership";
        var expiry = member.ExpiryDate?.ToString("MMMM d, yyyy") ?? "N/A";

        return emailType switch
        {
            "Welcome" => $"""
                Hi {member.FullName},

                Welcome to Austin Ballroom Dancers! We're thrilled to have you.

                Your membership is active until {expiry}.

                Here's what's coming up — check our calendar for the latest:
                https://danceatx.org/calendar

                See you on the dance floor!
                — The ABD Team
                https://danceatx.org
                """,

            "Reminder60" => $"""
                Hi {member.FullName},

                Just a friendly heads-up — your Austin Ballroom Dancers
                membership expires on {expiry}, which is 60 days away.

                Renew early and you won't miss a thing:
                {renewUrl}

                See you at the next social!
                — The ABD Team
                """,

            "Reminder30" => $"""
                Hi {member.FullName},

                Your Austin Ballroom Dancers membership expires on {expiry}
                — just 30 days from now.

                Don't let your membership lapse. Renew here:
                {renewUrl}

                — The ABD Team
                """,

            "Reminder7" => $"""
                Hi {member.FullName},

                Your Austin Ballroom Dancers membership expires on {expiry}.
                That's only 7 days away — please renew now to keep your
                access to club events and socials.

                Renew here:
                {renewUrl}

                — The ABD Team
                """,

            "Expired" => $"""
                Hi {member.FullName},

                Your Austin Ballroom Dancers membership expired on {expiry}.

                We'd love to have you back! Renew your membership here:
                {renewUrl}

                Questions? Reply to this email or contact an officer at
                the next social dance.

                — The ABD Team
                """,

            _ => $"""
                Hi {member.FullName},

                A message from Austin Ballroom Dancers.

                Visit us at https://danceatx.org

                — The ABD Team
                """
        };
    }

    public Task SendMembershipReminderAsync(Member member)
    {
        throw new NotImplementedException();
    }

    public Task SendVolunteerReminderAsync(Dance dance, Volunteer volunteer)
    {
        throw new NotImplementedException();
    }

    public Task SendOfficerReminderAsync(Dance dance, Member officer)
    {
        throw new NotImplementedException();
    }

    public Task SendEventNotificationToAllMembersAsync(Dance dance, string subject, string body)
    {
        throw new NotImplementedException();
    }
}