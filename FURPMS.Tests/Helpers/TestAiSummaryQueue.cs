using FURPMS.Application.Interfaces.Services;

namespace FURPMS.Tests.Helpers;

/// <summary>
/// Hàng đợi sinh tóm tắt giả — <b>ghi lại</b> những gì được xếp hàng thay vì gọi Gemini thật.
/// <para>
/// Test không được bắn request ra ngoài, nhưng vẫn cần kiểm tra "nộp đề cương xong CÓ xếp hàng
/// sinh tóm tắt không" — đó chính là điều thầy góp ý (tóm tắt phải có sẵn khi người chấm mở).
/// </para>
/// </summary>
public class TestAiSummaryQueue : IAiSummaryQueue
{
    public List<(Guid ProposalId, Guid OnBehalfOfUserId)> Enqueued { get; } = [];

    public void Enqueue(Guid proposalId, Guid onBehalfOfUserId) =>
        Enqueued.Add((proposalId, onBehalfOfUserId));

    /// <summary>Test không có worker nền nên không ai gọi tới đây.</summary>
    public ValueTask<(Guid ProposalId, Guid OnBehalfOfUserId)> DequeueAsync(CancellationToken ct) =>
        throw new NotSupportedException("Test không chạy worker nền.");
}
