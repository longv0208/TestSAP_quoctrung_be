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

    public AdminController(IClock clock, IDeadlineReminderScanner scanner)
    {
        _clock = clock;
        _scanner = scanner;
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
