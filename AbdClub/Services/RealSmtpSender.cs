using System;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AbdClub.Services;

public class RealSmtpSender : ISmtpSender
{
    private readonly IConfiguration _config;
    private readonly ILogger<RealSmtpSender> _logger;

    // 🌟 Injecting ILogger so you can track safety interceptions directly in your console logs
    public RealSmtpSender(IConfiguration config, ILogger<RealSmtpSender> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendMailAsync(MailMessage message)
    {
        // 1. Read environmental sandbox safety configuration parameters
        bool isRerouteActive = _config.GetValue<bool>("EmailTesting:EnforceSandboxReroute", false);
        string safeTargetAddress = _config["EmailTesting:RedirectTargetAddress"] ?? "dev-sandbox@abdclub.org";

        // Safely extract whitelisted testing domains from appsettings array blocks
        var allowedDomains = _config.GetSection("EmailTesting:AllowedRecipientDomains").Get<string[]>() ?? Array.Empty<string>();

        // 2. 🌟 THE CENTRAL SAFETY INTERCEPTOR GATEWAY
        if (isRerouteActive)
        {
            // Gather all current recipients across To, CC, and BCC pools for validation checks
            var allRecipients = message.To.Concat(message.CC).Concat(message.Bcc).ToList();

            foreach (var recipient in allRecipients)
            {
                // Verify if the recipient belongs to an explicitly whitelisted test domain (e.g., @abdclub.org)
                bool isDomainWhitelisted = allowedDomains.Any(domain =>
                    recipient.Address.EndsWith($"@{domain}", StringComparison.OrdinalIgnoreCase));

                if (!isDomainWhitelisted)
                {
                    _logger.LogWarning("🛡️ SECURITY NETWORK SAFETY INTERCEPT: Suppressed live email distribution to outside member account '{RealEmail}'. Hard routing payload to sandbox target destination: '{SandboxInbox}'",
                        recipient.Address, safeTargetAddress);

                    // Modify the email subject line in-flight so you know exactly who it was originally meant for when checking your test inbox
                    message.Subject = $"[UAT REDIRECT - Original To: {recipient.Address}] {message.Subject}";

                    // Wipe the real production recipients clear out of the message transport headers
                    message.To.Clear();
                    message.CC.Clear();
                    message.Bcc.Clear();

                    // Re-route the payload exclusively to your safe testing sandbox inbox
                    message.To.Add(new MailAddress(safeTargetAddress));
                    break;
                }
            }
        }

        // 3. PHYSICAL NETWORK DISPATCH LAYER
        var host = _config["Email:SmtpHost"]!;
        var port = int.Parse(_config["Email:SmtpPort"]!);
        var username = _config["Email:Username"]!;
        var password = _config["Email:Password"]!;
        bool useSsl = bool.Parse(_config["Email:EnableSsl"] ?? "true");

        using var client = new SmtpClient(host, port)
        {
            Credentials = new NetworkCredential(username, password),
            EnableSsl = useSsl
        };

        // Fire physical transmission over the wire
        await client.SendMailAsync(message);
    }
}

