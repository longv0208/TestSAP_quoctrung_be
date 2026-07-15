namespace FURPMS.Application.DTOs.ReviewRounds;

public class CreateReviewRoundRequest
{
    public string Dimension { get; set; } = null!;
    public string RoundType { get; set; } = null!;
    public int? RubricTemplateId { get; set; }
    public Guid? PrerequisiteRoundId { get; set; }
}
