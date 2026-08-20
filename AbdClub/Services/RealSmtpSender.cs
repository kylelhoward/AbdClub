using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace AbdClub.Services;

public class RealSmtpSender : ISmtpSender
{
    private readonly IConfiguration _config;

    public RealSmtpSender(IConfiguration config)
    {
        _config = config;
    }

    public async Task SendMailAsync(MailMessage message)
    {
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

        await client.SendMailAsync(message);
    }
}

