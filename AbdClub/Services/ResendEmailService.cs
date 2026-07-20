using AbdClub.Data;
using AbdClub.Models;
using Microsoft.EntityFrameworkCore;
using Resend;

namespace AbdClub.Services;

public class ResendEmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<ResendEmailService> _logger;
    private readonly AbdContext _db;
    private readonly ResendClient _resendClient;

    public ResendEmailService(IConfiguration config, ILogger<ResendEmailService> logger, AbdContext db, ResendClient resendClient)
    {
        _config = config;
        _logger = logger;
        _db = db;
        _resendClient = resendClient;
    }

    private string GetFromEmail() => _config["Email:FromEmail"]!;
    private string GetFromName() => _config["Email:FromName"]!;
    private string GetFromAddress() => $"{GetFromName()} <{GetFromEmail()}>";

    public async Task SendMagicLinkEmailAsync(Member member, string magicUrl)
    {
        var subject = "Your Austin Ballroom Dancers login link";

        var body = $@"
        <h2>Your Login Link</h2>
        <p>Hi {member.FullName},</p>
        <p>Click the button below to log in to your Austin Ballroom Dancers account.</p>
        <p>
            <a href=""{magicUrl}""
               style=""background:#D4537E; color:white; padding:12px 24px;
                      text-decoration:none; border-radius:4px; font-weight:bold;"">
                Log in to ABD
            </a>
        </p>
        <p>Or copy and paste this link into your browser:</p>
        <p style=""color:#666; font-size:12px;"">{magicUrl}</p>
        <p><strong>This link expires in 15 minutes</strong> and can only be used once.</p>
        <p>If you didn't request this link, you can safely ignore this email.</p>
        <p>— The ABD Team</p>
    ";

        try
        {
            var email = new EmailMessage
            {
                From = GetFromAddress(),
                To = member.Email,
                Subject = subject,
                HtmlBody = body
            };

            var response = await _resendClient.EmailSendAsync(email);

            if (response?.Exception != null)
                _logger.LogError("Failed to send magic link to {Email}: {Error}",
                    member.Email, response.Exception.Message);
            else
                _logger.LogInformation("Magic link email sent to {Email}", member.Email);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to send magic link to {Email}: {Exception}",
                member.Email, ex.Message);
        }
    }

    public async Task SendReminderAsync(Member member, string emailType)
    {
        if (string.IsNullOrEmpty(member.Email)) return;

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
            var email = new EmailMessage
            {
                From = GetFromAddress(),
                To = member.Email,
                Subject = subject,
                HtmlBody = body
            };

            var response = await _resendClient.EmailSendAsync(email);

            if (response != null && response.Exception == null)
            {
                _logger.LogInformation("Email sent via Resend: {EmailType} to {Email}",
                    emailType, member.Email);
            }
            else
            {
                var errorMsg = response?.Exception?.Message ?? "Unknown error";
                _logger.LogError("Failed to send {EmailType} to {Email}: {Error}",
                    emailType, member.Email, errorMsg);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to send {EmailType} to {Email}: {Exception}",
                emailType, member.Email, ex.Message);
        }
    }

    public async Task SendBroadcastAsync(List<Member> recipients, string subject, string body)
    {
        foreach (var member in recipients)
        {
            if (string.IsNullOrEmpty(member.Email)) continue;

            try
            {
                var email = new EmailMessage
                {
                    From = GetFromAddress(),
                    To = member.Email,
                    Subject = subject,
                    HtmlBody = body
                };

                var response = await _resendClient.EmailSendAsync(email);

                if (response != null && response.Exception == null)
                {
                    _logger.LogInformation("Broadcast sent via Resend to {Email}",
                        member.Email);
                }
                else
                {
                    var errorMsg = response?.Exception?.Message ?? "Unknown error";
                    _logger.LogError("Failed broadcast to {Email}: {Error}",
                        member.Email, errorMsg);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed broadcast to {Email}: {Exception}",
                    member.Email, ex.Message);
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
            var email = new EmailMessage
            {
                From = GetFromAddress(),
                To = member.Email,
                Subject = subject,
                HtmlBody = body
            };

            var response = await _resendClient.EmailSendAsync(email);

            if (response != null && response.Exception == null)
            {
                _logger.LogInformation("Membership reminder sent via Resend to {Email}",
                    member.Email);
            }
            else
            {
                var errorMsg = response?.Exception?.Message ?? "Unknown error";
                _logger.LogError("Failed to send membership reminder to {Email}: {Error}",
                    member.Email, errorMsg);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to send membership reminder to {Email}: {Exception}",
                member.Email, ex.Message);
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
            var email = new EmailMessage
            {
                From = GetFromAddress(),
                To = volunteer.Email,
                Subject = subject,
                HtmlBody = body
            };

            var response = await _resendClient.EmailSendAsync(email);

            if (response != null && response.Exception == null)
            {
                _logger.LogInformation("Volunteer reminder sent via Resend to {Email} for dance {DanceId}",
                    volunteer.Email, dance.Id);
            }
            else
            {
                var errorMsg = response?.Exception?.Message ?? "Unknown error";
                _logger.LogError("Failed to send volunteer reminder to {Email}: {Error}",
                    volunteer.Email, errorMsg);
            }
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
            var email = new EmailMessage
            {
                From = GetFromAddress(),
                To = officer.Email,
                Subject = subject,
                HtmlBody = body
            };

            var response = await _resendClient.EmailSendAsync(email);

            if (response != null && response.Exception == null)
            {
                _logger.LogInformation("Officer reminder sent via Resend to {Email} for dance {DanceId}",
                    officer.Email, dance.Id);
            }
            else
            {
                var errorMsg = response?.Exception?.Message ?? "Unknown error";
                _logger.LogError("Failed to send officer reminder to {Email}: {Error}",
                    officer.Email, errorMsg);
            }
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

                var email = new EmailMessage
                {
                    From = GetFromAddress(),
                    To = member.Email,
                    Subject = subject,
                    HtmlBody = emailBody
                };

                var response = await _resendClient.EmailSendAsync(email);

                if (response != null && response.Exception == null)
                {
                    _logger.LogInformation("Event notification sent via Resend to {Email} for {DanceTitle}",
                        member.Email, dance.Title);
                }
                else
                {
                    var errorMsg = response?.Exception?.Message ?? "Unknown error";
                    _logger.LogError("Failed to send event notification to {Email}: {Error}",
                        member.Email, errorMsg);
                }
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
