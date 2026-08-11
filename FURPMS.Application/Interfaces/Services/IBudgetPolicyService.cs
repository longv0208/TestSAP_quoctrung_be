namespace FURPMS.Application.Interfaces.Services;

/// <summary>
/// Trần kinh phí có hiệu lực cho một đề tài, kèm nguồn của trần để thông báo lỗi nói rõ vì sao.
/// </summary>
/// <param name="ResearchTypeName">Tên loại đề tài (Nghiên cứu cơ bản / ứng dụng).</param>
/// <param name="TypeCap">Trần theo loại đề tài (QĐ543 Điều 14.1), 0 = chưa cấu hình.</param>
/// <param name="OrderCap">Trần riêng của đơn đặt hàng, nếu đề tài đi theo danh mục đặt hàng.</param>
/// <param name="EffectiveCap">Trần thực sự áp dụng — cái NGHIÊM NGẶT hơn; null = không giới hạn.</param>
public record BudgetCapInfo(
    string ResearchTypeName,
    decimal TypeCap,
    decimal? OrderCap,
    decimal? EffectiveCap);

/// <summary>
/// Kiểm tra dự toán kinh phí so với trần của QĐ543 Điều 14.
/// <para>
/// Tách riêng vì có <b>ba</b> đường ghi kinh phí (tạo đề cương, sửa dự toán, tạo bản chỉnh sửa) và
/// thêm một cửa nộp — chặn ở một chỗ thì ba chỗ kia vẫn lọt.
/// </para>
/// </summary>
public interface IBudgetPolicyService
{
    /// <summary>Trần đang áp cho đề cương này (để FE hiện lên form, không phải chờ lỗi mới biết).</summary>
    Task<BudgetCapInfo> GetCapAsync(Guid proposalId);

    /// <summary>Ném <see cref="ArgumentException"/> (400) nếu tổng dự toán vượt trần.</summary>
    Task AssertWithinCapAsync(Guid proposalId, decimal totalAmount);

    /// <summary>
    /// Soi tỷ lệ từng hạng mục trên tổng dự toán — QĐ543 <b>Điều 15.1</b>.
    /// Ném <see cref="ArgumentException"/> (400) và liệt kê <b>tất cả</b> hạng mục vi phạm trong
    /// một lần, để chủ nhiệm sửa một lượt thay vì bị báo lỗi từng dòng.
    /// </summary>
    /// <param name="amountsByCategoryId">Số tiền đã gộp theo hạng mục.</param>
    /// <param name="totalAmount">Tổng dự toán để tính tỷ lệ.</param>
    Task AssertCategoryLimitsAsync(IReadOnlyDictionary<int, decimal> amountsByCategoryId, decimal totalAmount);
}
