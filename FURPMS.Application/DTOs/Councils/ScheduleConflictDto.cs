namespace FURPMS.Application.DTOs.Councils;

// Cảnh báo 1 giảng viên bị xếp vào 2 hội đồng có lịch họp GIAO GIỜ (rule tuần 10 — thầy Đức).
public class ScheduleConflictDto
{
    public Guid MemberUserId { get; set; }
    public string MemberName { get; set; } = null!;
    public Guid OtherCouncilId { get; set; }
    public string? OtherCouncilType { get; set; }
    public DateTime ThisMeetingAt { get; set; }
    public DateTime OtherMeetingAt { get; set; }
}
