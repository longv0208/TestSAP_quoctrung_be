using FURPMS.Application.Interfaces.Repositories;
using FURPMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FURPMS.Infrastructure.Repositories;

public class Repository<T> : IRepository<T> where T : class
{
    protected readonly FURPMSDbContext _db;
    protected readonly DbSet<T> _set;

    public Repository(FURPMSDbContext db)
    {
        _db = db;
        _set = db.Set<T>();
    }

    public IQueryable<T> Query() => _set;

    public async Task<T?> GetByIdAsync(object id) => await _set.FindAsync(id);

    public async Task AddAsync(T entity) => await _set.AddAsync(entity);

    public void AddRange(IEnumerable<T> entities) => _set.AddRange(entities);

    public void Update(T entity) => _set.Update(entity);

    public void Remove(T entity) => _set.Remove(entity);

    public async Task SaveChangesAsync(CancellationToken ct = default)
        => await _db.SaveChangesAsync(ct);
}
