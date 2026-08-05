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

/// <summary>
/// Một phiếu của một thành viên hội đồng — dùng cho mục **"Kết quả bỏ phiếu đánh giá"**
/// (QĐ543 **BM12 mục 10.1**), và cho màn Thư ký soạn biên bản.
/// </summary>
public class MemberBallotDto
{
    public Guid MemberId { get; set; }
    public string MemberName { get; set; } = null!;
    public string? MemberRole { get; set; }          // Chủ tịch / Thư ký / Phản biện / Thành viên
    public bool HasSubmitted { get; set; }
    public bool IsValidBallot { get; set; }
    public decimal? TotalScore { get; set; }         // vòng XÉT DUYỆT — chấm điểm (BM03)
    public decimal? MaxScore { get; set; }
    public string? Result { get; set; }              // vòng NGHIỆM THU — Đạt/Không đạt (BM11)
    public string? Comments { get; set; }
    public DateTime? SubmittedAt { get; set; }
}

/// <summary>
/// BM12 mục 10.1 — *"Số phiếu phát ra / thu về / hợp lệ / không hợp lệ; Kết quả đánh giá:
/// Đạt … Không đạt …"*. Trước đây màn biên bản chỉ có 4 ô (thành viên, có mặt, phiếu hợp lệ,
/// điểm TB) nên Thư ký không có số để điền vào biểu mẫu.
/// </summary>
public class BallotTallyDto
{
    public Guid CouncilId { get; set; }
    public Guid ProjectId { get; set; }
    public bool IsAcceptanceRound { get; set; }
    public int TotalMembers { get; set; }            // = số phiếu phát ra
    public int BallotsReturned { get; set; }
    public int ValidBallots { get; set; }
    public int InvalidBallots { get; set; }
    public int PassCount { get; set; }
    public int FailCount { get; set; }
    public decimal? AverageScore { get; set; }
    public List<MemberBallotDto> Ballots { get; set; } = new();
}
