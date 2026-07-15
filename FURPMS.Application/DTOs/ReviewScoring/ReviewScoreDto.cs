namespace FURPMS.Application.DTOs.ReviewScoring;

public class ReviewScoreDetailDto
{
    public int Id { get; set; }
    public int CriterionId { get; set; }
    public string CriterionName { get; set; } = null!;
    public decimal MaxScore { get; set; }
    public decimal GivenScore { get; set; }
    public string? Comments { get; set; }
}

public class ReviewScoreDto
{
    public int Id { get; set; }
    public Guid CouncilId { get; set; }
    public Guid EvaluatorMemberId { get; set; }
    public string EvaluatorName { get; set; } = null!;
    public int TemplateId { get; set; }
    public decimal TotalScore { get; set; }
    public decimal MaxPossibleScore { get; set; }
    public bool IsValidBallot { get; set; }
    public string? GeneralComments { get; set; }
    public string? OtherRecommendations { get; set; }
    public DateTime? SubmittedAt { get; set; }
    public List<ReviewScoreDetailDto> ScoreDetails { get; set; } = new();
}
