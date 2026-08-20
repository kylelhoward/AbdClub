using AbdClub.Data;
using AbdClub.Models;
using AbdClub.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Mail;
using System.Net.Mime;

namespace AbdClub.Services;

public class SmtpEmailService : IEmailService
{
    private readonly IConfiguration _config;
    private readonly ILogger<SmtpEmailService> _logger;
    private readonly AbdContext _db;
    private readonly IWebHostEnvironment _env; // Inject environment to resolve web root paths
    private readonly ISmtpSender _smtpSender;

    public SmtpEmailService(IConfiguration config,
        ILogger<SmtpEmailService> logger,
        AbdContext db,
        IWebHostEnvironment env,
        ISmtpSender smtpSender)
    {
        _config = config;
        _logger = logger;
        _db = db;
        _env = env;
        _smtpSender = smtpSender;

    }

    public async Task SendReminderAsync(Member member, string emailType)
    {
        const string sourceTrigger = "System:ReminderService";

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
            using var message = BuildMessage(member.Email, member.LastName, subject, body, isHtml: true);

            // 🌟 Dispatches through abstraction (RealSmtpSender or FakeSmtpSender)
            await _smtpSender.SendMailAsync(message);

            _logger.LogInformation("Email sent via SMTP: {EmailType} to {Email}", emailType, member.Email);

            // 🌟 SUCCESS LOGGING: Records delivery confirmation into your Admin audit tables
            await WriteAuditLogAsync(member.Email, subject, body, emailType, sourceTrigger, member.Id, isSuccess: true);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to send {EmailType} to {Email}: {Error}", emailType, member.Email, ex.Message);

            // 🌟 FAILURE LOGGING: Captures the precise SMTP network exception error message for officer review
            await WriteAuditLogAsync(member.Email, subject, body, emailType, sourceTrigger, member.Id, isSuccess: false, errorMessage: ex.Message);
        }
    }

    // 🌟 THE UNIFIED DATABASE LOGGER HOOK: Call this at the end of every email transmission method
    private async Task WriteAuditLogAsync(
        string recipientEmail,
        string subject,
        string body,
        string emailType,
        string triggeredBy,
        int? memberId,
        bool isSuccess,
        string? errorMessage = null)
    {
        try
        {
            var log = new EmailLog
            {
                MemberId = memberId,
                RecipientEmail = recipientEmail,
                Subject = subject,
                Body = body,
                EmailType = emailType,
                TriggeredBy = triggeredBy,
                SentAt = DateTime.UtcNow,
                IsSuccess = isSuccess,
                ErrorMessage = errorMessage
            };

            _db.EmailLogs.Add(log);
            await _db.SaveChangesAsync();
        }
        catch (Exception dbEx)
        {
            // Fallback safety gate: Prevents a database exception from crashing the email transaction pipeline
            _logger.LogError(dbEx, "Audit Ledger Error: Failed to write email log entry to database tables.");
        }
    }


    private SmtpClient GetSmtpClient()
    {
        var host = _config["Email:SmtpHost"]!;
        var port = int.Parse(_config["Email:SmtpPort"]!);
        var username = _config["Email:Username"]!;
        var password = _config["Email:Password"]!;

        // Read the boolean directly out of appsettings, defaulting to true if missing
        bool useSsl = bool.Parse(_config["Email:EnableSsl"] ?? "true");

        return new SmtpClient(host, port)
        {
            Credentials = new NetworkCredential(username, password),
            EnableSsl = useSsl
        };
    }

    private MailMessage BuildMessage(
        string toEmail,
        string toName,
        string subject,
        string body,
        bool isHtml = false)
    {
        var fromAddress = _config["Email:FromAddress"]!;
        var fromName = _config["Email:FromName"]!;
        // Defensive fallback safety net to prevent system drops if configuration parameters are ever vacant
        if (string.IsNullOrWhiteSpace(fromAddress))
        {
            throw new InvalidOperationException("Email server configuration error: 'Email:FromEmail' parameter is unassigned or null in settings.");
        }
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

    public async Task SendMagicLinkEmailAsync(Member member, string magicUrl)
    {
        const string emailType = "MagicLink";
        const string sourceTrigger = "System:MagicLinkAuth";

        // 1. Log the initiation event with structured variables
        _logger.LogInformation(
            "{sourceTrigger}. Recipient: {RecipientEmail}"
            ,sourceTrigger
            ,member.Email);

        var subject = "Your Austin Ballroom Dancers login link";

        var body = $@"
        <h2>Your Login Link</h2>
        <p>Hi {member.LastName},</p>
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
            using var message = BuildMessage(
                member.Email, member.LastName, subject, body, isHtml: true);

            // 2. Execute the third-party network API transaction call
            // 🌟 Dispatches through abstraction (RealSmtpSender or FakeSmtpSender)
            await _smtpSender.SendMailAsync(message);

            // 3. Log a clear, successful operation footprint
            _logger.LogInformation(
                "Communication successfully accepted by remote SMTP relay server. Message type: MagicLink, Destination: {RecipientEmail}",
                member.Email);
            await WriteAuditLogAsync(member.Email, subject, body, emailType, sourceTrigger, member.Id, isSuccess: true);
        }
        catch (SmtpException ex)
        {
            // 4. CRITICAL LOG: Catches explicit network configuration glitches (Bad credentials, port blocking)
            _logger.LogError(ex,
                "Relay Handshake Exception: Zoho SMTP gateway rejected outbound authentication headers for recipient {RecipientEmail}.",
                member.Email);
            await WriteAuditLogAsync(member.Email, subject, body, emailType, sourceTrigger, member.Id, isSuccess: false, errorMessage: ex.Message);
            throw; // Bubble up to let the controller gracefully alert the user
        }
        catch (Exception ex)
        {
            // 5. Catch any unhandled thread system drops or parsing anomalies
            _logger.LogError(ex,
                "Mailing Operational Error: An unexpected failure occurred while processing message envelopes targeting {RecipientEmail}.",
                member.Email);
            await WriteAuditLogAsync(member.Email, subject, body, emailType, sourceTrigger, member.Id, isSuccess: false, errorMessage: ex.Message);
            throw;
        }
    }

    public async Task SendMembershipReminderAsync(Member member)
    {
        const string emailType = "SendMembershipReminder";
        const string sourceTrigger = "System:SendMembershipReminderAsync";

        if (string.IsNullOrEmpty(member.Email)) return;

        var subject = "Membership Renewal Reminder";
        var expiry = member.ExpiryDate?.ToString("MMMM d, yyyy") ?? "N/A";
        var renewUrl = _config["App:RenewUrl"] ?? "https://yourdomain.com/membership";

        var body = $@"
            <h2>Membership Renewal Reminder</h2>
            <p>Hi {member.LastName},</p>
            <p>Your Austin Ballroom Dancers membership expires on <strong>{expiry}</strong>.</p>
            <p>Please renew to continue enjoying club events and benefits:</p>
            <p><a href=""{renewUrl}"">Renew Membership</a></p>
            <p>See you on the dance floor!<br/>— The ABD Team</p>
        ";

        try
        {
            using var message = BuildMessage(member.Email, member.LastName, subject, body, isHtml: true);

            // 🌟 Dispatches through abstraction (RealSmtpSender or FakeSmtpSender)
            await _smtpSender.SendMailAsync(message);

            _logger.LogInformation("Membership reminder sent via SMTP to {Email}", member.Email);
            await WriteAuditLogAsync(member.Email, subject, body, emailType, sourceTrigger, member.Id, isSuccess: true);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to send membership reminder to {Email}: {Exception}", member.Email, ex.Message);
            await WriteAuditLogAsync(member.Email, subject, body, emailType, sourceTrigger, member.Id, isSuccess: false, errorMessage: ex.Message);
            throw;
        }
    }

    public async Task SendOfficerReminderAsync(Dance dance, Member member)
    {
        const string emailType = "SendOfficerReminder";
        const string sourceTrigger = "System:SendOfficerReminderAsync";

        if (string.IsNullOrEmpty(member.Email)) return;

        var subject = $"Officer Reminder: {dance.Title}";
        var body = $@"
            <h2>Officer Reminder</h2>
            <p>Hi {member.LastName},</p>
            <p>You are scheduled to serve as an officer at <strong>{dance.Title}</strong>.</p>
            <h3>Event Details:</h3>
            <ul>
                <li>Date: {dance.Date:MMMM d, yyyy}</li>
                <li>Time: {dance.StartTime} - {dance.EndTime}</li>
                <li>Location: {dance.Location}</li>
                <li>Role: {member.OfficerRole ?? "Officer"}</li>
                <li>Contact: {dance.ContactEmail ?? "Not provided"}</li>
            </ul>
            <p>Thank you for your leadership!<br/>— The ABD Team</p>
        ";

        try
        {
            using var message = BuildMessage(member.Email, member.LastName, subject, body, isHtml: true);

            // 🌟 Dispatches through abstraction (RealSmtpSender or FakeSmtpSender)
            await _smtpSender.SendMailAsync(message);

            _logger.LogInformation("Officer reminder sent via SMTP to {Email} for dance {DanceId}",
                member.Email, dance.Id);
            await WriteAuditLogAsync(member.Email, subject, body, emailType, sourceTrigger, member.Id, isSuccess: true);
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to send officer reminder to {Email}: {Exception}",
                member.Email, ex.Message);
            await WriteAuditLogAsync(member.Email, subject, body, emailType, sourceTrigger, member.Id, isSuccess: false, errorMessage: ex.Message);
        }
    }

    public async Task SendEventNotificationToAllMembersAsync(Dance dance, string subject, string body)
    {
        const string emailType = "SendEventNotificationToAllMembers";
        const string sourceTrigger = "System:SendEventNotificationToAllMembersAsync";

        var members = await _db.Members.Where(m => !m.IsSuspended &&
                m.ExpiryDate.HasValue &&
                m.ExpiryDate.Value >= DateTime.UtcNow).ToListAsync();

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

                using var message = BuildMessage(member.Email, member.LastName, subject, emailBody, isHtml: true);

            // 🌟 Dispatches through abstraction (RealSmtpSender or FakeSmtpSender)
            await _smtpSender.SendMailAsync(message);

                _logger.LogInformation("Event notification sent via SMTP to {Email} for {DanceTitle}",
                    member.Email, dance.Title);
            await WriteAuditLogAsync(member.Email, subject, body, emailType, sourceTrigger, member.Id, isSuccess: true);
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to send event notification to {Email}: {Exception}",
                    member.Email, ex.Message);
                await WriteAuditLogAsync(member.Email, subject, body, emailType, sourceTrigger, member.Id, isSuccess: false, errorMessage: ex.Message);
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
                <p>Hi {member.LastName},</p>
                <p>We're thrilled to have you. Your membership is active until <strong>{expiry}</strong>.</p>
                <p><a href=""https://yourdomain.com/calendar"">Check our calendar for upcoming events</a></p>
                <p>See you on the dance floor!<br/>— The ABD Team</p>
            ",

            "Reminder60" => $@"
                <h2>Membership Renewal Reminder</h2>
                <p>Hi {member.LastName},</p>
                <p>Your Austin Ballroom Dancers membership expires on <strong>{expiry}</strong> — just 60 days away.</p>
                <p><a href=""{renewUrl}"">Renew your membership</a></p>
                <p>See you at the next social!<br/>— The ABD Team</p>
            ",

            "Reminder30" => $@"
                <h2>Membership Expiring in 30 Days</h2>
                <p>Hi {member.LastName},</p>
                <p>Your membership expires on <strong>{expiry}</strong> — just 30 days away.</p>
                <p>Don't let your membership lapse: <a href=""{renewUrl}"">Renew now</a></p>
                <p>— The ABD Team</p>
            ",

            "Reminder7" => $@"
                <h2>Last Week to Renew!</h2>
                <p>Hi {member.LastName},</p>
                <p>Your membership expires on <strong>{expiry}</strong> — only 7 days away!</p>
                <p><a href=""{renewUrl}"">Renew now to keep your access</a></p>
                <p>— The ABD Team</p>
            ",

            "Expired" => $@"
                <h2>Your Membership Has Expired</h2>
                <p>Hi {member.LastName},</p>
                <p>Your membership expired on {expiry}. We'd love to have you back!</p>
                <p><a href=""{renewUrl}"">Renew your membership</a></p>
                <p>Questions? Contact an officer at the next social.<br/>— The ABD Team</p>
            ",

            _ => $@"
                <p>Hi {member.LastName},</p>
                <p>A message from Austin Ballroom Dancers.</p>
                <p><a href=""https://yourdomain.com"">Visit us</a></p>
                <p>— The ABD Team</p>
            "
        };
    }

    // Implement the new service method
    // 2. ADD THE NEW NEWSLETTER METHOD HERE

    public async Task SendNewsletterWelcomeEmailAsync(string email, string firstName)
    {
        const string emailType = "SendNewsletterWelcomeEmail";
        const string sourceTrigger = "System:SendNewsletterWelcomeEmailAsync";
        const string subject = "Welcome to the AbdClub Newsletter!";

        var subscriber = await _db.NewsletterSubscribers
            .FirstOrDefaultAsync(s => s.Email.ToLower() == email.ToLower());

        if (subscriber == null) return;

        var senderEmail = _config["Email:Username"] ?? "newsletter@abdclub.com";
        var baseUrl = _config["App:BaseUrl"] ?? "https://localhost:7193";
        var unsubscribeUrl = $"{baseUrl}/Newsletter/Unsubscribe?token={subscriber.UnsubscribeToken}";

        // 1. Establish unique Content-IDs (CIDs) for your images
        string logoCid = "club_logo_header";

        // 2. Reference the images inside your HTML layout markup using standard 'cid:' syntax
        var body = $@"
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset='utf-8'>
            <style>
                body {{ font-family: Arial, sans-serif; background-color: #f4f6f9; margin: 0; padding: 0; }}
                .container {{ max-width: 600px; margin: 30px auto; background: #ffffff; border-radius: 8px; overflow: hidden; }}
                .header {{ background-color: #212529; padding: 20px; text-align: center; }}
                .logo-img {{ max-height: 80px; width: auto; display: inline-block; }}
                .content {{ padding: 30px; line-height: 1.6; font-size: 16px; color: #333333; }}
                .footer {{ background-color: #f8f9fa; padding: 20px; text-align: center; font-size: 12px; color: #6c757d; }}
            </style>
        </head>
        <body>
            <div class='container'>
                <div class='header'>
                    <!-- Inline mapping points dynamically to the structural attachment payload -->
                    <img src='cid:{logoCid}' alt='AbdClub Logo' class='logo-img' />
                </div>
                <div class='content'>
                    <p>Hi <strong>{firstName}</strong>,</p>
                    <p>Thank you for joining our public mailing list! You are now locked in to receive our official updates and seasonal dance calendars.</p>
                    <p>Warm regards,<br><strong>The AbdClub Team</strong></p>
                </div>
                <div class='footer'>
                    <p>Changed your mind? <a href='{unsubscribeUrl}'>Unsubscribe here</a>.</p>
                </div>
            </div>
        </body>
        </html>";

        var mailMessage = new MailMessage(senderEmail, email)
        {
            Subject = subject
        };

        // 3. Construct the HTML Alternate View container
        var htmlView = AlternateView.CreateAlternateViewFromString(body, null, MediaTypeNames.Text.Html);

        // 4. Resolve the local image file path on disk (e.g., located inside wwwroot/images/)
        //var imagePath = Path.Combine(_env.WebRootPath, "images", "club-logo.png");
        var imagePath = Path.Combine(_env.WebRootPath, "images", "club-logo.webp");

        if (File.Exists(imagePath))
        {
            // Create the linked resource mapping from the file stream bytes
            var logoResource = new LinkedResource(imagePath, "image/png")
            {
                ContentId = logoCid,
                TransferEncoding = TransferEncoding.Base64
            };

            // Bind the asset context to the view layer metadata lists
            htmlView.LinkedResources.Add(logoResource);
        }
        else
        {
            _logger.LogWarning("Inline email mapping target asset was missing at path: {Path}", imagePath);
        }

        // Attach the compiled alternative multi-part view directly to your root message object
        mailMessage.AlternateViews.Add(htmlView);

        try
        {
            // 🌟 Dispatches through abstraction (RealSmtpSender or FakeSmtpSender)
            await _smtpSender.SendMailAsync(mailMessage);

            _logger.LogInformation("HTML Newsletter with mapped inline assets sent to {Email}", email);
            await WriteAuditLogAsync(email, subject, body, emailType, sourceTrigger,null , isSuccess: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute inline asset email transmission to {Email}.", email);
            await WriteAuditLogAsync(email, subject, body, emailType, sourceTrigger, null, isSuccess: false, errorMessage: ex.Message);
        }
    }

    // New method required by the IEmailService interface to generate raw HTML for previews
    public string GenerateBroadcastHtmlBody(string recipientName, string bodyContent)
    {
        var formattedBody = bodyContent?.Replace("\n", "<br />") ?? string.Empty;

        return $@"
        <!DOCTYPE html>
        <html>
        <head>
            <meta charset='utf-8'>
            <style>
                body {{ font-family: Arial, sans-serif; padding: 15px; background-color: #f9f9f9; margin: 0; }}
                .wrapper {{ background-color: #ffffff; border: 1px solid #e0e0e0; border-radius: 4px; max-width: 600px; margin: 0 auto; overflow: hidden; box-shadow: 0 2px 5px rgba(0,0,0,0.05); }}
                .banner {{ background-color: #343a40; color: #ffffff; padding: 15px 20px; font-size: 18px; font-weight: bold; text-align: center; }}
                .body-text {{ padding: 25px; line-height: 1.5; font-size: 15px; color: #222222; }}
                .footer {{ background-color: #f1f1f1; padding: 15px; text-align: center; font-size: 11px; color: #777777; border-top: 1px solid #e0e0e0; }}
            </style>
        </head>
        <body>
            <div class='wrapper'>
                <div class='banner'>Official AbdClub Announcement</div>
                <div class='body-text'>
                    <p>Dear {recipientName},</p>
                    <p>{formattedBody}</p>
                </div>
                <div class='footer'>
                    This email was broadcasted by an authorized club officer to active system members.
                </div>
            </div>
        </body>
        </html>";
    }

    public async Task SendBroadcastEmailAsync(
        string recipientEmail,
        string recipientName,
        string subject,
        string bodyContent)
    {
        const string emailType = "SendBroadcastEmail";
        const string sourceTrigger = "System:SendBroadcastEmailAsyn";

        var senderEmail = _config["Email:Username"] ?? "management@abdclub.com";

        // Call the unified template generator
        var body = GenerateBroadcastHtmlBody(recipientName, bodyContent);

        var mailMessage = new MailMessage(senderEmail, recipientEmail)
        {
            Subject = subject,
            Body = body,
            IsBodyHtml = true
        };

        try
        {

            // 🌟 Dispatches through abstraction (RealSmtpSender or FakeSmtpSender)
            await _smtpSender.SendMailAsync(mailMessage);

            _logger.LogInformation("SendBroadcastEmailAsync sent to {Email}", recipientEmail);
            await WriteAuditLogAsync(recipientEmail, subject, body, emailType, sourceTrigger, null, isSuccess: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to SendBroadcastEmailAsync transmission to {Email}.", recipientEmail);
            await WriteAuditLogAsync(recipientEmail, subject, body, emailType, sourceTrigger, null, isSuccess: false, errorMessage: ex.Message);
        }
    }

    public async Task SendVolunteerAssignmentNotificationAsync(
        string recipientEmail,
        string recipientName,
        string danceTitle,
        string dateString,
        string dutyType,
        bool isAddition)
    {
        const string emailType = "SendVolunteerAssignmentNotificationAsync";
        const string sourceTrigger = "System:SendVolunteerAssignmentNotificationAsync";

        if (string.IsNullOrEmpty(recipientEmail)) return;

        var senderEmail = _config["Email:Username"] ?? "coordination@abdclub.com";
        string subjectAction = isAddition ? "Assignment Confirmed" : "Assignment Cancelled";

        string statusBodyText = isAddition
            ? $"Excellent news! You have been successfully scheduled for the <strong>{dutyType}</strong> team position."
            : $"This notification confirms that your scheduled shift position for <strong>{dutyType}</strong> has been cancelled.";

        var body = $@"
            <!DOCTYPE html>
            <html>
            <head><meta charset='utf-8'></head>
            <body style='font-family: Arial, sans-serif; background-color: #f8f9fa; padding: 20px;'>
                <div style='background-color: #ffffff; padding: 25px; border-radius: 6px; max-width: 550px; margin: 0 auto; border: 1px solid #dee2e6;'>
                    <h3 style='color: {(isAddition ? "#198754" : "#dc3545")}; margin-top: 0;'>AbdClub Staffing Update: {subjectAction}</h3>
                    <p>Hi {recipientName},</p>
                    <p>{statusBodyText}</p>
                    <hr style='border: none; border-top: 1px solid #dee2e6; margin: 20px 0;' />
                    <p style='margin-bottom: 5px;'><strong>Event Details:</strong></p>
                    <ul style='margin-top: 0; padding-left: 20px;'>
                        <li>Event Title: {danceTitle}</li>
                        <li>Scheduled Date: {dateString}</li>
                    </ul>
                    <p style='font-size: 13px; color: #6c757d; margin-top: 25px;'>If you have scheduling questions, reply directly to this message tracking lane.</p>
                </div>
            </body>
            </html>";

        var subject = $"[Staff Notification] {danceTitle} - {subjectAction}";

        var mailMessage = new MailMessage(senderEmail, recipientEmail)
        {
            Subject =subject,
            Body = body,
            IsBodyHtml = true
        };

        try
        {
            // 🌟 Dispatches through abstraction (RealSmtpSender or FakeSmtpSender)
            await _smtpSender.SendMailAsync(mailMessage);

            _logger.LogInformation("Staff notification dispatch delivered successfully targeting {Email}.", recipientEmail);
            await WriteAuditLogAsync(recipientEmail, subject, body, emailType, sourceTrigger, null, isSuccess: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Background transport wrapper failed distributing email notifications targeting {Email}", recipientEmail);
            await WriteAuditLogAsync(recipientEmail, subject, body, emailType, sourceTrigger, null, isSuccess: false, errorMessage: ex.Message);
        }
    }

    public async Task SendOfficerDutyNotificationAsync(
        string recipientEmail,
        string recipientName,
        string danceTitle,
        string dateString,
        string dutyActionText,
        int memberId)
    {
        const string emailType = "SendOfficerDutyNotificationAsync";
        const string sourceTrigger = "System:SendOfficerDutyNotificationAsync";

        if (string.IsNullOrEmpty(recipientEmail)) return;

        var senderEmail = _config["Email:Username"] ?? "coordination@abdclub.com";

        var body = $@"
    <!DOCTYPE html>
    <html>
    <head><meta charset='utf-8'></head>
    <body style='font-family: Arial, sans-serif; background-color: #f4f6f9; padding: 20px; color: #333;'>
        <div style='background-color: #ffffff; padding: 30px; border-radius: 8px; max-width: 600px; margin: 0 auto; border: 1px solid #e2e8f0; box-shadow: 0 4px 6px rgba(0,0,0,0.05);'>
            <div style='background-color: #212529; color: #ffffff; padding: 15px; border-radius: 6px 6px 0 0; text-align: center; font-weight: bold; font-size: 16px; letter-spacing: 0.5px;'>
                OFFICIAL OFFICER DUTY ROSTER NOTICE
            </div>
            <div style='padding-top: 20px;'>
                <p>Attention <strong>{recipientName}</strong>,</p>
                <p>This automated transmission confirms a change to your scheduled event assignments:</p>
                
                <div style='background-color: #f8f9fa; border-left: 4px solid #0d6efd; padding: 15px; margin: 20px 0; border-radius: 0 4px 4px 0;'>
                    <p style='margin: 0 0 8px 0;'><strong>Action Status:</strong> {dutyActionText}</p>
                    <p style='margin: 0 0 8px 0;'><strong>Dance Event:</strong> {danceTitle}</p>
                    <p style='margin: 0;'><strong>Event Date:</strong> {dateString}</p>
                </div>

                <p style='font-size: 14px; color: #6c757d; margin-top: 30px;'>
                    Please update your calendar accordingly. If you have conflict management concerns, reply to your commanding team or event coordinator directly.
                </p>
            </div>
            <div style='border-top: 1px solid #edf2f7; margin-top: 25px; padding-top: 15px; text-align: center; font-size: 11px; color: #a0aec0;'>
                Automated System Message | AbdClub Operations Matrix
            </div>
        </div>
    </body>
    </html>";

        var subject = $"[Duty Roster Notice] {danceTitle} Update";

        var mailMessage = new MailMessage(senderEmail, recipientEmail)
        {
            Subject = subject,
            Body = body,
            IsBodyHtml = true
        };

        try
        {
            // 🌟 Dispatches through abstraction (RealSmtpSender or FakeSmtpSender)
            await _smtpSender.SendMailAsync(mailMessage);

            _logger.LogInformation("Officer notification dispatched to {Email}.", recipientEmail);
            await WriteAuditLogAsync(recipientEmail, subject, body, emailType, sourceTrigger, memberId, isSuccess: true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed background transport delivery to officer destination: {Email}", recipientEmail);
            await WriteAuditLogAsync(recipientEmail, subject, body, emailType, sourceTrigger, memberId, isSuccess: false, errorMessage: ex.Message);
        }
    }

    public Task SendVolunteerReminderAsync(Dance dance, MasterVolunteer volunteer)
    {
        const string emailType = "SendVolunteerReminderAsync";
        const string sourceTrigger = "System:SendVolunteerReminderAsync";

        throw new NotImplementedException();
    }

}
