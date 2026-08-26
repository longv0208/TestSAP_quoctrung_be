using FURPMS.Application.DTOs.Councils;

namespace FURPMS.Application.DTOs.ReviewRounds;

public class ReviewRoundResponse
{
    public Guid Id { get; set; }
    public int RoundNumber { get; set; }
    public string Dimension { get; set; } = null!;
    public string RoundType { get; set; } = null!;
    public int? RubricTemplateId { get; set; }
    public int Sequence { get; set; }
    public Guid? PrerequisiteRoundId { get; set; }
    public string Status { get; set; } = null!;
    public DateTime? OpenedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public string? Result { get; set; }

    /// <summary>
    /// Hạn hội đồng phải chấm xong vòng này. <c>null</c> = chưa đặt (vòng tạo trước 25/08).
    /// Đây là hạn HIỆU LỰC — đã tính các lần dời hạn ghi ở <c>deadline_extension</c>.
    /// </summary>
    public string? ScoringDeadline { get; set; }

    /// <summary>Vòng còn mở mà đã quá hạn chấm. Chỉ để gắn cờ — hệ thống KHÔNG tự đóng vòng (rule #12).</summary>
    public bool IsScoringOverdue { get; set; }

    public Guid? CouncilId { get; set; }
    public IList<CouncilMemberResponse> Members { get; set; } = new List<CouncilMemberResponse>();
}
