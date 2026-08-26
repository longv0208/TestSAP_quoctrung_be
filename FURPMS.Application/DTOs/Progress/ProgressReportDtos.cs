namespace FURPMS.Application.DTOs.Progress;

public class ProgressReportItemDto
{
    public int Id { get; set; }
    public int ActivityId { get; set; }
    public string ActivityName { get; set; } = null!;
    public decimal CompletionRate { get; set; }
    public string CompletionStatus { get; set; } = null!;
    public string? EvidenceDescription { get; set; }
    public string? Notes { get; set; }
}

public class ProgressReportSummaryDto
{
    public Guid Id { get; set; }
    public Guid ContractId { get; set; }
    public int ReportRound { get; set; }
    public string? RoundName { get; set; }   // tên đợt Staff đặt; null → FE hiện "Kỳ {số}"
    /// <summary>Link báo cáo PI dán thay cho upload (file quá lớn).</summary>
    public string? ReportFileUrl { get; set; }
    public string ReportingPeriodStart { get; set; } = null!;
    public string ReportingPeriodEnd { get; set; } = null!;
    public decimal OverallCompletionPct { get; set; }
    public decimal ExpenditureToDate { get; set; }
    public string Status { get; set; } = null!;
    public DateTime? SubmittedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    // Staff lên lịch: hạn nộp + lịch họp + link Meet/Teams.
    public string? DueDate { get; set; }
    /// <summary>Số ngày còn lại tới hạn nộp, âm = quá hạn. Máy chủ tính.</summary>
    public int? DaysLeft { get; set; }
    public DateTime? ScheduledMeetingAt { get; set; }
    public string? MeetingLink { get; set; }
}

public class ProgressReportDto : ProgressReportSummaryDto
{
    public string CompletedContent { get; set; } = null!;
    public string? PendingContent { get; set; }
    public string? NextPeriodPlan { get; set; }
    public string? PiRecommendations { get; set; }
    public string? EvaluationResult { get; set; }
    public string? EvaluationComments { get; set; }
    public DateTime? EvaluatedAt { get; set; }
    public List<ProgressReportItemDto> Items { get; set; } = new();
}

public class CreateProgressReportItemRequest
{
    public int ActivityId { get; set; }
    public decimal CompletionRate { get; set; }
    public string CompletionStatus { get; set; } = null!;
    public string? EvidenceDescription { get; set; }
    public string? Notes { get; set; }
}

public class CreateProgressReportRequest
{
    public string ReportingPeriodStart { get; set; } = null!;
    public string ReportingPeriodEnd { get; set; } = null!;
    public string CompletedContent { get; set; } = null!;
    public string? PendingContent { get; set; }
    public decimal OverallCompletionPct { get; set; }
    public decimal ExpenditureToDate { get; set; }
    public string? NextPeriodPlan { get; set; }
    public string? PiRecommendations { get; set; }
    public List<CreateProgressReportItemRequest> Items { get; set; } = new();
}

/// <summary>Sửa nội dung báo cáo khi còn nháp. Không đụng danh sách hoạt động (Items) và kỳ báo cáo.</summary>
public class UpdateProgressReportRequest
{
    public string CompletedContent { get; set; } = null!;
    public string? PendingContent { get; set; }
    public decimal OverallCompletionPct { get; set; }
    public decimal ExpenditureToDate { get; set; }
    public string? NextPeriodPlan { get; set; }
    public string? PiRecommendations { get; set; }
    /// <summary>Link báo cáo — dùng THAY upload khi file quá lớn. Bỏ trống thì giữ giá trị cũ.</summary>
    public string? ReportFileUrl { get; set; }
    /// <summary>Bảng tiến độ theo từng hoạt động (BM06). Gửi lên = thay toàn bộ bảng cũ.</summary>
    public List<CreateProgressReportItemRequest>? Items { get; set; }
}

public class EvaluateProgressReportRequest
{
    public string EvaluationResult { get; set; } = null!;   // SATISFACTORY / UNSATISFACTORY / NEEDS_IMPROVEMENT
    public string? EvaluationComments { get; set; }
}

// Staff lên lịch báo cáo theo từng đề tài: hạn nộp + lịch họp + link trực tuyến.
public class ScheduleProgressReportRequest
{
    public string? DueDate { get; set; }            // yyyy-MM-dd — đặt/GIA HẠN hạn nộp
    public string? ScheduledMeetingAt { get; set; } // ISO datetime
    public string? MeetingLink { get; set; }
    public string? RoundName { get; set; }          // Staff đặt/sửa tên đợt (vd "Giữa kỳ")
}
