using FURPMS.Domain.Entities.AI;
using FURPMS.Domain.Entities.Logs;

namespace FURPMS.Application.Interfaces.Repositories;

public interface INotificationRepository : IRepository<Notification>
{
    IQueryable<EmailLog> EmailLogs { get; }
    void AddEmailLog(EmailLog log);
}
