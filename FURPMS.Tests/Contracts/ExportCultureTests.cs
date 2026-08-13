using System.Globalization;

namespace FURPMS.Tests.Contracts;

/// <summary>
/// Ngày tháng và số tiền trong văn bản xuất ra <b>không được phụ thuộc máy đang chạy</b>.
///
/// <para>
/// Ứng dụng không đặt culture ở đâu cả nên mặc định lấy theo máy. Mà trong .NET, dấu <c>/</c>
/// trong chuỗi định dạng <c>"dd/MM/yyyy"</c> <b>không phải ký tự cố định</b> — nó là chỗ dành cho
/// dấu phân cách ngày của culture hiện hành. Máy dev cho ra đúng kiểu Việt, nhưng máy chủ
/// (Render/Railway) thường chạy <c>en-US</c> hoặc invariant.
/// </para>
///
/// <para>
/// Hậu quả nếu không ghim: <b>cùng một hợp đồng xuất ở hai nơi ra hai kiểu ngày</b>. Với văn bản
/// đem đi ký thì đó là chuyện không chấp nhận được.
/// </para>
/// </summary>
public class ExportCultureTests
{
    private static readonly CultureInfo Vi = new("vi-VN");

    /// <summary>
    /// Chứng minh cái bẫy là THẬT, không phải lo xa: cùng một chuỗi định dạng, đổi culture là đổi
    /// kết quả. Đây là lý do tồn tại của mọi <c>, Vi)</c> trong DocumentExportService.
    /// </summary>
    [Fact]
    public void Dau_phan_cach_ngay_DOI_theo_culture_neu_khong_ghim()
    {
        var date = new DateOnly(2026, 9, 1);

        // da-DK dùng dấu chấm làm phân cách ngày.
        var danish = date.ToString("dd/MM/yyyy", new CultureInfo("da-DK"));
        var vietnamese = date.ToString("dd/MM/yyyy", Vi);

        Assert.Equal("01.09.2026", danish);      // CÙNG chuỗi định dạng, khác kết quả
        Assert.Equal("01/09/2026", vietnamese);
        Assert.NotEqual(danish, vietnamese);
    }

    [Fact]
    public void Ngay_ghim_culture_Viet_thi_luon_ra_dd_MM_yyyy()
    {
        var date = new DateOnly(2026, 9, 1);

        foreach (var host in new[] { "en-US", "da-DK", "de-DE", "ja-JP", "" })
            Assert.Equal("01/09/2026", date.ToString("dd/MM/yyyy", Vi));

        // Và không bao giờ là kiểu Mỹ tháng-trước-ngày.
        Assert.DoesNotContain("09/01", date.ToString("dd/MM/yyyy", Vi));
    }

    [Fact]
    public void So_tien_ghim_culture_Viet_thi_dung_dau_cham_ngan()
    {
        // 145 triệu phải đọc là "145.000.000" — người Việt đọc dấu phẩy là phần thập phân, để
        // culture Mỹ ra "145,000,000" là số tiền trên hợp đồng bị hiểu sai.
        Assert.Equal("145.000.000", 145_000_000m.ToString("N0", Vi));
        Assert.Equal("145,000,000", 145_000_000m.ToString("N0", new CultureInfo("en-US")));
    }
}
