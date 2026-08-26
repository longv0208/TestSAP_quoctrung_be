using FURPMS.Application.DTOs.Timeline;

namespace FURPMS.Application.Interfaces.Services;

/// <summary>
/// Dựng dòng thời gian đầy đủ của một đề tài. Xem <see cref="ProjectTimelineResponse"/> để biết
/// vì sao cần và vì sao đây là read-model chứ không phải bảng mới.
/// </summary>
public interface IProjectTimelineService
{
    Task<ProjectTimelineResponse> GetAsync(Guid projectId, Guid userId, IEnumerable<string> roles);

    /// <summary>
    /// Các hạn <b>sắp tới hoặc đã quá</b> của người đang đăng nhập, gộp từ mọi đề tài họ liên quan.
    ///
    /// <para>Cố ý dựng lại từ chính <see cref="GetAsync"/> thay vì viết một truy vấn riêng gom các
    /// cột hạn: hai đường tính hạn khác nhau thì sớm muộn cũng lệch, và lúc đó thẻ trên bảng điều
    /// khiển sẽ nói khác dòng thời gian của cùng một đề tài.</para>
    /// </summary>
    /// <param name="days">Cửa sổ nhìn tới trước, tính bằng ngày.</param>
    Task<IReadOnlyList<UpcomingDeadlineDto>> GetUpcomingAsync(
        Guid userId, IEnumerable<string> roles, int days);
}
