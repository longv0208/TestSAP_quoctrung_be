using FURPMS.Domain.Entities.Cycles;
using FURPMS.Domain.Entities.MasterData;

namespace FURPMS.Application.Interfaces.Repositories;

public interface ICycleRepository : IRepository<ResearchCycle>
{
    IQueryable<ResearchTrack> Tracks { get; }
    IQueryable<CycleTrack> CycleTracks { get; }
    IQueryable<ResearchOrder> Orders { get; }
    IQueryable<DeadlineExtension> DeadlineExtensions { get; }
    Task AddCycleTrackAsync(CycleTrack cycleTrack);
    void RemoveCycleTrack(CycleTrack cycleTrack);
    void RemoveOrder(ResearchOrder order);
    Task AddOrderAsync(ResearchOrder order);
    Task AddDeadlineExtensionAsync(DeadlineExtension extension);
    void RemoveDeadlineExtension(DeadlineExtension extension);
}
