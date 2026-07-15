using FURPMS.Application.DTOs.Councils;

namespace FURPMS.Application.DTOs.ReviewRounds;

public class ReviewRoundResponse
{
    public Guid Id { get; set; }
    public int RoundNumber { get; set; }
    public string Dimension { get; set; } = null!;
    public string RoundType { get; set; } = null!;
    public int? RubricTemplateId { get; set; }
    public int Sequence { get; set; }
    public Guid? PrerequisiteRoundId { get; set; }
    public string Status { get; set; } = null!;
    public DateTime? OpenedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public string? Result { get; set; }
    public Guid? CouncilId { get; set; }
    public IList<CouncilMemberResponse> Members { get; set; } = new List<CouncilMemberResponse>();
}
