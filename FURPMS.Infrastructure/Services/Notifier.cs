using FURPMS.Application.Interfaces.Repositories;
using FURPMS.Application.Interfaces.Services;
using FURPMS.Domain.Entities.AI;
using Microsoft.EntityFrameworkCore;

namespace FURPMS.Infrastructure.Services;

/// <inheritdoc cref="INotifier"/>
public class Notifier : INotifier
{
    private readonly INotificationRepository _notifications;
    private readonly IUserRepository _users;
    private readonly IEmailService _email;

    public Notifier(
        INotificationRepository notifications,
        IUserRepository users,
        IEmailService email)
    {
        _notifications = notifications;
        _users = users;
        _email = email;
    }

    public Task NotifyAsync(
        Guid userId,
        string type,
        string title,
        string body,
        string? actionUrl = null,
        string? entityType = null,
        string? entityId = null,
        string priority = "NORMAL",
        bool alsoEmail = true)
        => NotifyManyAsync(
            new[] { userId }, type, title, body, actionUrl, entityType, entityId, priority, alsoEmail);

    public async Task NotifyManyAsync(
        IReadOnlyCollection<Guid> userIds,
        string type,
        string title,
        string body,
        string? actionUrl = null,
        string? entityType = null,
        string? entityId = null,
        string priority = "NORMAL",
        bool alsoEmail = true)
    {
        var recipients = userIds.Distinct().ToList();
        if (recipients.Count == 0) return;

        foreach (var userId in recipients)
        {
            await _notifications.AddAsync(new Notification
            {
                UserId = userId,
                NotificationType = type,
                Title = title,
                Body = body,
                ActionUrl = actionUrl,
                RelatedEntityType = entityType,
                RelatedEntityId = entityId,
                Priority = priority
            });
        }

        if (!alsoEmail) return;

        // 1 query cho cả nhóm; bỏ qua tài khoản không có email thay vì ném lỗi —
        // thông báo in-app đã lưu, không được để lỗi mail chặn nghiệp vụ.
        var mailboxes = await _users.Query()
            .Where(u => recipients.Contains(u.Id) && u.Email != null && u.Email != "")
            .Select(u => new { u.Id, u.Email })
            .ToListAsync();

        foreach (var box in mailboxes)
            await _email.SendAsync(box.Email, title, body, type, box.Id, actionUrl);
    }
}
