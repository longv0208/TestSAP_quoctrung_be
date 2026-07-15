namespace FURPMS.Domain.Entities.MasterData;

public class BudgetExpenseCategory
{
    public int Id { get; set; }
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public int Sequence { get; set; }
    public bool IsActive { get; set; } = true;
}
