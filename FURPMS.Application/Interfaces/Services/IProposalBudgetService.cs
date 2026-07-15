using FURPMS.Application.DTOs.Budget;

namespace FURPMS.Application.Interfaces.Services;

public interface IProposalBudgetService
{
    Task<BudgetResponse> GetBudgetAsync(Guid proposalId);
    Task<BudgetResponse> UpdateBudgetAsync(Guid proposalId, UpdateBudgetRequest request);
    Task<IEnumerable<LaborDetailResponse>> GetLaborDetailsAsync(Guid proposalId);
    Task<LaborDetailResponse> UpdateLaborDetailAsync(Guid proposalId, int detailId, UpdateLaborDetailRequest request);
}
