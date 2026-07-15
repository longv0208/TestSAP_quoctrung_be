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
    public string? Result { get; set; }

    public CycleTrack CycleTrack { get; set; } = null!;
    public RubricTemplate? RubricTemplate { get; set; }
    public ReviewRound? PrerequisiteRound { get; set; }
    public ICollection<ProjectRound> ProjectRounds { get; set; } = new List<ProjectRound>();
    public ICollection<ReviewCouncil> Councils { get; set; } = new List<ReviewCouncil>();
}
