using FURPMS.Application.Interfaces.Repositories;
using FURPMS.Domain.Entities.AI;
using FURPMS.Domain.Entities.Logs;
using FURPMS.Infrastructure.Data;

namespace FURPMS.Infrastructure.Repositories;

public class NotificationRepository : Repository<Notification>, INotificationRepository
{
    public NotificationRepository(FURPMSDbContext db) : base(db) { }

    public IQueryable<EmailLog> EmailLogs => _db.EmailLogs;
    public void AddEmailLog(EmailLog log) => _db.EmailLogs.Add(log);
}
