namespace FURPMS.Application.DTOs.ReviewScoring;

public class ScoreDetailRequest
{
    public int CriterionId { get; set; }
    public decimal GivenScore { get; set; }
    public string? Comments { get; set; }
}

public class SubmitScoreRequest
{
    public int TemplateId { get; set; }
    public Guid? ProjectId { get; set; }         // Phase B: council chấm nhiều đề tài → chỉ rõ (1 đề tài thì tự suy)
    public string? GeneralComments { get; set; }
    public string? OtherRecommendations { get; set; }
    public List<ScoreDetailRequest> ScoreDetails { get; set; } = new();
}
