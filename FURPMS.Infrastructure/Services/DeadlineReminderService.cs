using FURPMS.Application.Interfaces.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FURPMS.Infrastructure.Services;

public class DeadlineReminderService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DeadlineReminderService> _logger;

    public DeadlineReminderService(
        IServiceScopeFactory scopeFactory,
        ILogger<DeadlineReminderService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // .NET 8 mặc định BackgroundServiceExceptionBehavior.StopHost: exception nào lọt ra khỏi
        // đây là GIẾT cả tiến trình. Việc nhắc hạn không đáng để đánh sập API, nên chặn ở đây.
        try
        {
            await RunAsync(stoppingToken);

            using var timer = new PeriodicTimer(TimeSpan.FromHours(24));
            while (!stoppingToken.IsCancellationRequested && await timer.WaitForNextTickAsync(stoppingToken))
            {
                await RunAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Đang tắt ứng dụng — bình thường.
        }
        catch (Exception ex)
        {
            SafeLog(() => _logger.LogError(ex, "DeadlineReminderService dừng vì lỗi ngoài dự kiến"));
        }
    }

    private async Task RunAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var scanner = scope.ServiceProvider.GetRequiredService<IDeadlineReminderScanner>();
            await scanner.ScanAsync(ct);
            SafeLog(() => _logger.LogInformation("DeadlineReminderService scan complete"));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            SafeLog(() => _logger.LogError(ex, "DeadlineReminderService scan failed"));
        }
    }

    /// <summary>
    /// Ghi log cũng có thể ném lỗi: lúc host đang tắt, provider EventLog đã bị dispose nên
    /// <c>LogError</c> ném <c>ObjectDisposedException</c> ngay trong khối catch — lỗi thoát ra
    /// khỏi <c>ExecuteAsync</c> và đánh sập luôn tiến trình (exit code 0xe0434352). Đã gặp thật:
    /// mở app khi cổng 5068 đang bị chiếm ⇒ host tắt giữa chừng ⇒ lần quét đang chạy mất kết nối
    /// DB ⇒ log lỗi ⇒ chết. Không có gì để làm khi chính đường ghi log hỏng, nên nuốt lặng.
    /// </summary>
    private static void SafeLog(Action write)
    {
        try { write(); }
        catch { /* mất đường ghi log thì thôi, không được để nó giết tiến trình */ }
    }
}
