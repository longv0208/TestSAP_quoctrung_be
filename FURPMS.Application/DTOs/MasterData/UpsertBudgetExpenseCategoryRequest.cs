namespace FURPMS.Application.DTOs.MasterData;

public class UpsertBudgetExpenseCategoryRequest
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public int Sequence { get; set; }
    public bool IsActive { get; set; } = true;
}
