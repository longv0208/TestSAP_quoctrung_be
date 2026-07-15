using FURPMS.Domain.Entities.Projects;
using FURPMS.Domain.Entities.Users;

namespace FURPMS.Domain.Entities.Contracts;

// Sau Review 2 (điểm e): contract thuộc PROJECT, 1 project → n contract (ký từng phần/giai đoạn).
public class Contract
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public string ContractNumber { get; set; } = null!;
    public string? ScopeTitle { get; set; }        // hợp đồng này ký hạng mục/giai đoạn gì
    public DateTime? SignedAt { get; set; }
    public decimal TotalAmount { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public DateOnly OriginalEndDate { get; set; }
    public int MaxExtensionMonths { get; set; }
    public string? EcontractId { get; set; }
    public string? EcontractUrl { get; set; }
    public string? SideARepresentative { get; set; } = "Nguyễn Kim Ánh";
    public string Status { get; set; } = "PENDING_SIGNATURE";
    public DateTime? TerminatedAt { get; set; }
    public string? TerminatedReason { get; set; }
    public Guid? TerminatedBy { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Project Project { get; set; } = null!;
    public User? TerminatedByUser { get; set; }
    public User CreatedByUser { get; set; } = null!;
    public ICollection<ContractPhase> Phases { get; set; } = new List<ContractPhase>();
    public ICollection<ContractDisbursement> Disbursements { get; set; } = new List<ContractDisbursement>();
    public ContractSettlement? Settlement { get; set; }
}
