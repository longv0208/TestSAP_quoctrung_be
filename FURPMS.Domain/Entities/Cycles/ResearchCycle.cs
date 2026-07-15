using FURPMS.Domain.Entities.MasterData;
using FURPMS.Domain.Entities.Users;

namespace FURPMS.Domain.Entities.Cycles;

public class ResearchCycle
{
    public int Id { get; set; }
    public int CycleYear { get; set; }
    public string? SemesterCode { get; set; }
    public int ResearchTypeId { get; set; }
    public DateOnly? OrderCollectionDeadline { get; set; }
    public DateOnly SubmissionOpenDate { get; set; }
    public DateOnly SubmissionDeadline { get; set; }
    public DateOnly ReviewDeadline { get; set; }
    public string Status { get; set; } = "PLANNING";
    public string? Description { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public ResearchType ResearchType { get; set; } = null!;
    public User CreatedByUser { get; set; } = null!;
    public ICollection<ResearchOrder> ResearchOrders { get; set; } = new List<ResearchOrder>();
}
