using System.Security.Claims;
using FURPMS.Application.Common;
using FURPMS.Application.DTOs.Decisions;
using FURPMS.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FURPMS.API.Controllers;

/// <summary>
/// Hồ sơ quyết định của đề tài — trả lời nửa sau yêu cầu số (2) của hội đồng bảo vệ lần 2:
/// *"lưu trữ lại các quyết định liên quan đến đề tài"*.
/// </summary>
[ApiController]
[Authorize]
public class ProjectDecisionsController : ControllerBase
{
    private readonly IProjectDecisionService _decisions;

    public ProjectDecisionsController(IProjectDecisionService decisions)
    {
        _decisions = decisions;
    }

    [ProducesResponseType(typeof(ApiResponse<ProjectDecisionDossierResponse>), StatusCodes.Status200OK)]
    [HttpGet("api/projects/{projectId:guid}/decisions")]
    public async Task<IActionResult> Get(Guid projectId)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var roles = User.FindAll(ClaimTypes.Role).Select(r => r.Value);

        var result = await _decisions.GetDossierAsync(projectId, userId, roles);
        return Ok(ApiResponse<ProjectDecisionDossierResponse>.Ok(result));
    }

    /// <summary>
    /// Dựng lại sổ quyết định từ dữ liệu nghiệp vụ đã có.
    ///
    /// <para>Sổ chỉ bắt đầu ghi từ lúc tính năng lên, nên mọi đề tài đã chạy xong trước đó sẽ có hồ
    /// sơ trống. Gọi endpoint này một lần sau khi deploy để hồ sơ cũ hiện đủ.</para>
    ///
    /// <para><b>Chạy lại được nhiều lần</b> — dòng đã có thì bỏ qua. Nên gọi với
    /// <c>dryRun=true</c> trước để xem sẽ thêm bao nhiêu và thuộc những loại nào.</para>
    /// </summary>
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<BackfillDecisionsResponse>), StatusCodes.Status200OK)]
    [HttpPost("api/admin/backfill-decisions")]
    public async Task<IActionResult> Backfill([FromQuery] Guid? projectId, [FromQuery] bool dryRun = false)
    {
        var result = await _decisions.BackfillAsync(projectId, dryRun);
        return Ok(ApiResponse<BackfillDecisionsResponse>.Ok(result));
    }
}
