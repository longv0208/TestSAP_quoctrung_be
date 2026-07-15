namespace FURPMS.Application.DTOs.ReviewRounds;

public class AddRoundMemberRequest
{
    public Guid ReviewerId { get; set; }
    public string MemberRole { get; set; } = "Member"; // Member | Chair | Opponent
    public bool IsExternal { get; set; } = false;
}
