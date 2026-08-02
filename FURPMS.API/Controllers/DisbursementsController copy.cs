using System.Security.Claims;
using FURPMS.Application.Common;
using FURPMS.Application.DTOs.Contract;
using FURPMS.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FURPMS.API.Controllers;

[ApiController]
[Route("api/disbursements")]
[Authorize]
public class DisbursementsController : ControllerBase
{
    private readonly IDisbursementService _service;

    public DisbursementsController(IDisbursementService service)
    {
        _service = service;
    }

    [HttpPost("{id:int}/confirm")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> Confirm(int id, [FromBody] ConfirmDisbursementRequest request)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _service.ConfirmAsync(id, request, userId);
        return Ok(ApiResponse<DisbursementResponse>.Ok(result));
    }
}
