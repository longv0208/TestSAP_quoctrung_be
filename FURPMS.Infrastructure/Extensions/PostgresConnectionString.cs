using Microsoft.Extensions.Configuration;

namespace FURPMS.Infrastructure.Extensions;

/// <summary>
/// Lấy chuỗi kết nối Postgres, chấp nhận <b>cả hai định dạng</b> mà các nhà cung cấp hay dùng.
///
/// <para>
/// <b>Vì sao cần.</b> Railway, Render, Heroku, Fly… đều phơi biến <c>DATABASE_URL</c> dạng
/// <b>URI</b>:
/// </para>
/// <code>postgresql://nguoidung:matkhau@may-chu.railway.internal:5432/railway</code>
/// <para>
/// Nhưng Npgsql chỉ hiểu dạng <b>khoá=giá trị</b>:
/// </para>
/// <code>Host=may-chu;Port=5432;Database=railway;Username=nguoidung;Password=matkhau</code>
/// <para>
/// Dán thẳng biến của nhà cung cấp vào cấu hình là ứng dụng chết ngay lúc khởi động với thông báo
/// khó hiểu (<i>"Format of the initialization string does not conform to specification"</i>). Đây
/// là chỗ vấp kinh điển khi đưa .NET lên các nền tảng đó, nên đổi định dạng hộ luôn thay vì bắt
/// người triển khai tự ghép tay từ 5 biến rời.
/// </para>
/// </summary>
public static class PostgresConnectionString
{
    /// <summary>
    /// Thứ tự ưu tiên: <c>ConnectionStrings:DefaultConnection</c> → biến <c>DATABASE_URL</c>.
    /// Dạng URI được đổi sang dạng khoá=giá trị; dạng khoá=giá trị giữ nguyên.
    /// </summary>
    public static string Resolve(IConfiguration configuration)
    {
        var raw = configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(raw))
            raw = configuration["DATABASE_URL"];

        // Chưa đặt gì thì thử ghép từ các biến rời PG* — Postgres của Railway/Render đều phơi bộ này.
        if (string.IsNullOrWhiteSpace(raw))
            raw = FromPgVariables(configuration);

        if (string.IsNullOrWhiteSpace(raw))
            throw new InvalidOperationException(
                "Chưa có chuỗi kết nối cơ sở dữ liệu. Đặt biến môi trường " +
                "ConnectionStrings__DefaultConnection (hoặc DATABASE_URL) trên nền tảng triển khai.");

        AssertNotSqlServer(raw);
        return LooksLikeUri(raw) ? FromUri(raw) : raw;
    }

    /// <summary>
    /// Bắt sớm trường hợp <b>đưa chuỗi SQL Server cho Npgsql</b>.
    ///
    /// <para>
    /// Đây là chỗ vấp đầu tiên khi đổi nền tảng: mã nguồn đã chuyển sang Postgres nhưng biến môi
    /// trường trên máy chủ vẫn là chuỗi SQL Server cũ. Npgsql báo
    /// <i>"Couldn't set data source"</i> kèm <c>KeyNotFoundException</c> — đọc xong không ai đoán
    /// được là do <b>sai loại chuỗi kết nối</b>, và ứng dụng chết ngay lúc chạy migration nên
    /// không có gì khác để bám vào.
    /// </para>
    /// </summary>
    private static void AssertNotSqlServer(string raw)
    {
        string[] sqlServerOnly =
            ["Data Source=", "Initial Catalog=", "Integrated Security=", "TrustServerCertificate=", "User Id="];

        var hit = sqlServerOnly.FirstOrDefault(k => raw.Contains(k, StringComparison.OrdinalIgnoreCase));
        // "Server=" cũng là của SQL Server, nhưng chỉ tính khi KHÔNG có "Host=" (Npgsql dùng Host).
        if (hit == null && raw.Contains("Server=", StringComparison.OrdinalIgnoreCase)
                        && !raw.Contains("Host=", StringComparison.OrdinalIgnoreCase))
            hit = "Server=";

        if (hit == null) return;

        throw new InvalidOperationException(
            $"Chuỗi kết nối đang là của SQL SERVER (thấy từ khoá '{hit.TrimEnd('=')}') nhưng ứng dụng " +
            "này chạy trên PostgreSQL. Trên Railway: thêm service PostgreSQL, rồi đặt " +
            "ConnectionStrings__DefaultConnection = ${{Postgres.DATABASE_URL}} " +
            "(hoặc dạng Host=…;Port=…;Database=…;Username=…;Password=…).");
    }

    /// <summary>Ghép chuỗi từ bộ biến rời <c>PGHOST/PGPORT/PGDATABASE/PGUSER/PGPASSWORD</c>.</summary>
    private static string? FromPgVariables(IConfiguration cfg)
    {
        var host = cfg["PGHOST"];
        if (string.IsNullOrWhiteSpace(host)) return null;

        var port = cfg["PGPORT"] ?? "5432";
        var db = cfg["PGDATABASE"] ?? "railway";
        var user = cfg["PGUSER"] ?? "postgres";
        var pass = cfg["PGPASSWORD"] ?? "";
        var ssl = host.EndsWith(".internal", StringComparison.OrdinalIgnoreCase) ? "Disable" : "Require";

        return $"Host={host};Port={port};Database={db};Username={user};Password={pass};" +
               $"SSL Mode={ssl};Trust Server Certificate=true";
    }

    private static bool LooksLikeUri(string value) =>
        value.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
        value.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase);

    private static string FromUri(string url)
    {
        var uri = new Uri(url);
        var userInfo = uri.UserInfo.Split(':', 2);

        var user = Uri.UnescapeDataString(userInfo[0]);
        var password = userInfo.Length > 1 ? Uri.UnescapeDataString(userInfo[1]) : "";
        var database = uri.AbsolutePath.TrimStart('/');
        var port = uri.Port > 0 ? uri.Port : 5432;

        // SSL: máy chủ nội bộ của Railway (*.railway.internal) không dùng TLS, còn mọi địa chỉ công
        // khai thì gần như luôn bắt buộc. Đoán sai bên nào cũng làm kết nối hỏng, nên tự chọn theo
        // tên máy chủ thay vì để người triển khai tự nhớ.
        var isInternal = uri.Host.EndsWith(".internal", StringComparison.OrdinalIgnoreCase) ||
                         uri.Host is "localhost" or "127.0.0.1";
        var ssl = isInternal ? "Disable" : "Require";

        return $"Host={uri.Host};Port={port};Database={database};" +
               $"Username={user};Password={password};SSL Mode={ssl};Trust Server Certificate=true";
    }
}
