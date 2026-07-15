using FURPMS.Domain.Entities.MasterData;

namespace FURPMS.Domain.Entities.Proposals;

public class ProposalBudgetItem
{
    public int Id { get; set; }
    public Guid ProposalId { get; set; }
    public int CategoryId { get; set; }
    public decimal Amount { get; set; }
    public string? Note { get; set; }
    public decimal SourceKhoan { get; set; }
    public decimal SourceNgoaiKhoan { get; set; }
    public decimal SourceNsnn { get; set; }
    public decimal SourceOther { get; set; }
    public int Sequence { get; set; }

    public Proposal Proposal { get; set; } = null!;
    public BudgetExpenseCategory Category { get; set; } = null!;
}
