namespace FURPMS.Application.Interfaces.Services;

public interface IEmailService
{
    Task SendAsync(
        string recipientEmail,
        string subject,
        string body,
        string emailType,
        Guid? recipientUserId = null);
}
