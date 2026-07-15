namespace FURPMS.Application.DTOs.Councils;

public class CreateCouncilRequest
{
    public Guid ProposalId { get; set; }
    public Guid RoundId { get; set; }
    public string CouncilType { get; set; } = null!;
    public string? EstablishmentDecisionNo { get; set; }
    public DateOnly? EstablishedAt { get; set; }
    public DateOnly? MeetingDeadline { get; set; }
    public int MinMembersRequired { get; set; } = 3;
    public int MaxMembersAllowed { get; set; } = 5;
}
