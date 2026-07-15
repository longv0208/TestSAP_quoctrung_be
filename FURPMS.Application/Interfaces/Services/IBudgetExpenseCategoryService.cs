using FURPMS.Application.DTOs.MasterData;

namespace FURPMS.Application.Interfaces.Services;

public interface IBudgetExpenseCategoryService
{
    Task<IEnumerable<BudgetExpenseCategoryResponse>> GetAllAsync();
    Task<BudgetExpenseCategoryResponse> GetByIdAsync(int id);
    Task<BudgetExpenseCategoryResponse> CreateAsync(UpsertBudgetExpenseCategoryRequest request);
    Task<BudgetExpenseCategoryResponse> UpdateAsync(int id, UpsertBudgetExpenseCategoryRequest request);
}
