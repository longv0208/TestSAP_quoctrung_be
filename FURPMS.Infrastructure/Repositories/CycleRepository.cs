using FURPMS.Application.Interfaces.Repositories;
using FURPMS.Domain.Entities.Cycles;
using FURPMS.Domain.Entities.MasterData;
using FURPMS.Infrastructure.Data;

namespace FURPMS.Infrastructure.Repositories;

public class CycleRepository : Repository<ResearchCycle>, ICycleRepository
{
    public CycleRepository(FURPMSDbContext db) : base(db) { }

    public IQueryable<ResearchTrack> Tracks => _db.ResearchTracks;
    public IQueryable<CycleTrack> CycleTracks => _db.CycleTracks;
    public IQueryable<ResearchOrder> Orders => _db.ResearchOrders;
    public IQueryable<DeadlineExtension> DeadlineExtensions => _db.DeadlineExtensions;
    public async Task AddCycleTrackAsync(CycleTrack cycleTrack) => await _db.CycleTracks.AddAsync(cycleTrack);
    public void RemoveCycleTrack(CycleTrack cycleTrack) => _db.CycleTracks.Remove(cycleTrack);
    public void RemoveOrder(ResearchOrder order) => _db.ResearchOrders.Remove(order);
    public async Task AddOrderAsync(ResearchOrder order) => await _db.ResearchOrders.AddAsync(order);
    public async Task AddDeadlineExtensionAsync(DeadlineExtension extension) => await _db.DeadlineExtensions.AddAsync(extension);
}
