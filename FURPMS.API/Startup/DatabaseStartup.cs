using FURPMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace FURPMS.API.Startup;

/// <summary>
/// Chạy migration + seed lúc khởi động, <b>có thử lại</b> khi chưa kết nối được cơ sở dữ liệu.
///
/// <para>
/// <b>Vì sao phải thử lại.</b> Mạng nội bộ của Railway (<c>*.railway.internal</c>) mất <b>vài giây</b>
/// mới sẵn sàng sau khi container khởi động. Ứng dụng gọi <c>Migrate()</c> ngay dòng đầu thì DNS
/// chưa phân giải được tên máy chủ ⇒ <c>SocketException: Name or service not known</c> ⇒ tiến trình
/// chết ⇒ Railway đánh dấu Crashed. Nhìn log thì tưởng sai cấu hình, thực ra chỉ là <b>vào sớm quá</b>.
/// </para>
/// <para>
/// Chuyện này không riêng Railway: DB khởi động chậm hơn app là cảnh bình thường ở mọi nơi dùng
/// container (docker-compose, Kubernetes). Chết hẳn vì cơ sở dữ liệu bận vài giây là hành vi sai.
/// </para>
/// </summary>
public static class DatabaseStartup
{
    /// <summary>Số lần thử và khoảng chờ — tổng ~30 giây, đủ cho mọi trường hợp đã gặp.</summary>
    private const int MaxAttempts = 10;
    private static readonly TimeSpan Delay = TimeSpan.FromSeconds(3);

    public static async Task MigrateAndSeedAsync(WebApplication app)
    {
        var log = app.Services.GetRequiredService<ILoggerFactory>().CreateLogger("DbStartup");

        for (var attempt = 1; attempt <= MaxAttempts; attempt++)
        {
            try
            {
                using var scope = app.Services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<FURPMSDbContext>();

                await db.Database.MigrateAsync();

                var seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
                await seeder.SeedAsync();

                // Dữ liệu demo KHÔNG được phép chặn ứng dụng khởi động: trên bản deploy, seeder đổ
                // là API sập và giao diện mất luôn backend. Hỏng thì ghi log rồi chạy tiếp.
                try
                {
                    var demoSeeder = scope.ServiceProvider.GetRequiredService<DemoScenarioSeeder>();
                    await demoSeeder.SeedAsync();
                }
                catch (Exception ex)
                {
                    log.LogWarning(ex, "Không dựng được dữ liệu demo — ứng dụng vẫn khởi động bình thường.");
                }

                if (attempt > 1)
                    log.LogInformation("Kết nối được cơ sở dữ liệu ở lần thử {Attempt}.", attempt);
                return;
            }
            catch (Exception ex) when (IsTransient(ex) && attempt < MaxAttempts)
            {
                log.LogWarning(
                    "Chưa kết nối được cơ sở dữ liệu (lần {Attempt}/{Max}): {Reason}. Thử lại sau {Delay} giây…",
                    attempt, MaxAttempts, Describe(ex), Delay.TotalSeconds);
                await Task.Delay(Delay);
            }
        }

        // Hết lượt vẫn không được ⇒ để lỗi thoát ra, KHÔNG chạy tiếp với DB hỏng. Chạy tiếp thì mọi
        // request đều lỗi 500 mà nguyên nhân thật đã trôi mất khỏi log.
        log.LogError(
            "Không kết nối được cơ sở dữ liệu sau {Max} lần thử trong {Seconds} giây. " +
            "Kiểm tra ConnectionStrings__DefaultConnection, và xem service cơ sở dữ liệu có nằm " +
            "CÙNG project trên Railway không (mạng nội bộ không thông giữa hai project).",
            MaxAttempts, MaxAttempts * Delay.TotalSeconds);

        using var last = app.Services.CreateScope();
        await last.ServiceProvider.GetRequiredService<FURPMSDbContext>().Database.MigrateAsync();
    }

    /// <summary>
    /// Lỗi có đáng thử lại không. Chỉ tính lỗi <b>mạng/kết nối</b> — sai mật khẩu hay sai tên
    /// database thì thử lại 10 lần cũng vô ích, chỉ làm chậm 30 giây rồi vẫn chết.
    /// </summary>
    private static bool IsTransient(Exception ex)
    {
        for (var e = ex; e != null; e = e.InnerException)
        {
            if (e is System.Net.Sockets.SocketException) return true;
            if (e is TimeoutException) return true;
            // Npgsql gói lỗi mạng trong NpgsqlException; PostgresException là lỗi từ chính máy chủ
            // (sai mật khẩu, thiếu quyền) nên KHÔNG thử lại.
            if (e.GetType().Name == "NpgsqlException") return true;
        }
        return false;
    }

    private static string Describe(Exception ex)
    {
        for (var e = ex; e != null; e = e.InnerException)
            if (e is System.Net.Sockets.SocketException se) return se.Message;
        return ex.Message;
    }
}
