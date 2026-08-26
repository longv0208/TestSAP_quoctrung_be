using FURPMS.Application.DTOs.Ai;

namespace FURPMS.Application.Interfaces.Services;

/// <summary>
/// Rà trùng lặp đề cương — gạch 3 của biên bản hội đồng bảo vệ lần 2.
///
/// <para><b>Hai tầng.</b> Tầng 1 vector hoá bằng <c>text-embedding-004</c> rồi so cosine với cả kho:
/// rẻ, chạy mọi lần, và <b>tất định</b> nên đo được Precision/Recall. Tầng 2 chỉ chạy khi có cặp
/// vượt ngưỡng — gọi mô hình sinh chữ để viết ra <i>giống ở chỗ nào</i>, thứ mà một con số không
/// nói được.</para>
///
/// <para><b>⚠️ Hệ thống không tự kết luận đề tài này trùng.</b> Nó xếp thứ tự và giải thích; kết
/// luận là của Phòng QLKH, ghi qua <see cref="ReviewAsync"/> và vào sổ quyết định của đề tài. Đây
/// chính là "người trong vòng lặp" mà cẩm nang chống trượt đòi hỏi ở mọi tính năng AI.</para>
///
/// <para><b>Phân biệt với FE-08 trong tài liệu cũ</b> — hai chiều ngược nhau, dễ hiểu nhầm thành
/// nhóm mô tả sai chính sản phẩm mình:</para>
/// <list type="bullet">
/// <item>FE-08: đề cương ↔ <b>đơn đặt hàng</b>, cảnh báo khi điểm <b>THẤP</b> (không bám đặt hàng).</item>
/// <item>Đây: đề cương ↔ <b>kho đề tài đã có</b>, cảnh báo khi điểm <b>CAO</b> (nghi trùng).</item>
/// </list>
/// </summary>
public interface IDuplicateCheckService
{
    /// <summary>Kết quả rà trùng của một đề cương. Đọc thuần, không gọi AI.</summary>
    Task<DuplicateCheckResponse> GetAsync(Guid proposalId, Guid userId, IEnumerable<string> roles);

    /// <summary>
    /// Cờ trùng lặp rút gọn cho NHIỀU đề cương cùng lúc — dùng cho màn danh sách xét duyệt, để
    /// Phòng QLKH thấy đề tài nào cần xem trước khi phải mở từng cái ra.
    ///
    /// <para>Không kiểm quyền theo từng đề cương như <see cref="GetAsync"/>: chỉ Staff/Admin gọi
    /// được (kiểm ở controller), và họ vốn xem được toàn bộ danh sách rồi.</para>
    /// </summary>
    Task<Dictionary<Guid, DuplicateFlagDto>> GetFlagsAsync(IEnumerable<Guid> proposalIds);

    /// <summary>
    /// Chạy tầng 2: nhờ mô hình sinh chữ giải thích các cặp giống nhất.
    ///
    /// <para>Kết quả lưu vào <c>llm_outputs</c> kèm số token và độ trễ; gọi lại mà đề cương chưa
    /// đổi thì <b>trả bản đã lưu</b>, không đốt quota lần hai.</para>
    /// </summary>
    Task<DuplicateCheckResponse> ExplainAsync(Guid proposalId, Guid userId, IEnumerable<string> roles, bool force);

    /// <summary>Phòng QLKH chốt kết luận — sinh một dòng trong sổ quyết định của đề tài.</summary>
    Task<DuplicateCheckResponse> ReviewAsync(
        Guid proposalId, ReviewDuplicateRequest request, Guid reviewerId, IEnumerable<string> roles);

    /// <summary>
    /// Vector hoá (hoặc vector hoá lại) đề cương trong kho.
    ///
    /// <para>Nội dung không đổi thì bỏ qua — so bằng <c>ContentHash</c> vốn đã có sẵn trong bảng.
    /// Đây là cơ chế khống chế quota: chạy lại mười lần cũng chỉ tốn cho những bản thật sự mới.</para>
    /// </summary>
    Task<ReindexEmbeddingsResponse> ReindexAsync(Guid? proposalId, int max, CancellationToken ct = default);
}
