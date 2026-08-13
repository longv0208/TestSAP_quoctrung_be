namespace FURPMS.Application.Common;

/// <summary>
/// Lỗi nghiệp vụ có <b>mã ổn định</b> kèm theo.
///
/// <para>
/// Ngoại lệ chuẩn của .NET (<c>ArgumentException</c>, <c>KeyNotFoundException</c>…) chỉ mang được
/// câu chữ. Lớp này thêm <see cref="Code"/> để giao diện <b>phản ứng theo loại lỗi</b> thay vì
/// đoán qua chuỗi chữ — và để dịch được sang ngôn ngữ khác mà không phải sửa máy chủ.
/// </para>
///
/// <para>
/// <b>Không bắt buộc dùng ở mọi chỗ.</b> Ngoại lệ chuẩn vẫn chạy như cũ, chỉ nhận mã chung
/// (<c>NOT_FOUND</c>, <c>VALIDATION_FAILED</c>…) do middleware suy ra. Dùng lớp này khi cần một mã
/// <i>cụ thể</i> mà giao diện phải xử lý riêng.
/// </para>
/// </summary>
public class AppException : Exception
{
    /// <summary>Mã lỗi ổn định — xem <see cref="ErrorCodes"/>.</summary>
    public string Code { get; }

    /// <summary>Mã HTTP tương ứng.</summary>
    public int StatusCode { get; }

    /// <summary>
    /// Dữ liệu kèm theo để giao diện ghép câu — vd trần kinh phí là bao nhiêu, còn thiếu mấy phiếu.
    /// Nhờ đó câu dịch bên giao diện vẫn nêu được con số cụ thể.
    /// </summary>
    public IReadOnlyDictionary<string, object>? Details { get; }

    public AppException(
        string code, string message, int statusCode = 400,
        IReadOnlyDictionary<string, object>? details = null) : base(message)
    {
        Code = code;
        StatusCode = statusCode;
        Details = details;
    }

    // ── Lối tắt cho các trường hợp hay gặp ──────────────────────────────────

    public static AppException NotFound(string message) =>
        new(ErrorCodes.NotFound, message, 404);

    public static AppException Validation(string code, string message,
        IReadOnlyDictionary<string, object>? details = null) =>
        new(code, message, 400, details);

    public static AppException Conflict(string code, string message,
        IReadOnlyDictionary<string, object>? details = null) =>
        new(code, message, 409, details);

    public static AppException Forbidden(string code, string message) =>
        new(code, message, 403);
}
