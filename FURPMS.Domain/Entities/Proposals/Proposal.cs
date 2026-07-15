using FURPMS.Domain.Entities.Projects;
using FURPMS.Domain.Entities.Users;

namespace FURPMS.Domain.Entities.Proposals;

// Sau Review 2 (điểm a): Proposal = TÀI LIỆU CÓ VERSION thuộc Project.
// Cột neo (cycle/track/order/PI/hosting/type) đã dời lên Project.
public class Proposal
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public int VersionNo { get; set; } = 1;      // v1, v2… — REVISION tạo bản mới, giữ lịch sử
    public bool IsCurrent { get; set; } = true;
    public string TitleVi { get; set; } = null!;
    public string? TitleEn { get; set; }
    public int DurationMonths { get; set; }
    public DateOnly PlannedStartDate { get; set; }
    public DateOnly PlannedEndDate { get; set; }
    public string AbstractVi { get; set; } = null!;
    public string? AbstractEn { get; set; }
    public string? OverallIntro { get; set; }
    public string ResearchObjectives { get; set; } = null!;
    public string? LiteratureReview { get; set; }
    public string? ResearchApproach { get; set; }
    public string? Methodology { get; set; }
    public string? NoveltyOriginality { get; set; }
    public string? FacilitiesEquipment { get; set; }
    public string? ApplicationPotential { get; set; }
    public string? TransferPotential { get; set; }
    public string? ExpectedOutput { get; set; }     // sản phẩm/kết quả dự kiến (tóm tắt tự do)
    public string Status { get; set; } = "DRAFT";   // status TÀI LIỆU; vòng đời lớn ở Project.Status
    public string? FundingMethod { get; set; }      // WHOLE / PARTIAL
    public DateTime? RevisionRequestedAt { get; set; }
    public DateTime? RevisionDeadline { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public Guid? ApprovedBy { get; set; }
    public string? RejectionReason { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Project Project { get; set; } = null!;
    public User? ApprovedByUser { get; set; }
    public ProposalBudget? Budget { get; set; }
    public ICollection<ProposalResearchContent> ResearchContents { get; set; } = new List<ProposalResearchContent>();
}
