namespace FURPMS.Application.DTOs.Councils;

public class MyMembershipDto
{
    public Guid MemberId { get; set; }
    public Guid CouncilId { get; set; }
    public Guid? RoundId { get; set; }
    public string RoundType { get; set; } = null!;
    public string RoundStatus { get; set; } = null!;
    public string MemberRole { get; set; } = null!;
    public string Status { get; set; } = null!;
    public Guid ProposalId { get; set; }
    public string ProposalTitleVI { get; set; } = null!;
    public string ProposalStatus { get; set; } = null!;
}
