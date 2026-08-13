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
        Guid? recipientUserId = null,
        string? actionUrl = null)
    {
        // Admin có thể tắt gửi mail để demo/thử nghiệm mà không làm phiền người thật.
        // Vẫn ghi log là SKIPPED để biết lẽ ra đã gửi cho ai.
        var emailEnabled = await _sysSettings.GetBoolAsync(
            SystemSettingKeys.EmailEnabled, SystemSettingKeys.DefaultEmailEnabled);

        var status = "SENT";
        string? errorMessage = null;
        // Log giữ NGƯỜI NHẬN THẬT (để trả lời được "đã báo cho PI chưa?"), còn tiêu đề
        // ghi kèm dấu chuyển hướng nếu có — nhìn log là biết mail thực sự đi đâu.
        var loggedSubject = subject;

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

            // Dev/demo: hứng hộ mail của các địa chỉ KHÔNG CÓ THẬT (tài khoản seed) về một
            // hộp thư thật, tiêu đề ghi rõ ai mới là người nhận. Địa chỉ thật vẫn đi thẳng —
            // nếu chuyển hướng cả địa chỉ thật thì tạo tài khoản bằng mail thật sẽ không bao
            // giờ nhận được thư, mà log vẫn báo "SENT" nên rất khó lần ra.
            var redirect = _email.CatchFakeMailInbox?.Trim();
            var isRedirected = !string.IsNullOrEmpty(redirect) && ShouldRedirect(recipientEmail);

            using var message = new MailMessage
            {
                From = new MailAddress(_email.FromEmail, _email.FromName),
                Subject = isRedirected ? $"[→ {recipientEmail}] {subject}" : subject
            };
            message.To.Add(isRedirected ? redirect! : recipientEmail);
            loggedSubject = message.Subject;

            // Gửi kèm CẢ hai bản (text + HTML). Trước đây body là text trần nhưng cờ
            // IsBodyHtml=true và không có bản text thay thế — đúng đặc điểm mail rác,
            // bị bộ lọc chấm điểm xấu. Nay đúng chuẩn multipart/alternative.
            var link = BuildLink(actionUrl);
            message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(
                BuildPlainText(subject, body, link), null, "text/plain"));
            message.AlternateViews.Add(AlternateView.CreateAlternateViewFromString(
                BuildHtml(subject, body, link), null, "text/html"));

            await client.SendMailAsync(message);
        }
        catch (Exception ex)
        {
            status = "FAILED";
            errorMessage = ex.Message;
        }

        await AddLogAndSaveAsync(recipientEmail, emailType, loggedSubject, status, errorMessage, recipientUserId);
    }

    /// <summary>
    /// Địa chỉ này có thuộc miền giả cần hứng hộ không. Không khai <c>RedirectDomains</c>
    /// ⇒ hứng tất cả (giữ nguyên hành vi cũ cho cấu hình đã có sẵn).
    /// </summary>
    private bool ShouldRedirect(string recipientEmail)
    {
        var domains = _email.RedirectDomains;
        if (domains == null || domains.Length == 0) return true;

        var at = recipientEmail.LastIndexOf('@');
        if (at < 0) return true; // địa chỉ hỏng — cứ hứng về, đừng bắn ra ngoài

        var domain = recipientEmail[(at + 1)..].Trim();
        return domains.Any(d => string.Equals(d.Trim().TrimStart('@'), domain, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>Ghép ActionUrl tương đối của thông báo với gốc FE thành link bấm được.</summary>
    private string? BuildLink(string? actionUrl)
    {
        if (string.IsNullOrWhiteSpace(actionUrl)) return null;
        if (actionUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase)) return actionUrl;

        // Một số thông báo cũ lưu đường dẫn API ("/api/contracts/..") — không mở được
        // trên trình duyệt người dùng, thà không hiện nút còn hơn hiện link chết.
        if (actionUrl.StartsWith("/api/", StringComparison.OrdinalIgnoreCase)) return null;

        return $"{_email.FrontendUrl.TrimEnd('/')}/{actionUrl.TrimStart('/')}";
    }

    private static string BuildPlainText(string subject, string body, string? link)
    {
        var text = $"{subject}\n\n{body}\n";
        if (link != null) text += $"\nXem chi tiết: {link}\n";
        return text + "\n—\nFURPMS — Hệ thống quản lý đề tài nghiên cứu khoa học\nEmail tự động, vui lòng không trả lời.";
    }

    private static string BuildHtml(string subject, string body, string? link)
    {
        var button = link == null ? "" :
            $"""<p style="margin:24px 0"><a href="{Esc(link)}" style="background:#1d4ed8;color:#fff;text-decoration:none;padding:10px 20px;border-radius:6px;display:inline-block;font-weight:600">Xem chi tiết</a></p>""";

        return $"""
            <!doctype html>
            <html lang="vi"><body style="margin:0;padding:24px;background:#f4f5f7;font-family:Segoe UI,Arial,sans-serif;color:#111827">
              <div style="max-width:560px;margin:0 auto;background:#fff;border-radius:10px;padding:28px">
                <p style="margin:0 0 4px;font-size:13px;letter-spacing:.06em;color:#6b7280;text-transform:uppercase">FURPMS</p>
                <h1 style="margin:0 0 16px;font-size:19px;line-height:1.35">{Esc(subject)}</h1>
                <p style="margin:0;font-size:15px;line-height:1.6;color:#374151">{Esc(body)}</p>
                {button}
                <p style="margin:24px 0 0;padding-top:16px;border-top:1px solid #e5e7eb;font-size:12px;color:#6b7280">
                  Hệ thống quản lý đề tài nghiên cứu khoa học — Trường Đại học FPT.<br>
                  Email tự động, vui lòng không trả lời.
                </p>
              </div>
            </body></html>
            """;
    }

    private static string Esc(string value) =>
        System.Net.WebUtility.HtmlEncode(value);

    private async Task AddLogAndSaveAsync(
        string recipientEmail, string emailType, string subject,
        string status, string? errorMessage, Guid? recipientUserId)
    {
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
