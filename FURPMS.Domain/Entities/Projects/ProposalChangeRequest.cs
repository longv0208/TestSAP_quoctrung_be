using FURPMS.Domain.Entities.Users;

namespace FURPMS.Domain.Entities.Projects;

// Yêu cầu thay đổi đề tài (PI gửi → Staff/Admin duyệt) — KHÁC amendment theo hợp đồng:
// đây là thay đổi ở mức ĐỀ TÀI (gia hạn, nội dung, nhân sự, kinh phí, tạm dừng),
// dùng cho cả giai đoạn trước khi ký hợp đồng.
public class ProposalChangeRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProjectId { get; set; }
    public int Type { get; set; }                    // 1 ExtendTime · 2 ContentChange · 3 PersonnelChange · 4 BudgetChange · 5 Suspend
    public string Description { get; set; } = null!;
    public string? NewValue { get; set; }
    public string Status { get; set; } = "Pending";  // Pending / Approved / Rejected (PascalCase — khớp FE)
    public string? AdminNote { get; set; }
    public Guid RequestedBy { get; set; }
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public Guid? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }

    public Project Project { get; set; } = null!;
    public User RequestedByUser { get; set; } = null!;
    public User? ReviewedByUser { get; set; }
}
