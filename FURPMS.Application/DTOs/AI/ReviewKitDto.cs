namespace FURPMS.Application.DTOs.AI;

/// <summary>
/// Bộ tài liệu AI cho người chấm — <b>tóm tắt + gợi ý điểm trong MỘT lần bấm</b>.
///
/// <para>
/// <b>Vì sao gộp.</b> Trước đây người chấm phải bấm "Tóm tắt" chờ 30–60 giây, đọc xong mới bấm
/// tiếp "Gợi ý điểm" rồi chờ thêm một lượt nữa — đúng lúc hội đồng đang ngồi nhìn. Chưa kể gói
/// Gemini miễn phí giới hạn số request mỗi phút, bấm hai lần liên tiếp rất dễ bị chặn ngay giữa
/// buổi họp.
/// </para>
///
/// <para>
/// Hai phần chạy <b>song song</b> nên tổng thời gian chờ xấp xỉ một lần gọi, không phải hai.
/// </para>
/// </summary>
public class ReviewKitDto
{
    /// <summary>Tóm tắt đề cương. <c>null</c> nếu phần này lỗi — xem <see cref="SummaryError"/>.</summary>
    public AiSummaryDto? Summary { get; set; }

    /// <summary>Gợi ý điểm từng tiêu chí. Rỗng nếu phần này lỗi — xem <see cref="SuggestionsError"/>.</summary>
    public IReadOnlyList<AiScoreSuggestionDto> Suggestions { get; set; } = [];

    /// <summary>
    /// Vì sao phần tóm tắt không có. Tách riêng từng lỗi thay vì để cả lời gọi đổ vỡ:
    /// một phần hỏng thì phần còn lại vẫn dùng được, và người chấm biết chính xác thiếu cái gì.
    /// </summary>
    public string? SummaryError { get; set; }

    /// <summary>Vì sao phần gợi ý điểm không có.</summary>
    public string? SuggestionsError { get; set; }
}
