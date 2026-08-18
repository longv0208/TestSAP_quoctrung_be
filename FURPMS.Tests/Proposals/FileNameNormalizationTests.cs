using FURPMS.Application.Common;

namespace FURPMS.Tests.Proposals;

/// <summary>
/// Tên file hiện trên màn hình phải là tên PI thấy trên máy mình.
/// Lỗi thật 18/08: hội đồng mở tài liệu thì thấy
/// <c>02%20%C4%90e%CC%82%CC%80%20cu%CC%9Bo%CC%9Bng…</c> thay vì <i>02 Đề cương…</i>.
/// </summary>
public class FileNameNormalizationTests
{
    [Fact]
    public void TenBiMaHoaUrl_DuocGiaiMa()
    {
        // Đúng chuỗi chụp được trên màn hình hội đồng.
        const string raw = "02%20%C4%90e%CC%82%CC%80%20cu%CC%9Bo%CC%9Bng%20nghie%CC%82n%20cu%CC%9B%CC%81u.docx";

        Assert.Equal("02 Đề cương nghiên cứu.docx", FileNames.Normalize(raw));
    }

    [Fact]
    public void DauTiengVietTachRoi_GomVeDangDungSan()
    {
        // File từ macOS lưu dạng NFD: "ề" = e + ◌̂ + ◌̀. Nhìn giống hệt nhưng so chuỗi thì khác.
        var nfd = "Đe\u0302\u0300 cương.docx";

        var normalized = FileNames.Normalize(nfd);

        Assert.Equal("Đề cương.docx", normalized);
        Assert.NotEqual(nfd.Length, normalized.Length); // đã thật sự gộp, không phải trùng hợp
    }

    [Fact]
    public void TenThatCoDauPhanTram_KhongBiDungToi()
    {
        // Cạm bẫy của việc giải mã vô điều kiện: tên hợp lệ vẫn có thể chứa '%'.
        const string raw = "Giải ngân 50% đợt 1.pdf";

        Assert.Equal(raw, FileNames.Normalize(raw));
    }

    [Fact]
    public void DuongDanDayDu_ChiGiuTenFile()
    {
        Assert.Equal("de-cuong.docx", FileNames.Normalize(@"C:\Users\pi\Desktop\de-cuong.docx"));
        Assert.Equal("de-cuong.docx", FileNames.Normalize("/home/pi/tai-lieu/de-cuong.docx"));
    }

    [Fact]
    public void KyTuDieuKhien_BiLoaiBo()
    {
        // Xuống dòng lọt vào tên file sẽ bẻ header Content-Disposition lúc tải về.
        Assert.Equal("de-cuong.docx", FileNames.Normalize("de-cuong\r\n.docx"));
        Assert.Equal("de cuong.docx", FileNames.Normalize("de cuong\t.docx"));
    }

    [Fact]
    public void TenRong_TraVeChuoiRong_DeTangTrenBaoLoiDuoiFile()
    {
        Assert.Equal(string.Empty, FileNames.Normalize(null));
        Assert.Equal(string.Empty, FileNames.Normalize("   "));
    }

    [Fact]
    public void GiaiMaXongMaLoiRaDuongDan_ThiGiuNguyenTenGoc()
    {
        // %2F là '/', giải mã ra sẽ thành đường dẫn — không nhận.
        const string raw = "..%2F..%2Fetc%2Fpasswd";

        Assert.Equal(raw, FileNames.Normalize(raw));
    }
}
