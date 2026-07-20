using AbdClub.Data;
using AbdClub.Models;
using Microsoft.EntityFrameworkCore;

namespace AbdClub.Services;

public interface IMagicLinkService
{
    Task<bool> SendMagicLinkAsync(string email, string baseUrl);
    Task<Member?> ValidateTokenAsync(string token);
}

public class MagicLinkService : IMagicLinkService
{
    private readonly AbdContext _db;
    private readonly IEmailService _email;
    private readonly ILogger<MagicLinkService> _logger;

    public MagicLinkService(
        AbdContext db,
        IEmailService email,
        ILogger<MagicLinkService> logger)
    {
        _db = db;
        _email = email;
        _logger = logger;
    }

    public async Task<bool> SendMagicLinkAsync(string email, string baseUrl)
    {
        // Check if email exists in Members table
        var member = await _db.Members
            .FirstOrDefaultAsync(m => m.Email == email && m.IsActive);

        if (member == null)
        {
            // Don't reveal whether email exists — just return true
            // This prevents email enumeration attacks
            _logger.LogInformation(
                "Magic link requested for unknown email: {Email}", email);
            return true;
        }

        // Invalidate any existing unused tokens for this email
        var existingTokens = await _db.MagicLinks
            .Where(m => m.Email == email && !m.Used && m.ExpiresAt > DateTime.UtcNow)
            .ToListAsync();

        foreach (var t in existingTokens)
            t.Used = true;

        // Generate a new token
        var token = Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");

        _db.MagicLinks.Add(new MagicLink
        {
            Email = email,
            Token = token,
            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
            Used = false
        });

        await _db.SaveChangesAsync();

        // Build the magic link URL
        var magicUrl = $"{baseUrl}/Auth/MagicLink?token={token}";

        // Send the email
        await _email.SendMagicLinkEmailAsync(member, magicUrl);

        _logger.LogInformation(
            "Magic link sent to {Email}", email);

        return true;
    }

    public async Task<Member?> ValidateTokenAsync(string token)
    {
        var magicLink = await _db.MagicLinks
            .FirstOrDefaultAsync(m =>
                m.Token == token &&
                !m.Used &&
                m.ExpiresAt > DateTime.UtcNow);

        if (magicLink == null)
        {
            _logger.LogWarning(
                "Invalid or expired magic link token: {Token}", token);
            return null;
        }

        // Mark as used — one time only
        magicLink.Used = true;
        await _db.SaveChangesAsync();

        // Return the member
        return await _db.Members
            .FirstOrDefaultAsync(m =>
                m.Email == magicLink.Email && m.IsActive);
    }
}