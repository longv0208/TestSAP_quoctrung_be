using FURPMS.Application.DTOs.Settlements;

namespace FURPMS.Application.Interfaces.Services;

public interface IContractSettlementService
{
    Task<SettlementDto?> GetByContractAsync(Guid contractId);
    Task<SettlementDto> CreateAsync(Guid contractId, CreateSettlementRequest request);
    Task<SettlementDto> SignAsync(int settlementId, SignSettlementRequest request);
    Task<SettlementDto> MarkAccountingClearedAsync(int settlementId, DateOnly clearedDate);
    Task<SettlementDto> MarkAssetsClearedAsync(int settlementId, DateOnly clearedDate);
}
