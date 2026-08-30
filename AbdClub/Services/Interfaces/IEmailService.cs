using AbdClub.Models;

namespace AbdClub.Services.Interfaces;

public interface IEmailService
{
    Task SendMembershipReminderAsync(Member member);
    Task SendOfficerReminderAsync(Dance dance, Member officer);
    Task SendEventNotificationToAllMembersAsync(Dance dance, string subject, string body);
    Task SendReminderAsync(Member member, string emailType);
    Task SendMagicLinkEmailAsync(string recipientEmail, string recipientName, string magicUrl);
    Task SendMembershipStatusAsync(string recipientEmail, IReadOnlyList<Member> members);
    Task SendNewsletterWelcomeEmailAsync(string email, string firstName);
    // Add the new batch method signature here
    Task SendBroadcastEmailAsync(
        string recipientEmail,
        string recipientName,
        string subject,
        string bodyContent);
    string GenerateBroadcastHtmlBody(string recipientName, string bodyContent);
    Task SendVolunteerAssignmentNotificationAsync(
        string recipientEmail,
        string recipientName,
        string danceTitle,
        string dateString,
        string dutyType,
        bool isAddition);

    Task SendOfficerDutyNotificationAsync(
        string recipientEmail,
        string recipientName,
        string danceTitle,
        string dateString,
        string dutyActionText,
        int memberId);

    Task SendVolunteerReminderAsync(Dance dance, MasterVolunteer volunteer);


}
