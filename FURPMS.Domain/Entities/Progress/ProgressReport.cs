using FURPMS.Domain.Entities.Contracts;
using FURPMS.Domain.Entities.Users;

namespace FURPMS.Domain.Entities.Progress;

public class ProgressReport
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ContractId { get; set; }
    public int ReportRound { get; set; }
    // Tên đợt do Staff đặt (vd "Giữa kỳ", "Đợt 1 - Quý I"). Null → FE hiện "Kỳ {số}".
    // Thầy 29/07: không fix cứng số đợt/tên đợt, Staff chỉnh linh hoạt.
    public string? RoundName { get; set; }

    /// <summary>
    /// Link báo cáo do PI dán, dùng THAY cho upload khi file quá lớn (giống sản phẩm).
    /// Staff cần xem được bản báo cáo mới đánh giá — file hoặc link đều được, miễn có một đường.
    /// </summary>
    public string? ReportFileUrl { get; set; }
    public DateOnly ReportingPeriodStart { get; set; }
    public DateOnly ReportingPeriodEnd { get; set; }
    public string CompletedContent { get; set; } = null!;
    public string? PendingContent { get; set; }
    public decimal OverallCompletionPct { get; set; }
    public decimal ExpenditureToDate { get; set; }
    public string? NextPeriodPlan { get; set; }
    public string? PiRecommendations { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public string Status { get; set; } = "DRAFT";
    // Staff lên lịch báo cáo theo từng đề tài: hạn nộp + lịch họp + link Meet/Teams.
    public DateOnly? DueDate { get; set; }
    public DateTime? ScheduledMeetingAt { get; set; }
    public string? MeetingLink { get; set; }
    public Guid? EvaluatedBy { get; set; }
    public DateTime? EvaluatedAt { get; set; }
    public string? EvaluationResult { get; set; }
    public string? EvaluationComments { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Contract Contract { get; set; } = null!;
    public User? EvaluatedByUser { get; set; }
    public ICollection<ProgressReportItem> Items { get; set; } = new List<ProgressReportItem>();
}
