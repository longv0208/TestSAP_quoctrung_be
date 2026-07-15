using FURPMS.Domain.Entities.Projects;

namespace FURPMS.Domain.Entities.Review;

// Phase B (Review 2 điểm c): Project–Round NHIỀU-NHIỀU — kết quả chấm của TỪNG đề tài
// trong round nằm ở đây; đạt round này mới được vào round sau.
public class ProjectRound
{
    public int Id { get; set; }
    public Guid ProjectId { get; set; }
    public Guid RoundId { get; set; }
    public string Status { get; set; } = "PENDING";   // PENDING / PASSED / FAILED / REVISION
    public string? Result { get; set; }
    public DateTime? FinalizedAt { get; set; }

    public Project Project { get; set; } = null!;
    public ReviewRound Round { get; set; } = null!;
}
