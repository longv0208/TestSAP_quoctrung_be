namespace FURPMS.Domain.Entities.Proposals;

public class ProposalBudget
{
    public int Id { get; set; }
    public Guid ProposalId { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal LaborAmount { get; set; }
    public decimal EquipmentAmount { get; set; }
    public decimal ExternalServiceAmount { get; set; }
    public decimal ConferenceAmount { get; set; }
    public decimal OfficeSuppliesAmount { get; set; }
    public decimal IncidentalIpAmount { get; set; }

    public Proposal Proposal { get; set; } = null!;
    public ICollection<ProposalBudgetLaborDetail> LaborDetails { get; set; } = new List<ProposalBudgetLaborDetail>();
}
