namespace FURPMS.Application.Interfaces;

public interface IClock
{
    DateTime UtcNow { get; }

    // Số ngày tua tới so với thời gian thực (dùng cho test các mốc hạn dài ngày).
    // Có thể chỉnh lúc chạy qua endpoint admin; mặc định 0 = thời gian thật.
    int OffsetDays { get; set; }
}
