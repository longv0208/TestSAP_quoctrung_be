using FURPMS.Application.Interfaces.Repositories;
using FURPMS.Application.Interfaces.Services;
using FURPMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FURPMS.Infrastructure.Services;

/// <summary>
/// Sinh sẵn tóm tắt AI cho đề cương đã nộp, chạy nền.
///
/// <para>
/// Thầy góp ý 05/08 và nhắc lại ở demo 14/08: tóm tắt phải có <b>sẵn</b> khi người chấm mở đề
/// tài. Trước đây họ phải bấm rồi chờ 30–60 giây đúng lúc hội đồng đang ngồi nhìn. Nay việc chờ
/// dời sang lúc PI nộp — khi đó không ai đứng đợi.
/// </para>
///
/// <para>
/// <b>Chạy tuần tự một việc một lúc.</b> Gói Gemini miễn phí giới hạn số request mỗi phút; bắn
/// song song là bị chặn hàng loạt, mà chặn thì chẳng đề cương nào có tóm tắt.
/// </para>
/// </summary>
public class AiSummaryPregenerationService : BackgroundService
{
    private readonly IAiSummaryQueue _queue;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<AiSummaryPregenerationService> _logger;

    /// <summary>Nghỉ giữa hai lần gọi để không chạm trần request/phút của gói miễn phí.</summary>
    private static readonly TimeSpan PauseBetweenCalls = TimeSpan.FromSeconds(5);

    public AiSummaryPregenerationService(
        IAiSummaryQueue queue,
        IServiceScopeFactory scopeFactory,
        ILogger<AiSummaryPregenerationService> logger)
    {
        _queue = queue;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // .NET 8 mặc định BackgroundServiceExceptionBehavior.StopHost: lỗi lọt ra khỏi đây là
        // GIẾT cả tiến trình. Sinh tóm tắt không đáng để đánh sập API.
        try
        {
            await SweepMissingAsync(stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                var job = await _queue.DequeueAsync(stoppingToken);
                await GenerateOneAsync(job.ProposalId, job.OnBehalfOfUserId, stoppingToken);
                await Task.Delay(PauseBetweenCalls, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Đang tắt ứng dụng — bình thường.
        }
        catch (Exception ex)
        {
            SafeLog(() => _logger.LogError(ex, "AiSummaryPregenerationService dừng vì lỗi ngoài dự kiến"));
        }
    }

    /// <summary>
    /// Lúc khởi động, quét các đề cương <b>đã nộp mà chưa có tóm tắt</b> rồi xếp hàng.
    /// <para>
    /// Hàng đợi nằm trong bộ nhớ nên khởi động lại là mất. Không quét thì những đề cương nộp ngay
    /// trước lần khởi động lại sẽ vĩnh viễn không có tóm tắt sẵn, và không ai biết vì sao.
    /// </para>
    /// </summary>
    private async Task SweepMissingAsync(CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<FURPMSDbContext>();

            // Đề cương đang chờ/đang chấm mới cần tóm tắt. Bản nháp thì PI còn sửa, sinh sớm là phí.
            var pending = await db.Proposals
                .Where(p => p.SubmittedAt != null)
                .Where(p => !db.LlmOutputs.Any(o =>
                    o.EntityType == "Proposal" &&
                    o.EntityId == p.Id.ToString() &&
                    o.OutputType == "SUMMARY" &&
                    o.IsActive))
                .OrderByDescending(p => p.SubmittedAt)
                .Take(50)   // đủ để bù phần thiếu, không biến khởi động thành đợt nã Gemini
                .Select(p => new { p.Id, p.Project!.PiUserId })
                .ToListAsync(ct);

            foreach (var p in pending)
                _queue.Enqueue(p.Id, p.PiUserId);

            if (pending.Count > 0)
                SafeLog(() => _logger.LogInformation(
                    "Xếp hàng sinh tóm tắt cho {Count} đề cương đã nộp mà chưa có tóm tắt", pending.Count));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            SafeLog(() => _logger.LogError(ex, "Không quét được đề cương thiếu tóm tắt lúc khởi động"));
        }
    }

    private async Task GenerateOneAsync(Guid proposalId, Guid onBehalfOfUserId, CancellationToken ct)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var sp = scope.ServiceProvider;
            var ai = sp.GetRequiredService<IAiSummaryService>();

            // Đã có tóm tắt thì thôi — xếp hàng trùng là chuyện bình thường (nộp lại, quét khởi
            // động trùng với lần nộp mới), nhưng gọi Gemini lại thì tốn quota mà không được gì.
            if (await ai.GetAsync(proposalId) != null) return;

            // Nền không có người đăng nhập nên phải mượn danh tính chủ nhiệm; tầng kiểm quyền
            // của GetProposalByIdAsync cần đúng userId + vai thật của họ.
            var users = sp.GetRequiredService<IUserRepository>();
            var roles = await users.UserRoles
                .Where(ur => ur.UserId == onBehalfOfUserId)
                .Select(ur => ur.Role.Name)
                .ToListAsync(ct);

            await ai.GenerateAsync(proposalId, onBehalfOfUserId, roles);
            SafeLog(() => _logger.LogInformation("Đã sinh sẵn tóm tắt AI cho đề cương {Id}", proposalId));
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Một đề cương hỏng không được làm dừng cả hàng đợi. Người chấm vẫn còn nút bấm tay.
            SafeLog(() => _logger.LogWarning(ex, "Không sinh được tóm tắt AI cho đề cương {Id}", proposalId));
        }
    }

    /// <summary>
    /// Ghi log cũng có thể ném lỗi lúc host đang tắt (provider đã dispose) — lỗi đó thoát ra khỏi
    /// <c>ExecuteAsync</c> là đánh sập tiến trình. Đã gặp thật với DeadlineReminderService.
    /// </summary>
    private static void SafeLog(Action write)
    {
        try { write(); }
        catch { /* mất đường ghi log thì thôi, không được để nó giết tiến trình */ }
    }
}
