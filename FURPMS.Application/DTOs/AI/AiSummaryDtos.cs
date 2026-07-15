namespace FURPMS.Application.DTOs.AI;

public class AiSummaryDto
{
    public string Id { get; set; } = null!;
    public string ProposalId { get; set; } = null!;
    public string SummaryText { get; set; } = null!;
    public bool IsEditedByHuman { get; set; }
    public string? EditedText { get; set; }
    public DateTime GeneratedAt { get; set; }
    public string? Source { get; set; }
    public string? SourceFileName { get; set; }
}

public class UpdateSummaryRequest
{
    public string EditedText { get; set; } = null!;
}
