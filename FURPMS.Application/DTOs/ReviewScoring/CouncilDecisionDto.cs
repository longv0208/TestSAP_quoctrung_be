namespace FURPMS.Application.DTOs.ReviewScoring;

public class CouncilDecisionDto
{
    public int Id { get; set; }
    public Guid CouncilId { get; set; }
    public int TotalMembers { get; set; }
    public int AttendingMembers { get; set; }
    public int ValidBallots { get; set; }
    public int InvalidBallots { get; set; }
    public decimal? AverageScore { get; set; }
    public string Result { get; set; } = null!;
    public string? CouncilComments { get; set; }
    public string? Recommendations { get; set; }
    public DateTime? FinalizedAt { get; set; }
}
