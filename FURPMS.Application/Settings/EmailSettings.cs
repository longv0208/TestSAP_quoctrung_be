namespace FURPMS.Application.Settings;

public class EmailSettings
{
    public string SmtpServer { get; set; } = string.Empty;
    public int SmtpPort { get; set; } = 587;
    public string SmtpUsername { get; set; } = string.Empty;
    public string SmtpPassword { get; set; } = string.Empty;
    public string FromEmail { get; set; } = string.Empty;
    public string FromName { get; set; } = "FURPMS System";

    /// <summary>
    /// Gốc URL của FE, để ghép với <c>ActionUrl</c> của thông báo thành link bấm được
    /// trong email (thông báo in-app chỉ lưu đường dẫn tương đối như <c>/proposals/my</c>).
    /// Local: <c>http://localhost:5173</c> — prod: đặt qua env <c>EmailSettings__FrontendUrl</c>.
    /// </summary>
    public string FrontendUrl { get; set; } = "http://localhost:5173";

    /// <summary>
    /// CHỈ DÙNG KHI DEV/DEMO. Có giá trị thì **mọi** email đều gửi về đúng địa chỉ này
    /// thay vì người nhận thật, tiêu đề ghi kèm người đáng lẽ nhận.
    /// <para>
    /// Lý do: tài khoản seed dùng email không có thật (<c>pi.demo@furpms.edu.vn</c>…),
    /// test luồng sẽ không thấy mail nào. Đổi email seed thì hỏng — seeder dùng email
    /// làm khoá định danh. Chuyển hướng ở tầng gửi là chỗ đúng.
    /// </para>
    /// <para>PRODUCTION phải để TRỐNG, nếu không mọi người dùng thật đều mất thư.</para>
    /// </summary>
    public string RedirectAllTo { get; set; } = string.Empty;
}
