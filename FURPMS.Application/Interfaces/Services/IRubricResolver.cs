using FURPMS.Domain.Entities.Financial;

namespace FURPMS.Application.Interfaces.Services;

/// <summary>
/// Tìm bộ tiêu chí đang áp cho một hội đồng, theo thứ tự ưu tiên:
/// <list type="number">
///   <item>Bộ gắn RIÊNG cho vòng đó (<c>ReviewRound.RubricTemplateId</c>).</item>
///   <item>Bộ theo (đợt + lĩnh vực + loại vòng).</item>
///   <item>Bộ mặc định chung (bộ chưa gắn phạm vi nào) — để không bao giờ kẹt không chấm được.</item>
/// </list>
/// Tách riêng vì cả màn chấm điểm lẫn tính năng AI gợi ý điểm đều cần —
/// để mỗi nơi tự truy vấn thì sớm muộn cũng lệch nhau.
/// </summary>
public interface IRubricResolver
{
    /// <summary>Trả về template kèm <c>Criteria</c> và <c>Scopes</c>; null nếu không có bộ nào khớp.</summary>
    Task<RubricTemplate?> ResolveForCouncilAsync(Guid councilId, CancellationToken ct = default);
}
