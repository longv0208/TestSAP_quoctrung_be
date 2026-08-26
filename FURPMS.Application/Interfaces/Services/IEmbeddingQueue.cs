namespace FURPMS.Application.Interfaces.Services;

/// <summary>
/// Hàng đợi vector hoá đề cương vừa nộp, phục vụ rà trùng lặp.
///
/// <para><b>Vì sao cần (26/08).</b> Trước đó chỉ Quản trị chạy tay
/// <c>POST /api/admin/reindex-embeddings</c> mới có vector. Hệ quả: chủ nhiệm nộp một đề cương
/// trùng y hệt bài đã có, Phòng QLKH mở tab "Rà trùng lặp" thì thấy <i>"chưa lập chỉ mục"</i> —
/// đúng lúc tính năng cần hoạt động nhất thì nó im lặng.</para>
///
/// <para><b>Vì sao là hàng đợi chứ không gọi thẳng.</b> Cùng lý do với hàng đợi tóm tắt: gọi model
/// nhúng mất một hai giây, nhét vào trong lời gọi "Nộp đề cương" là bắt chủ nhiệm ngồi chờ cho một
/// việc họ không cần. Xếp hàng rồi trả lời ngay, việc vector hoá chạy nền.</para>
/// </summary>
public interface IEmbeddingQueue
{
    /// <summary>
    /// Xếp một đề cương vào hàng chờ vector hoá. Gọi nhiều lần với cùng đề cương không sao —
    /// worker so <c>ContentHash</c>, nội dung không đổi thì bỏ qua, không đốt quota.
    /// </summary>
    void Enqueue(Guid proposalId);

    /// <summary>Lấy việc tiếp theo; chờ nếu hàng đợi rỗng.</summary>
    ValueTask<Guid> DequeueAsync(CancellationToken ct);
}
