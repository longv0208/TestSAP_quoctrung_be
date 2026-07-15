using FURPMS.Application.DTOs.Councils;

namespace FURPMS.Application.Interfaces.Services;

public interface ICouncilService
{
    Task<CouncilResponse> CreateCouncilAsync(CreateCouncilRequest request, Guid createdBy);
    Task<CouncilMemberResponse> AddMemberAsync(Guid councilId, AddCouncilMemberRequest request);
    Task<int> SendInvitationsAsync(Guid councilId, DateTime? confirmDeadline);
    Task<IEnumerable<MyMembershipDto>> GetMyMembershipsAsync(Guid userId);
    Task<CouncilMemberResponse> RespondToMembershipAsync(Guid memberId, Guid userId, bool accept, string? declineReason);
    Task<IEnumerable<CouncilMemberResponse>> GetMembersAsync(Guid councilId);
    Task RemoveMemberAsync(Guid memberId);
}
