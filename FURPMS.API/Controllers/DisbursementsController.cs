using System.Security.Claims;
using FURPMS.Application.Common;
using FURPMS.Application.DTOs.Contract;
using FURPMS.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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

    [ProducesResponseType(typeof(ApiResponse<DisbursementResponse>), StatusCodes.Status200OK)]
    [HttpPost("{id:int}/confirm")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> Confirm(int id, [FromBody] ConfirmDisbursementRequest request)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _service.ConfirmAsync(id, request, userId);
        return Ok(ApiResponse<DisbursementResponse>.Ok(result));
    }

    /// <summary>
    /// Gắn sản phẩm minh chứng cho đợt giải ngân (<c>deliverableId = null</c> để gỡ).
    /// Sản phẩm phải thuộc cùng hợp đồng; đợt đã giải ngân thì không đổi được nữa.
    /// </summary>
    [ProducesResponseType(typeof(ApiResponse<DisbursementResponse>), StatusCodes.Status200OK)]
    [HttpPut("{id:int}/deliverable")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> LinkDeliverable(int id, [FromBody] LinkDeliverableRequest request)
    {
        var result = await _service.LinkDeliverableAsync(id, request);
        return Ok(ApiResponse<DisbursementResponse>.Ok(result));
    }
}
