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

    public DeadlineReminderScanner(
        IContractRepository contracts,
        INotificationRepository notifications,
        IClock clock,
        IEmailService email)
    {
        _contracts = contracts;
        _notifications = notifications;
        _clock = clock;
        _email = email;
    }

    public async Task ScanAsync(CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(_clock.UtcNow);

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

            string? notificationType = daysUntilDue switch
            {
                30 => "DEADLINE_REMINDER_T30",
                14 => "DEADLINE_REMINDER_T14",
                7  => "DEADLINE_REMINDER_T7",
                < 0 => "DEADLINE_OVERDUE",
                _ => null
            };

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

        await _notifications.SaveChangesAsync(ct);
    }
}
