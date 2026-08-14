using FURPMS.Infrastructure.Extensions;
using Microsoft.Extensions.Configuration;

namespace FURPMS.Tests.Infrastructure;

/// <summary>
/// Đổi chuỗi kết nối dạng URI của nhà cung cấp sang dạng Npgsql hiểu được.
///
/// <para>
/// Railway/Render/Heroku phơi <c>DATABASE_URL</c> dạng <c>postgresql://user:pass@host:port/db</c>,
/// còn Npgsql chỉ hiểu <c>Host=…;Port=…;Database=…</c>. Dán thẳng biến của nhà cung cấp vào là ứng
/// dụng chết lúc khởi động với thông báo khó hiểu — và <b>không có cách nào biết trước</b> cho tới
/// khi deploy hỏng, nên phải khoá bằng test.
/// </para>
/// </summary>
public class PostgresConnectionStringTests
{
    private static IConfiguration Config(params (string Key, string Value)[] entries) =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(entries.Select(e => new KeyValuePair<string, string?>(e.Key, e.Value)))
            .Build();

    [Fact]
    public void Doi_dung_URI_cua_Railway_sang_dang_Npgsql()
    {
        var cfg = Config(("ConnectionStrings:DefaultConnection",
            "postgresql://postgres:matkhau123@monorail.proxy.rlwy.net:41234/railway"));

        var result = PostgresConnectionString.Resolve(cfg);

        Assert.Contains("Host=monorail.proxy.rlwy.net", result);
        Assert.Contains("Port=41234", result);
        Assert.Contains("Database=railway", result);
        Assert.Contains("Username=postgres", result);
        Assert.Contains("Password=matkhau123", result);
    }

    [Fact]
    public void Chuoi_dang_khoa_gia_tri_thi_GIU_NGUYEN()
    {
        // Người triển khai tự viết chuỗi Npgsql chuẩn thì đừng đụng vào.
        const string raw = "Host=localhost;Port=5432;Database=furpms;Username=postgres;Password=abc";
        var result = PostgresConnectionString.Resolve(Config(("ConnectionStrings:DefaultConnection", raw)));

        Assert.Equal(raw, result);
    }

    [Fact]
    public void Doc_duoc_bien_DATABASE_URL_khi_khong_dat_ConnectionStrings()
    {
        // Railway đặt sẵn DATABASE_URL — người triển khai không phải chép sang tên khác.
        var cfg = Config(("DATABASE_URL", "postgres://u:p@db.internal:5432/app"));

        var result = PostgresConnectionString.Resolve(cfg);

        Assert.Contains("Host=db.internal", result);
        Assert.Contains("Database=app", result);
    }

    [Fact]
    public void May_chu_NOI_BO_thi_tat_SSL_may_chu_CONG_KHAI_thi_bat()
    {
        // Mạng nội bộ Railway không dùng TLS; địa chỉ công khai thì gần như luôn bắt buộc.
        // Đoán sai bên nào cũng làm kết nối hỏng.
        var noiBo = PostgresConnectionString.Resolve(
            Config(("DATABASE_URL", "postgresql://u:p@postgres.railway.internal:5432/railway")));
        var congKhai = PostgresConnectionString.Resolve(
            Config(("DATABASE_URL", "postgresql://u:p@monorail.proxy.rlwy.net:41234/railway")));

        Assert.Contains("SSL Mode=Disable", noiBo);
        Assert.Contains("SSL Mode=Require", congKhai);
    }

    [Fact]
    public void Mat_khau_co_ky_tu_dac_biet_van_giai_ma_dung()
    {
        // Mật khẩu sinh tự động hay có @ : / — nếu không giải mã thì chuỗi bị cắt sai chỗ.
        var cfg = Config(("DATABASE_URL", "postgresql://u:p%40ss%3Aword@host:5432/db"));

        Assert.Contains("Password=p@ss:word", PostgresConnectionString.Resolve(cfg));
    }

    /// <summary>
    /// Đưa chuỗi SQL Server cho Npgsql phải báo lỗi NÓI RÕ nguyên nhân.
    /// <para>
    /// Đây là lỗi thật gặp lúc deploy Railway 14/08: mã nguồn đã sang Postgres nhưng biến môi
    /// trường vẫn là chuỗi SQL Server cũ. Npgsql chỉ báo "Couldn't set data source" kèm
    /// KeyNotFoundException — đọc xong không ai đoán được là do sai LOẠI chuỗi kết nối.
    /// </para>
    /// </summary>
    [Theory]
    [InlineData("Server=sql8005.site4now.net;Database=db;User Id=admin;Password=x;Encrypt=True")]
    [InlineData("Data Source=localhost;Initial Catalog=FURPMS;Integrated Security=True")]
    [InlineData("Server=localhost,1435;Database=FURPMS_V2;User Id=sa;Password=x;TrustServerCertificate=True")]
    public void Chuoi_SQL_SERVER_thi_bao_loi_chi_ro_cach_sua(string sqlServerConn)
    {
        var ex = Assert.Throws<InvalidOperationException>(
            () => PostgresConnectionString.Resolve(Config(("ConnectionStrings:DefaultConnection", sqlServerConn))));

        Assert.Contains("SQL SERVER", ex.Message);
        Assert.Contains("PostgreSQL", ex.Message);
        // Phải chỉ ĐÚNG chỗ sửa, không chỉ nói "sai".
        Assert.Contains("Railway", ex.Message);
    }

    [Fact]
    public void Ghep_duoc_tu_bo_bien_roi_PG()
    {
        var cfg = Config(("PGHOST", "postgres.railway.internal"), ("PGPORT", "5432"),
                         ("PGDATABASE", "railway"), ("PGUSER", "postgres"), ("PGPASSWORD", "bimat"));

        var result = PostgresConnectionString.Resolve(cfg);

        Assert.Contains("Host=postgres.railway.internal", result);
        Assert.Contains("Password=bimat", result);
        Assert.Contains("SSL Mode=Disable", result);   // mạng nội bộ
    }

    [Fact]
    public void Khong_co_chuoi_ket_noi_thi_bao_loi_NOI_RO_phai_dat_bien_nao()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => PostgresConnectionString.Resolve(Config()));

        Assert.Contains("ConnectionStrings__DefaultConnection", ex.Message);
    }
}
