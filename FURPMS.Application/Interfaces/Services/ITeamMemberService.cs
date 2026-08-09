using FURPMS.Application.DTOs.TeamMembers;

namespace FURPMS.Application.Interfaces.Services;

public interface ITeamMemberService
{
    Task<IEnumerable<TeamMemberResponse>> GetTeamMembersAsync(Guid proposalId);
    Task<TeamMemberResponse> AddTeamMemberAsync(Guid proposalId, CreateTeamMemberRequest request);
    Task<TeamMemberResponse> UpdateTeamMemberAsync(Guid proposalId, int memberId, CreateTeamMemberRequest request, Guid actingUserId);
    Task DeleteTeamMemberAsync(Guid proposalId, int memberId, Guid actingUserId);
}
