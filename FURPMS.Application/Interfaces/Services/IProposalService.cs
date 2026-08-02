using FURPMS.Application.DTOs.Proposals;

namespace FURPMS.Application.Interfaces.Services;

public interface IProposalService
{
    Task<IEnumerable<ProposalSummaryDto>> GetProposalsAsync(ProposalQueryParams queryParams, Guid requesterId, IEnumerable<string> requesterRoles, bool ownOnly = false);
    Task<ProposalDto> GetProposalByIdAsync(Guid proposalId, Guid callerId, IEnumerable<string> callerRoles);
    Task<ProposalDto> CreateProposalAsync(CreateProposalRequest request, Guid piUserId);
    Task<ProposalDto> UpdateProposalAsync(Guid proposalId, CreateProposalRequest request, Guid userId);
    Task<ProposalDto> SubmitProposalAsync(Guid proposalId, Guid userId, bool confirmCvUpToDate = false);
    Task<ProposalDto> WithdrawProposalAsync(Guid proposalId, Guid userId);
}
