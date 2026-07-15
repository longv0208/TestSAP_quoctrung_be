namespace FURPMS.Domain.Entities.Review;

public class AcceptanceEvaluation
{
    public int Id { get; set; }
    public Guid CouncilId { get; set; }
    public Guid ProjectId { get; set; }    // Phase B
    public Guid EvaluatorMemberId { get; set; }
    public string Result { get; set; } = null!;
    public string? FailReason { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public bool IsValidBallot { get; set; }

    public ReviewCouncil Council { get; set; } = null!;
    public Projects.Project Project { get; set; } = null!;
    public CouncilMember EvaluatorMember { get; set; } = null!;
}
