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
