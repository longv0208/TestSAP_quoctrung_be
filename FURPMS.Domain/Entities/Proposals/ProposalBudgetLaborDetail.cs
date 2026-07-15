using FURPMS.Domain.Entities.Projects;

namespace FURPMS.Domain.Entities.Proposals;

public class ProposalBudgetLaborDetail
{
    public int Id { get; set; }
    public Guid ProposalId { get; set; }
    public int ProjectMemberId { get; set; }    // đổi FK: thành viên giờ thuộc project
    public decimal TotalResearchHours { get; set; }
    public decimal HourlyRate { get; set; }
    public decimal TotalAmount { get; private set; }
    public decimal? WorkDays { get; set; }
    public decimal? Coefficient { get; set; }
    public decimal? DailyRate { get; set; }
    public int Sequence { get; set; }

    public Proposal Proposal { get; set; } = null!;
    public ProjectMember ProjectMember { get; set; } = null!;
}
