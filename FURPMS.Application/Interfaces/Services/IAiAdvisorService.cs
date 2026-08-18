using FURPMS.Application.DTOs.AI;

namespace FURPMS.Application.Interfaces.Services;

/// <summary>
/// Các tính năng AI mang tính "trợ lý": góp ý đề cương cho PI và gợi ý chấm điểm cho
/// thành viên hội đồng. Kết quả luôn là GỢI Ý — người dùng quyết định cuối (rule #12:
/// kết quả là quyết định của Chủ tịch, hệ thống không tự chốt).
/// <para>Mọi kết quả được lưu vào <c>llm_outputs</c> để xem lại không phải gọi Gemini lần nữa.</para>
/// </summary>
public interface IAiAdvisorService
{
    /// <summary>Lấy góp ý đã sinh trước đó (null nếu chưa có) — không gọi Gemini.</summary>
    Task<IReadOnlyList<AiFeedbackDto>?> GetProposalFeedbackAsync(Guid proposalId, Guid userId, IEnumerable<string> roles);

    /// <summary>Sinh góp ý mới cho đề cương (gọi Gemini, ghi đè bản cũ).</summary>
    Task<IReadOnlyList<AiFeedbackDto>> GenerateProposalFeedbackAsync(Guid proposalId, Guid userId, IEnumerable<string> roles);

    /// <summary>
    /// Đối chiếu thông tin PI đã điền với FILE đề cương họ đính kèm, chỉ ra chỗ
    /// thiếu/lệch. Đây là thứ thầy yêu cầu (29/07), khác với góp ý chung ở trên —
    /// cái này AI thật sự đọc file gốc.
    /// </summary>
    Task<AiConsistencyResultDto> CheckConsistencyAsync(Guid proposalId, Guid userId, IEnumerable<string> roles);

    /// <summary>
    /// Gợi ý điểm theo từng tiêu chí của bộ tiêu chí đang áp cho hội đồng này.
    /// Chỉ thành viên hội đồng (hoặc Admin/Staff) được gọi.
    /// </summary>
    Task<IReadOnlyList<AiScoreSuggestionDto>> SuggestScoresAsync(
        Guid councilId, Guid proposalId, Guid userId, bool isStaffOrAdmin);

    /// <summary>Lấy gợi ý điểm gần nhất đã lưu, không gọi Gemini.</summary>
    Task<IReadOnlyList<AiScoreSuggestionDto>?> GetScoreSuggestionsAsync(
        Guid councilId, Guid proposalId, Guid userId, bool isStaffOrAdmin);
}
