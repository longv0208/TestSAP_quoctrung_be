using System.Security.Claims;
using FURPMS.Application.Common;
using FURPMS.Application.DTOs.AI;
using FURPMS.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FURPMS.API.Controllers;

[ApiController]
[Authorize]
public class AiController : ControllerBase
{
    private readonly IAiSummaryService _ai;

    public AiController(IAiSummaryService ai)
    {
        _ai = ai;
    }

    // Lấy tóm tắt AI hiện có của đề xuất (null nếu chưa tạo)
    [HttpGet("api/proposals/{proposalId:guid}/summary")]
    public async Task<IActionResult> Get(Guid proposalId)
    {
        var result = await _ai.GetAsync(proposalId);
        return Ok(ApiResponse<AiSummaryDto?>.Ok(result));
    }

    // Sinh tóm tắt AI mới bằng Gemini
    [HttpPost("api/proposals/{proposalId:guid}/generate-summary")]
    public async Task<IActionResult> Generate(Guid proposalId)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _ai.GenerateAsync(proposalId, userId);
        return Ok(ApiResponse<AiSummaryDto>.Ok(result));
    }

    // Sửa lại nội dung tóm tắt (con người chỉnh)
    [HttpPatch("api/proposals/{proposalId:guid}/summary")]
    public async Task<IActionResult> Update(Guid proposalId, [FromBody] UpdateSummaryRequest request)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _ai.UpdateAsync(proposalId, request.EditedText, userId);
        return Ok(ApiResponse<AiSummaryDto>.Ok(result));
    }
}
