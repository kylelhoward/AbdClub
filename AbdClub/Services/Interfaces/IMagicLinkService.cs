using AbdClub.Models;

namespace AbdClub.Services.Interfaces;

public interface IMagicLinkService
{
    Task<bool> SendMagicLinkAsync(string email, string baseUrl);
    Task<Member?> ValidateTokenAsync(string token);
}
