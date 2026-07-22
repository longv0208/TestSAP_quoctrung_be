using FURPMS.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace FURPMS.Infrastructure.Services;

// Singleton — giữ offset có thể chỉnh lúc chạy (mặc định lấy từ config SYSTEM_CLOCK_OFFSET_DAYS).
public class SystemClock : IClock
{
    public SystemClock(IConfiguration config)
    {
        OffsetDays = config.GetValue<int>("SYSTEM_CLOCK_OFFSET_DAYS", 0);
    }

    public int OffsetDays { get; set; }

    public DateTime UtcNow => DateTime.UtcNow.AddDays(OffsetDays);
}
