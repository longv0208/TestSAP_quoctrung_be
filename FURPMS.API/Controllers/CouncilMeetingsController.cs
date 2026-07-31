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
}
