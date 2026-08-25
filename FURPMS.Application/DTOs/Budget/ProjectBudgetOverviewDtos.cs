namespace FURPMS.Application.DTOs.Budget;

/// <summary>
/// Bức tranh kinh phí của MỘT đề tài, gom từ 4 nguồn đang nằm rời rạc: dự toán đề cương (Điều 15),
/// trần theo loại đề tài (Điều 14), hợp đồng đã ký, và lịch giải ngân.
///
/// <para><b>Vì sao cần (25/08):</b> hội đồng bảo vệ lần 2 yêu cầu *"thể hiện rõ ngân sách tương ứng
/// cho các đề tài"*. Dữ liệu vốn đã có đủ trong DB nhưng chưa màn nào gom lại — muốn biết một đề
/// tài được cấp bao nhiêu, đã chi bao nhiêu, còn lại bao nhiêu thì phải tự mở 3 tab rồi cộng tay.</para>
///
/// <para>Đúng rule #15 (sửa 25/08): đây là <b>hồ sơ kinh phí</b> — hệ thống bày ra kế hoạch và mốc,
/// không thực hiện chi trả và không tự tính thay kế toán.</para>
/// </summary>
public class ProjectBudgetOverviewResponse
{
    public Guid ProjectId { get; set; }
    public string? ProjectCode { get; set; }
    public string? TitleVi { get; set; }
    public string? ResearchTypeName { get; set; }
    public string? FundingMethod { get; set; }

    /// <summary>Trần kinh phí của loại đề tài (QĐ543 Điều 14). Null = loại này không đặt trần.</summary>
    public decimal? FundingCap { get; set; }

    /// <summary>Tổng dự toán của bản đề cương HIỆN HÀNH.</summary>
    public decimal ApprovedTotal { get; set; }

    /// <summary>Cơ cấu theo 06 hạng mục Điều 15 — để vẽ biểu đồ, không phải để tính lại tổng.</summary>
    public List<BudgetHeadingDto> ApprovedByHeading { get; set; } = new();

    /// <summary>Chi tiết mục chi + 4 nguồn kinh phí (khoán / ngoài khoán / NSNN / khác).</summary>
    public List<BudgetItemBreakdownDto> ApprovedItems { get; set; } = new();

    /// <summary>Tổng giá trị các hợp đồng của đề tài (thường chỉ 1).</summary>
    public decimal ContractedTotal { get; set; }
    public List<ContractBriefDto> Contracts { get; set; } = new();

    /// <summary>Tổng kế hoạch giải ngân theo các đợt đã sinh.</summary>
    public decimal PlannedTotal { get; set; }

    /// <summary>
    /// Phần đã ĐÁNH DẤU giải ngân. <c>ActualAmount</c> để trống nghĩa là Phòng Tài chính chưa báo
    /// lại con số — khi đó lấy theo kế hoạch, và cờ <see cref="HasUnreportedActuals"/> bật lên để
    /// giao diện nói rõ đây là số tạm tính, tránh việc hệ thống "tự quyết" thay kế toán.
    /// </summary>
    public decimal MarkedDisbursedTotal { get; set; }
    public bool HasUnreportedActuals { get; set; }

    /// <summary>Ký rồi mà chưa chi = <c>ContractedTotal − MarkedDisbursedTotal</c>.</summary>
    public decimal RemainingTotal { get; set; }

    public List<DisbursementBriefDto> Tranches { get; set; } = new();

    /// <summary>Đợt kế tiếp đang chờ — để trả lời ngay câu "sắp tới chi cái gì, vướng gì".</summary>
    public DisbursementBriefDto? NextTranche { get; set; }

    public SettlementBriefDto? Settlement { get; set; }

    /// <summary>Dự toán vượt trần (chỉ xảy ra khi trần bị siết SAU lúc duyệt) — bày cờ đỏ.</summary>
    public bool CapExceeded { get; set; }
}

public class BudgetHeadingDto
{
    /// <summary>Mã hạng mục cố định: LABOR · EQUIPMENT · EXTERNAL_SERVICE · CONFERENCE · OFFICE · INCIDENTAL_IP.</summary>
    public string Code { get; set; } = null!;
    public decimal Amount { get; set; }
    /// <summary>Tỷ trọng trên tổng dự toán, đã làm tròn 1 chữ số. Tổng = 0 thì trả 0, không chia 0.</summary>
    public decimal Percentage { get; set; }
}

public class BudgetItemBreakdownDto
{
    public string CategoryName { get; set; } = null!;
    public decimal Amount { get; set; }
    public decimal SourceKhoan { get; set; }
    public decimal SourceNgoaiKhoan { get; set; }
    public decimal SourceNsnn { get; set; }
    public decimal SourceOther { get; set; }
    public string? Note { get; set; }
}

public class ContractBriefDto
{
    public Guid Id { get; set; }
    public string? ContractNumber { get; set; }
    public decimal TotalAmount { get; set; }
    public string Status { get; set; } = null!;
    public DateTime? SignedAt { get; set; }
}

public class DisbursementBriefDto
{
    public int Id { get; set; }
    public int RoundNumber { get; set; }
    public decimal Percentage { get; set; }
    public decimal PlannedAmount { get; set; }
    public decimal? ActualAmount { get; set; }
    public string Status { get; set; } = null!;
    public string? ConditionDescription { get; set; }
    public DateTime? DisbursedAt { get; set; }
    /// <summary>Có gắn sản phẩm mà sản phẩm chưa nghiệm thu Đạt ⇒ chưa mở khoá được đợt này.</summary>
    public bool IsBlockedByDeliverable { get; set; }
}

public class SettlementBriefDto
{
    public decimal TotalContractedAmount { get; set; }
    public decimal TotalDisbursedAmount { get; set; }
    public decimal TotalReturnedAmount { get; set; }
    public string? SettlementDeadline { get; set; }
    public DateTime? SettlementSignedAt { get; set; }
}
