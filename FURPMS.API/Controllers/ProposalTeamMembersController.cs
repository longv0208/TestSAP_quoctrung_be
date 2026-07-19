using FURPMS.Application.Common;
using FURPMS.Application.DTOs.TeamMembers;
using FURPMS.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FURPMS.API.Controllers;

[ApiController]
[Route("api/proposals/{proposalId:guid}/team-members")]
[Authorize]
public class ProposalTeamMembersController : ControllerBase
{
    private readonly ITeamMemberService _service;

    public ProposalTeamMembersController(ITeamMemberService service)
    {
        _service = service;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<IEnumerable<TeamMemberResponse>>>> GetAll(Guid proposalId)
    {
        var result = await _service.GetTeamMembersAsync(proposalId);
        return Ok(ApiResponse<IEnumerable<TeamMemberResponse>>.Ok(result));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<TeamMemberResponse>>> Add(
        Guid proposalId,
        [FromBody] CreateTeamMemberRequest request)
    {
        var result = await _service.AddTeamMemberAsync(proposalId, request);
        return Ok(ApiResponse<TeamMemberResponse>.Ok(result, "Team member added."));
    }
}
