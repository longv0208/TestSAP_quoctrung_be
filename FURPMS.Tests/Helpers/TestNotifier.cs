using FURPMS.Application.Interfaces.Services;
using FURPMS.Infrastructure.Data;
using FURPMS.Infrastructure.Repositories;
using FURPMS.Infrastructure.Services;

namespace FURPMS.Tests.Helpers;

/// <summary>
/// Dựng <see cref="INotifier"/> thật (vẫn ghi thông báo in-app vào DB test) nhưng
/// email đi vào hư vô — test không được gửi mail thật ra ngoài.
/// </summary>
public static class TestNotifier
{
    public static INotifier Create(FURPMSDbContext db) =>
        new Notifier(new NotificationRepository(db), new UserRepository(db), new SilentEmailService());

    private sealed class SilentEmailService : IEmailService
    {
        public Task SendAsync(
            string recipientEmail, string subject, string body,
            string emailType, Guid? recipientUserId = null,
            string? actionUrl = null) => Task.CompletedTask;
    }
}
