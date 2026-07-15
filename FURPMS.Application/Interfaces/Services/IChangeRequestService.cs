using FURPMS.Application.DTOs.ChangeRequests;

namespace FURPMS.Application.Interfaces.Services;

public interface IChangeRequestService
{
    Task<ChangeRequestDto> CreateAsync(Guid proposalId, CreateChangeRequestRequest request, Guid requestedBy);
    Task<IEnumerable<ChangeRequestDto>> GetByProposalAsync(Guid proposalId);
    Task<IEnumerable<ChangeRequestDto>> GetPendingAsync();
    Task<ChangeRequestDto> ReviewAsync(Guid id, ReviewChangeRequestRequest request, Guid reviewedBy);
}
