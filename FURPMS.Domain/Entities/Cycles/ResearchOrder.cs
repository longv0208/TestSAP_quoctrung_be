using FURPMS.Domain.Entities.Projects;
using FURPMS.Domain.Entities.Users;

namespace FURPMS.Domain.Entities.Cycles;

public class ResearchOrder
{
    public int Id { get; set; }
    public int CycleId { get; set; }
    public int OrderingUnitId { get; set; }
    public string ResearchArea { get; set; } = null!;
    public string ProblemDescription { get; set; } = null!;
    public string? ExpectedProducts { get; set; }
    public decimal? BudgetCap { get; set; }     // trần kinh phí riêng của order (nếu có)
    public bool IsDefault { get; set; }         // order "Nghiên cứu cơ bản" chung — đề tài tự do map về đây (Review 2 điểm d)
    public string Status { get; set; } = "OPEN";
    public Guid? MatchedProjectId { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ResearchCycle Cycle { get; set; } = null!;
    public OrganizationalUnit OrderingUnit { get; set; } = null!;
    public Project? MatchedProject { get; set; }
    public User CreatedByUser { get; set; } = null!;
}
