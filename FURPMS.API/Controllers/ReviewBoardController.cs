using System.Security.Claims;
using FURPMS.Application.Common;
using FURPMS.Application.DTOs.ReviewRounds;
using FURPMS.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FURPMS.API.Controllers;

[ApiController]
[Authorize]
public class ReviewBoardController : ControllerBase
{
    private readonly IReviewBoardService _board;

    public ReviewBoardController(IReviewBoardService board)
    {
        _board = board;
    }

    private Guid GetCurrentUserId()
    {
        var claim = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
        return Guid.Parse(claim!);
    }

    [HttpGet("api/cycles/{cycleId:int}/tracks/{trackId:int}/review-board")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> GetBoard(int cycleId, int trackId)
    {
        var result = await _board.GetReviewBoardAsync(cycleId, trackId);
        return Ok(ApiResponse<ReviewBoardDto>.Ok(result));
    }

    [HttpPost("api/cycles/{cycleId:int}/tracks/{trackId:int}/rounds")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> CreateRoundForTrack(int cycleId, int trackId, [FromBody] CreateTrackRoundRequest request)
    {
        var result = await _board.CreateRoundForTrackAsync(cycleId, trackId, request);
        return Ok(ApiResponse<ReviewRoundResponse>.Ok(result, "Đã tạo vòng chấm."));
    }

    [HttpDelete("api/rounds/{roundId:guid}")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> DeleteRound(Guid roundId)
    {
        await _board.DeleteRoundAsync(roundId);
        return Ok(ApiResponse.Ok("Đã xoá vòng."));
    }

    [HttpPost("api/rounds/{roundId:guid}/projects")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> AddProject(Guid roundId, [FromBody] AddProjectToRoundRequest request)
    {
        await _board.AddProjectToRoundAsync(roundId, request.ProjectId);
        return Ok(ApiResponse.Ok("Đã thêm đề tài vào vòng."));
    }

    [HttpDelete("api/rounds/{roundId:guid}/projects/{projectId:guid}")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> RemoveProject(Guid roundId, Guid projectId)
    {
        await _board.RemoveProjectFromRoundAsync(roundId, projectId);
        return Ok(ApiResponse.Ok("Đã gỡ đề tài khỏi vòng."));
    }

    [HttpPost("api/rounds/{roundId:guid}/councils")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> CreateCouncilPackage(Guid roundId, [FromBody] CreateCouncilPackageRequest request)
    {
        var result = await _board.CreateCouncilPackageAsync(roundId, request, GetCurrentUserId());
        return Ok(ApiResponse<ReviewBoardCouncilDto>.Ok(result, "Đã tạo hội đồng."));
    }
}
