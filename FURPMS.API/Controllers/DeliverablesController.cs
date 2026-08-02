using System.Security.Claims;
using FURPMS.Application.Common;
using FURPMS.Application.DTOs.Contract;
using FURPMS.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FURPMS.API.Controllers;

[ApiController]
[Route("api/deliverables")]
[Authorize]
public class DeliverablesController : ControllerBase
{
    private readonly IDeliverableService _service;

    public DeliverablesController(IDeliverableService service)
    {
        _service = service;
    }

    [HttpPost("{id:int}/submit")]
    public async Task<IActionResult> Submit(int id, [FromBody] SubmitDeliverableRequest request)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _service.SubmitAsync(id, request, userId);
        return Ok(ApiResponse<DeliverableResponse>.Ok(result));
    }

    [HttpPost("{id:int}/evaluate")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> Evaluate(int id, [FromBody] EvaluateDeliverableRequest request)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _service.EvaluateAsync(id, request, userId);
        return Ok(ApiResponse<DeliverableResponse>.Ok(result));
    }
}
