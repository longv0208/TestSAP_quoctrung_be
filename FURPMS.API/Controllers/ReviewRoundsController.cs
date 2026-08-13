using System.Security.Claims;
using FURPMS.Application.Common;
using FURPMS.Application.DTOs.Councils;
using FURPMS.Application.DTOs.ReviewRounds;
using FURPMS.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FURPMS.API.Controllers;

[ApiController]
[Authorize]
public class ReviewRoundsController : ControllerBase
{
    private readonly IReviewRoundService _service;

    public ReviewRoundsController(IReviewRoundService service)
    {
        _service = service;
    }

    // GET /api/proposals/{proposalId}/rounds
    [HttpGet("api/proposals/{proposalId:guid}/rounds")]
    public async Task<ActionResult<ApiResponse<IEnumerable<ReviewRoundResponse>>>> GetRounds(Guid proposalId)
    {
        var result = await _service.GetProposalRoundsAsync(proposalId);
        return Ok(ApiResponse<IEnumerable<ReviewRoundResponse>>.Ok(result));
    }

    // POST /api/proposals/{proposalId}/rounds
    [HttpPost("api/proposals/{proposalId:guid}/rounds")]
    [Authorize(Roles = "Staff,Admin")]
    public async Task<ActionResult<ApiResponse<ReviewRoundResponse>>> CreateRound(
        Guid proposalId,
        [FromBody] CreateReviewRoundRequest request)
    {
        var userId = GetCurrentUserId();
        var result = await _service.CreateRoundAsync(proposalId, request, userId);
        return Ok(ApiResponse<ReviewRoundResponse>.Ok(result, "Round created."));
    }

    // POST /api/rounds/{roundId}/open
    [HttpPost("api/rounds/{roundId:guid}/open")]
    [Authorize(Roles = "Staff,Admin")]
    public async Task<ActionResult<ApiResponse<ReviewRoundResponse>>> OpenRound(Guid roundId)
    {
        var result = await _service.OpenRoundAsync(roundId);
        return Ok(ApiResponse<ReviewRoundResponse>.Ok(result, "Round opened."));
    }

    // POST /api/rounds/{roundId}/close
    [HttpPost("api/rounds/{roundId:guid}/close")]
    [Authorize(Roles = "Staff,Admin")]
    public async Task<ActionResult<ApiResponse<ReviewRoundResponse>>> CloseRound(
        Guid roundId,
        [FromBody] CloseRoundRequest request)
    {
        var result = await _service.CloseRoundAsync(roundId, request);
        return Ok(ApiResponse<ReviewRoundResponse>.Ok(result, "Round closed."));
    }

    // GET /api/rounds/{roundId}/members
    [HttpGet("api/rounds/{roundId:guid}/members")]
    public async Task<ActionResult<ApiResponse<IEnumerable<CouncilMemberResponse>>>> GetRoundMembers(Guid roundId)
    {
        var result = await _service.GetRoundMembersAsync(roundId);
        return Ok(ApiResponse<IEnumerable<CouncilMemberResponse>>.Ok(result));
    }

    // POST /api/rounds/{roundId}/members
    [HttpPost("api/rounds/{roundId:guid}/members")]
    [Authorize(Roles = "Staff,Admin")]
    public async Task<ActionResult<ApiResponse<CouncilMemberResponse>>> AddRoundMember(
        Guid roundId,
        [FromBody] AddRoundMemberRequest request)
    {
        var userId = GetCurrentUserId();
        var result = await _service.AddRoundMemberAsync(roundId, request, userId);
        return Ok(ApiResponse<CouncilMemberResponse>.Ok(result, "Member added to round."));
    }

    // DELETE /api/rounds/{roundId}/members/{memberId}
    [HttpDelete("api/rounds/{roundId:guid}/members/{memberId:guid}")]
    [Authorize(Roles = "Staff,Admin")]
    public async Task<ActionResult<ApiResponse>> RemoveRoundMember(Guid roundId, Guid memberId)
    {
        await _service.RemoveRoundMemberAsync(roundId, memberId);
        return Ok(ApiResponse.Ok("Member removed from round."));
    }

    private Guid GetCurrentUserId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? throw new UnauthorizedAccessException("Phiên đăng nhập không hợp lệ. Hãy đăng nhập lại.");
        return Guid.Parse(value);
    }
}
