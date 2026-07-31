using System.Security.Claims;
using FURPMS.Application.Common;
using FURPMS.Application.DTOs.ChangeRequests;
using FURPMS.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FURPMS.API.Controllers;

// Yêu cầu thay đổi đề tài — route khớp FE changeRequestService.ts.
[ApiController]
[Authorize]
public class ChangeRequestsController : ControllerBase
{
    private readonly IChangeRequestService _changeRequests;

    public ChangeRequestsController(IChangeRequestService changeRequests)
    {
        _changeRequests = changeRequests;
    }

    private Guid CallerId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost("api/proposals/{proposalId:guid}/change-requests")]
    public async Task<IActionResult> Create(Guid proposalId, [FromBody] CreateChangeRequestRequest request)
    {
        var result = await _changeRequests.CreateAsync(proposalId, request, CallerId);
        return Ok(ApiResponse<ChangeRequestDto>.Ok(result));
    }

    [HttpGet("api/proposals/{proposalId:guid}/change-requests")]
    public async Task<IActionResult> GetByProposal(Guid proposalId)
    {
        var result = await _changeRequests.GetByProposalAsync(proposalId);
        return Ok(ApiResponse<IEnumerable<ChangeRequestDto>>.Ok(result));
    }

    [HttpGet("api/change-requests/pending")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> GetPending()
    {
        var result = await _changeRequests.GetPendingAsync();
        return Ok(ApiResponse<IEnumerable<ChangeRequestDto>>.Ok(result));
    }

    [HttpPatch("api/change-requests/{id:guid}/review")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> Review(Guid id, [FromBody] ReviewChangeRequestRequest request)
    {
        var result = await _changeRequests.ReviewAsync(id, request, CallerId);
        return Ok(ApiResponse<ChangeRequestDto>.Ok(result));
    }
}
