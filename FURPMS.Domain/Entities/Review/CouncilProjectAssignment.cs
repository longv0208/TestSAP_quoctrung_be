using FURPMS.Domain.Entities.Projects;

namespace FURPMS.Domain.Entities.Review;

// Phase B (Review 2 điểm c): 1 hội đồng chấm 1 NHÓM đề tài trong round
// (nhiều council chạy song song / round do số lượng bài nộp).
public class CouncilProjectAssignment
{
    public int Id { get; set; }
    public Guid CouncilId { get; set; }
    public Guid ProjectId { get; set; }

    public ReviewCouncil Council { get; set; } = null!;
    public Project Project { get; set; } = null!;
}
