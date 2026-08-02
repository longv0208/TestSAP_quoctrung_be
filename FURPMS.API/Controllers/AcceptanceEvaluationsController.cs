using System.Security.Claims;
using FURPMS.Application.Common;
using FURPMS.Application.DTOs.ReviewScoring;
using FURPMS.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FURPMS.API.Controllers;

[ApiController]
[Authorize]
[Route("api/councils/{councilId:guid}/acceptance")]
public class AcceptanceEvaluationsController : ControllerBase
{
    private readonly IAcceptanceEvaluationService _service;

    public AcceptanceEvaluationsController(IAcceptanceEvaluationService service) => _service = service;

    // Tổng hợp mọi phiếu của hội đồng — Staff/Admin xem; thành viên hội đồng xem để lập biên bản.
    [HttpGet]
    public async Task<IActionResult> GetAll(Guid councilId)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var isStaffOrAdmin = User.IsInRole("Admin") || User.IsInRole("Staff");
        var list = await _service.GetByCouncilAsync(councilId, userId, isStaffOrAdmin);
        return Ok(ApiResponse<IReadOnlyList<AcceptanceEvaluationDto>>.Ok(list));
    }

    // Phiếu của CHÍNH tôi (null nếu chưa chấm) — form chấm nghiệm thu của reviewer dùng endpoint này.
    [HttpGet("my")]
    public async Task<IActionResult> GetMy(Guid councilId)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var dto = await _service.GetMyAsync(councilId, userId);
        return Ok(ApiResponse<AcceptanceEvaluationDto?>.Ok(dto));
    }

    [HttpPost]
    public async Task<IActionResult> Submit(Guid councilId, [FromBody] SubmitAcceptanceRequest request)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var dto = await _service.SubmitAsync(councilId, userId, request);
        return Ok(ApiResponse<AcceptanceEvaluationDto>.Ok(dto));
    }
}
