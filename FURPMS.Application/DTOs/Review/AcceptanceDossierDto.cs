namespace FURPMS.Application.DTOs.Review;

/// <summary>
/// Hồ sơ để hội đồng NGHIỆM THU chấm.
/// <para>
/// Vòng xét duyệt chỉ cần đọc đề cương; nghiệm thu thì phải nhìn được **đề tài đã làm ra
/// những gì** — `Process_Spec_v2` §Giai đoạn 8: `ProgressReport[]` · `ProjectDeliverable[]`
/// · `FinalReport`. Trước đây màn chấm nghiệm thu hiện y hệt vòng 1 (chỉ đề cương + file)
/// nên người chấm không có căn cứ nào ngoài buổi họp.
/// </para>
/// <para>
/// Gom vào MỘT endpoint có gác quyền theo hội đồng, thay vì mở các endpoint hợp đồng
/// (vốn chỉ dành cho PI/Staff) cho reviewer.
/// </para>
/// </summary>
public class AcceptanceDossierDto
{
    public string? ContractNumber { get; set; }
    public string? ContractStatus { get; set; }

    public List<DossierProgressReportDto> ProgressReports { get; set; } = new();
    public List<DossierDeliverableDto> Deliverables { get; set; } = new();
    public DossierFinalReportDto? FinalReport { get; set; }

    /// <summary>Đếm nhanh để hội đồng thấy ngay bức tranh tổng thể.</summary>
    public int DeliverablesPassed { get; set; }
    public int DeliverablesTotal { get; set; }
}

public class DossierProgressReportDto
{
    public int ReportRound { get; set; }
    public string? RoundName { get; set; }
    public string ReportingPeriodStart { get; set; } = null!;
    public string ReportingPeriodEnd { get; set; } = null!;
    public decimal OverallCompletionPct { get; set; }
    public string Status { get; set; } = null!;
    /// <summary>Kết quả Staff đánh giá: PASS / CONDITIONAL / FAIL.</summary>
    public string? EvaluationResult { get; set; }
    public string? EvaluationComments { get; set; }
    public DateTime? SubmittedAt { get; set; }
}

public class DossierDeliverableDto
{
    public int Id { get; set; }
    public string ProductName { get; set; } = null!;
    public string? Description { get; set; }
    public string? AcceptanceStatus { get; set; }
    public string? QualityAssessment { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public string? DueDate { get; set; }
    public bool HasFile { get; set; }
}

public class DossierFinalReportDto
{
    public string Status { get; set; } = null!;
    public DateTime? SubmittedAt { get; set; }
    public string? Deadline { get; set; }
    public bool HasFile { get; set; }
}
