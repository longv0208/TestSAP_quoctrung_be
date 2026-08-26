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
    private readonly ICouncilCandidateService _candidates;

    public CouncilsController(ICouncilService service, ICouncilCandidateService candidates)
    {
        _service = service;
        _candidates = candidates;
    }

    /// <summary>
    /// Ứng viên ủy viên hội đồng, <b>đã xếp hạng theo chuyên môn</b> (QĐ543 Điều 8.2).
    ///
    /// <para>Đúng lĩnh vực lên đầu; người vướng xung đột lợi ích hoặc đã có tên xuống cuối nhưng
    /// <b>vẫn hiện</b> — giấu đi thì Phòng QLKH không hiểu vì sao tìm mãi không thấy một cái tên.</para>
    ///
    /// <para>Đây là thứ tài liệu cũ gọi là <c>/ai/suggest-reviewers</c>. Nó <b>không phải AI</b> —
    /// là một phép nối bảng rồi sắp xếp, và được gọi đúng tên như vậy.</para>
    /// </summary>
    [HttpGet("candidates")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<ActionResult<ApiResponse<CouncilCandidatesResponse>>> GetCandidates(
        [FromQuery] Guid? projectId, [FromQuery] int? trackId, [FromQuery] Guid? councilId)
    {
        var result = await _candidates.GetCandidatesAsync(projectId, trackId, councilId);
        return Ok(ApiResponse<CouncilCandidatesResponse>.Ok(result));
    }

    // GET /api/councils/my-memberships
    /// <summary>
    /// Danh sách hội đồng cho Phòng QLKH — kèm số liệu tóm tắt và <b>việc còn thiếu để gửi thư mời</b>.
    /// <para>
    /// Trước 14/08 không có endpoint nào liệt kê hội đồng, nên màn "Hội đồng" của chuyên viên phải
    /// hiện tạm bảng ĐỀ TÀI — trùng y hệt màn "Xét duyệt đề cương".
    /// </para>
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<ActionResult<ApiResponse<IEnumerable<CouncilListItemDto>>>> GetCouncils(
        [FromQuery] CouncilQueryParams query)
    {
        var result = await _service.GetCouncilsAsync(query);
        return Ok(ApiResponse<IEnumerable<CouncilListItemDto>>.Ok(result));
    }

    [HttpGet("{councilId:guid}")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<ActionResult<ApiResponse<CouncilListItemDto>>> GetCouncil(Guid councilId)
    {
        var result = await _service.GetCouncilByIdAsync(councilId);
        return Ok(ApiResponse<CouncilListItemDto>.Ok(result));
    }

    /// <summary>Sửa thông tin hành chính (số quyết định, ngày thành lập, hạn họp, số thành viên).</summary>
    [HttpPut("{councilId:guid}")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<ActionResult<ApiResponse<CouncilListItemDto>>> UpdateCouncil(
        Guid councilId, [FromBody] UpdateCouncilRequest request)
    {
        var result = await _service.UpdateCouncilAsync(councilId, request);
        return Ok(ApiResponse<CouncilListItemDto>.Ok(result));
    }

    /// <summary>
    /// Đổi vai trò một thành viên (Chủ tịch / Thư ký / Phản biện / Uỷ viên).
    /// <para>
    /// Trước đây gán sai vai thì chỉ còn cách <b>xoá khỏi hội đồng rồi thêm lại</b> — mất luôn dấu
    /// vết đã mời và đã xác nhận, phải mời lại từ đầu.
    /// </para>
    /// </summary>
    [HttpPut("/api/council-members/{memberId:guid}")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<ActionResult<ApiResponse<CouncilMemberResponse>>> UpdateMemberRole(
        Guid memberId, [FromBody] UpdateCouncilMemberRequest request)
    {
        var result = await _service.UpdateMemberRoleAsync(memberId, request.MemberRole);
        return Ok(ApiResponse<CouncilMemberResponse>.Ok(result));
    }

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

    /// <summary>
    /// Chuyên viên ghi nhận trả lời thư mời <b>thay</b> thành viên — họ đã đồng ý hoặc từ chối
    /// ngoài hệ thống (gọi điện, email), hoặc lúc demo không có sẵn tài khoản người chấm.
    ///
    /// <para>
    /// Trước 14/08 nút "Đánh dấu từ chối" ở giao diện gọi nhầm sang endpoint dành cho <i>chính
    /// thành viên</i> (<c>PATCH /respond</c>) ⇒ chuyên viên luôn nhận <b>403 "Bạn chỉ trả lời được
    /// thư mời gửi cho chính mình"</b> — câu hoàn toàn vô nghĩa với người đang ghi nhận hộ.
    /// Nút "Xác nhận thay" đã được chuyển sang endpoint riêng từ trước, nhưng nhánh TỪ CHỐI thì bị
    /// bỏ sót.
    /// </para>
    /// </summary>
    [HttpPost("/api/council-members/{memberId:guid}/respond-on-behalf")]
    [Authorize(Roles = "Staff,Admin")]
    public async Task<ActionResult<ApiResponse<CouncilMemberResponse>>> RespondOnBehalf(
        Guid memberId, [FromBody] RespondMembershipRequest request)
    {
        var result = await _service.RespondOnBehalfAsync(
            memberId, GetCurrentUserId(), request.Accept, request.DeclineReason);
        return Ok(ApiResponse<CouncilMemberResponse>.Ok(
            result, request.Accept ? "Đã xác nhận thay thành viên." : "Đã ghi nhận thành viên từ chối."));
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
            ?? throw new UnauthorizedAccessException("Phiên đăng nhập không hợp lệ. Hãy đăng nhập lại.");
        return Guid.Parse(value);
    }
}
