using FURPMS.Application.DTOs.Contract;

namespace FURPMS.Application.Interfaces.Services;

public interface IDisbursementService
{
    Task<IEnumerable<DisbursementResponse>> GetByContractAsync(Guid contractId);
    Task<IEnumerable<DisbursementResponse>> GenerateAsync(Guid contractId);
    Task<DisbursementResponse> ConfirmAsync(int disbursementId, ConfirmDisbursementRequest request, Guid processedBy);
}
