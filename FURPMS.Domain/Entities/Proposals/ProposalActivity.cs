namespace FURPMS.Domain.Entities.Proposals;

public class ProposalActivity
{
    public int Id { get; set; }
    public int ContentId { get; set; }
    public Guid ProposalId { get; set; }
    public string ActivityName { get; set; } = null!;
    public string ExpectedResult { get; set; } = null!;
    public int StartMonth { get; set; }
    public int EndMonth { get; set; }
    public string? ResponsiblePerson { get; set; }
    public decimal EstimatedCost { get; set; }
    public int Sequence { get; set; }
    public string ActivityType { get; set; } = "NORMAL";
    public bool RequiresApproval { get; set; }

    public ProposalResearchContent Content { get; set; } = null!;
    public Proposal Proposal { get; set; } = null!;
}
