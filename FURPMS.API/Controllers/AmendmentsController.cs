using System.Security.Claims;
using FURPMS.Application.Common;
using FURPMS.Application.DTOs.Contract;
using FURPMS.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace FURPMS.API.Controllers;

[ApiController]
[Route("api/amendments")]
[Authorize]
public class AmendmentsController : ControllerBase
{
    private readonly IAmendmentService _service;

    public AmendmentsController(IAmendmentService service)
    {
        _service = service;
    }

    [ProducesResponseType(typeof(ApiResponse<AmendmentDetailResponse>), StatusCodes.Status200OK)]
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var result = await _service.GetByIdAsync(id);
        return Ok(ApiResponse<AmendmentDetailResponse>.Ok(result));
    }

    [ProducesResponseType(typeof(ApiResponse<AmendmentDetailResponse>), StatusCodes.Status200OK)]
    [HttpPost("{id:guid}/approve")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> Approve(Guid id, [FromBody] ReviewAmendmentRequest request)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _service.ApproveAsync(id, request, userId);
        return Ok(ApiResponse<AmendmentDetailResponse>.Ok(result));
    }

    [ProducesResponseType(typeof(ApiResponse<AmendmentDetailResponse>), StatusCodes.Status200OK)]
    [HttpPost("{id:guid}/reject")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> Reject(Guid id, [FromBody] ReviewAmendmentRequest request)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _service.RejectAsync(id, request, userId);
        return Ok(ApiResponse<AmendmentDetailResponse>.Ok(result));
    }
}
