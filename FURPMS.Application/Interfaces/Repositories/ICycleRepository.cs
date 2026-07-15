using FURPMS.Domain.Entities.Cycles;
using FURPMS.Domain.Entities.MasterData;

namespace FURPMS.Application.Interfaces.Repositories;

public interface ICycleRepository : IRepository<ResearchCycle>
{
    IQueryable<ResearchTrack> Tracks { get; }
    IQueryable<CycleTrack> CycleTracks { get; }
    IQueryable<ResearchOrder> Orders { get; }
    Task AddCycleTrackAsync(CycleTrack cycleTrack);
    Task AddOrderAsync(ResearchOrder order);
}
