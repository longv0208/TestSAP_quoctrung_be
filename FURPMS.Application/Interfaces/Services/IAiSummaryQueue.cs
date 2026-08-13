namespace FURPMS.Application.Interfaces.Services;

/// <summary>
/// Hàng đợi sinh sẵn tóm tắt AI cho đề cương vừa nộp.
///
/// <para>
/// <b>Vì sao cần.</b> Thầy góp ý từ 05/08 và nhắc lại ở demo 14/08: tóm tắt phải có SẴN khi người
/// chấm mở đề tài, không để họ ngồi bấm rồi chờ 30–60 giây <i>đúng lúc hội đồng đang ngồi nhìn</i>.
/// Cách chữa đúng là dời việc chờ sang lúc PI nộp — khi đó không ai đứng đợi.
/// </para>
///
/// <para>
/// <b>Vì sao là hàng đợi chứ không gọi thẳng.</b> Gọi Gemini mất 30–60 giây; nhét vào trong lời
/// gọi "Nộp đề cương" là bắt PI ngồi nhìn màn hình quay tròn một phút cho một việc họ không cần.
/// Xếp hàng rồi trả lời ngay, việc sinh tóm tắt chạy nền.
/// </para>
/// </summary>
public interface IAiSummaryQueue
{
    /// <summary>
    /// Xếp một đề cương vào hàng chờ sinh tóm tắt. Gọi nhiều lần với cùng một đề cương không sao —
    /// worker bỏ qua nếu đã có bản tóm tắt (không đốt quota Gemini vô ích).
    /// </summary>
    /// <param name="onBehalfOfUserId">
    /// Danh tính dùng để đọc đề cương — thường là chủ nhiệm. Nền không có người đăng nhập nên
    /// phải mượn danh tính hợp lệ, nếu không tầng kiểm quyền sẽ chặn.
    /// </param>
    void Enqueue(Guid proposalId, Guid onBehalfOfUserId);

    /// <summary>Lấy việc tiếp theo; chờ nếu hàng đợi rỗng.</summary>
    ValueTask<(Guid ProposalId, Guid OnBehalfOfUserId)> DequeueAsync(CancellationToken ct);
}
