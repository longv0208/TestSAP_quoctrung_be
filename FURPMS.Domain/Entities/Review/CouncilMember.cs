using FURPMS.Domain.Entities.Users;

namespace FURPMS.Domain.Entities.Review;

public class CouncilMember
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid CouncilId { get; set; }
    public Guid UserId { get; set; }
    public string MemberRole { get; set; } = null!;
    public bool IsExternal { get; set; }
    public DateTime? InvitationSentAt { get; set; }
    public string? InvitationToken { get; set; }
    public DateTime? TokenExpiresAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? DeclinedAt { get; set; }
    public string? DeclineReason { get; set; }
    public string Status { get; set; } = "INVITED";

    public ReviewCouncil Council { get; set; } = null!;
    public User User { get; set; } = null!;
}
