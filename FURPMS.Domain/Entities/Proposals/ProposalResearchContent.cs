namespace FURPMS.Domain.Entities.Proposals;

public class ProposalResearchContent
{
    public int Id { get; set; }
    public Guid ProposalId { get; set; }
    public int ContentNumber { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public int Sequence { get; set; }

    public Proposal Proposal { get; set; } = null!;
    public ICollection<ProposalActivity> Activities { get; set; } = new List<ProposalActivity>();
}
