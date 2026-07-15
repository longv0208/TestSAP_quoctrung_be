namespace FURPMS.Application.DTOs.Councils;

public class CouncilResponse
{
    public Guid Id { get; set; }
    public Guid ProposalId { get; set; }
    public Guid? RoundId { get; set; }
    public string CouncilType { get; set; } = null!;
    public string? EstablishmentDecisionNo { get; set; }
    public DateOnly? EstablishedAt { get; set; }
    public DateOnly? MeetingDeadline { get; set; }
    public int MinMembersRequired { get; set; }
    public int MaxMembersAllowed { get; set; }
    public string Status { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}
