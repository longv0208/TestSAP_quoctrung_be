namespace FURPMS.Application.Interfaces.Services;

public interface IEmailService
{
    Task SendAsync(
        string recipientEmail,
        string subject,
        string body,
        string emailType,
        Guid? recipientUserId = null,
        // Đường dẫn tương đối trong FE (vd "/proposals/my"). Có thì email hiện nút bấm.
        string? actionUrl = null);
}
