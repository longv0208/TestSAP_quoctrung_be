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
    private readonly IAiAdvisorService _advisor;

    public AiController(IAiSummaryService ai, IAiAdvisorService advisor)
    {
        _ai = ai;
        _advisor = advisor;
    }

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private List<string> CurrentRoles => User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();

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
        var roles = User.FindAll(ClaimTypes.Role).Select(c => c.Value).ToList();
        var result = await _ai.GenerateAsync(proposalId, userId, roles);
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

    // ── AI trợ lý (P8) ───────────────────────────────────────────────────────

    /// <summary>Góp ý AI đã sinh trước đó cho đề cương — trả null nếu chưa có (không tốn quota).</summary>
    [HttpGet("api/ai/proposals/{proposalId:guid}/feedback")]
    public async Task<IActionResult> GetFeedback(Guid proposalId)
    {
        var result = await _advisor.GetProposalFeedbackAsync(proposalId, CurrentUserId, CurrentRoles);
        return Ok(ApiResponse<IReadOnlyList<AiFeedbackDto>?>.Ok(result));
    }

    /// <summary>Sinh góp ý AI mới cho đề cương (gọi Gemini, ghi đè bản cũ).</summary>
    [HttpPost("api/ai/proposals/{proposalId:guid}/feedback")]
    public async Task<IActionResult> GenerateFeedback(Guid proposalId)
    {
        var result = await _advisor.GenerateProposalFeedbackAsync(proposalId, CurrentUserId, CurrentRoles);
        return Ok(ApiResponse<IReadOnlyList<AiFeedbackDto>>.Ok(result));
    }

    /// <summary>
    /// Đối chiếu thông tin PI đã điền với FILE đề cương đính kèm — chỉ ra chỗ thiếu/lệch
    /// (thầy 29/07). Chưa đính kèm file thì trả <c>hasFile=false</c>, không phải lỗi.
    /// </summary>
    [HttpPost("api/ai/proposals/{proposalId:guid}/consistency-check")]
    public async Task<IActionResult> CheckConsistency(Guid proposalId)
    {
        var result = await _advisor.CheckConsistencyAsync(proposalId, CurrentUserId, CurrentRoles);
        return Ok(ApiResponse<AiConsistencyResultDto>.Ok(result));
    }

    /// <summary>
    /// AI gợi ý điểm theo từng tiêu chí của bộ tiêu chí đang áp cho hội đồng (thầy nhắc trực tiếp).
    /// Chỉ GỢI Ý — người chấm vẫn tự nhập điểm cuối (rule #12).
    /// </summary>
    [HttpPost("api/ai/councils/{councilId:guid}/proposals/{proposalId:guid}/score-suggestion")]
    public async Task<IActionResult> SuggestScores(Guid councilId, Guid proposalId)
    {
        var isStaffOrAdmin = User.IsInRole("Admin") || User.IsInRole("Staff");
        var result = await _advisor.SuggestScoresAsync(councilId, proposalId, CurrentUserId, isStaffOrAdmin);
        return Ok(ApiResponse<IReadOnlyList<AiScoreSuggestionDto>>.Ok(result));
    }
}
