using FURPMS.Application.DTOs.Contract;

namespace FURPMS.Application.Interfaces.Services;

public interface IContractService
{
    Task<IEnumerable<ContractListResponse>> GetListAsync(Guid? piUserId = null);
    Task<ContractDetailResponse> GetByIdAsync(Guid contractId);
    Task<ContractDetailResponse> CreateAsync(CreateContractRequest request, Guid createdBy);
    Task<ContractDetailResponse> UpdateAsync(Guid contractId, UpdateContractRequest request);
    Task DeleteAsync(Guid contractId);
    Task<ContractDetailResponse> SignAsync(Guid contractId, Guid signedBy);
}
