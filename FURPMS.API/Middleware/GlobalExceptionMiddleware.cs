using System.Net;
using System.Text.Json;
using FURPMS.Application.Common;

namespace FURPMS.API.Middleware;

/// <summary>
/// Biến mọi ngoại lệ thành phản hồi thống nhất: <b>mã HTTP + mã lỗi ổn định + câu chữ dự phòng</b>.
///
/// <para>
/// Trước đây chỉ trả câu chữ, giao diện hiện nguyên văn. Nghĩa là ngôn ngữ bị khoá cứng ở máy chủ,
/// và giao diện không thể phản ứng theo <i>loại</i> lỗi trừ khi đi so khớp chuỗi — thứ gãy ngay khi
/// ai đó sửa một dấu chấm.
/// </para>
/// <para>
/// Nay kèm <c>errorCode</c>. Câu chữ tiếng Việt <b>vẫn giữ</b> làm phương án dự phòng cho những mã
/// giao diện chưa dịch, nên chuyển đổi được từng phần.
/// </para>
/// </summary>
public class GlobalExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;

    public GlobalExceptionMiddleware(RequestDelegate next, ILogger<GlobalExceptionMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception: {Message}", ex.Message);
            await HandleExceptionAsync(context, ex);
        }
    }

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var (statusCode, message, code, details) = Describe(exception);

        context.Response.StatusCode = statusCode;

        var response = ApiResponse.Fail(message, errorCode: code);
        response.Details = details;

        var json = JsonSerializer.Serialize(response, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            // Bỏ null cho gọn: phản hồi thành công không có errorCode/details.
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        });

        await context.Response.WriteAsync(json);
    }

    private static (int Status, string Message, string Code, IReadOnlyDictionary<string, object>? Details)
        Describe(Exception ex) => ex switch
    {
        // Ngoại lệ có mã riêng thì tôn trọng mã đó — nó cụ thể hơn mọi phép suy ra.
        AppException app => (app.StatusCode, app.Message, app.Code, app.Details),

        // 401 = chưa/hết đăng nhập (giao diện sẽ đăng xuất). 403 = đã đăng nhập nhưng thiếu quyền.
        UnauthorizedAccessException => ((int)HttpStatusCode.Unauthorized, ex.Message, ErrorCodes.InvalidCredentials, null),
        ForbiddenException => ((int)HttpStatusCode.Forbidden, ex.Message, ErrorCodes.Forbidden, null),
        KeyNotFoundException => ((int)HttpStatusCode.NotFound, ex.Message, ErrorCodes.NotFound, null),
        ArgumentException => ((int)HttpStatusCode.BadRequest, ex.Message, ErrorCodes.ValidationFailed, null),
        InvalidOperationException => ((int)HttpStatusCode.Conflict, ex.Message, ErrorCodes.Conflict, null),

        // Lỗi ngoài dự kiến: KHÔNG lộ chi tiết kỹ thuật ra ngoài. Log đã giữ đủ để tra.
        _ => ((int)HttpStatusCode.InternalServerError,
              "Hệ thống gặp sự cố ngoài dự kiến. Vui lòng thử lại; nếu vẫn lỗi hãy báo quản trị viên.",
              ErrorCodes.Unexpected, null)
    };
}
