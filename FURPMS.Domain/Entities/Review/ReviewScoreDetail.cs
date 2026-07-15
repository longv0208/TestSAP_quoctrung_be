using FURPMS.Domain.Entities.Financial;

namespace FURPMS.Domain.Entities.Review;

public class ReviewScoreDetail
{
    public int Id { get; set; }
    public int ScoreId { get; set; }
    public int CriterionId { get; set; }
    public decimal GivenScore { get; set; }
    public string? Comments { get; set; }

    public ProposalReviewScore Score { get; set; } = null!;
    public RubricCriterion Criterion { get; set; } = null!;
}
