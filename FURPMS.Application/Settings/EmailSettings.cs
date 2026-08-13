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
    /// CHỈ DÙNG KHI DEV/DEMO. Hộp thư hứng mail của những địa chỉ **không có thật**
    /// (xem <see cref="RedirectDomains"/>), tiêu đề ghi kèm người đáng lẽ nhận.
    /// <para>
    /// Lý do: tài khoản seed dùng email không có thật (<c>pi.demo@furpms.edu.vn</c>…),
    /// test luồng sẽ không thấy mail nào. Đổi email seed thì hỏng — seeder dùng email
    /// làm khoá định danh. Chuyển hướng ở tầng gửi là chỗ đúng.
    /// </para>
    /// <para>PRODUCTION phải để TRỐNG, nếu không người dùng thật có thể mất thư.</para>
    /// <para>
    /// <b>Tên cũ là <c>RedirectAllTo</c></b> — đổi vì cái tên đó nói dối: từ 13/08 nó chỉ hứng
    /// mail của miền giả, không còn hứng tất cả. Người đọc cấu hình tưởng mọi thư đều bị giữ
    /// lại (đã xảy ra thật).
    /// </para>
    /// </summary>
    public string CatchFakeMailInbox { get; set; } = string.Empty;

    /// <summary>
    /// Các tên miền được coi là giả — chỉ mail gửi tới những miền này mới bị chuyển hướng
    /// về <see cref="CatchFakeMailInbox"/>. Địa chỉ thật (gmail, fpt.edu.vn…) vẫn đi thẳng tới
    /// người nhận, nên demo "tạo tài khoản bằng mail thật" xem được thư ở đúng hộp thư đó.
    /// <para>Để TRỐNG mà <see cref="CatchFakeMailInbox"/> có giá trị ⇒ chuyển hướng TẤT CẢ (hành vi cũ).</para>
    /// </summary>
    public string[] RedirectDomains { get; set; } = [];
}
