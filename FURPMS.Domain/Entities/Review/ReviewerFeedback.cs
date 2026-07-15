namespace FURPMS.Domain.Entities.Review;

public class ReviewerFeedback
{
    public int Id { get; set; }
    public Guid CouncilId { get; set; }
    public Guid ProjectId { get; set; }    // Phase B
    public Guid ReviewerMemberId { get; set; }
    public int? UrgencyScore { get; set; }
    public int? ScientificContributionScore { get; set; }
    public int? PracticalSignificanceScore { get; set; }
    public int? ActualVsExpectedScore { get; set; }
    public string? OtherComments { get; set; }
    public string? OverallAssessment { get; set; }
    public DateTime? SubmittedAt { get; set; }

    public ReviewCouncil Council { get; set; } = null!;
    public Projects.Project Project { get; set; } = null!;
    public CouncilMember ReviewerMember { get; set; } = null!;
}
