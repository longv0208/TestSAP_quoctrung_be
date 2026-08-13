namespace FURPMS.Application.Common;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public T? Data { get; set; }
    public List<string>? Errors { get; set; }

    /// <summary>
    /// Mã lỗi ổn định (xem <see cref="ErrorCodes"/>) — chỉ có khi <see cref="Success"/> = false.
    /// <para>
    /// Giao diện tra mã này trong bảng dịch để hiện đúng ngôn ngữ người dùng đang chọn, và để
    /// <b>phản ứng theo loại lỗi</b> (hết phiên thì đăng xuất, xung đột thì mời tải lại) thay vì
    /// so khớp chuỗi chữ — cách so chuỗi gãy ngay khi ai đó sửa một dấu chấm.
    /// </para>
    /// </summary>
    public string? ErrorCode { get; set; }

    /// <summary>Dữ liệu kèm theo để giao diện ghép câu (trần kinh phí, số phiếu còn thiếu…).</summary>
    public IReadOnlyDictionary<string, object>? Details { get; set; }

    public static ApiResponse<T> Ok(T data, string? message = null) =>
        new() { Success = true, Data = data, Message = message };

    public static ApiResponse<T> Fail(string message, List<string>? errors = null, string? errorCode = null) =>
        new() { Success = false, Message = message, Errors = errors, ErrorCode = errorCode };
}

public class ApiResponse
{
    public bool Success { get; set; }
    public string? Message { get; set; }
    public List<string>? Errors { get; set; }

    /// <inheritdoc cref="ApiResponse{T}.ErrorCode"/>
    public string? ErrorCode { get; set; }

    /// <inheritdoc cref="ApiResponse{T}.Details"/>
    public IReadOnlyDictionary<string, object>? Details { get; set; }

    public static ApiResponse Ok(string? message = null) =>
        new() { Success = true, Message = message };

    public static ApiResponse Fail(string message, List<string>? errors = null, string? errorCode = null) =>
        new() { Success = false, Message = message, Errors = errors, ErrorCode = errorCode };
}
