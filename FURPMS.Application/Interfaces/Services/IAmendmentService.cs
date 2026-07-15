using FURPMS.Application.DTOs.Contract;

namespace FURPMS.Application.Interfaces.Services;

public interface IAmendmentService
{
    Task<IEnumerable<AmendmentListResponse>> GetByContractAsync(Guid contractId);
    Task<AmendmentDetailResponse> GetByIdAsync(Guid amendmentId);
    Task<AmendmentDetailResponse> CreateAsync(Guid contractId, CreateAmendmentRequest request, Guid requestedBy);
    Task<AmendmentDetailResponse> ApproveAsync(Guid amendmentId, ReviewAmendmentRequest request, Guid reviewedBy);
    Task<AmendmentDetailResponse> RejectAsync(Guid amendmentId, ReviewAmendmentRequest request, Guid reviewedBy);
}
