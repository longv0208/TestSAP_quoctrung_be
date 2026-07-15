namespace FURPMS.Application.DTOs.Contract;

public class ContractListResponse
{
    public Guid Id { get; set; }
    public string ContractNumber { get; set; } = null!;
    public Guid ProjectId { get; set; }
    public Guid ProposalId { get; set; }    // bản đề cương hiện hành (giữ cho FE cũ)
    public string? ProposalCode { get; set; }
    public string? ProposalTitle { get; set; }
    public string Status { get; set; } = null!;
    public decimal TotalAmount { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public DateTime? SignedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ContractDetailResponse
{
    public Guid Id { get; set; }
    public string ContractNumber { get; set; } = null!;
    public Guid ProjectId { get; set; }
    public Guid ProposalId { get; set; }    // bản đề cương hiện hành (giữ cho FE cũ)
    public string? ProposalCode { get; set; }
    public string? ProposalTitle { get; set; }
    public string? FundingMethod { get; set; }
    public string? ScopeTitle { get; set; }
    public string Status { get; set; } = null!;
    public decimal TotalAmount { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public DateOnly OriginalEndDate { get; set; }
    public int MaxExtensionMonths { get; set; }
    public string? SideARepresentative { get; set; }
    public string? EcontractUrl { get; set; }
    public DateTime? SignedAt { get; set; }
    public DateTime? TerminatedAt { get; set; }
    public string? TerminatedReason { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateContractRequest
{
    public Guid ProposalId { get; set; }
    public string ContractNumber { get; set; } = null!;
    public string? ScopeTitle { get; set; }    // ký hạng mục/giai đoạn gì (Review 2 điểm e)
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public int MaxExtensionMonths { get; set; } = 6;
    public string? SideARepresentative { get; set; }
    public string? EcontractUrl { get; set; }
}
