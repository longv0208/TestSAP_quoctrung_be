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

    [HttpGet]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> GetAll(Guid councilId)
    {
        var list = await _service.GetByCouncilAsync(councilId);
        return Ok(ApiResponse<IReadOnlyList<AcceptanceEvaluationDto>>.Ok(list));
    }

    [HttpPost]
    public async Task<IActionResult> Submit(Guid councilId, [FromBody] SubmitAcceptanceRequest request)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var dto = await _service.SubmitAsync(councilId, userId, request);
        return Ok(ApiResponse<AcceptanceEvaluationDto>.Ok(dto));
    }
}
