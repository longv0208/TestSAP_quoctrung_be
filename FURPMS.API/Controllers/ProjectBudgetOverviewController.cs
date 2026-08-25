using System.Security.Claims;
using FURPMS.Application.Common;
using FURPMS.Application.DTOs.Budget;
using FURPMS.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FURPMS.API.Controllers;

/// <summary>
/// Kinh phí nhìn theo ĐỀ TÀI (không phải theo đề cương, cũng không theo hợp đồng) — vì một đề tài
/// đi qua cả ba: dự toán ở đề cương, giá trị ở hợp đồng, tiền ra ở các đợt giải ngân.
/// </summary>
[ApiController]
[Route("api/projects/{projectId:guid}/budget")]
[Authorize]
public class ProjectBudgetOverviewController : ControllerBase
{
    private readonly IProjectBudgetOverviewService _budget;

    public ProjectBudgetOverviewController(IProjectBudgetOverviewService budget)
    {
        _budget = budget;
    }

    [ProducesResponseType(typeof(ApiResponse<ProjectBudgetOverviewResponse>), StatusCodes.Status200OK)]
    [HttpGet]
    public async Task<IActionResult> Get(Guid projectId)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var roles = User.FindAll(ClaimTypes.Role).Select(r => r.Value);

        var result = await _budget.GetAsync(projectId, userId, roles);
        return Ok(ApiResponse<ProjectBudgetOverviewResponse>.Ok(result));
    }
}
