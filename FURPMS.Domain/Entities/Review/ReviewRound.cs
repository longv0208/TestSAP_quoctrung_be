using FURPMS.Domain.Entities.Cycles;
using FURPMS.Domain.Entities.Financial;

namespace FURPMS.Domain.Entities.Review;

// Phase B (Review 2 điểm c): round thuộc TRACK-trong-đợt (cycle_track), KHÔNG thuộc
// từng đề tài. Đề tài tham gia round qua bảng nối ProjectRound (nhiều-nhiều).
public class ReviewRound
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int CycleTrackId { get; set; }
    public int RoundNumber { get; set; }
    public string Dimension { get; set; } = null!;       // SCIENCE / FINANCE
    public string RoundType { get; set; } = null!;        // SCREENING / REVIEW / ACCEPTANCE
    public int? RubricTemplateId { get; set; }            // bộ tiêu chí riêng của round
    public int Sequence { get; set; }
    public Guid? PrerequisiteRoundId { get; set; }
    public string Status { get; set; } = "PENDING";       // PENDING / OPEN / PASSED / FAILED
    public DateTime? OpenedAt { get; set; }
    public DateTime? ClosedAt { get; set; }

    /// <summary>
    /// Hạn hội đồng phải chấm xong vòng này (thêm 25/08).
    ///
    /// <para>Hội đồng bảo vệ lần 2 yêu cầu *"thể hiện rõ các mốc thời gian deadline cho các giai
    /// đoạn của 1 đề tài"* — mà giai đoạn CHẤM trước đây là chặng duy nhất **không có hạn nào cả**:
    /// vòng mở ra rồi để đấy, không ai biết bao giờ phải xong.</para>
    ///
    /// <para><c>null</c> = chưa đặt hạn (mọi vòng tạo trước 25/08 đều vậy) — giao diện hiện "chưa
    /// đặt hạn", KHÔNG bịa ra một ngày. Mở vòng mới thì tự đặt theo
    /// <c>SCORING_WINDOW_DAYS</c>.</para>
    ///
    /// <para>⚠️ Quá hạn thì vòng bị <b>gắn cờ</b>, hệ thống <b>không tự đóng</b> — kết luận Đạt/Không
    /// đạt là quyết định của Chủ tịch hội đồng (rule #12). Dời hạn thì ghi
    /// <c>deadline_extension</c>, không ghi đè (rule #19).</para>
    /// </summary>
    public DateOnly? ScoringDeadline { get; set; }
    public string? Result { get; set; }

    public CycleTrack CycleTrack { get; set; } = null!;
    public RubricTemplate? RubricTemplate { get; set; }
    public ReviewRound? PrerequisiteRound { get; set; }
    public ICollection<ProjectRound> ProjectRounds { get; set; } = new List<ProjectRound>();
    public ICollection<ReviewCouncil> Councils { get; set; } = new List<ReviewCouncil>();
}
