using FURPMS.Domain.Entities.Financial;

namespace FURPMS.Domain.Entities.Review;

public class ProposalReviewScore
{
    public int Id { get; set; }
    public Guid CouncilId { get; set; }
    public Guid ProjectId { get; set; }    // Phase B: council chấm nhiều đề tài — điểm theo (council, project, evaluator)
    public Guid EvaluatorMemberId { get; set; }
    public int TemplateId { get; set; }
    public string? GeneralComments { get; set; }
    public string? OtherRecommendations { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public bool IsValidBallot { get; set; }
    public string? AiFeedbackSuggestion { get; set; }

    public ReviewCouncil Council { get; set; } = null!;
    public Projects.Project Project { get; set; } = null!;
    public CouncilMember EvaluatorMember { get; set; } = null!;
    public RubricTemplate Template { get; set; } = null!;
    public ICollection<ReviewScoreDetail> ScoreDetails { get; set; } = new List<ReviewScoreDetail>();
}
