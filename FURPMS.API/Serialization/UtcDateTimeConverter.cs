using System.Text.Json;
using System.Text.Json.Serialization;

namespace FURPMS.API.Serialization;

/// <summary>
/// Đưa mọi <see cref="DateTime"/> đọc từ thân yêu cầu về <see cref="DateTimeKind.Utc"/>.
///
/// <para>
/// Sau khi chuyển sang PostgreSQL (14/08), cột ngày giờ là <c>timestamp with time zone</c>.
/// Npgsql <b>ném lỗi</b> khi ghi một <c>DateTime</c> có <c>Kind = Unspecified</c> — mà đó đúng là
/// thứ trình duyệt gửi lên: ô <c>&lt;input type="datetime-local"&gt;</c> cho ra chuỗi
/// <c>"2026-08-26T11:04"</c>, không có múi giờ. Ngoại lệ đó không phải loại nào middleware biết
/// nên rơi vào nhánh 500 "Hệ thống gặp sự cố ngoài dự kiến" — người dùng chỉ thấy lịch họp không
/// lên được, không có manh mối nào.
/// </para>
///
/// <para>
/// Có <b>67 trường ngày giờ</b> rải khắp các DTO (hạn xác nhận thư mời, khung giờ hội đồng, mốc
/// hợp đồng…), nên vá từng chỗ là chắc chắn sót. Chuẩn hoá một lần ở tầng đọc JSON thì mọi
/// endpoint hiện có lẫn sắp viết đều an toàn.
/// </para>
///
/// <para>
/// Quy ước: <b>không ghi múi giờ nghĩa là UTC</b> (không dịch giờ) — giữ nguyên con số client gửi
/// thay vì đoán múi giờ máy chủ, vì máy chủ chạy ở Railway còn người dùng ở Việt Nam. Client nào
/// muốn đúng giờ tuyệt đối thì gửi kèm <c>Z</c> hoặc offset; giao diện FURPMS đã gửi
/// <c>toISOString()</c>.
/// </para>
/// </summary>
public class UtcDateTimeConverter : JsonConverter<DateTime>
{
    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => Normalize(reader.GetDateTime());

    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
        => writer.WriteStringValue(Normalize(value));

    internal static DateTime Normalize(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
    };
}

/// <summary>Bản cho trường ngày giờ có thể bỏ trống — <c>null</c> đi thẳng qua.</summary>
public class NullableUtcDateTimeConverter : JsonConverter<DateTime?>
{
    public override DateTime? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        => reader.TokenType == JsonTokenType.Null ? null : UtcDateTimeConverter.Normalize(reader.GetDateTime());

    public override void Write(Utf8JsonWriter writer, DateTime? value, JsonSerializerOptions options)
    {
        if (value is null) writer.WriteNullValue();
        else writer.WriteStringValue(UtcDateTimeConverter.Normalize(value.Value));
    }
}
