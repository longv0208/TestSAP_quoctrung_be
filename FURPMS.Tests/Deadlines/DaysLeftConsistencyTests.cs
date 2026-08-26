using FURPMS.Application.Common;
using FURPMS.Tests.Reminders; // FakeClock

namespace FURPMS.Tests.Deadlines;

/// <summary>
/// Đếm ngược tới hạn phải là <b>một phép tính duy nhất</b> cho cả hệ thống.
///
/// <para><b>Vì sao có bộ test này (25/08):</b> khi giao diện tự trừ ngày bằng đồng hồ trình duyệt,
/// thẻ "Hạn sắp tới" hiện <i>"Còn 7 ngày"</i> còn tab Sản phẩm hiện <i>"Còn 6 ngày"</i> cho
/// <b>cùng một sản phẩm</b> — máy chủ chạy UTC, máy người dùng ở UTC+7. Nay mọi nơi đều đi qua
/// <see cref="DeadlineMath"/>, và bộ test này giữ cho nó không bị tách ra lần nữa.</para>
/// </summary>
public class DaysLeftConsistencyTests
{
    private static readonly DateOnly Today = new(2026, 6, 18);

    [Theory]
    [InlineData(0, 0)]      // đúng hôm nay
    [InlineData(1, 1)]
    [InlineData(30, 30)]
    [InlineData(-1, -1)]    // quá hạn 1 ngày
    [InlineData(-40, -40)]
    public void DemNguoc_DungTheoLICH_KhongPhaiChiaMiliGiay(int offsetDays, int expected)
    {
        var due = Today.AddDays(offsetDays);
        Assert.Equal(expected, DeadlineMath.DaysLeft(due, Today));
    }

    [Fact]
    public void ChuaDatHan_TraVeNull_ChuKhongPhaiSoKhong()
    {
        // 0 nghĩa là "đến hạn hôm nay" — rất khác với "chưa ai đặt hạn". Trộn hai thứ này là
        // giao diện sẽ hiện "Còn 0 ngày" (đỏ) cho một việc chưa hề có hạn.
        Assert.Null(DeadlineMath.DaysLeft(null, Today));
    }

    [Fact]
    public void DungDongHoHeThong_NenCongCuTuaThoiGianTacDongDung()
    {
        var clock = new FakeClock { UtcNow = Today.ToDateTime(TimeOnly.MinValue) };
        var due = Today.AddDays(10);

        Assert.Equal(10, DeadlineMath.DaysLeft(due, clock));

        // Tua tới 12 ngày: việc đang "còn 10 ngày" phải thành "quá hạn 2 ngày" — đây chính là
        // cách kiểm thử các mốc dài ngày mà không phải sửa dữ liệu.
        clock.UtcNow = Today.AddDays(12).ToDateTime(TimeOnly.MinValue);
        Assert.Equal(-2, DeadlineMath.DaysLeft(due, clock));
    }

    [Fact]
    public void GioTrongNgay_KhongLamLechKetQua()
    {
        // 23:59 hôm nay và 00:01 hôm nay đều là "hôm nay". Nếu tính bằng hiệu mili-giây rồi chia
        // 86400000 thì hai mốc này cho hai kết quả khác nhau.
        var due = Today.AddDays(3);
        var sang = new FakeClock { UtcNow = Today.ToDateTime(new TimeOnly(0, 1)) };
        var khuya = new FakeClock { UtcNow = Today.ToDateTime(new TimeOnly(23, 59)) };

        Assert.Equal(DeadlineMath.DaysLeft(due, sang), DeadlineMath.DaysLeft(due, khuya));
        Assert.Equal(3, DeadlineMath.DaysLeft(due, khuya));
    }
}
