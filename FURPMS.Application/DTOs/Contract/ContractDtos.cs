namespace FURPMS.Application.DTOs.Contract;

public class ContractListResponse
{
    public Guid Id { get; set; }
    public string ContractNumber { get; set; } = null!;
    public Guid ProjectId { get; set; }
    public Guid ProposalId { get; set; }    // bản đề cương hiện hành (giữ cho FE cũ)
    public string? ProposalCode { get; set; }
    public string? ProposalTitle { get; set; }
    public string? PiName { get; set; }        // chủ nhiệm đề tài (hiển thị ở danh sách hợp đồng)
    public string Status { get; set; } = null!;
    public string? ProjectStatus { get; set; }
    public decimal TotalAmount { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    /// <summary>
    /// Hạn GỐC lúc ký. So với <see cref="EndDate"/> để biết đã gia hạn bao lâu —
    /// trước đây danh sách không trả field này nên duyệt gia hạn xong không đâu
    /// thể hiện là hạn đã đổi.
    /// </summary>
    public DateOnly OriginalEndDate { get; set; }
    public int MaxExtensionMonths { get; set; }
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
    /// <summary>
    /// Trạng thái đề tài tách khỏi trạng thái hợp đồng. Nghiệm thu Đạt làm đề tài COMPLETED,
    /// còn hợp đồng chỉ SETTLED sau khi ký BM13.
    /// </summary>
    public string? ProjectStatus { get; set; }
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

/// <summary>
/// Sửa hợp đồng đã tạo. Chỉ mở các trường Staff **gõ tay lúc tạo** — số HĐ, phạm vi, thời hạn,
/// đại diện Bên A, link HĐ điện tử. KHÔNG cho sửa `ProposalId` (đổi đề tài = hợp đồng khác hẳn,
/// phải xoá tạo lại) và KHÔNG cho sửa `TotalAmount` (lấy từ dự toán đề cương, rule #15 không quản tiền).
/// </summary>
public class UpdateContractRequest
{
    public string ContractNumber { get; set; } = null!;
    public string? ScopeTitle { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public int MaxExtensionMonths { get; set; } = 6;
    public string? SideARepresentative { get; set; }
    public string? EcontractUrl { get; set; }
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

public class TerminateContractRequest
{
    public string Reason { get; set; } = null!;
}
