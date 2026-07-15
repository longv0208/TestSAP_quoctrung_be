using FURPMS.Domain.Entities.MasterData;

namespace FURPMS.Domain.Entities.Cycles;

// Review 2 điểm (b): 1 đợt chứa nhiều track; PI nộp vào 1 track cụ thể trong đợt.
public class CycleTrack
{
    public int Id { get; set; }
    public int CycleId { get; set; }
    public int TrackId { get; set; }
    public bool IsOpen { get; set; } = true;

    public ResearchCycle Cycle { get; set; } = null!;
    public ResearchTrack Track { get; set; } = null!;
}
