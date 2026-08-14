using FURPMS.Application.Settings;

namespace FURPMS.API.Startup;

/// <summary>
/// Tự soi cấu hình khi khởi động và <b>kêu to</b> những thứ chỉ hỏng trên máy chủ.
///
/// <para>
/// <b>Vì sao cần.</b> Mọi giá trị dưới đây đều <i>đúng cho máy dev</i> nên chạy local không bao giờ
/// lộ ra. Lên máy chủ thì hỏng âm thầm: người dùng bấm link trong email ra <c>localhost</c>, tệp
/// đính kèm biến mất sau mỗi lần redeploy, DB thật đầy đề tài giả. Không cái nào báo lỗi cả — chỉ
/// sai kết quả.
/// </para>
/// <para>
/// Chỉ <b>ghi cảnh báo</b> chứ không chặn khởi động: thiếu cấu hình mà chặn thì mất luôn API,
/// tệ hơn là chạy với cấu hình chưa chuẩn.
/// </para>
/// </summary>
public static class ProductionReadinessCheck
{
    public static void Run(WebApplication app)
    {
        // Chỉ soi khi KHÔNG phải môi trường phát triển — ở local những giá trị này đều đúng.
        if (app.Environment.IsDevelopment()) return;

        var log = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("DeployCheck");
        var cfg = app.Configuration;
        var problems = new List<string>();

        // ── Link trong email ────────────────────────────────────────────────
        var frontendUrl = cfg["EmailSettings:FrontendUrl"] ?? "";
        if (frontendUrl.Contains("localhost", StringComparison.OrdinalIgnoreCase))
            problems.Add(
                $"EmailSettings__FrontendUrl đang là '{frontendUrl}' — mọi nút \"Xem chi tiết\" trong " +
                "email sẽ trỏ về máy người gửi, người nhận bấm vào không ra gì. Đặt URL thật của giao diện.");

        // ── Hộp thư hứng mail của địa chỉ giả ───────────────────────────────
        var catchInbox = cfg["EmailSettings:CatchFakeMailInbox"];
        if (!string.IsNullOrWhiteSpace(catchInbox))
            problems.Add(
                $"EmailSettings__CatchFakeMailInbox đang bật ('{catchInbox}') — thư của các miền cấu hình " +
                "trong RedirectDomains sẽ KHÔNG tới người nhận thật. Bỏ hẳn hai khoá này trên máy chủ.");

        // ── Nơi lưu tệp ─────────────────────────────────────────────────────
        if (string.IsNullOrWhiteSpace(cfg["Cloudinary:CloudName"]))
            problems.Add(
                "Chưa cấu hình Cloudinary__* — tệp đính kèm rơi về đĩa của máy chủ, mà đĩa đó là TẠM: " +
                "redeploy một lần là mất sạch tài liệu đã tải lên (DB vẫn trỏ tới chúng nên mọi link thành 404).");

        // ── Khoá bí mật vẫn là giá trị mẫu ──────────────────────────────────
        var jwt = cfg["JwtSettings:SecretKey"] ?? "";
        if (jwt.Contains("Capstone", StringComparison.OrdinalIgnoreCase) || jwt.Length < 32)
            problems.Add(
                "JwtSettings__SecretKey vẫn là khoá mẫu trong repo — ai đọc mã nguồn cũng tự ký được " +
                "token hợp lệ và đăng nhập bằng bất kỳ tài khoản nào. Đặt khoá riêng dài ≥ 32 ký tự.");

        // ── AI ──────────────────────────────────────────────────────────────
        if (string.IsNullOrWhiteSpace(cfg["GeminiAI:ApiKey"]))
            problems.Add("Chưa cấu hình GeminiAI__ApiKey — mọi tính năng AI sẽ báo lỗi khi người dùng bấm.");

        if (problems.Count == 0)
        {
            log.LogInformation("Kiểm tra cấu hình triển khai: không phát hiện vấn đề.");
            return;
        }

        log.LogWarning(
            "═══ CẤU HÌNH TRIỂN KHAI CÓ {Count} VẤN ĐỀ — hệ thống vẫn chạy nhưng sẽ sai âm thầm ═══",
            problems.Count);
        for (var i = 0; i < problems.Count; i++)
            log.LogWarning("  {Index}. {Problem}", i + 1, problems[i]);
        log.LogWarning("═══ Sửa ở phần Variables của Railway/Render, không phải trong mã nguồn ═══");
    }
}
