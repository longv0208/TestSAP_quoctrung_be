namespace FURPMS.Application.DTOs.Meetings;

public class MeetingDto
{
    public Guid Id { get; set; }
    public Guid CouncilId { get; set; }
    public string? Title { get; set; }
    public string Platform { get; set; } = "IN_PERSON";
    public string? MeetingLink { get; set; }
    public string? Location { get; set; }
    public DateTime ScheduledAt { get; set; }
    public int DurationMinutes { get; set; }
    public string? Agenda { get; set; }
    public DateTime? ActualStartAt { get; set; }
    public DateTime? ActualEndAt { get; set; }
    public string Status { get; set; } = "SCHEDULED";
    public string? CancellationReason { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class MeetingListDto : MeetingDto
{
    public Guid? ProposalId { get; set; }
    public string? ProposalTitle { get; set; }
    public string? RoundType { get; set; }
    public int? RoundNumber { get; set; }
}

// Điểm danh (rule tuần 10): Thư ký tick có mặt/vắng+lý do cho từng TV → điền vào biên bản.
public class AttendanceEntryDto
{
    public Guid MemberId { get; set; }          // = CouncilMember.Id
    public string? MemberName { get; set; }
    public string? MemberRole { get; set; }
    public bool? Attended { get; set; }
    public string? AbsenceReason { get; set; }
}

public class SaveAttendanceRequest
{
    public List<AttendanceEntryDto> Entries { get; set; } = new();
}

/// <summary>
/// Sửa buổi họp đã đặt. Rule #17 (thầy tuần 10): "thay người/đổi lịch **bất kỳ lúc nào**,
/// không đóng băng hội đồng" — nên đây là thao tác bình thường, không phải ngoại lệ.
/// Dùng lại đúng bộ trường của lúc tạo để hai đường không lệch ràng buộc.
/// </summary>
public class UpdateMeetingRequest : ScheduleMeetingRequest;

public class ScheduleMeetingRequest
{
    public string? Title { get; set; }
    public string Platform { get; set; } = "IN_PERSON";
    public string? MeetingLink { get; set; }
    public string? Location { get; set; }
    public DateTime ScheduledAt { get; set; }
    public int DurationMinutes { get; set; } = 120;
    public string? Agenda { get; set; }
}
