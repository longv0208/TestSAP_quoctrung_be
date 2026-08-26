namespace FURPMS.Application.DTOs.ReviewRounds;

/// <summary>
/// Đặt hoặc DỜI hạn chấm của một vòng.
///
/// <para>Dời một hạn đã có thì <b>bắt buộc</b> nêu lý do — rule #19: gia hạn là LOG, mỗi lần dời
/// ghi một dòng <c>deadline_extension</c>, ngày gốc giữ nguyên. Không lý do thì sổ gia hạn thành
/// một danh sách ngày trơ, không ai truy được vì sao.</para>
/// </summary>
public class SetRoundDeadlineRequest
{
    /// <summary>Ngày phải chấm xong, dạng <c>yyyy-MM-dd</c>.</summary>
    public string ScoringDeadline { get; set; } = null!;

    /// <summary>Lý do dời hạn. Bắt buộc khi vòng ĐÃ có hạn; bỏ trống được khi đặt hạn lần đầu.</summary>
    public string? Reason { get; set; }
}
