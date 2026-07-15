using FURPMS.Domain.Entities.Proposals;

namespace FURPMS.Domain.Entities.Progress;

public class ProgressReportItem
{
    public int Id { get; set; }
    public Guid ReportId { get; set; }
    public int ActivityId { get; set; }
    public decimal CompletionRate { get; set; }
    public string CompletionStatus { get; set; } = null!;
    public string? EvidenceDescription { get; set; }
    public string? Notes { get; set; }

    public ProgressReport Report { get; set; } = null!;
    public ProposalActivity Activity { get; set; } = null!;
}
