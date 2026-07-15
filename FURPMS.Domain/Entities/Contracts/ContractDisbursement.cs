using FURPMS.Domain.Entities.Projects;
using FURPMS.Domain.Entities.Users;

namespace FURPMS.Domain.Entities.Contracts;

public class ContractDisbursement
{
    public int Id { get; set; }
    public Guid ContractId { get; set; }
    public int? PhaseId { get; set; }            // Review 2 (e): tranche gắn giai đoạn HĐ (applied)
    public int RoundNumber { get; set; }
    public decimal Percentage { get; set; }
    public decimal PlannedAmount { get; set; }
    public decimal? ActualAmount { get; set; }
    public string ConditionDescription { get; set; } = null!;
    public DateTime? ConditionMetAt { get; set; }
    public Guid? ConditionMetBy { get; set; }
    public DateTime? DisbursedAt { get; set; }
    public string? BankReference { get; set; }
    public string Status { get; set; } = "PENDING";
    public Guid? ProcessedBy { get; set; }
    public string? Notes { get; set; }
    public int? DeliverableId { get; set; }      // hoặc gắn sản phẩm đã nghiệm thu (basic)

    public Contract Contract { get; set; } = null!;
    public ContractPhase? Phase { get; set; }
    public User? ConditionMetByUser { get; set; }
    public User? ProcessedByUser { get; set; }
    public ProjectDeliverable? Deliverable { get; set; }
}
