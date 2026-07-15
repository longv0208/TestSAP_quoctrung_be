using FURPMS.Domain.Entities.Contracts;
using FURPMS.Domain.Entities.MasterData;

namespace FURPMS.Domain.Entities.Projects;

// GỘP ProposalExpectedProduct + ProductDeliverable (Review 2 điểm a):
// sản phẩm là CON CỦA PROJECT; contract ký một TẬP CON (contract_id/phase nullable).
public class ProjectDeliverable
{
    public int Id { get; set; }
    public Guid ProjectId { get; set; }
    public Guid? ContractId { get; set; }        // null = mới cam kết, chưa gắn hợp đồng nào
    public int? ContractPhaseId { get; set; }    // thuộc giai đoạn nào của hợp đồng
    public int? CategoryId { get; set; }
    public string ProductName { get; set; } = null!;
    public string? ScientificRequirements { get; set; }
    public string? Description { get; set; }
    public string? Notes { get; set; }
    public DateOnly? DueDate { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public string? FileUrl { get; set; }
    public string? TrialEvidenceUrl { get; set; }
    public string? QualityAssessment { get; set; }
    public bool IsCompleted { get; set; }
    public string? AcceptanceStatus { get; set; }
    public int Sequence { get; set; }

    public Project Project { get; set; } = null!;
    public Contract? Contract { get; set; }
    public ContractPhase? ContractPhase { get; set; }
    public ProductCategory? Category { get; set; }
}
