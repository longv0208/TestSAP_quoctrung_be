using FURPMS.Application.DTOs.MasterData;

namespace FURPMS.Application.Interfaces.Services;

public interface ISystemFinancialConfigService
{
    Task<IEnumerable<SystemFinancialConfigResponse>> GetAllAsync();
    Task<SystemFinancialConfigResponse> GetByIdAsync(int id);
    Task<SystemFinancialConfigResponse> CreateAsync(UpsertSystemFinancialConfigRequest request);
    Task<SystemFinancialConfigResponse> UpdateAsync(int id, UpsertSystemFinancialConfigRequest request);
}
