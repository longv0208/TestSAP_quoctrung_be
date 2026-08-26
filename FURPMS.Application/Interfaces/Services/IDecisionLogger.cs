namespace FURPMS.Application.Interfaces.Services;

/// <summary>
/// Ghi một dòng vào sổ quyết định của đề tài.
///
/// <para>Được gọi rải rác ở ~13 chỗ trong các service nghiệp vụ. Hai quy ước bắt buộc:</para>
///
/// <list type="number">
/// <item><b>Chỉ ghi, không quyết.</b> Người gọi đã có kết luận trong tay rồi mới gọi vào đây —
/// logger không suy luận Đạt/Không đạt (rule #12).</item>
/// <item><b>Không được làm hỏng nghiệp vụ chính.</b> Ghi sổ hỏng thì nuốt lỗi và ghi log, tuyệt đối
/// không ném ngược lên: mất một dòng sổ còn hơn là chặn một quyết định hợp lệ của hội đồng.</item>
/// </list>
///
/// <para>Phương thức <b>không</b> tự gọi <c>SaveChanges</c> — nó chỉ <c>Add</c> vào cùng
/// <c>DbContext</c> mà service đang dùng, để dòng sổ và thay đổi nghiệp vụ nằm chung một giao dịch:
/// nghiệp vụ rollback thì dòng sổ cũng biến mất, không để lại quyết định ma.</para>
/// </summary>
public interface IDecisionLogger
{
    /// <param name="projectId">Đề tài mà quyết định này thuộc về.</param>
    /// <param name="decisionType">Một trong <c>DecisionTypes</c>.</param>
    /// <param name="summary">Một câu tiếng Việt đọc là hiểu.</param>
    /// <param name="sourceEntityType">Tên bảng gốc, để giao diện mở đúng bản gốc.</param>
    /// <param name="sourceEntityId">Khoá của bản gốc (dạng chuỗi — khoá có thể là Guid hoặc int).</param>
    /// <param name="result">Kết luận, <b>chép lại</b> từ bản gốc.</param>
    /// <param name="decidedBy">Người ra quyết định; null nếu do hệ thống ghi nhận.</param>
    /// <param name="decidedByRole">Chức danh tại thời điểm chốt — chép cứng, không suy ra lúc đọc.</param>
    /// <param name="decidedAt">Thời điểm quyết định thật sự; bỏ trống thì lấy đồng hồ hệ thống.</param>
    void Log(
        Guid projectId,
        string decisionType,
        string summary,
        string sourceEntityType,
        string sourceEntityId,
        string? result = null,
        string? reason = null,
        string? documentNo = null,
        Guid? decidedBy = null,
        string? decidedByRole = null,
        DateTime? decidedAt = null);
}
