using FURPMS.Application.Interfaces.Repositories;
using FURPMS.Domain.Entities.AI;
using FURPMS.Infrastructure.Data;

namespace FURPMS.Infrastructure.Repositories;

public class DocumentRepository : Repository<Document>, IDocumentRepository
{
    public DocumentRepository(FURPMSDbContext db) : base(db) { }
}
