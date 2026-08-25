namespace FURPMS.Application.Interfaces.Services;

/// <summary>
/// Trả về <b>hạn HIỆU LỰC</b> của một mốc — tức là đã tính cả các lần gia hạn.
///
/// <para><b>Vì sao phải có service riêng (25/08):</b> rule #19 quy định *"gia hạn deadline = LOG,
/// không ghi đè; deadline hiệu lực = bản mới nhất"*. Nhưng logic đọc bảng <c>deadline_extension</c>
/// lại nằm <c>private</c> trong <c>CycleService</c>, nên <c>ProposalService</c> không với tới được
/// và vẫn so ngày với <c>ResearchCycle.SubmissionDeadline</c> thô.</para>
///
/// <para><b>Hậu quả đã xảy ra:</b> Admin gia hạn đợt → hệ thống gửi thông báo *"đã gia hạn"* cho
/// chủ nhiệm → chủ nhiệm bấm nộp → <b>vẫn bị chặn theo hạn cũ</b>. Hai chỗ trong cùng hệ thống hiểu
/// "hạn" khác nhau.</para>
///
/// <para>Tách ra đây thay vì mở rộng <c>ICycleService</c> để <c>ProposalService</c> không phải phụ
/// thuộc ngược vào <c>CycleService</c> (dễ thành vòng phụ thuộc khi DI dựng đồ thị).</para>
/// </summary>
public interface IDeadlineResolver
{
    /// <summary>Loại mốc gia hạn được — khớp cột <c>deadline_extension.target_type</c>.</summary>
    public const string TargetTypeCycle = "CYCLE";
    public const string TargetTypeProject = "PROJECT";
    public const string TargetTypeContract = "CONTRACT";

    /// <summary>
    /// Hạn hiệu lực của một mốc. Chưa gia hạn lần nào thì trả về đúng <paramref name="original"/>.
    /// </summary>
    /// <param name="targetType">Xem các hằng <c>TargetType*</c> ở trên.</param>
    /// <param name="targetId">Khoá của đối tượng, ép về chuỗi (đợt dùng id số).</param>
    /// <param name="original">Hạn gốc ghi trên chính đối tượng đó.</param>
    Task<DateOnly> EffectiveAsync(string targetType, string targetId, DateOnly original);

    /// <summary>
    /// Lấy hạn hiệu lực cho NHIỀU đối tượng cùng loại trong một truy vấn — dùng cho màn danh sách,
    /// tránh N+1. Khoá của từ điển trả về là <c>targetId</c>.
    /// </summary>
    Task<IReadOnlyDictionary<string, DateOnly>> EffectiveManyAsync(
        string targetType, IEnumerable<string> targetIds);
}
