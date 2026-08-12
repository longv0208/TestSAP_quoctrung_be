namespace FURPMS.Application.Interfaces.Services;

/// <summary>
/// Gửi thông báo tới người dùng: LUÔN tạo thông báo in-app (chuông), và mặc định
/// gửi kèm EMAIL cùng nội dung.
/// <para>
/// Gom về một chỗ để không lặp lại tình trạng cũ: có nơi bắn chuông kèm mail
/// (thư mời hội đồng, nhắc hạn), có nơi chỉ bắn chuông mà quên mail
/// (kết quả xét duyệt gửi PI, sản phẩm đạt/không đạt) — người dùng không mở app
/// thì không biết gì.
/// </para>
/// <para>
/// Việc gửi mail vẫn chịu công tắc tổng của Admin
/// (<c>SystemSettingKeys.EmailEnabled</c>) do <c>IEmailService</c> kiểm tra —
/// tắt thì ghi <c>email_log</c> trạng thái SKIPPED, chuông vẫn chạy bình thường.
/// </para>
/// </summary>
public interface INotifier
{
    /// <summary>Thông báo cho 1 người.</summary>
    Task NotifyAsync(
        Guid userId,
        string type,
        string title,
        string body,
        string? actionUrl = null,
        string? entityType = null,
        string? entityId = null,
        string priority = "NORMAL",
        bool alsoEmail = true);

    /// <summary>
    /// Thông báo cho <b>mọi người mang một vai</b> (vd toàn bộ Phòng QLKH).
    /// <para>
    /// Tra vai → danh sách người dùng gom về một chỗ; trước đây mỗi service tự viết lại truy vấn
    /// <c>UserRoles.Where(Role.Name == "Staff")</c>, thêm một chỗ báo là chép lại một lần nữa.
    /// </para>
    /// </summary>
    Task NotifyRoleAsync(
        string roleName,
        string type,
        string title,
        string body,
        string? actionUrl = null,
        string? entityType = null,
        string? entityId = null,
        string priority = "NORMAL",
        bool alsoEmail = true);

    /// <summary>
    /// Thông báo cho nhiều người cùng nội dung (vd toàn bộ Staff).
    /// Chỉ 1 query lấy email cho cả nhóm — tránh N+1.
    /// </summary>
    Task NotifyManyAsync(
        IReadOnlyCollection<Guid> userIds,
        string type,
        string title,
        string body,
        string? actionUrl = null,
        string? entityType = null,
        string? entityId = null,
        string priority = "NORMAL",
        bool alsoEmail = true);
}
