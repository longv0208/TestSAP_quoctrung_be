namespace FURPMS.Application.DTOs.Councils;

// Slot theo đề tài (rule tuần 10): khung giờ con của từng đề tài trong buổi họp hội đồng.
public class CouncilSlotDto
{
    public Guid ProjectId { get; set; }
    public string ProjectTitle { get; set; } = null!;
    public Guid? MeetingId { get; set; }
    public DateTime? SlotStartAt { get; set; }
    public int? SlotDurationMinutes { get; set; }
    public int? SlotOrder { get; set; }
}

/// <summary>
/// Khung giờ buổi họp + tình hình đã xếp — để màn lịch chấm hiện "đã xếp 60/90 phút" thay vì
/// bắt Staff tự cộng, rồi tới lúc lưu mới ăn 400 vì tràn giờ.
/// </summary>
public class CouncilSlotBoardDto
{
    public Guid? MeetingId { get; set; }
    public DateTime? MeetingStartAt { get; set; }
    public int? MeetingDurationMinutes { get; set; }
    public int AssignedMinutes { get; set; }
    public List<CouncilSlotDto> Slots { get; set; } = new();
}

public class SlotEntryDto
{
    public Guid ProjectId { get; set; }
    public DateTime? SlotStartAt { get; set; }
    public int? SlotDurationMinutes { get; set; }
    public int? SlotOrder { get; set; }
}

public class SaveSlotsRequest
{
    public List<SlotEntryDto> Entries { get; set; } = new();
}
