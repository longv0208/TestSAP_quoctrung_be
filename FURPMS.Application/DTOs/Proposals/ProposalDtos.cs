namespace FURPMS.Application.DTOs.Proposals;

public class ProposalMemberDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = null!;
    public string? Email { get; set; }
    public string? Department { get; set; }
    public string Role { get; set; } = null!;
    public decimal WorkMonths { get; set; }
    public string? AcademicTitle { get; set; }
    public string? MemberRoleCode { get; set; }
    public bool IsSecretary { get; set; }
}

public class ProposalBudgetItemDto
{
    public int Id { get; set; }

    /// <summary>Tên hạng mục để hiển thị.</summary>
    public string Category { get; set; } = null!;

    /// <summary>
    /// Mã hạng mục — FE nạp lại form thì đối chiếu bằng mã, không bằng tên: tên đổi theo quy định
    /// (bộ 12 hạng mục cũ đã đổi sang 06 hạng mục Điều 15) mà mã thì giữ nguyên.
    /// </summary>
    public string? CategoryCode { get; set; }

    public decimal Amount { get; set; }
    public string? Note { get; set; }
}

public class ProposalDocumentDto
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = null!;
    public string DocumentType { get; set; } = null!;
    public long FileSizeBytes { get; set; }
    public DateTime UploadedAt { get; set; }
    public string? DownloadUrl { get; set; }
    // enriched fields for global list
    public Guid? ProposalId { get; set; }
    public string? ProposalTitle { get; set; }
    public string? PrincipalInvestigatorName { get; set; }
}

public class ProposalSummaryDto
{
    public Guid Id { get; set; }
    public string TitleVI { get; set; } = null!;
    public string? TitleEN { get; set; }
    public string ResearchType { get; set; } = null!;  // "Applied" | "Basic"
    public string Status { get; set; } = null!;
    // Tên đợt + lĩnh vực để FE danh sách hiển thị thẳng (cột Cycle / Research Field),
    // khỏi phải tự resolve từ ID.
    public string? CycleName { get; set; }
    public string TrackName { get; set; } = null!;
    public string PrincipalInvestigatorName { get; set; } = null!;
    public decimal TotalBudget { get; set; }
    /// <summary>
    /// Thời gian thực hiện. Cần ngay ở DANH SÁCH vì màn tạo hợp đồng phải suy ra mức gia hạn
    /// tối đa (QĐ543 Điều 10.4: tối đa **1/2** tổng thời gian thực hiện) khi Staff chọn đề tài.
    /// </summary>
    public int DurationMonths { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? SubmittedAt { get; set; }
}

public class ProposalDto : ProposalSummaryDto
{
    // Project-centric (Review 2): proposal là tài liệu có version thuộc project.
    public Guid ProjectId { get; set; }
    public string? ProjectStatus { get; set; }
    public int VersionNo { get; set; } = 1;
    public string CycleId { get; set; } = null!;
    public string TrackId { get; set; } = null!;
    public int ResearchTypeId { get; set; }
    public int DurationMonths { get; set; }
    public string Objectives { get; set; } = null!;
    public string? Methodology { get; set; }
    public string? ExpectedOutput { get; set; }
    public string? RejectionReason { get; set; }
    // Mẫu 1 — các mục bổ sung
    public string? AbstractEN { get; set; }
    public string? Urgency { get; set; }              // tổng quan / tính cấp thiết
    public string? Novelty { get; set; }              // tính mới, sáng tạo
    public string? ApplicationPotential { get; set; } // khả năng ứng dụng
    public string? TransferPotential { get; set; }    // khả năng chuyển giao
    public string? Facilities { get; set; }           // cơ sở vật chất
    public string? FundingMethod { get; set; }        // PARTIAL | WHOLE
    public List<ProposalMemberDto> Members { get; set; } = new();
    public List<ProposalBudgetItemDto> BudgetItems { get; set; } = new();
    public List<ProposalDocumentDto> Documents { get; set; } = new();
}

public class CreateMemberRequest
{
    public string FullName { get; set; } = null!;
    public string? Email { get; set; }
    public string? Department { get; set; }
    public string Role { get; set; } = null!;
    public decimal WorkMonths { get; set; }
    public string? AcademicTitle { get; set; }
    public string? MemberRoleCode { get; set; }
    public bool IsSecretary { get; set; }
}

public class CreateBudgetItemRequest
{
    public string Category { get; set; } = null!;
    public decimal Amount { get; set; }
    public string? Note { get; set; }
}

public class CreateProposalRequest
{
    public int? CycleId { get; set; }   // đợt PI chọn; null → fallback đợt OPEN mới nhất
    public int? OrderId { get; set; }   // Applied: đề tài đặt hàng PI đăng ký (nhiều PI cùng OrderId = cạnh tranh)
    public string TrackId { get; set; } = null!;
    public string TitleVI { get; set; } = null!;
    public string? TitleEN { get; set; }
    public int ResearchType { get; set; }  // 1 = Applied, 2 = Basic (maps to ResearchType.Id)
    public int DurationMonths { get; set; }
    public string Objectives { get; set; } = null!;
    public string? Methodology { get; set; }
    public string? ExpectedOutput { get; set; }
    // Mẫu 1 — các mục bổ sung
    public string? AbstractEN { get; set; }
    public string? Urgency { get; set; }
    public string? Novelty { get; set; }
    public string? ApplicationPotential { get; set; }
    public string? TransferPotential { get; set; }
    public string? Facilities { get; set; }
    public string? FundingMethod { get; set; }
    public List<CreateMemberRequest> Members { get; set; } = new();
    public List<CreateBudgetItemRequest> BudgetItems { get; set; } = new();

    /// <summary>
    /// Tổng dự toán kinh phí khi chủ nhiệm chưa tách theo hạng mục (wizard nộp đề cương).
    /// Chỉ dùng nếu <see cref="BudgetItems"/> rỗng — có hạng mục thì tổng luôn lấy từ tổng hạng mục
    /// để hai con số không bao giờ đá nhau. Vẫn bị soi trần QĐ543 Điều 14 như mọi đường ghi khác.
    /// </summary>
    public decimal? TotalBudget { get; set; }
}

public class ProposalQueryParams
{
    public string? CycleId { get; set; }
    public string? TrackId { get; set; }
    public string? Status { get; set; }
    public string? Type { get; set; }
    public string? Search { get; set; }
}
