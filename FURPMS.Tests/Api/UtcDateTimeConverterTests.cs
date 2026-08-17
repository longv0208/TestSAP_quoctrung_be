using System.Text.Json;
using FURPMS.API.Serialization;

namespace FURPMS.Tests.Api;

/// <summary>
/// Ô <c>&lt;input type="datetime-local"&gt;</c> của trình duyệt gửi lên chuỗi KHÔNG có múi giờ
/// (<c>"2026-08-26T11:04"</c>). Sau khi chuyển sang PostgreSQL, Npgsql từ chối ghi
/// <c>DateTime</c> có <c>Kind = Unspecified</c> vào cột <c>timestamptz</c> — ngoại lệ rơi vào
/// nhánh 500 nên người dùng chỉ thấy "Hệ thống gặp sự cố ngoài dự kiến", không lên được lịch họp.
/// <para>
/// Có 67 trường ngày giờ rải khắp các DTO, nên chuẩn hoá một lần ở tầng đọc JSON.
/// </para>
/// </summary>
public class UtcDateTimeConverterTests
{
    private static JsonSerializerOptions Options()
    {
        var o = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        o.Converters.Add(new UtcDateTimeConverter());
        o.Converters.Add(new NullableUtcDateTimeConverter());
        return o;
    }

    private class Payload
    {
        public DateTime ScheduledAt { get; set; }
        public DateTime? ConfirmDeadline { get; set; }
    }

    [Fact]
    public void Chuoi_khong_co_mui_gio_thi_thanh_Utc_va_GIU_NGUYEN_gio()
    {
        // Đây là ca gây lỗi 500 thật: đúng chuỗi trình duyệt gửi khi Staff đặt lịch 11:04.
        var p = JsonSerializer.Deserialize<Payload>(
            """{"scheduledAt":"2026-08-26T11:04:00"}""", Options())!;

        Assert.Equal(DateTimeKind.Utc, p.ScheduledAt.Kind);
        // Không đoán múi giờ máy chủ: giờ client gửi sao thì giữ vậy, chỉ gắn nhãn UTC.
        Assert.Equal(new DateTime(2026, 8, 26, 11, 4, 0, DateTimeKind.Utc), p.ScheduledAt);
    }

    [Fact]
    public void Chuoi_co_Z_thi_khong_bi_dich_them_lan_nua()
    {
        // Giao diện đã sửa để gửi toISOString(); chuẩn hoá không được cộng/trừ thêm giờ nào.
        var p = JsonSerializer.Deserialize<Payload>(
            """{"scheduledAt":"2026-08-26T04:04:00Z"}""", Options())!;

        Assert.Equal(DateTimeKind.Utc, p.ScheduledAt.Kind);
        Assert.Equal(new DateTime(2026, 8, 26, 4, 4, 0, DateTimeKind.Utc), p.ScheduledAt);
    }

    [Fact]
    public void Chuoi_co_offset_thi_quy_ve_Utc_dung_gio()
    {
        // +07:00 là giờ Việt Nam — 11:04 ở đó là 04:04 UTC.
        var p = JsonSerializer.Deserialize<Payload>(
            """{"scheduledAt":"2026-08-26T11:04:00+07:00"}""", Options())!;

        Assert.Equal(DateTimeKind.Utc, p.ScheduledAt.Kind);
        Assert.Equal(new DateTime(2026, 8, 26, 4, 4, 0, DateTimeKind.Utc), p.ScheduledAt);
    }

    [Fact]
    public void Truong_bo_trong_van_nhan_null()
    {
        var p = JsonSerializer.Deserialize<Payload>(
            """{"scheduledAt":"2026-08-26T11:04:00","confirmDeadline":null}""", Options())!;

        Assert.Null(p.ConfirmDeadline);
    }

    [Fact]
    public void Truong_co_the_bo_trong_ma_co_gia_tri_thi_cung_thanh_Utc()
    {
        var p = JsonSerializer.Deserialize<Payload>(
            """{"scheduledAt":"2026-08-26T11:04:00","confirmDeadline":"2026-08-20T09:00:00"}""", Options())!;

        Assert.Equal(DateTimeKind.Utc, p.ConfirmDeadline!.Value.Kind);
    }
}
