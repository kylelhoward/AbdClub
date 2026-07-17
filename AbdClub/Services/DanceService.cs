using AbdClub.Data;
using AbdClub.Models;
using Microsoft.EntityFrameworkCore;

namespace AbdClub.Services;

public class DanceService
{
    private readonly AbdContext _db;
    private readonly IEmailService _email;

    public DanceService(AbdContext db, IEmailService email)
    {
        _db = db;
        _email = email;
    }

    public async Task SendVolunteerRemindersAsync(int danceId)
    {
        var dance = await _db.Dances
            .Include(d => d.Volunteers)
            .FirstOrDefaultAsync(d => d.Id == danceId);

        if (dance == null) return;

        foreach (var volunteer in dance.Volunteers)
        {
            await _email.SendVolunteerReminderAsync(dance, volunteer);
        }
    }

    public async Task SendAttendingOfficerRemindersAsync(int danceId)
    {
        var dance = await _db.Dances
            .Include(d => d.AttendingOfficers)
            .FirstOrDefaultAsync(d => d.Id == danceId);

        if (dance == null) return;

        foreach (var officer in dance.AttendingOfficers)
        {
            await _email.SendOfficerReminderAsync(dance, officer);
        }
    }

    public async Task SendAllMembersNotificationAsync(int danceId, string subject, string body)
    {
        var dance = await _db.Dances.FirstOrDefaultAsync(d => d.Id == danceId);

        if (dance == null) return;

        await _email.SendEventNotificationToAllMembersAsync(dance, subject, body);
    }
}
