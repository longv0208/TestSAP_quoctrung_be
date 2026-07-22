using System.Net;
using System.Net.Mail;
using FURPMS.Application.Interfaces.Repositories;
using FURPMS.Application.Interfaces.Services;
using FURPMS.Application.Settings;
using FURPMS.Domain.Entities.Logs;
using Microsoft.Extensions.Options;

namespace FURPMS.Infrastructure.Services;

public class SmtpEmailService : IEmailService
{
    private readonly INotificationRepository _notifications;
    private readonly EmailSettings _settings;

    public SmtpEmailService(INotificationRepository notifications, IOptions<EmailSettings> settings)
    {
        _notifications = notifications;
        _settings = settings.Value;
    }

    public async Task SendAsync(
        string recipientEmail,
        string subject,
        string body,
        string emailType,
        Guid? recipientUserId = null)
    {
        var status = "SENT";
        string? errorMessage = null;

        try
        {
            using var client = new SmtpClient(_settings.SmtpServer, _settings.SmtpPort)
            {
                Credentials = new NetworkCredential(_settings.SmtpUsername, _settings.SmtpPassword),
                EnableSsl = true
            };

            var message = new MailMessage
            {
                From = new MailAddress(_settings.FromEmail, _settings.FromName),
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
