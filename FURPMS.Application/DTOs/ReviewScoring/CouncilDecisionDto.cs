namespace FURPMS.Application.DTOs.ReviewScoring;

public class CouncilDecisionDto
{
    public int Id { get; set; }
    public Guid CouncilId { get; set; }
    public int TotalMembers { get; set; }
    public int AttendingMembers { get; set; }
    public int ValidBallots { get; set; }
    public int InvalidBallots { get; set; }
    public decimal? AverageScore { get; set; }
    public string Result { get; set; } = null!;
    public string? CouncilComments { get; set; }
    public string? Recommendations { get; set; }
    public DateTime? FinalizedAt { get; set; }
    public List<QaEntryDto> QaEntries { get; set; } = new();
    public List<MemberOpinionDto> MemberOpinions { get; set; } = new();
}

// BM04/BM12 mục II.1 — 1 lượt hỏi–đáp trong biên bản (cách ghi Q&A).
public class QaEntryDto
{
    public string? AskedBy { get; set; }
    public string Question { get; set; } = null!;
    public string? Answer { get; set; }
    public int Order { get; set; }
}

// BM04/BM12 mục II.1 — ý kiến 1 thành viên (2 cột chuyên môn / kinh phí).
public class MemberOpinionDto
{
    public string MemberName { get; set; } = null!;
    public string? AcademicComment { get; set; }
    public string? BudgetComment { get; set; }
    public int Order { get; set; }
}
