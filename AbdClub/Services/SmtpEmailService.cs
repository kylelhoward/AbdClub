using AbdClub.Data;
using AbdClub.Models;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Mail;

namespace AbdClub.Services;

public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<SmtpEmailService> _logger;
    private readonly AbdContext _db;

    public SmtpEmailService(IConfiguration config, ILogger<SmtpEmailService> logger, AbdContext db)
    {
        _config = config;
        _logger = logger;
        _db = db;
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

    private MailMessage BuildMessage(string toEmail, string toName, string subject, string body, bool isHtml = false)
    {
        var fromAddress = _config["Email:FromAddress"]!;
        var fromName = _config["Email:FromName"]!;

        var message = new MailMessage
        {
            From = new MailAddress(fromAddress, fromName),
            Subject = subject,
            Body = body,
            IsBodyHtml = isHtml
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
            using var message = BuildMessage(member.Email, member.FullName, subject, body, isHtml: true);

            await smtp.SendMailAsync(message);

            _logger.LogInformation("Email sent via SMTP: {EmailType} to {Email}", emailType, member.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to send {EmailType} to {Email}: {Error}", emailType, member.Email, ex.Message);
        }
    }

    public async Task SendBroadcastAsync(List<Member> recipients, string subject, string body)
    {
        using var smtp = GetSmtpClient();

        foreach (var member in recipients)
        {
            try
            {
                using var message = BuildMessage(member.Email, member.FullName, subject, body, isHtml: true);

                await smtp.SendMailAsync(message);

                _logger.LogInformation("Broadcast sent via SMTP to {Email}", member.Email);
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed broadcast to {Email}: {Error}", member.Email, ex.Message);
            }
        }
    }

    public async Task SendMembershipReminderAsync(Member member)
    {
        if (string.IsNullOrEmpty(member.Email)) return;

        var subject = "Membership Renewal Reminder";
        var expiry = member.ExpiryDate?.ToString("MMMM d, yyyy") ?? "N/A";
        var renewUrl = _config["App:RenewUrl"] ?? "https://yourdomain.com/membership";

        var body = $@"
            <h2>Membership Renewal Reminder</h2>
            <p>Hi {member.FullName},</p>
            <p>Your Austin Ballroom Dancers membership expires on <strong>{expiry}</strong>.</p>
            <p>Please renew to continue enjoying club events and benefits:</p>
            <p><a href=""{renewUrl}"">Renew Membership</a></p>
            <p>See you on the dance floor!<br/>— The ABD Team</p>
        ";

        try
        {
            using var smtp = GetSmtpClient();
            using var message = BuildMessage(member.Email, member.FullName, subject, body, isHtml: true);

            await smtp.SendMailAsync(message);

            _logger.LogInformation("Membership reminder sent via SMTP to {Email}", member.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to send membership reminder to {Email}: {Exception}", member.Email, ex.Message);
        }
    }

    public async Task SendVolunteerReminderAsync(Dance dance, Volunteer volunteer)
    {
        if (string.IsNullOrEmpty(volunteer.Email)) return;

        var subject = $"Volunteer Reminder: {dance.Title}";
        var body = $@"
            <h2>Volunteer Reminder</h2>
            <p>Hi {volunteer.Name},</p>
            <p>Thank you for volunteering for <strong>{dance.Title}</strong>!</p>
            <h3>Event Details:</h3>
            <ul>
                <li>Date: {dance.Date:MMMM d, yyyy}</li>
                <li>Time: {dance.StartTime} - {dance.EndTime}</li>
                <li>Location: {dance.Location}</li>
                <li>Contact: {dance.ContactEmail ?? "Not provided"}</li>
            </ul>
            <p>We appreciate your support!<br/>— The ABD Team</p>
        ";

        try
        {
            using var smtp = GetSmtpClient();
            using var message = BuildMessage(volunteer.Email, volunteer.Name, subject, body, isHtml: true);

            await smtp.SendMailAsync(message);

            _logger.LogInformation("Volunteer reminder sent via SMTP to {Email} for dance {DanceId}",
                volunteer.Email, dance.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to send volunteer reminder to {Email}: {Exception}",
                volunteer.Email, ex.Message);
        }
    }

    public async Task SendOfficerReminderAsync(Dance dance, Member officer)
    {
        if (string.IsNullOrEmpty(officer.Email)) return;

        var subject = $"Officer Reminder: {dance.Title}";
        var body = $@"
            <h2>Officer Reminder</h2>
            <p>Hi {officer.FullName},</p>
            <p>You are scheduled to serve as an officer at <strong>{dance.Title}</strong>.</p>
            <h3>Event Details:</h3>
            <ul>
                <li>Date: {dance.Date:MMMM d, yyyy}</li>
                <li>Time: {dance.StartTime} - {dance.EndTime}</li>
                <li>Location: {dance.Location}</li>
                <li>Role: {officer.OfficerRole ?? "Officer"}</li>
                <li>Contact: {dance.ContactEmail ?? "Not provided"}</li>
            </ul>
            <p>Thank you for your leadership!<br/>— The ABD Team</p>
        ";

        try
        {
            using var smtp = GetSmtpClient();
            using var message = BuildMessage(officer.Email, officer.FullName, subject, body, isHtml: true);

            await smtp.SendMailAsync(message);

            _logger.LogInformation("Officer reminder sent via SMTP to {Email} for dance {DanceId}",
                officer.Email, dance.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to send officer reminder to {Email}: {Exception}",
                officer.Email, ex.Message);
        }
    }

    public async Task SendEventNotificationToAllMembersAsync(Dance dance, string subject, string body)
    {
        var members = await _db.Members.Where(m => m.IsActive).ToListAsync();

        if (!members.Any())
        {
            _logger.LogWarning("No active members found for event notification");
            return;
        }

        using var smtp = GetSmtpClient();

        foreach (var member in members)
        {
            if (string.IsNullOrEmpty(member.Email)) continue;

            try
            {
                var emailBody = $@"
                    {body}
                    <hr/>
                    <h3>Event Details:</h3>
                    <ul>
                        <li>Title: {dance.Title}</li>
                        <li>Date: {dance.Date:MMMM d, yyyy}</li>
                        <li>Time: {dance.StartTime} - {dance.EndTime}</li>
                        <li>Location: {dance.Location}</li>
                        {(string.IsNullOrEmpty(dance.Description) ? "" : $"<li>Description: {dance.Description}</li>")}
                    </ul>
                    <p>See you there!<br/>— The ABD Team</p>
                ";

                using var message = BuildMessage(member.Email, member.FullName, subject, emailBody, isHtml: true);

                await smtp.SendMailAsync(message);

                _logger.LogInformation("Event notification sent via SMTP to {Email} for {DanceTitle}",
                    member.Email, dance.Title);
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to send event notification to {Email}: {Exception}",
                    member.Email, ex.Message);
            }
        }
    }

    private string BuildReminderBody(Member member, string emailType)
    {
        var renewUrl = _config["App:RenewUrl"] ?? "https://yourdomain.com/membership";
        var expiry = member.ExpiryDate?.ToString("MMMM d, yyyy") ?? "N/A";

        return emailType switch
        {
            "Welcome" => $@"
                <h2>Welcome to Austin Ballroom Dancers!</h2>
                <p>Hi {member.FullName},</p>
                <p>We're thrilled to have you. Your membership is active until <strong>{expiry}</strong>.</p>
                <p><a href=""https://yourdomain.com/calendar"">Check our calendar for upcoming events</a></p>
                <p>See you on the dance floor!<br/>— The ABD Team</p>
            ",

            "Reminder60" => $@"
                <h2>Membership Renewal Reminder</h2>
                <p>Hi {member.FullName},</p>
                <p>Your Austin Ballroom Dancers membership expires on <strong>{expiry}</strong> — just 60 days away.</p>
                <p><a href=""{renewUrl}"">Renew your membership</a></p>
                <p>See you at the next social!<br/>— The ABD Team</p>
            ",

            "Reminder30" => $@"
                <h2>Membership Expiring in 30 Days</h2>
                <p>Hi {member.FullName},</p>
                <p>Your membership expires on <strong>{expiry}</strong> — just 30 days away.</p>
                <p>Don't let your membership lapse: <a href=""{renewUrl}"">Renew now</a></p>
                <p>— The ABD Team</p>
            ",

            "Reminder7" => $@"
                <h2>Last Week to Renew!</h2>
                <p>Hi {member.FullName},</p>
                <p>Your membership expires on <strong>{expiry}</strong> — only 7 days away!</p>
                <p><a href=""{renewUrl}"">Renew now to keep your access</a></p>
                <p>— The ABD Team</p>
            ",

            "Expired" => $@"
                <h2>Your Membership Has Expired</h2>
                <p>Hi {member.FullName},</p>
                <p>Your membership expired on {expiry}. We'd love to have you back!</p>
                <p><a href=""{renewUrl}"">Renew your membership</a></p>
                <p>Questions? Contact an officer at the next social.<br/>— The ABD Team</p>
            ",

            _ => $@"
                <p>Hi {member.FullName},</p>
                <p>A message from Austin Ballroom Dancers.</p>
                <p><a href=""https://yourdomain.com"">Visit us</a></p>
                <p>— The ABD Team</p>
            "
        };
    }
}
