namespace FURPMS.Application.DTOs.AI;

/// <summary>
/// Một góp ý của AI cho đề cương. <c>Category</c> là nhóm vấn đề (Mục tiêu, Phương pháp,
/// Sản phẩm, Kinh phí…), <c>Suggestion</c> là gợi ý chỉnh sửa cụ thể.
/// AI chỉ GỢI Ý — PI tự quyết sửa hay không.
/// </summary>
public class AiFeedbackDto
{
    public string Category { get; set; } = null!;
    public string Suggestion { get; set; } = null!;
}

/// <summary>
/// Một điểm LỆCH giữa thông tin PI điền vào form và nội dung file đề cương họ nộp
/// (thầy 29/07: *"cho AI coi lại mấy cái PI điền vô và so với proposal của họ xem có sai sót gì"*).
/// </summary>
public class AiConsistencyIssueDto
{
    /// <summary>Nhãn trường tiếng Việt: "Mục tiêu", "Thời gian thực hiện"…</summary>
    public string Field { get; set; } = null!;
    /// <summary>MISSING (form thiếu) · MISMATCH (lệch nội dung) · EXTRA (form có, file không có).</summary>
    public string Kind { get; set; } = null!;
    public string Detail { get; set; } = null!;
}

/// <summary>Kết quả đối chiếu form ↔ file.</summary>
public class AiConsistencyResultDto
{
    /// <summary>Tên file đã dùng để đối chiếu — để PI biết AI đọc bản nào.</summary>
    public string? FileName { get; set; }
    /// <summary>Không có file đính kèm ⇒ không đối chiếu được; FE hiện lời nhắc thay vì báo lỗi.</summary>
    public bool HasFile { get; set; }
    public IReadOnlyList<AiConsistencyIssueDto> Issues { get; set; } = new List<AiConsistencyIssueDto>();
}

/// <summary>Gợi ý điểm cho MỘT tiêu chí trong bộ tiêu chí của vòng chấm.</summary>
public class AiScoreSuggestionDto
{
    public int CriterionId { get; set; }
    public string CriterionName { get; set; } = null!;
    public decimal MaxScore { get; set; }
    /// <summary>Điểm AI đề xuất, đã kẹp trong [0, MaxScore].</summary>
    public decimal SuggestedScore { get; set; }
    /// <summary>Lý do ngắn — người chấm đọc để đối chiếu, KHÔNG phải kết luận.</summary>
    public string Comment { get; set; } = null!;
}
