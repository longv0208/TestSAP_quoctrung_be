using FURPMS.Application.Common;
using FURPMS.Application.Interfaces;
using FURPMS.Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FURPMS.API.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController : ControllerBase
{
    private readonly IClock _clock;
    private readonly IDeadlineReminderScanner _scanner;
    private readonly IWebHostEnvironment _env;

    public AdminController(IClock clock, IDeadlineReminderScanner scanner, IWebHostEnvironment env)
    {
        _clock = clock;
        _scanner = scanner;
        _env = env;
    }

    /// <summary>
    /// Tua thời gian là công cụ TEST. Offset lưu ở singleton phía server nên ảnh hưởng
    /// MỌI người dùng cùng lúc — bật nhầm trên production là toàn hệ thống lệch ngày
    /// (hạn nộp, nhắc hạn, ngày ký…). Vì vậy chặn cứng theo môi trường, không chỉ theo vai trò.
    /// Muốn bật ở môi trường staging: đặt biến môi trường ASPNETCORE_ENVIRONMENT=Staging.
    /// </summary>
    private void EnsureTimeTravelAllowed()
    {
        if (_env.IsProduction())
            throw new ForbiddenException(
                "Không được tua thời gian trên môi trường production — đây là công cụ test.");
    }

    public class SystemClockResponse
    {
        public int OffsetDays { get; set; }
        public DateTime EffectiveNow { get; set; }
        public DateTime RealNow { get; set; }
    }

    public class SetClockRequest
    {
        public int OffsetDays { get; set; }
    }

    // Xem mốc thời gian hiện tại của hệ thống (đã cộng offset test).
    [HttpGet("system-clock")]
    public IActionResult GetSystemClock()
    {
        return Ok(ApiResponse<SystemClockResponse>.Ok(BuildResponse()));
    }

    // Đặt offset (số ngày tua tới). 0 = thời gian thật. Dùng để test các mốc hạn dài ngày.
    [HttpPost("system-clock")]
    public IActionResult SetSystemClock([FromBody] SetClockRequest request)
    {
        EnsureTimeTravelAllowed();
        if (request.OffsetDays < 0)
            throw new ArgumentException("OffsetDays phải >= 0.");
        if (request.OffsetDays > 3650)
            throw new ArgumentException("OffsetDays tối đa 3650 (10 năm).");

        _clock.OffsetDays = request.OffsetDays;
        return Ok(ApiResponse<SystemClockResponse>.Ok(BuildResponse()));
    }

    // Chạy ngay tác vụ quét nhắc hạn (bình thường chạy mỗi 24h) để test không phải chờ.
    [HttpPost("run-deadline-scan")]
    public async Task<IActionResult> RunDeadlineScan()
    {
        await _scanner.ScanAsync();
        return Ok(ApiResponse.Ok("Đã chạy quét nhắc hạn theo mốc thời gian hiện tại."));
    }

    private SystemClockResponse BuildResponse() => new()
    {
        OffsetDays = _clock.OffsetDays,
        EffectiveNow = _clock.UtcNow,
        RealNow = DateTime.UtcNow
    };
}
