using FURPMS.Application.DTOs.Decisions;

namespace FURPMS.Application.Interfaces.Services;

/// <summary>
/// Đọc hồ sơ quyết định của một đề tài, và dựng lại hồ sơ cho dữ liệu có từ trước khi có sổ.
/// </summary>
public interface IProjectDecisionService
{
    /// <summary>Toàn bộ quyết định của một đề tài, xếp theo thời gian (cũ → mới).</summary>
    Task<ProjectDecisionDossierResponse> GetDossierAsync(
        Guid projectId, Guid userId, IEnumerable<string> roles);

    /// <summary>
    /// Dựng lại sổ quyết định từ dữ liệu <b>đã có sẵn</b> trong các bảng nghiệp vụ.
    ///
    /// <para><b>Vì sao bắt buộc phải có:</b> sổ chỉ bắt đầu ghi từ lúc tính năng này lên. Đề tài đã
    /// chạy xong trước đó — kể cả trên dữ liệu thật đang nằm ở Railway — sẽ có hồ sơ <b>trống trơn</b>
    /// đúng lúc mở ra cho hội đồng xem.</para>
    ///
    /// <para><b>Chạy lại được nhiều lần</b> (idempotent): nhận diện theo bộ ba
    /// <c>SourceEntityType + SourceEntityId + DecisionType</c>, đã có thì bỏ qua chứ không thêm bản
    /// trùng. Nên chạy <paramref name="dryRun"/> trước để xem sẽ thêm bao nhiêu dòng.</para>
    /// </summary>
    Task<BackfillDecisionsResponse> BackfillAsync(Guid? projectId, bool dryRun);
}
