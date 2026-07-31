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
    public async Task AddCycleTrackAsync(CycleTrack cycleTrack) => await _db.CycleTracks.AddAsync(cycleTrack);
    public async Task AddOrderAsync(ResearchOrder order) => await _db.ResearchOrders.AddAsync(order);
}
