using FURPMS.Application.DTOs.Councils;

namespace FURPMS.Application.DTOs.ReviewRounds;

// 1 phát trả toàn bộ dữ liệu màn "Hội đồng & Chấm" của 1 lĩnh vực trong 1 đợt:
// đề tài của track + các vòng của track + hội đồng trong từng vòng.
public class ReviewBoardDto
{
    public List<ReviewBoardProjectDto> Projects { get; set; } = new();
    public List<ReviewBoardRoundDto> Rounds { get; set; } = new();
}

public class ReviewBoardProjectDto
{
    public Guid ProjectId { get; set; }
    public Guid ProposalId { get; set; }
    public string TitleVi { get; set; } = null!;
    public string ProjectStatus { get; set; } = null!;

    /// <summary>
    /// Chủ nhiệm đề tài — để giao diện <b>loại sẵn</b> họ khỏi danh sách chọn ủy viên hội đồng.
    /// <para>
    /// COI (rule #5) đã được chặn ở tầng dịch vụ, nhưng chặn ở tầng đó nghĩa là Staff vẫn thấy tên
    /// chủ nhiệm trong danh sách, chọn xong mới ăn lỗi — vừa mất công vừa dễ hiểu nhầm là hệ thống
    /// hỏng. Có sẵn id ở đây thì danh sách không bao giờ hiện người không được phép.
    /// </para>
    /// </summary>
    public Guid PiUserId { get; set; }
}

public class ReviewBoardRoundDto
{
    public Guid Id { get; set; }
    public int RoundNumber { get; set; }
    public string Dimension { get; set; } = null!;
    public string RoundType { get; set; } = null!;
    public string Status { get; set; } = null!;
    public string? Result { get; set; }
    public int? RubricTemplateId { get; set; }   // bộ tiêu chí gắn RIÊNG cho vòng này (null = theo đợt/lĩnh vực)
    public bool CanDelete { get; set; }

    /// <summary>Hạn chấm HIỆU LỰC (đã tính gia hạn) — null = chưa đặt hạn.</summary>
    public string? ScoringDeadline { get; set; }
    /// <summary>Chỉ true khi vòng còn MỞ và đã quá hạn hiệu lực — vòng đã chốt thì hạn hết ý nghĩa.</summary>
    public bool IsScoringOverdue { get; set; }
    /// <summary>Số ngày còn lại tới hạn, âm = quá hạn — máy chủ tính (xem <c>DeadlineMath</c>), FE không tự trừ ngày.</summary>
    public int? ScoringDaysLeft { get; set; }

    public List<ReviewBoardProjectRoundDto> Projects { get; set; } = new();
    public List<ReviewBoardCouncilDto> Councils { get; set; } = new();
}

public class ReviewBoardProjectRoundDto
{
    public Guid ProjectId { get; set; }
    public string TitleVi { get; set; } = null!;
    public string Status { get; set; } = null!;
    public string? Result { get; set; }

    /// <summary>Chủ nhiệm đề tài — giao diện dùng để loại khỏi danh sách chọn ủy viên (xem <see cref="ReviewBoardProjectDto.PiUserId"/>).</summary>
    public Guid PiUserId { get; set; }
}

public class ReviewBoardCouncilDto
{
    public Guid Id { get; set; }
    public string Status { get; set; } = null!;
    public List<Guid> ProjectIds { get; set; } = new();
    public IList<CouncilMemberResponse> Members { get; set; } = new List<CouncilMemberResponse>();
}
