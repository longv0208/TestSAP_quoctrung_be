using System.Threading.Channels;
using FURPMS.Application.Interfaces.Services;

namespace FURPMS.Infrastructure.Services;

/// <inheritdoc cref="IAiSummaryQueue"/>
public class AiSummaryQueue : IAiSummaryQueue
{
    /// <summary>
    /// Hàng đợi <b>có giới hạn</b>. Không giới hạn thì một vòng nộp dồn dập (hoặc lỗi khiến xếp
    /// hàng liên tục) sẽ phình bộ nhớ không kiểm soát. Đầy thì bỏ mục CŨ nhất: đề cương vừa nộp
    /// đáng ưu tiên hơn, và mục bị bỏ vẫn sinh được sau — người chấm còn nút bấm tay, và lần khởi
    /// động sau sẽ quét lại các đề cương chưa có tóm tắt.
    /// </summary>
    private readonly Channel<(Guid ProposalId, Guid OnBehalfOfUserId)> _channel =
        Channel.CreateBounded<(Guid, Guid)>(new BoundedChannelOptions(200)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true
        });

    public void Enqueue(Guid proposalId, Guid onBehalfOfUserId) =>
        _channel.Writer.TryWrite((proposalId, onBehalfOfUserId));

    public ValueTask<(Guid ProposalId, Guid OnBehalfOfUserId)> DequeueAsync(CancellationToken ct) =>
        _channel.Reader.ReadAsync(ct);
}
