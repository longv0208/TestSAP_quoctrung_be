using FURPMS.Application.Constants;
using FURPMS.Application.Interfaces;
using FURPMS.Application.Interfaces.Repositories;
using FURPMS.Application.Interfaces.Services;
using FURPMS.Domain.Entities.AI;
using Microsoft.EntityFrameworkCore;

namespace FURPMS.Infrastructure.Services;

public class DeadlineReminderScanner : IDeadlineReminderScanner
{
    private readonly IContractRepository _contracts;
    private readonly INotificationRepository _notifications;
    private readonly IClock _clock;
    private readonly IEmailService _email;
    private readonly ISystemSettingService _settings;

    public DeadlineReminderScanner(
        IContractRepository contracts,
        INotificationRepository notifications,
        IClock clock,
        IEmailService email,
        ISystemSettingService settings)
    {
        _contracts = contracts;
        _notifications = notifications;
        _clock = clock;
        _email = email;
        _settings = settings;
    }

    public async Task ScanAsync(CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(_clock.UtcNow);

        // Mốc nhắc trước hạn do Admin cấu hình (mặc định 30/14/7 ngày).
        var reminderDays = await _settings.GetIntListAsync(
            SystemSettingKeys.DeadlineReminderDays,
            SystemSettingKeys.DefaultDeadlineReminderDays
                .Split(',').Select(int.Parse).ToList());

        var deliverables = await _contracts.Deliverables
            .Include(d => d.Contract)
                .ThenInclude(c => c.Project)
                    .ThenInclude(p => p.PiUser)
            .Where(d => d.DueDate.HasValue && d.AcceptanceStatus != AcceptanceStatus.Passed)
            .ToListAsync(ct);

        foreach (var deliverable in deliverables)
        {
            var dueDate = deliverable.DueDate!.Value;
            int daysUntilDue = dueDate.DayNumber - today.DayNumber;

            string? notificationType =
                daysUntilDue < 0 ? "DEADLINE_OVERDUE"
                : reminderDays.Contains(daysUntilDue) ? $"DEADLINE_REMINDER_T{daysUntilDue}"
                : null;

            if (notificationType == null)
                continue;

            var deliverableIdStr = deliverable.Id.ToString();

            bool alreadySent = await _notifications.Query().AnyAsync(
                n => n.RelatedEntityType == "ProductDeliverable"
                  && n.RelatedEntityId == deliverableIdStr
                  && n.NotificationType == notificationType,
                ct);

            if (alreadySent)
                continue;

            var piUser = deliverable.Contract?.Project?.PiUser;
            if (piUser == null)
                continue;

            var isOverdue = notificationType == "DEADLINE_OVERDUE";
            string title = isOverdue
                ? $"Sản phẩm quá hạn: {deliverable.ProductName}"
                : $"Nhắc nhở: {deliverable.ProductName} đến hạn trong {daysUntilDue} ngày";

            string body = isOverdue
                ? $"Sản phẩm \"{deliverable.ProductName}\" đã quá hạn (hạn: {dueDate}). " +
                  "Vui lòng xem xét tạo yêu cầu gia hạn hợp đồng."
                : $"Sản phẩm \"{deliverable.ProductName}\" đến hạn ngày {dueDate} " +
                  $"(còn {daysUntilDue} ngày).";

            string actionUrl = isOverdue
                ? $"/api/contracts/{deliverable.ContractId}/amendments"
                : $"/api/contracts/{deliverable.ContractId}/deliverables";

            await _notifications.AddAsync(new Notification
            {
                UserId = piUser.Id,
                NotificationType = notificationType,
                Title = title,
                Body = body,
                ActionUrl = actionUrl,
                RelatedEntityType = "ProductDeliverable",
                RelatedEntityId = deliverableIdStr,
                Priority = isOverdue ? "URGENT" : "HIGH"
            });

            await _email.SendAsync(
                recipientEmail: piUser.Email,
                subject: title,
                body: body,
                emailType: notificationType,
                recipientUserId: piUser.Id);
        }

        await ScanProgressReportsAsync(today, reminderDays, ct);

        await _notifications.SaveChangesAsync(ct);
    }

    // Nhắc hạn nộp BÁO CÁO TIẾN ĐỘ (thầy 29/07). Trước đây scanner chỉ quét sản phẩm nên PI
    // không được nhắc gì về báo cáo. Chỉ nhắc kỳ CHƯA nộp và đã được Staff đặt hạn.
    private async Task ScanProgressReportsAsync(DateOnly today, IReadOnlyList<int> reminderDays, CancellationToken ct)
    {
        var reports = await _contracts.ProgressReports
            .Include(r => r.Contract)
                .ThenInclude(c => c.Project)
                    .ThenInclude(p => p.PiUser)
            .Where(r => r.DueDate.HasValue && r.SubmittedAt == null)
            .ToListAsync(ct);

        foreach (var report in reports)
        {
            var dueDate = report.DueDate!.Value;
            int daysUntilDue = dueDate.DayNumber - today.DayNumber;

            string? notificationType =
                daysUntilDue < 0 ? "REPORT_OVERDUE"
                : reminderDays.Contains(daysUntilDue) ? $"REPORT_REMINDER_T{daysUntilDue}"
                : null;
            if (notificationType == null) continue;

            var reportIdStr = report.Id.ToString();
            bool alreadySent = await _notifications.Query().AnyAsync(
                n => n.RelatedEntityType == "ProgressReport"
                  && n.RelatedEntityId == reportIdStr
                  && n.NotificationType == notificationType,
                ct);
            if (alreadySent) continue;

            var piUser = report.Contract?.Project?.PiUser;
            if (piUser == null) continue;

            var roundLabel = string.IsNullOrWhiteSpace(report.RoundName)
                ? $"Kỳ {report.ReportRound}"
                : report.RoundName!;
            var isOverdue = notificationType == "REPORT_OVERDUE";

            string title = isOverdue
                ? $"Báo cáo tiến độ quá hạn: {roundLabel}"
                : $"Nhắc nộp báo cáo tiến độ {roundLabel} — còn {daysUntilDue} ngày";
            string body = isOverdue
                ? $"Báo cáo tiến độ \"{roundLabel}\" đã quá hạn nộp (hạn: {dueDate}). Vui lòng nộp sớm hoặc liên hệ phòng QLKH để xin gia hạn."
                : $"Báo cáo tiến độ \"{roundLabel}\" đến hạn nộp ngày {dueDate} (còn {daysUntilDue} ngày).";

            await _notifications.AddAsync(new Notification
            {
                UserId = piUser.Id,
                NotificationType = notificationType,
                Title = title,
                Body = body,
                ActionUrl = "/progress-reports",
                RelatedEntityType = "ProgressReport",
                RelatedEntityId = reportIdStr,
                Priority = isOverdue ? "URGENT" : "HIGH"
            });

            await _email.SendAsync(
                recipientEmail: piUser.Email,
                subject: title,
                body: body,
                emailType: notificationType,
                recipientUserId: piUser.Id);
        }
    }
}
