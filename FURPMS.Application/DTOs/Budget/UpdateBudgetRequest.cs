namespace FURPMS.Application.DTOs.Budget;

public class UpdateBudgetRequest
{
    public decimal TotalAmount { get; set; }
    public List<BudgetItemDto> Items { get; set; } = new();
}
