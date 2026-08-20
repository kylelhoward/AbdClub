using System.Net.Mail;
using System.Threading.Tasks;

namespace AbdClub.Services;

public interface ISmtpSender
{
    Task SendMailAsync(MailMessage message);
}

