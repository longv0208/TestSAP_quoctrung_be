namespace FURPMS.Domain.Entities.MasterData;

public class ResearchType
{
    public int Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public decimal MaxBudgetCap { get; set; }
    public bool RequireOrderingUnit { get; set; }
    public bool RequirePublication { get; set; }
    public bool IsActive { get; set; } = true;
}
