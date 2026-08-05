namespace FURPMS.Application.DTOs.AI;

public class AiSummaryDto
{
    public string Id { get; set; } = null!;
    public string ProposalId { get; set; } = null!;
    public string SummaryText { get; set; } = null!;
    public bool IsEditedByHuman { get; set; }
    public string? EditedText { get; set; }
    public DateTime GeneratedAt { get; set; }
    /// <summary>`file+form` nếu AI đọc được file đề cương gốc; `textFields` nếu chỉ có form.</summary>
    public string? Source { get; set; }
    public string? SourceFileName { get; set; }

    // BM04 mục "ý kiến thành viên" cần nêu rõ mạnh/yếu — thầy 05/08: tóm tắt phải gồm
    // "tên đề tài, tóm tắt thông tin, ưu điểm, nhược điểm".
    public string? Title { get; set; }
    public List<string> Strengths { get; set; } = new();
    public List<string> Weaknesses { get; set; } = new();
}

public class UpdateSummaryRequest
{
    public string EditedText { get; set; } = null!;
}
