using FURPMS.Domain.Entities.MasterData;

namespace FURPMS.Domain.Entities.Financial;

public class BudgetAllocationRule
{
    public int Id { get; set; }
    public int ResearchTypeId { get; set; }
    public string CategoryCode { get; set; } = null!;
    public string CategoryName { get; set; } = null!;
    public decimal MaxPercentage { get; set; }
    public bool IsActive { get; set; } = true;

    public ResearchType ResearchType { get; set; } = null!;
}
