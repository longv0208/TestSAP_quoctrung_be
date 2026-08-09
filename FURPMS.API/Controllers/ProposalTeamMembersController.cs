using System.Security.Claims;
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
        return Ok(ApiResponse<TeamMemberResponse>.Ok(result, "Đã thêm thành viên."));
    }

    // Sửa/xoá chỉ dành cho CHỦ NHIỆM và chỉ khi đề cương còn nháp — đã nộp rồi mà vẫn thêm bớt
    // người thì hội đồng chấm một danh sách, hồ sơ lưu một danh sách khác.
    [HttpPut("{memberId:int}")]
    public async Task<ActionResult<ApiResponse<TeamMemberResponse>>> Update(
        Guid proposalId, int memberId, [FromBody] CreateTeamMemberRequest request)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _service.UpdateTeamMemberAsync(proposalId, memberId, request, userId);
        return Ok(ApiResponse<TeamMemberResponse>.Ok(result, "Đã cập nhật thành viên."));
    }

    [HttpDelete("{memberId:int}")]
    public async Task<IActionResult> Delete(Guid proposalId, int memberId)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await _service.DeleteTeamMemberAsync(proposalId, memberId, userId);
        return Ok(ApiResponse.Ok("Đã xoá thành viên khỏi nhóm nghiên cứu."));
    }
}
