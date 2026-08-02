using System.Security.Claims;
using FURPMS.Application.Common;
using FURPMS.Application.DTOs.Meetings;
using FURPMS.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FURPMS.API.Controllers;

[ApiController]
[Authorize]
public class CouncilMeetingsController : ControllerBase
{
    private readonly ICouncilMeetingService _service;

    public CouncilMeetingsController(ICouncilMeetingService service)
    {
        _service = service;
    }

    // GET /api/meetings  — all meetings (admin view)
    [HttpGet("api/meetings")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> GetAll()
    {
        var result = await _service.GetAllAsync();
        return Ok(ApiResponse<IEnumerable<MeetingListDto>>.Ok(result));
    }

    // GET /api/meetings/my — lịch họp hội đồng CHẤM ĐỀ TÀI CỦA TÔI (PI).
    // Process_Spec §xét duyệt: "PI trình bày, rời phòng khi họp kín" → PI phải biết họp lúc nào,
    // ở đâu / link nào. Trước đây chỉ Staff + Reviewer xem được lịch, PI không có màn nào.
    [HttpGet("api/meetings/my")]
    public async Task<IActionResult> GetMine()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _service.GetForPiAsync(userId);
        return Ok(ApiResponse<IEnumerable<MeetingListDto>>.Ok(result));
    }

    // GET /api/councils/{councilId}/meetings
    [HttpGet("api/councils/{councilId:guid}/meetings")]
    public async Task<IActionResult> GetByCouncil(Guid councilId)
    {
        var result = await _service.GetByCouncilAsync(councilId);
        return Ok(ApiResponse<IEnumerable<MeetingDto>>.Ok(result));
    }

    // POST /api/councils/{councilId}/meetings
    [HttpPost("api/councils/{councilId:guid}/meetings")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> Schedule(Guid councilId, [FromBody] ScheduleMeetingRequest request)
    {
        var result = await _service.ScheduleAsync(councilId, request);
        return Ok(ApiResponse<MeetingDto>.Ok(result));
    }

    // POST /api/meetings/{id}/start
    [HttpPost("api/meetings/{id:guid}/start")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> Start(Guid id)
    {
        var result = await _service.StartAsync(id);
        return Ok(ApiResponse<MeetingDto>.Ok(result));
    }

    // POST /api/meetings/{id}/end
    [HttpPost("api/meetings/{id:guid}/end")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> End(Guid id)
    {
        var result = await _service.EndAsync(id);
        return Ok(ApiResponse<MeetingDto>.Ok(result));
    }

    // GET /api/meetings/{id}/attendance — điểm danh (theo DS hội đồng)
    [HttpGet("api/meetings/{id:guid}/attendance")]
    public async Task<IActionResult> GetAttendance(Guid id)
    {
        var result = await _service.GetAttendanceAsync(id);
        return Ok(ApiResponse<IEnumerable<AttendanceEntryDto>>.Ok(result));
    }

    // PUT /api/meetings/{id}/attendance — Thư ký/Admin/Staff lưu điểm danh
    [HttpPut("api/meetings/{id:guid}/attendance")]
    public async Task<IActionResult> SaveAttendance(Guid id, [FromBody] SaveAttendanceRequest request)
    {
        var callerId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var isAdminStaff = User.FindAll(ClaimTypes.Role).Select(c => c.Value).Any(r => r is "Admin" or "Staff");
        await _service.SaveAttendanceAsync(id, callerId, isAdminStaff, request);
        return Ok(ApiResponse.Ok("Đã lưu điểm danh."));
    }
}
