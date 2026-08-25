using FURPMS.Application.DTOs.Budget;

namespace FURPMS.Application.Interfaces.Services;

/// <summary>
/// Gom bức tranh kinh phí của một đề tài. Xem <see cref="ProjectBudgetOverviewResponse"/> để biết
/// vì sao cần.
/// </summary>
public interface IProjectBudgetOverviewService
{
    /// <param name="userId">Người gọi — dùng để kiểm quyền xem.</param>
    /// <param name="roles">Vai hệ thống của người gọi.</param>
    Task<ProjectBudgetOverviewResponse> GetAsync(Guid projectId, Guid userId, IEnumerable<string> roles);
}
