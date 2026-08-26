namespace FURPMS.Application.DTOs.Progress;

public class FinalReportDto
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string Status { get; set; } = null!;
    public string? ReportFileUrl { get; set; }
    public string? SummaryFileUrl { get; set; }
    public string Language { get; set; } = "VI";
    public string? Deadline { get; set; }
    /// <summary>Số ngày còn lại tới hạn nộp báo cáo tổng kết, âm = quá hạn. Máy chủ tính.</summary>
    public int? DaysLeft { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public string? RevisionNotes { get; set; }
    public DateTime? RevisionRequestedAt { get; set; }
    public DateTime? FinalSubmittedAt { get; set; }
    public string? ArchivalDeadline { get; set; }
    /// <summary>Số ngày còn lại tới hạn lưu trữ hồ sơ, âm = quá hạn. Máy chủ tính.</summary>
    public int? ArchivalDaysLeft { get; set; }
    public DateTime? ArchivedAt { get; set; }
}

public class SubmitFinalReportRequest
{
    public string ReportFileUrl { get; set; } = null!;
    public string? SummaryFileUrl { get; set; }
    public string Language { get; set; } = "VI";
}

public class RequestRevisionRequest
{
    public string RevisionNotes { get; set; } = null!;
}
