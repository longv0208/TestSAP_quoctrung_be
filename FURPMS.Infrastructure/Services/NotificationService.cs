using FURPMS.Application.DTOs.Notifications;
using FURPMS.Application.Interfaces.Repositories;
using FURPMS.Application.Interfaces.Services;
using Microsoft.EntityFrameworkCore;

namespace FURPMS.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _notifications;

    public NotificationService(INotificationRepository notifications)
    {
        _notifications = notifications;
    }

    public async Task<IEnumerable<NotificationDto>> GetMyNotificationsAsync(Guid userId)
    {
        var items = await _notifications.Query()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(50)
            .ToListAsync();

        return items.Select(n => new NotificationDto
        {
            Id = n.Id,
            Type = n.NotificationType,
            Title = n.Title,
            Message = n.Body,
            ActionUrl = n.ActionUrl,
            RelatedEntityType = n.RelatedEntityType,
            RelatedEntityId = n.RelatedEntityId,
            IsRead = n.IsRead,
            Priority = n.Priority,
            CreatedAt = n.CreatedAt
        });
    }

    public async Task<NotificationCountDto> GetUnreadCountAsync(Guid userId)
    {
        var total = await _notifications.Query().CountAsync(n => n.UserId == userId);
        var unread = await _notifications.Query().CountAsync(n => n.UserId == userId && !n.IsRead);
        return new NotificationCountDto { Unread = unread, Total = total };
    }

    public async Task MarkReadAsync(Guid notificationId, Guid userId)
    {
        var notification = await _notifications.Query()
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId)
            ?? throw new KeyNotFoundException($"Notification {notificationId} not found.");

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            await _notifications.SaveChangesAsync();
        }
    }

    public async Task MarkAllReadAsync(Guid userId)
    {
        var unread = await _notifications.Query()
            .Where(n => n.UserId == userId && !n.IsRead)
            .ToListAsync();

        foreach (var n in unread)
        {
            n.IsRead = true;
            n.ReadAt = DateTime.UtcNow;
        }

        if (unread.Count > 0)
            await _notifications.SaveChangesAsync();
    }
}
