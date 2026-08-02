using System.Net;
using System.Net.Mail;
using FURPMS.Application.Constants;
using FURPMS.Application.Interfaces.Repositories;
using FURPMS.Application.Interfaces.Services;
using FURPMS.Application.Settings;
using FURPMS.Domain.Entities.Logs;
using Microsoft.Extensions.Options;

namespace FURPMS.Infrastructure.Services;

public class SmtpEmailService : IEmailService
{
    private readonly INotificationRepository _notifications;
    private readonly EmailSettings _email;
    private readonly ISystemSettingService _sysSettings;

    public SmtpEmailService(
        INotificationRepository notifications,
        IOptions<EmailSettings> settings,
        ISystemSettingService sysSettings)
    {
        _notifications = notifications;
        _email = settings.Value;
        _sysSettings = sysSettings;
    }

    public async Task SendAsync(
        string recipientEmail,
        string subject,
        string body,
        string emailType,
        Guid? recipientUserId = null)
    {
        // Admin có thể tắt gửi mail để demo/thử nghiệm mà không làm phiền người thật.
        // Vẫn ghi log là SKIPPED để biết lẽ ra đã gửi cho ai.
        var emailEnabled = await _sysSettings.GetBoolAsync(
            SystemSettingKeys.EmailEnabled, SystemSettingKeys.DefaultEmailEnabled);

        var status = "SENT";
        string? errorMessage = null;

        if (!emailEnabled)
        {
            status = "SKIPPED";
            errorMessage = "Gửi email đang tắt trong cấu hình hệ thống.";
        }
        else
        try
        {
            using var client = new SmtpClient(_email.SmtpServer, _email.SmtpPort)
            {
                Credentials = new NetworkCredential(_email.SmtpUsername, _email.SmtpPassword),
                EnableSsl = true
            };

            var message = new MailMessage
            {
                From = new MailAddress(_email.FromEmail, _email.FromName),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            message.To.Add(recipientEmail);

            await client.SendMailAsync(message);
        }
        catch (Exception ex)
        {
            status = "FAILED";
            errorMessage = ex.Message;
        }

        _notifications.AddEmailLog(new EmailLog
        {
            RecipientUserId = recipientUserId,
            RecipientEmail = recipientEmail,
            EmailType = emailType,
            Subject = subject,
            SentAt = DateTime.UtcNow,
            Status = status,
            ErrorMessage = errorMessage,
            CreatedAt = DateTime.UtcNow
        });
        await _notifications.SaveChangesAsync();
    }
}
