using System.Security.Claims;
using FURPMS.Application.Common;
using FURPMS.Application.DTOs.Councils;
using FURPMS.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FURPMS.API.Controllers;

[ApiController]
[Authorize]
[Route("api/councils")]
public class CouncilsController : ControllerBase
{
    private readonly ICouncilService _service;

    public CouncilsController(ICouncilService service)
    {
        _service = service;
    }

    // GET /api/councils/my-memberships
    [HttpGet("my-memberships")]
    public async Task<ActionResult<ApiResponse<IEnumerable<MyMembershipDto>>>> GetMyMemberships()
    {
        var userId = GetCurrentUserId();
        var result = await _service.GetMyMembershipsAsync(userId);
        return Ok(ApiResponse<IEnumerable<MyMembershipDto>>.Ok(result));
    }

    // POST /api/councils
    [HttpPost]
    [Authorize(Roles = "Staff,Admin")]
    public async Task<ActionResult<ApiResponse<CouncilResponse>>> CreateCouncil(
        [FromBody] CreateCouncilRequest request)
    {
        var userId = GetCurrentUserId();
        var result = await _service.CreateCouncilAsync(request, userId);
        return Ok(ApiResponse<CouncilResponse>.Ok(result, "Council created."));
    }

    // GET /api/councils/{councilId}/members
    [HttpGet("{councilId:guid}/members")]
    public async Task<ActionResult<ApiResponse<IEnumerable<CouncilMemberResponse>>>> GetMembers(Guid councilId)
    {
        var result = await _service.GetMembersAsync(councilId);
        return Ok(ApiResponse<IEnumerable<CouncilMemberResponse>>.Ok(result));
    }

    // POST /api/councils/{councilId}/members
    [HttpPost("{councilId:guid}/members")]
    [Authorize(Roles = "Staff,Admin")]
    public async Task<ActionResult<ApiResponse<CouncilMemberResponse>>> AddMember(
        Guid councilId,
        [FromBody] AddCouncilMemberRequest request)
    {
        var result = await _service.AddMemberAsync(councilId, request);
        return Ok(ApiResponse<CouncilMemberResponse>.Ok(result, "Member added."));
    }

    // POST /api/councils/{councilId}/send-invitations — gửi thư mời đồng loạt
    [HttpPost("{councilId:guid}/send-invitations")]
    [Authorize(Roles = "Staff,Admin")]
    public async Task<ActionResult<ApiResponse>> SendInvitations(
        Guid councilId,
        [FromBody] SendInvitationsRequest? request)
    {
        var count = await _service.SendInvitationsAsync(councilId, request?.ConfirmDeadline);
        return Ok(ApiResponse.Ok($"Đã gửi {count} thư mời."));
    }

    // PATCH /api/council-members/{memberId}/respond
    [HttpPatch("/api/council-members/{memberId:guid}/respond")]
    public async Task<ActionResult<ApiResponse<CouncilMemberResponse>>> Respond(
        Guid memberId,
        [FromBody] RespondMembershipRequest request)
    {
        var userId = GetCurrentUserId();
        var result = await _service.RespondToMembershipAsync(memberId, userId, request.Accept, request.DeclineReason);
        return Ok(ApiResponse<CouncilMemberResponse>.Ok(result));
    }

    // DELETE /api/council-members/{memberId}
    [HttpDelete("/api/council-members/{memberId:guid}")]
    [Authorize(Roles = "Staff,Admin")]
    public async Task<ActionResult<ApiResponse>> DeleteMember(Guid memberId)
    {
        await _service.RemoveMemberAsync(memberId);
        return Ok(ApiResponse.Ok("Member removed."));
    }

    private Guid GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? throw new UnauthorizedAccessException("User ID not found in token.");
        return Guid.Parse(value);
    }
}
