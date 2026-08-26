using System.Security.Claims;
using FURPMS.Application.Common;
using FURPMS.Application.DTOs.Timeline;
using FURPMS.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FURPMS.API.Controllers;

/// <summary>
/// Dòng thời gian đề tài — mọi giai đoạn kèm hạn, và hạn đó lấy từ đâu.
/// Trả lời yêu cầu số (2) của hội đồng bảo vệ lần 2.
/// </summary>
[ApiController]
[Authorize]
public class ProjectTimelineController : ControllerBase
{
    private readonly IProjectTimelineService _timeline;

    public ProjectTimelineController(IProjectTimelineService timeline)
    {
        _timeline = timeline;
    }

    [ProducesResponseType(typeof(ApiResponse<ProjectTimelineResponse>), StatusCodes.Status200OK)]
    [HttpGet("api/projects/{projectId:guid}/timeline")]
    public async Task<IActionResult> Get(Guid projectId)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var roles = User.FindAll(ClaimTypes.Role).Select(r => r.Value);

        var result = await _timeline.GetAsync(projectId, userId, roles);
        return Ok(ApiResponse<ProjectTimelineResponse>.Ok(result));
    }

    /// <summary>
    /// Hạn sắp tới (và đã quá) của chính người đang đăng nhập, gộp từ mọi đề tài họ liên quan.
    /// Dùng cho thẻ nhắc việc trên bảng điều khiển.
    /// </summary>
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<UpcomingDeadlineDto>>), StatusCodes.Status200OK)]
    [HttpGet("api/me/deadlines")]
    public async Task<IActionResult> GetMine([FromQuery] int days = 30)
    {
        // Kẹp cửa sổ: 0 ngày thì thẻ trống trơ, 365 ngày thì thành danh sách mọi mốc của cả đề tài.
        days = Math.Clamp(days, 1, 180);

        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var roles = User.FindAll(ClaimTypes.Role).Select(r => r.Value);

        var result = await _timeline.GetUpcomingAsync(userId, roles, days);
        return Ok(ApiResponse<IReadOnlyList<UpcomingDeadlineDto>>.Ok(result));
    }
}
