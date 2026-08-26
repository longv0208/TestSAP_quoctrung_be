using System.Threading.Channels;
using FURPMS.Application.Interfaces.Services;

namespace FURPMS.Infrastructure.Services;

/// <inheritdoc cref="IEmbeddingQueue"/>
public class EmbeddingQueue : IEmbeddingQueue
{
    /// <summary>
    /// Hàng đợi <b>có giới hạn</b>. Đầy thì bỏ mục CŨ nhất: đề cương vừa nộp đáng ưu tiên hơn, và
    /// mục bị bỏ vẫn được vá — lần khởi động sau quét lại các đề cương chưa có vector.
    /// </summary>
    private readonly Channel<Guid> _channel =
        Channel.CreateBounded<Guid>(new BoundedChannelOptions(200)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true
        });

    public void Enqueue(Guid proposalId) => _channel.Writer.TryWrite(proposalId);

    public ValueTask<Guid> DequeueAsync(CancellationToken ct) => _channel.Reader.ReadAsync(ct);
}
