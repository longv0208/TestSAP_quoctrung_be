using FURPMS.Domain.Entities.Users;

namespace FURPMS.Domain.Entities.Cycles;

// Log gia hạn deadline (rule tuần 10 — thầy Đức): KHÔNG ghi đè ngày gốc, mỗi lần gia hạn = 1 dòng.
// Chủ yếu cấp ĐỢT (TargetType="CYCLE"); TargetType mở rộng được cho PROJECT/CONTRACT sau.
// Deadline hiệu lực = NewDeadline của bản mới nhất (gốc giữ nguyên ở ResearchCycle.SubmissionDeadline).
public class DeadlineExtension
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string TargetType { get; set; } = null!;   // CYCLE | PROJECT | CONTRACT
    public string TargetId { get; set; } = null!;
    public DateOnly OldDeadline { get; set; }
    public DateOnly NewDeadline { get; set; }
    public string? Reason { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public User CreatedByUser { get; set; } = null!;
}
