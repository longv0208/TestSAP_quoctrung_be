using FURPMS.Application.Interfaces.Services;
using FURPMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FURPMS.Infrastructure.Services;

/// <summary>
/// Vector hoá đề cương đã nộp, chạy nền — để rà trùng lặp có dữ liệu ngay khi Phòng QLKH mở ra xem.
///
/// <para>Chép khuôn <see cref="AiSummaryPregenerationService"/>: mọi cái bẫy khó ở đó đã được giải
/// và ghi lại trong chú thích — nhả luồng ngay lúc khởi động, nuốt lỗi để không giết tiến trình,
/// nghỉ giữa hai lần gọi, và quét bù lúc khởi động vì hàng đợi nằm trong bộ nhớ.</para>
/// </summary>
public class EmbeddingWorker : BackgroundService
{
    private readonly IEmbeddingQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<EmbeddingWorker> _logger;

    /// <summary>Nghỉ giữa hai lần gọi để không chạm trần request/phút của gói miễn phí.</summary>
    private static readonly TimeSpan PauseBetweenCalls = TimeSpan.FromSeconds(2);

    /// <summary>Trần quét bù lúc khởi động — đủ vá phần thiếu, không biến khởi động thành đợt nã model.</summary>
    private const int SweepLimit = 50;

    public EmbeddingWorker(
        IEmbeddingQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<EmbeddingWorker> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // NHẢ LUỒNG NGAY — .NET 8 await ExecuteAsync cho tới lần nhả đầu tiên, nên gọi thẳng vòng
        // quét ở đây là web server chưa kịp lắng nghe cổng. Xem chú thích dài ở
        // AiSummaryPregenerationService để biết lỗi này trông như thế nào khi gặp thật.
        await Task.Yield();

        // BackgroundServiceExceptionBehavior.StopHost là mặc định: lỗi lọt ra khỏi đây GIẾT cả
        // tiến trình. Vector hoá không đáng để đánh sập API.
        try
        {
            await SweepMissingAsync(stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                var proposalId = await _queue.DequeueAsync(stoppingToken);
                await EmbedOneAsync(proposalId, stoppingToken);
                await Task.Delay(PauseBetweenCalls, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Đang tắt ứng dụng — bình thường.
        }
        catch (Exception ex)
        {
            SafeLog(() => _logger.LogError(ex, "EmbeddingWorker dừng vì lỗi ngoài dự kiến"));
        }
    }

    /// <summary>
    /// Lúc khởi động, xếp hàng các đề cương <b>đã nộp mà chưa có vector</b>.
    ///
    /// <para>Hàng đợi nằm trong bộ nhớ nên khởi động lại là mất. Không quét bù thì đề cương nộp
    /// ngay trước lần khởi động lại sẽ vĩnh viễn không vào kho đối chiếu, và không ai biết vì sao
    /// nó không bao giờ bị đem ra so.</para>
    /// </summary>
    private async Task SweepMissingAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<FURPMSDbContext>();

            var pending = await db.Proposals
                .Where(p => p.SubmittedAt != null)
                .Where(p => !db.SemanticSearchVectors.Any(v =>
                    v.EntityType == DuplicateCheckService.VectorEntityType &&
                    v.EntityId == p.Id.ToString() &&
                    v.Embedding != null))
                .OrderByDescending(p => p.SubmittedAt)
                .Take(SweepLimit)
                .Select(p => p.Id)
                .ToListAsync(ct);

            foreach (var id in pending) _queue.Enqueue(id);

            if (pending.Count > 0)
                SafeLog(() => _logger.LogInformation(
                    "Xếp hàng vector hoá {Count} đề cương đã nộp mà chưa có vector", pending.Count));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            SafeLog(() => _logger.LogError(ex, "Không quét được đề cương thiếu vector lúc khởi động"));
        }
    }

    private async Task EmbedOneAsync(Guid proposalId, CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var svc = scope.ServiceProvider.GetRequiredService<IDuplicateCheckService>();

            // Dùng lại đúng đường lập chỉ mục của Quản trị: nội dung không đổi thì nó tự bỏ qua,
            // nên xếp hàng trùng cũng không đốt quota. Một đường tính, một chỗ sửa.
            await svc.ReindexAsync(proposalId, max: 1, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Một đề cương hỏng (hoặc chưa cấu hình khoá AI) không được làm chết vòng lặp.
            SafeLog(() => _logger.LogWarning(ex, "Không vector hoá được đề cương {ProposalId}", proposalId));
        }
    }

    /// <summary>
    /// Ghi log mà ném lỗi (nhà cung cấp log hỏng) thì lỗi đó lọt ra khỏi <c>ExecuteAsync</c> và
    /// đánh sập tiến trình. Đã gặp thật với DeadlineReminderService.
    /// </summary>
    private static void SafeLog(Action log)
    {
        try { log(); }
        catch { /* mất đường ghi log thì thôi, không được để nó giết tiến trình */ }
    }
}
