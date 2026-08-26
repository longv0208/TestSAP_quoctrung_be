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
    private readonly INotifier _notifier;
    private readonly IReviewRepository _review;
    private readonly IDeadlineResolver _deadlines;

    public DeadlineReminderScanner(
        IContractRepository contracts,
        INotificationRepository notifications,
        IClock clock,
        IEmailService email,
        ISystemSettingService settings,
        INotifier notifier,
        IReviewRepository review,
        IDeadlineResolver deadlines)
    {
        _contracts = contracts;
        _notifications = notifications;
        _clock = clock;
        _email = email;
        _settings = settings;
        _notifier = notifier;
        _review = review;
        _deadlines = deadlines;
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
            // `Contract` là navigation CÓ THỂ NULL (sản phẩm gắn với đề tài trước, gắn hợp đồng
            // sau) — dùng `!` để nói rõ EF chỉ dựng đường Include, không truy cập giá trị ở đây.
            .Include(d => d.Contract!)
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

            // actionUrl phải là đường GIAO DIỆN của FE — /api/... không khớp route nào, bấm vào
            // rơi vào trang 404 (bug thật, người dùng phát hiện qua thao tác bấm thử).
            string actionUrl = isOverdue ? "/my-amendments" : "/deliverables";

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
        await ScanFinalReportsAsync(today, reminderDays, ct);
        await ScanReviewRoundsAsync(today, reminderDays, ct);
        await ScanSettlementsAsync(today, reminderDays, ct);

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

    /// <summary>
    /// Quyết định có nhắc hay không, và nhắc kiểu gì. Ba scanner dưới dùng chung để mọi loại hạn
    /// tuân theo cùng một luật: quá hạn thì báo ngay, chưa tới hạn thì chỉ báo ĐÚNG các mốc Admin
    /// cấu hình (mặc định T-30/14/7/3) — không phải ngày nào cũng dội thông báo.
    /// </summary>
    private static string? ReminderTypeFor(DateOnly dueDate, DateOnly today, IReadOnlyList<int> reminderDays)
    {
        var daysUntilDue = dueDate.DayNumber - today.DayNumber;
        if (daysUntilDue < 0) return "DEADLINE_OVERDUE";
        return reminderDays.Contains(daysUntilDue) ? $"DEADLINE_REMINDER_T{daysUntilDue}" : null;
    }

    /// <summary>Đã bắn đúng loại thông báo này cho đúng bản ghi này chưa — chống nhắc trùng.</summary>
    private Task<bool> AlreadySentAsync(string entityType, string entityId, string type, CancellationToken ct)
        => _notifications.Query().AnyAsync(
            n => n.RelatedEntityType == entityType && n.RelatedEntityId == entityId
              && n.NotificationType == type, ct);

    // ── Hạn nộp BÁO CÁO NGHIỆM THU (QĐ543 Điều 11.2.a) ───────────────────────
    // Cột `final_reports.deadline` trước 25/08 chưa bao giờ được ghi nên scanner không có gì để
    // quét. Nay đã ghi lúc nộp, và cũng suy được từ ngày kết thúc hợp đồng.
    private async Task ScanFinalReportsAsync(DateOnly today, IReadOnlyList<int> reminderDays, CancellationToken ct)
    {
        var leadDays = await _settings.GetIntAsync(
            SystemSettingKeys.FinalReportLeadDays, SystemSettingKeys.DefaultFinalReportLeadDays);

        var contracts = await _contracts.Query()
            .Include(c => c.Project).ThenInclude(p => p.PiUser)
            .Where(c => c.Status != ContractStatus.Settled && c.Status != ContractStatus.Terminated)
            .ToListAsync(ct);

        foreach (var contract in contracts)
        {
            var pi = contract.Project?.PiUser;
            if (pi is null) continue;

            var report = await _contracts.FinalReports
                .FirstOrDefaultAsync(f => f.ProjectId == contract.ProjectId, ct);

            // Nộp rồi thì thôi — hạn này chỉ có ý nghĩa TRƯỚC khi nộp.
            if (report?.FinalSubmittedAt is not null || report?.SubmittedAt is not null) continue;

            var due = report?.Deadline ?? contract.EndDate.AddDays(-leadDays);
            var type = ReminderTypeFor(due, today, reminderDays);
            if (type is null) continue;

            var entityId = contract.Id.ToString();
            if (await AlreadySentAsync("FinalReport", entityId, type, ct)) continue;

            var overdue = type == "DEADLINE_OVERDUE";
            var title = overdue
                ? "Quá hạn nộp báo cáo nghiệm thu"
                : $"Nhắc nộp báo cáo nghiệm thu — còn {due.DayNumber - today.DayNumber} ngày";
            var body = overdue
                ? $"Báo cáo nghiệm thu của đề tài \"{contract.Project?.TitleVi}\" đã quá hạn (hạn {due:dd/MM/yyyy}). " +
                  "QĐ543 Điều 11.2.a yêu cầu nộp trước khi kết thúc đề tài."
                : $"Báo cáo nghiệm thu của đề tài \"{contract.Project?.TitleVi}\" đến hạn ngày {due:dd/MM/yyyy}.";

            await _notifier.NotifyAsync(pi.Id, type, title, body,
                // actionUrl phải là ĐƯỜNG GIAO DIỆN của FE, không phải đường API — bấm vào một
                // đường /api/... thì router FE không khớp được route nào, rơi vào trang 404.
                actionUrl: "/final-reports",
                entityType: "FinalReport", entityId: entityId,
                priority: overdue ? "URGENT" : "HIGH");
        }
    }

    // ── Hạn CHẤM của vòng — nhắc Phòng QLKH, vì mở/đóng vòng là việc của họ ──
    private async Task ScanReviewRoundsAsync(DateOnly today, IReadOnlyList<int> reminderDays, CancellationToken ct)
    {
        var rounds = await _review.ReviewRounds
            .Where(r => r.Status == ReviewRoundStatus.Open && r.ScoringDeadline != null)
            .Select(r => new { r.Id, r.RoundNumber, r.RoundType, r.ScoringDeadline })
            .ToListAsync(ct);

        foreach (var round in rounds)
        {
            // Hạn HIỆU LỰC — Staff có thể đã dời hạn (rule #19).
            var due = await _deadlines.EffectiveAsync(
                IDeadlineResolver.TargetTypeReviewRound, round.Id.ToString(), round.ScoringDeadline!.Value);

            var type = ReminderTypeFor(due, today, reminderDays);
            if (type is null) continue;

            var entityId = round.Id.ToString();
            if (await AlreadySentAsync("ReviewRound", entityId, type, ct)) continue;

            var overdue = type == "DEADLINE_OVERDUE";
            var title = overdue
                ? $"Vòng {round.RoundNumber} đã quá hạn chấm"
                : $"Vòng {round.RoundNumber} còn {due.DayNumber - today.DayNumber} ngày phải chấm xong";
            var body = overdue
                ? $"Vòng chấm số {round.RoundNumber} quá hạn từ {due:dd/MM/yyyy} mà chưa chốt kết quả. " +
                  "Hệ thống KHÔNG tự đóng vòng — kết luận vẫn do Chủ tịch hội đồng quyết."
                : $"Vòng chấm số {round.RoundNumber} phải xong trước {due:dd/MM/yyyy}.";

            await _notifier.NotifyRoleAsync("Staff", type, title, body,
                actionUrl: "/review-board",
                entityType: "ReviewRound", entityId: entityId,
                priority: overdue ? "URGENT" : "HIGH");
        }
    }

    // ── Hạn QUYẾT TOÁN (BM13) — cũng là việc của Phòng QLKH ─────────────────
    private async Task ScanSettlementsAsync(DateOnly today, IReadOnlyList<int> reminderDays, CancellationToken ct)
    {
        var settlements = await _contracts.Settlements
            .Include(s => s.Contract).ThenInclude(c => c.Project)
            .Where(s => s.SettlementDeadline != null && s.SettlementSignedAt == null)
            .ToListAsync(ct);

        foreach (var settlement in settlements)
        {
            var due = settlement.SettlementDeadline!.Value;
            var type = ReminderTypeFor(due, today, reminderDays);
            if (type is null) continue;

            var entityId = settlement.Id.ToString();
            if (await AlreadySentAsync("ContractSettlement", entityId, type, ct)) continue;

            var overdue = type == "DEADLINE_OVERDUE";
            var title = overdue ? "Quá hạn quyết toán hợp đồng" : "Nhắc quyết toán hợp đồng";
            var body = $"Hợp đồng {settlement.Contract?.ContractNumber} " +
                       (overdue
                            ? $"đã quá hạn quyết toán từ {due:dd/MM/yyyy}."
                            : $"đến hạn quyết toán ngày {due:dd/MM/yyyy}.") +
                       " Cần ký Biên bản thanh lý (BM13) để đóng hồ sơ.";

            await _notifier.NotifyRoleAsync("Staff", type, title, body,
                actionUrl: "/contracts",
                entityType: "ContractSettlement", entityId: entityId,
                priority: overdue ? "URGENT" : "NORMAL");
        }
    }
}
