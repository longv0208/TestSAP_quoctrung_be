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

    // DELETE /api/councils/{councilId} — xoá hội đồng (chỉ khi chưa có phiếu chấm / biên bản / nghiệm thu)
    [HttpDelete("{councilId:guid}")]
    [Authorize(Roles = "Staff,Admin")]
    public async Task<ActionResult<ApiResponse>> DeleteCouncil(Guid councilId)
    {
        await _service.DeleteCouncilAsync(councilId);
        return Ok(ApiResponse.Ok("Đã xoá hội đồng."));
    }

    // GET /api/councils/{councilId}/schedule-conflicts — cảnh báo TV trùng lịch hội đồng khác
    [HttpGet("{councilId:guid}/schedule-conflicts")]
    [Authorize(Roles = "Staff,Admin")]
    public async Task<ActionResult<ApiResponse<IEnumerable<ScheduleConflictDto>>>> GetScheduleConflicts(Guid councilId)
    {
        var result = await _service.GetScheduleConflictsAsync(councilId);
        return Ok(ApiResponse<IEnumerable<ScheduleConflictDto>>.Ok(result));
    }

    // GET /api/councils/{councilId}/slots — lịch chấm theo đề tài (slot con) + khung giờ buổi họp
    [HttpGet("{councilId:guid}/slots")]
    [Authorize(Roles = "Staff,Admin")]
    public async Task<ActionResult<ApiResponse<CouncilSlotBoardDto>>> GetSlots(Guid councilId)
    {
        var result = await _service.GetCouncilSlotBoardAsync(councilId);
        return Ok(ApiResponse<CouncilSlotBoardDto>.Ok(result));
    }

    // PUT /api/councils/{councilId}/slots — gán khung giờ con cho từng đề tài
    [HttpPut("{councilId:guid}/slots")]
    [Authorize(Roles = "Staff,Admin")]
    public async Task<ActionResult<ApiResponse>> SaveSlots(Guid councilId, [FromBody] SaveSlotsRequest request)
    {
        await _service.SaveCouncilSlotsAsync(councilId, request);
        return Ok(ApiResponse.Ok("Đã lưu lịch chấm."));
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

    // POST /api/councils/{councilId}/projects — gán 1 đề tài vào hội đồng có sẵn
    [HttpPost("{councilId:guid}/projects")]
    [Authorize(Roles = "Staff,Admin")]
    public async Task<ActionResult<ApiResponse>> AssignProject(Guid councilId, [FromBody] AddProjectToRoundRequest request)
    {
        await _service.AssignProjectToCouncilAsync(councilId, request.ProjectId);
        return Ok(ApiResponse.Ok("Đã gán đề tài vào hội đồng."));
    }

    // DELETE /api/councils/{councilId}/projects/{projectId} — gỡ đề tài khỏi hội đồng
    [HttpDelete("{councilId:guid}/projects/{projectId:guid}")]
    [Authorize(Roles = "Staff,Admin")]
    public async Task<ActionResult<ApiResponse>> RemoveProject(Guid councilId, Guid projectId)
    {
        await _service.RemoveProjectFromCouncilAsync(councilId, projectId);
        return Ok(ApiResponse.Ok("Đã gỡ đề tài khỏi hội đồng."));
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

    // POST /api/council-members/{memberId}/confirm-on-behalf — Staff/Admin xác nhận thay
    // (reviewer đồng ý ngoài hệ thống, hoặc tiện demo khi thiếu tài khoản reviewer để tự bấm nhận).
    [HttpPost("/api/council-members/{memberId:guid}/confirm-on-behalf")]
    [Authorize(Roles = "Staff,Admin")]
    public async Task<ActionResult<ApiResponse<CouncilMemberResponse>>> ConfirmOnBehalf(Guid memberId)
    {
        var result = await _service.ConfirmMemberOnBehalfAsync(memberId);
        return Ok(ApiResponse<CouncilMemberResponse>.Ok(result, "Đã xác nhận thay thành viên."));
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
