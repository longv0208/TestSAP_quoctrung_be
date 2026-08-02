using FURPMS.Domain.Entities.Users;

namespace FURPMS.Domain.Entities.Review;

public class CouncilDecision
{
    public int Id { get; set; }
    public Guid CouncilId { get; set; }
    public Guid ProjectId { get; set; }    // Phase B: Chủ tịch chốt TỪNG đề tài trong phiên
    public int TotalMembers { get; set; }
    public int AttendingMembers { get; set; }
    public int ValidBallots { get; set; }
    public int InvalidBallots { get; set; }
    public decimal? AverageScore { get; set; }
    public string Result { get; set; } = null!;
    public string? CouncilComments { get; set; }
    public string? Recommendations { get; set; }
    public Guid? ChairUserId { get; set; }
    public Guid? SecretaryUserId { get; set; }
    public DateTime? FinalizedAt { get; set; }

    public ReviewCouncil Council { get; set; } = null!;
    public Projects.Project Project { get; set; } = null!;
    public User? ChairUser { get; set; }
    public User? SecretaryUser { get; set; }
    public ICollection<CouncilQaEntry> QaEntries { get; set; } = new List<CouncilQaEntry>();
    public ICollection<CouncilMemberOpinion> MemberOpinions { get; set; } = new List<CouncilMemberOpinion>();
}
