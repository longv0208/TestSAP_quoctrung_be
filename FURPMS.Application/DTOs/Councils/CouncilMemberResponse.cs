namespace FURPMS.Application.DTOs.Councils;

public class CouncilMemberResponse
{
    public Guid Id { get; set; }
    public Guid CouncilId { get; set; }
    public Guid UserId { get; set; }
    public string ReviewerName { get; set; } = null!;
    public string ReviewerEmail { get; set; } = null!;
    public string MemberRole { get; set; } = null!;
    public bool IsExternal { get; set; }
    public string Status { get; set; } = null!;
    public DateTime? InvitationSentAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? DeclinedAt { get; set; }
}
