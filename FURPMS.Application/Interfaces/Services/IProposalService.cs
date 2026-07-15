using FURPMS.Application.DTOs.Proposals;

namespace FURPMS.Application.Interfaces.Services;

public interface IProposalService
{
    Task<IEnumerable<ProposalSummaryDto>> GetProposalsAsync(ProposalQueryParams queryParams, Guid requesterId, IEnumerable<string> requesterRoles);
    Task<ProposalDto> GetProposalByIdAsync(Guid proposalId);
    Task<ProposalDto> CreateProposalAsync(CreateProposalRequest request, Guid piUserId);
    Task<ProposalDto> UpdateProposalAsync(Guid proposalId, CreateProposalRequest request, Guid userId);
    Task<ProposalDto> SubmitProposalAsync(Guid proposalId, Guid userId, bool confirmCvUpToDate = false);
    Task<ProposalDto> WithdrawProposalAsync(Guid proposalId, Guid userId);
}
