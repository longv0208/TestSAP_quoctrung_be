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

    /// <summary>Tổng số đề tài hội đồng này phải chấm.</summary>
    public int ProjectCount { get; set; }
    /// <summary>Số đề tài CHƯA được chia khung giờ.</summary>
    public int UnscheduledCount { get; set; }
    /// <summary>Thời lượng buổi họp còn trống sau khi trừ các khung đã chia. Âm = đã vượt.</summary>
    public int? RemainingMinutes { get; set; }

    /// <summary>
    /// Cảnh báo cho Staff, <c>null</c> nếu không có gì bất thường. **Cảnh báo chứ không chặn** —
    /// rule #17 cho đổi lịch bất kỳ lúc nào, nên khoá cứng ở đây sẽ cản đúng thao tác hợp lệ.
    /// <para>
    /// Có vì thứ tự thao tác thực tế: Staff đặt lịch họp trước, rồi mới gán thêm đề tài 2, 3 vào
    /// cùng hội đồng. Trước đây không có gì nhắc, nên buổi họp 90 phút gán 5 đề tài vẫn lưu được
    /// và chỉ vỡ ra vào đúng hôm họp.
    /// </para>
    /// </summary>
    public string? Warning { get; set; }
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
