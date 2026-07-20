using AbdClub.Models;

namespace AbdClub.Services;

public interface IEmailService
{
    Task SendMembershipReminderAsync(Member member);
    Task SendVolunteerReminderAsync(Dance dance, Volunteer volunteer);
    Task SendOfficerReminderAsync(Dance dance, Member officer);
    Task SendEventNotificationToAllMembersAsync(Dance dance, string subject, string body);
    Task SendReminderAsync(Member member, string emailType);
    Task SendBroadcastAsync(List<Member> recipients, string subject, string body);
    Task SendMagicLinkEmailAsync(Member member, string magicUrl);  // ← add this

}
