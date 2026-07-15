using FURPMS.Domain.Entities.Projects;

namespace FURPMS.Domain.Entities.Progress;

// Sau Review 2: nghiệm thu cuối là của ĐỀ TÀI (project), không phải từng hợp đồng.
public class FinalReport
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public string? ReportFileUrl { get; set; }
    public string? SummaryFileUrl { get; set; }
    public string Language { get; set; } = "VI";
    public DateTime? SubmittedAt { get; set; }
    public DateOnly? Deadline { get; set; }
    public DateTime? RevisionRequestedAt { get; set; }
    public string? RevisionNotes { get; set; }
    public DateTime? FinalSubmittedAt { get; set; }
    public DateOnly? ArchivalDeadline { get; set; }
    public DateTime? ArchivedAt { get; set; }
    public string Status { get; set; } = "DRAFT";

    public Project Project { get; set; } = null!;
}
