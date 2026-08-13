using System.Security.Claims;
using FURPMS.Application.Common;
using FURPMS.Application.DTOs.ReviewScoring;
using FURPMS.Application.Interfaces.Repositories;
using FURPMS.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FURPMS.API.Controllers;

[ApiController]
[Route("api/review-scoring")]
[Authorize]
public class ReviewScoringController : ControllerBase
{
    private readonly IReviewScoringService _scoring;
    private readonly IReviewRepository _review;

    public ReviewScoringController(IReviewScoringService scoring, IReviewRepository review)
    {
        _scoring = scoring;
        _review = review;
    }

    /// <summary>Admin/Staff xem tự do; ngoài ra phải là thành viên hội đồng (Thư ký cần điểm để lập biên bản).</summary>
    private async Task EnsureAdminStaffOrMemberAsync(Guid councilId)
    {
        var roles = User.FindAll(ClaimTypes.Role).Select(r => r.Value).ToHashSet();
        if (roles.Contains("Admin") || roles.Contains("Staff")) return;

        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var isMember = await _review.CouncilMembers.AnyAsync(m => m.CouncilId == councilId && m.UserId == userId);
        if (!isMember)
            throw new ForbiddenException("Bạn không thuộc hội đồng này.");
    }

    // GET /api/review-scoring/rubrics
    [HttpGet("rubrics")]
    public async Task<IActionResult> GetRubricTemplates()
    {
        var result = await _scoring.GetRubricTemplatesAsync();
        return Ok(ApiResponse<IEnumerable<RubricTemplateDto>>.Ok(result));
    }

    // GET /api/review-scoring/rubrics/{id}
    [HttpGet("rubrics/{id:int}")]
    public async Task<IActionResult> GetRubricTemplate(int id)
    {
        var result = await _scoring.GetRubricTemplateByIdAsync(id);
        return Ok(ApiResponse<RubricTemplateDto>.Ok(result));
    }

    // POST /api/review-scoring/councils/{councilId}/scores
    [HttpPost("councils/{councilId:guid}/scores")]
    public async Task<IActionResult> SubmitScore(Guid councilId, [FromBody] SubmitScoreRequest request)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _scoring.SubmitScoreAsync(councilId, userId, request);
        return Ok(ApiResponse<ReviewScoreDto>.Ok(result));
    }

    // GET /api/review-scoring/councils/{councilId}/scores/my
    [HttpGet("councils/{councilId:guid}/scores/my")]
    public async Task<IActionResult> GetMyScore(Guid councilId, [FromQuery] Guid? projectId)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _scoring.GetMyScoreAsync(councilId, userId, projectId);
        return Ok(ApiResponse<ReviewScoreDto?>.Ok(result));
    }

    // GET /api/review-scoring/councils/{councilId}/scores
    [HttpGet("councils/{councilId:guid}/scores")]
    public async Task<IActionResult> GetCouncilScores(Guid councilId, [FromQuery] Guid? projectId)
    {
        await EnsureAdminStaffOrMemberAsync(councilId);
        var result = await _scoring.GetCouncilScoresAsync(councilId, projectId);
        return Ok(ApiResponse<IEnumerable<ReviewScoreDto>>.Ok(result));
    }

    // POST /api/review-scoring/councils/{councilId}/decision
    [HttpPost("councils/{councilId:guid}/decision")]
    [Authorize(Roles = "Admin,Staff")]
    public async Task<IActionResult> FinalizeDecision(Guid councilId, [FromBody] FinalizeDecisionRequest request)
    {
        var result = await _scoring.FinalizeDecisionAsync(councilId, request);
        return Ok(ApiResponse<CouncilDecisionDto>.Ok(result));
    }

    // GET /api/review-scoring/councils/{councilId}/ballot-tally — BM12 mục 10.1:
    // kết quả bỏ phiếu chi tiết từng thành viên (ai chấm, vai gì, bao nhiêu điểm, Đạt/Không đạt).
    [HttpGet("councils/{councilId:guid}/ballot-tally")]
    public async Task<IActionResult> GetBallotTally(Guid councilId, [FromQuery] Guid? projectId)
    {
        var result = await _scoring.GetBallotTallyAsync(councilId, projectId);
        return Ok(ApiResponse<BallotTallyDto>.Ok(result));
    }

    // POST /api/review-scoring/councils/{councilId}/minutes — Thư ký soạn/sửa biên bản (nháp)
    [HttpPost("councils/{councilId:guid}/minutes")]
    public async Task<IActionResult> SaveMinutes(Guid councilId, [FromBody] SaveMinutesRequest request)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _scoring.SaveMinutesAsync(councilId, userId, request);
        return Ok(ApiResponse<CouncilDecisionDto>.Ok(result));
    }

    // POST /api/review-scoring/councils/{councilId}/minutes/approve — Chủ tịch duyệt = khoá
    [HttpPost("councils/{councilId:guid}/minutes/approve")]
    public async Task<IActionResult> ApproveMinutes(Guid councilId, [FromQuery] Guid? projectId)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _scoring.ApproveMinutesAsync(councilId, userId, projectId);
        return Ok(ApiResponse<CouncilDecisionDto>.Ok(result));
    }

    /// <summary>
    /// Chủ tịch <b>trả biên bản cho Thư ký sửa</b>, kèm ghi chú nêu rõ cần sửa gì.
    /// <para>
    /// QĐ543 Điều 8.3.c: Thư ký <i>ghi</i> biên bản, hội đồng <i>thông qua</i> — Chủ tịch không tự
    /// sửa chữ của Thư ký. Trước đây hệ thống chỉ có "duyệt (khoá luôn)" hoặc "không làm gì", nên
    /// muốn sửa một chỗ là phải nhắn tin ngoài hệ thống.
    /// </para>
    /// </summary>
    [HttpPost("councils/{councilId:guid}/minutes/request-revision")]
    public async Task<IActionResult> RequestMinutesRevision(
        Guid councilId, [FromBody] RequestMinutesRevisionRequest request, [FromQuery] Guid? projectId)
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await _scoring.RequestMinutesRevisionAsync(councilId, userId, request.Note, projectId);
        return Ok(ApiResponse<CouncilDecisionDto>.Ok(result));
    }

    // GET /api/review-scoring/councils/{councilId}/decision
    [HttpGet("councils/{councilId:guid}/decision")]
    public async Task<IActionResult> GetDecision(Guid councilId, [FromQuery] Guid? projectId)
    {
        var result = await _scoring.GetDecisionAsync(councilId, projectId);
        return Ok(ApiResponse<CouncilDecisionDto?>.Ok(result));
    }
}

/// <summary>Ghi chú Chủ tịch gửi kèm khi trả biên bản — bắt buộc, không cho trả lại suông.</summary>
public class RequestMinutesRevisionRequest
{
    public string Note { get; set; } = null!;
}
