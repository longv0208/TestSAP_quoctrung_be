namespace FURPMS.Application.DTOs.ReviewScoring;

public class AcceptanceEvaluationDto
{
    public int Id { get; set; }
    public Guid CouncilId { get; set; }
    public Guid ProjectId { get; set; }
    public Guid EvaluatorMemberId { get; set; }
    public string? EvaluatorName { get; set; }
    public string Result { get; set; } = null!;
    public string? FailReason { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public bool IsValidBallot { get; set; }
}

public class SubmitAcceptanceRequest
{
    public Guid ProjectId { get; set; }
    public string Result { get; set; } = null!; // PASS | FAIL
    public string? FailReason { get; set; }
}
