namespace FURPMS.Application.Common;

/// <summary>
/// Đã đăng nhập nhưng KHÔNG có quyền với tài nguyên này (không phải PI của đề tài, không phải
/// thành viên hội đồng…). Map sang <b>403</b>.
/// <para>
/// Đừng dùng <see cref="UnauthorizedAccessException"/> cho các trường hợp này — nó map sang 401,
/// mà 401 nghĩa là "chưa/hết đăng nhập" nên FE sẽ tự đăng xuất người dùng.
/// </para>
/// </summary>
public class ForbiddenException : Exception
{
    public ForbiddenException(string message) : base(message) { }
}
