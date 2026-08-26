using System.Security.Claims;
using FURPMS.Application.Common;
using FURPMS.Application.DTOs.Ai;
using FURPMS.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FURPMS.API.Controllers;

/// <summary>
/// Rà trùng lặp đề cương — gạch 3 của biên bản hội đồng bảo vệ lần 2.
///
/// <para>Hai tầng: vector + cosine (rẻ, tất định) rồi mới tới giải thích bằng mô hình sinh chữ.
/// Hệ thống <b>không tự kết luận</b> — kết luận là của Phòng QLKH qua <c>POST .../duplicate-check/review</c>.</para>
/// </summary>
[ApiController]
[Authorize]
public class DuplicateCheckController : ControllerBase
{
    private readonly IDuplicateCheckService _service;

    public DuplicateCheckController(IDuplicateCheckService service)
    {
        _service = service;
    }

    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private IEnumerable<string> CurrentRoles => User.FindAll(ClaimTypes.Role).Select(r => r.Value);

    /// <summary>Kết quả rà trùng của một đề cương. Đọc thuần — không gọi AI, không tốn quota.</summary>
    [ProducesResponseType(typeof(ApiResponse<DuplicateCheckResponse>), StatusCodes.Status200OK)]
    [HttpGet("api/proposals/{proposalId:guid}/duplicate-check")]
    public async Task<IActionResult> Get(Guid proposalId)
    {
        var result = await _service.GetAsync(proposalId, CurrentUserId, CurrentRoles);
        return Ok(ApiResponse<DuplicateCheckResponse>.Ok(result));
    }

    /// <summary>
    /// Cờ trùng lặp rút gọn cho nhiều đề cương — để màn danh sách hiện badge mà không phải mở
    /// từng đề cương một mới biết cái nào cần xem.
    /// </summary>
    [Authorize(Roles = "Admin,Staff")]
    [ProducesResponseType(typeof(ApiResponse<Dictionary<Guid, DuplicateFlagDto>>), StatusCodes.Status200OK)]
    [HttpPost("api/proposals/duplicate-flags")]
    public async Task<IActionResult> GetFlags([FromBody] List<Guid> proposalIds)
    {
        var result = await _service.GetFlagsAsync(proposalIds);
        return Ok(ApiResponse<Dictionary<Guid, DuplicateFlagDto>>.Ok(result));
    }

    /// <summary>
    /// Chạy tầng 2 — nhờ AI giải thích giống ở chỗ nào.
    ///
    /// <para>Có bản đã lưu thì trả lại bản đó; <c>force=true</c> mới gọi lại Gemini.</para>
    /// </summary>
    [Authorize(Roles = "Admin,Staff")]
    [ProducesResponseType(typeof(ApiResponse<DuplicateCheckResponse>), StatusCodes.Status200OK)]
    [HttpPost("api/proposals/{proposalId:guid}/duplicate-check/explain")]
    public async Task<IActionResult> Explain(Guid proposalId, [FromQuery] bool force = false)
    {
        var result = await _service.ExplainAsync(proposalId, CurrentUserId, CurrentRoles, force);
        return Ok(ApiResponse<DuplicateCheckResponse>.Ok(result));
    }

    /// <summary>
    /// Phòng QLKH chốt kết luận sau khi xem — <b>người trong vòng lặp</b>.
    ///
    /// <para>Sinh một dòng <c>DUPLICATE_REVIEWED</c> trong sổ quyết định của đề tài, nên về sau
    /// luôn tra được ai đã xem cảnh báo này và kết luận ra sao.</para>
    /// </summary>
    [Authorize(Roles = "Admin,Staff")]
    [ProducesResponseType(typeof(ApiResponse<DuplicateCheckResponse>), StatusCodes.Status200OK)]
    [HttpPost("api/proposals/{proposalId:guid}/duplicate-check/review")]
    public async Task<IActionResult> Review(Guid proposalId, [FromBody] ReviewDuplicateRequest request)
    {
        var result = await _service.ReviewAsync(proposalId, request, CurrentUserId, CurrentRoles);
        return Ok(ApiResponse<DuplicateCheckResponse>.Ok(result));
    }

    /// <summary>
    /// Vector hoá kho đề cương.
    ///
    /// <para>Nội dung không đổi thì bỏ qua (so theo <c>ContentHash</c>), nên chạy lại nhiều lần
    /// chỉ tốn quota cho những bản thật sự mới.</para>
    /// </summary>
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(typeof(ApiResponse<ReindexEmbeddingsResponse>), StatusCodes.Status200OK)]
    [HttpPost("api/admin/reindex-embeddings")]
    public async Task<IActionResult> Reindex(
        [FromQuery] Guid? proposalId, [FromQuery] int max = 200, CancellationToken ct = default)
    {
        var result = await _service.ReindexAsync(proposalId, max, ct);
        return Ok(ApiResponse<ReindexEmbeddingsResponse>.Ok(result));
    }
}
