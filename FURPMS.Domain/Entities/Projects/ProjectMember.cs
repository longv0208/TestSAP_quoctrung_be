using FURPMS.Domain.Entities.Proposals;
using FURPMS.Domain.Entities.Users;

namespace FURPMS.Domain.Entities.Projects;

// Đổi tên từ ProposalTeamMember: thành viên thuộc PROJECT (không theo từng bản proposal).
public class ProjectMember
{
    public int Id { get; set; }
    public Guid ProjectId { get; set; }
    public Guid? UserId { get; set; }
    public string FullName { get; set; } = null!;
    public string? Email { get; set; }
    public string? AcademicTitle { get; set; }
    public string? UnitName { get; set; }
    public string WorkContent { get; set; } = null!;
    public decimal WorkMonths { get; set; }
    public bool IsPi { get; set; }
    public bool IsSecretary { get; set; }
    public string? MemberRoleCode { get; set; }
    public decimal? SalaryCoefficient { get; set; }
    public int Sequence { get; set; }

    public Project Project { get; set; } = null!;
    public User? User { get; set; }
    public ICollection<ProposalBudgetLaborDetail> BudgetLaborDetails { get; set; } = new List<ProposalBudgetLaborDetail>();
}
