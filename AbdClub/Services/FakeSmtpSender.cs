using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace AbdClub.Services;

public class FakeSmtpSender : ISmtpSender
{
    private readonly ILogger<FakeSmtpSender> _logger;

    public FakeSmtpSender(ILogger<FakeSmtpSender> logger)
    {
        _logger = logger;
    }

    public Task SendMailAsync(MailMessage message)
    {
        // Suppresses outbound network activity while logging diagnostic context
        _logger.LogInformation("[SANDBOX SMTP SUPPRESSION] Intercepted '{Subject}' to '{Recipient}'",
            message.Subject,
            message.To.ToString());

        return Task.CompletedTask;
    }
}

