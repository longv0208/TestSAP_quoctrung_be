using FURPMS.Domain.Entities.Users;

namespace FURPMS.Domain.Entities.Review;

// Phase B (Review 2 điểm c): council chỉ thuộc ROUND; đề tài được gán qua
// CouncilProjectAssignment (1 council chấm nhiều project, nhiều council song song/round).
public class ReviewCouncil
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string CouncilType { get; set; } = null!;
    public string? EstablishmentDecisionNo { get; set; }
    public DateOnly? EstablishedAt { get; set; }
    public DateOnly? MeetingDeadline { get; set; }
    public int MinMembersRequired { get; set; } = 3;
    public int MaxMembersAllowed { get; set; } = 5;
    public int QuorumNumerator { get; set; } = 2;
    public int QuorumDenominator { get; set; } = 3;
    public string Status { get; set; } = "FORMING";
    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public Guid? RoundId { get; set; }

    public User CreatedByUser { get; set; } = null!;
    public ReviewRound? Round { get; set; }
    public ICollection<CouncilProjectAssignment> ProjectAssignments { get; set; } = new List<CouncilProjectAssignment>();
    public ICollection<CouncilMember> Members { get; set; } = new List<CouncilMember>();
    public ICollection<CouncilMeeting> Meetings { get; set; } = new List<CouncilMeeting>();
    public ICollection<CouncilDecision> Decisions { get; set; } = new List<CouncilDecision>();
}
