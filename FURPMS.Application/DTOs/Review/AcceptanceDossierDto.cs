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
/// Bổ sung 08/08 theo góp ý thầy (C4 + C5): thêm **thông tin đề tài đầy đủ**, và mỗi mục đều
/// kèm **danh sách file mở được** — trước đó chỉ có cờ `hasFile` nên người chấm biết là có file
/// mà không có đường nào mở ra xem. Riêng báo cáo tiến độ nay hiện **ai duyệt · vai gì · duyệt
/// lúc nào · nội dung đã làm/còn tồn** và **file của TỪNG kỳ**, không chỉ mỗi phần trăm.
/// </para>
/// </summary>
public class AcceptanceDossierDto
{
    /// <summary>Thông tin đề tài — hội đồng nghiệm thu cần đối chiếu với cái đã đăng ký ban đầu.</summary>
    public DossierProjectDto? Project { get; set; }

    public string? ContractNumber { get; set; }
    public string? ContractStatus { get; set; }
    public string? ContractStartDate { get; set; }
    public string? ContractEndDate { get; set; }
    public DateTime? ContractSignedAt { get; set; }
    public decimal? ContractTotalAmount { get; set; }
    /// <summary>File hợp đồng đã ký (minh chứng) — rule #21.</summary>
    public List<DossierFileDto> ContractFiles { get; set; } = new();

    public List<DossierProgressReportDto> ProgressReports { get; set; } = new();
    public List<DossierDeliverableDto> Deliverables { get; set; } = new();
    public DossierFinalReportDto? FinalReport { get; set; }

    /// <summary>Đếm nhanh để hội đồng thấy ngay bức tranh tổng thể.</summary>
    public int DeliverablesPassed { get; set; }
    public int DeliverablesTotal { get; set; }

    /// <summary>
    /// Báo cáo tiến độ do **Staff duyệt trực tiếp, không qua hội đồng** (rule #16) nên KHÔNG có
    /// điểm số — chỉ có kết quả đánh giá và % hoàn thành. Nói rõ để người chấm không đi tìm cột điểm.
    /// </summary>
    public string ProgressReportNote { get; set; } =
        "Báo cáo tiến độ giữa kỳ do Phòng QLKH duyệt trực tiếp, không lập hội đồng nên không chấm điểm.";
}

public class DossierProjectDto
{
    public Guid Id { get; set; }
    public string? ProjectCode { get; set; }
    public string TitleVi { get; set; } = null!;
    public string? TitleEn { get; set; }
    public string Status { get; set; } = null!;
    public string? PiName { get; set; }
    public string? PiEmail { get; set; }
    public string? HostingUnitName { get; set; }
    public string? ResearchTypeName { get; set; }
    public string? TrackName { get; set; }
    public string? CycleName { get; set; }
    public int DurationMonths { get; set; }
    public string PlannedStartDate { get; set; } = null!;
    public string PlannedEndDate { get; set; } = null!;
    public decimal? TotalBudget { get; set; }
    /// <summary>Mục tiêu/phương pháp/sản phẩm đã đăng ký — để đối chiếu với cái thực làm.</summary>
    public string? ResearchObjectives { get; set; }
    public string? Methodology { get; set; }
    public string? ExpectedOutput { get; set; }
    public List<DossierMemberDto> Members { get; set; } = new();
    /// <summary>File đề cương gốc đã nộp.</summary>
    public List<DossierFileDto> ProposalFiles { get; set; } = new();
}

public class DossierMemberDto
{
    public string FullName { get; set; } = null!;
    public string? AcademicTitle { get; set; }
    public string? UnitName { get; set; }
    public string? WorkContent { get; set; }
    public bool IsPi { get; set; }
}

public class DossierProgressReportDto
{
    public Guid Id { get; set; }
    public int ReportRound { get; set; }
    public string? RoundName { get; set; }
    public string ReportingPeriodStart { get; set; } = null!;
    public string ReportingPeriodEnd { get; set; } = null!;
    public decimal OverallCompletionPct { get; set; }
    public string Status { get; set; } = null!;

    public string? CompletedContent { get; set; }
    public string? PendingContent { get; set; }
    public string? NextPeriodPlan { get; set; }
    public string? PiRecommendations { get; set; }

    /// <summary>Kết quả Staff đánh giá — chỉ 3 giá trị: PASS / CONDITIONAL / FAIL (QĐ543 Điều 10, BM06).</summary>
    public string? EvaluationResult { get; set; }
    public string? EvaluationComments { get; set; }
    /// <summary>Người duyệt kỳ này — thiếu thì hội đồng không truy được trách nhiệm.</summary>
    public string? EvaluatedByName { get; set; }
    /// <summary>Vai của người duyệt (Staff / Admin) tại thời điểm xem.</summary>
    public string? EvaluatedByRole { get; set; }
    public DateTime? EvaluatedAt { get; set; }
    public DateTime? SubmittedAt { get; set; }

    /// <summary>Link báo cáo PI dán (dùng thay upload khi file quá lớn).</summary>
    public string? ReportFileUrl { get; set; }
    /// <summary>File của CHÍNH kỳ này — xem lại được tất cả các kỳ trước, không chỉ kỳ cuối.</summary>
    public List<DossierFileDto> Files { get; set; } = new();
}

public class DossierDeliverableDto
{
    public int Id { get; set; }
    public string ProductName { get; set; } = null!;
    public string? Description { get; set; }
    public string? ScientificRequirements { get; set; }
    public string? AcceptanceStatus { get; set; }
    public string? QualityAssessment { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public string? DueDate { get; set; }
    public bool HasFile { get; set; }
    /// <summary>Link sản phẩm do PI dán (kho mã nguồn, bài báo đã đăng…).</summary>
    public string? FileUrl { get; set; }
    /// <summary>Minh chứng thử nghiệm — QĐ543 Điều 13.1.</summary>
    public string? TrialEvidenceUrl { get; set; }
    public List<DossierFileDto> Files { get; set; } = new();
}

public class DossierFinalReportDto
{
    public string Status { get; set; } = null!;
    public DateTime? SubmittedAt { get; set; }
    public string? Deadline { get; set; }
    public bool HasFile { get; set; }
    public string? ReportFileUrl { get; set; }
    public string? SummaryFileUrl { get; set; }
    public List<DossierFileDto> Files { get; set; } = new();
}

/// <summary>
/// File mở được thật: <c>downloadUrl</c> lấy từ <c>Document.StorageUrl</c>, các endpoint tải về
/// đều chỉ yêu cầu đăng nhập nên thành viên hội đồng bấm là xem được.
/// </summary>
public class DossierFileDto
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = null!;
    public string? Category { get; set; }
    public long SizeBytes { get; set; }
    public DateTime UploadedAt { get; set; }
    public string DownloadUrl { get; set; } = null!;
}
