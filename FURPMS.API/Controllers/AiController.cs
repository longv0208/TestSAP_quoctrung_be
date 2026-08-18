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
    private readonly IServiceScopeFactory _scopes;

    public AiController(IAiSummaryService ai, IAiAdvisorService advisor, IServiceScopeFactory scopes)
    {
        _ai = ai;
        _advisor = advisor;
        _scopes = scopes;
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

    /// <summary>Đọc gợi ý điểm đã lưu — không gọi Gemini, dùng để mở màn chấm là thấy ngay.</summary>
    [HttpGet("api/ai/councils/{councilId:guid}/proposals/{proposalId:guid}/score-suggestion")]
    public async Task<IActionResult> GetScoreSuggestions(Guid councilId, Guid proposalId)
    {
        var isStaffOrAdmin = User.IsInRole("Admin") || User.IsInRole("Staff");
        var result = await _advisor.GetScoreSuggestionsAsync(councilId, proposalId, CurrentUserId, isStaffOrAdmin);
        return Ok(ApiResponse<IReadOnlyList<AiScoreSuggestionDto>?>.Ok(result));
    }

    /// <summary>
    /// <b>Một lần bấm ra cả tóm tắt lẫn gợi ý điểm.</b>
    ///
    /// <para>
    /// Trước đây người chấm bấm "Tóm tắt" chờ 30–60 giây, xong mới bấm "Gợi ý điểm" chờ thêm một
    /// lượt nữa — đúng lúc hội đồng đang ngồi nhìn. Gói Gemini miễn phí lại giới hạn request mỗi
    /// phút nên bấm hai lần liên tiếp rất dễ bị chặn giữa buổi họp.
    /// </para>
    ///
    /// <para>
    /// Hai phần chạy <b>song song</b> ⇒ tổng thời gian chờ xấp xỉ một lần gọi. Mỗi phần lấy
    /// <b>scope DI riêng</b>: <c>DbContext</c> không an toàn khi hai luồng dùng chung, dùng chung
    /// scope là tự tạo ra lỗi tranh chấp lúc chạy.
    /// </para>
    ///
    /// <para>
    /// Một phần hỏng <b>không kéo đổ phần kia</b> — trả về phần chạy được kèm lý do phần lỗi, vì
    /// tóm tắt vẫn dùng được dù chưa có gợi ý điểm và ngược lại.
    /// </para>
    /// </summary>
    [HttpPost("api/ai/councils/{councilId:guid}/proposals/{proposalId:guid}/review-kit")]
    public async Task<IActionResult> ReviewKit(Guid councilId, Guid proposalId)
    {
        var userId = CurrentUserId;
        var roles = CurrentRoles;
        var isStaffOrAdmin = User.IsInRole("Admin") || User.IsInRole("Staff");

        // Mỗi nhánh một scope riêng — xem chú thích trên về DbContext.
        var summaryTask = RunScoped(async sp =>
        {
            var summaries = sp.GetRequiredService<IAiSummaryService>();
            // PI submit đã xếp hàng sinh sẵn. Có cache thì dùng ngay, không đốt thêm một request
            // Gemini chỉ vì reviewer bấm gợi ý điểm.
            return await summaries.GetAsync(proposalId)
                ?? await summaries.GenerateAsync(proposalId, userId, roles);
        });

        var suggestionTask = RunScoped(async sp =>
            await sp.GetRequiredService<IAiAdvisorService>()
                .SuggestScoresAsync(councilId, proposalId, userId, isStaffOrAdmin));

        await Task.WhenAll(summaryTask, suggestionTask);

        var suggestions = suggestionTask.Result.Value;
        var suggestionError = suggestionTask.Result.Error;
        if (suggestions == null)
        {
            // Gemini lỗi lúc demo nhưng trước đó đã chạy thành công: giữ nguyên bản gần nhất thay
            // vì xoá trắng màn chấm. Thông báo vẫn cho biết đây là cache cũ.
            var cached = await _advisor.GetScoreSuggestionsAsync(councilId, proposalId, userId, isStaffOrAdmin);
            if (cached != null)
            {
                suggestions = cached;
                suggestionError = "AI đang quá tải; hệ thống đang hiển thị gợi ý đã lưu gần nhất.";
            }
        }

        var kit = new ReviewKitDto
        {
            Summary = summaryTask.Result.Value,
            SummaryError = summaryTask.Result.Error,
            Suggestions = suggestions ?? [],
            SuggestionsError = suggestionError
        };

        return Ok(ApiResponse<ReviewKitDto>.Ok(kit));
    }

    /// <summary>Chạy một việc trong scope DI riêng, nuốt lỗi thành thông điệp thay vì ném ra.</summary>
    private Task<(T? Value, string? Error)> RunScoped<T>(Func<IServiceProvider, Task<T>> work) =>
        Task.Run(async () =>
        {
            using var scope = _scopes.CreateScope();
            try
            {
                return (await work(scope.ServiceProvider), (string?)null);
            }
            catch (Exception ex)
            {
                return (default(T), ex.Message);
            }
        });
}
