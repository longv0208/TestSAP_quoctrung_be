namespace FURPMS.Application.Constants;

/// <summary>Khoá cấu hình vận hành trong bảng <c>system_settings</c>.</summary>
public static class SystemSettingKeys
{
    public const string UploadMaxFileSizeMb = "UPLOAD_MAX_FILE_SIZE_MB";
    public const string UploadAllowedExtensions = "UPLOAD_ALLOWED_EXTENSIONS";

    /// <summary>Mức khuyến cáo: hồ sơ QĐ 543 là văn bản (đề cương, lý lịch KH), 10 MB là dư.</summary>
    public const int RecommendedMaxFileSizeMb = 10;
    /// <summary>Chặn dưới — dưới 1 MB thì file Word có ảnh cũng không lọt.</summary>
    public const int MinAllowedFileSizeMb = 1;
    /// <summary>Chặn trên — tránh Admin lỡ tay đặt 10 GB làm sập ổ đĩa.</summary>
    public const int MaxAllowedFileSizeMb = 100;

    public const string DefaultAllowedExtensions = ".pdf,.doc,.docx,.xls,.xlsx,.png,.jpg,.jpeg";

    // ── Hội đồng ──────────────────────────────────────────────────────────────
    /// <summary>Số ngày reviewer được phép suy nghĩ trước khi xác nhận/từ chối lời mời (rule #13).</summary>
    public const string CouncilInviteDeadlineDays = "COUNCIL_INVITE_DEADLINE_DAYS";
    public const int DefaultCouncilInviteDeadlineDays = 7;

    /// <summary>
    /// Số chữ số thập phân cho phép khi chấm điểm (0 = chỉ số nguyên, 1 = cho 0.5 / 7.5…).
    /// QĐ543 **không quy định**; BM03 để điểm tối đa toàn số nguyên (10/20/40/20/10) nên mặc định 0.
    /// Admin chỉnh ở màn Cấu hình hệ thống — **không hồi tố**, chỉ áp cho phiếu chấm MỚI
    /// (cùng nguyên tắc với rule #13: đổi bộ tiêu chí active chỉ áp đề tài mới).
    /// </summary>
    public const string ScoreDecimalPlaces = "SCORE_DECIMAL_PLACES";
    public const int DefaultScoreDecimalPlaces = 0;

    // ── Nhắc hạn (thông báo) ──────────────────────────────────────────────────
    /// <summary>Các mốc nhắc trước hạn nộp sản phẩm, tính bằng ngày. Ví dụ "30,14,7".</summary>
    public const string DeadlineReminderDays = "DEADLINE_REMINDER_DAYS";
    // Thầy 29/07: "gần đến deadline thì gửi thông báo (ví dụ trước 3 ngày, quá hạn)" → thêm mốc 3.
    public const string DefaultDeadlineReminderDays = "30,14,7,3";

    /// <summary>Tắt để demo/thử nghiệm mà không gửi email thật ra ngoài. Thông báo trong app vẫn chạy.</summary>
    public const string EmailEnabled = "EMAIL_ENABLED";
    public const bool DefaultEmailEnabled = true;

    // ── Tài chính ─────────────────────────────────────────────────────────────
    /// <summary>Số đợt giải ngân cho đề tài cấp trọn gói (rule #6: tối thiểu 3 — đầu/giữa/cuối).</summary>
    public const string DisbursementWholeTranches = "DISBURSEMENT_WHOLE_TRANCHES";
    public const int DefaultDisbursementWholeTranches = 3;
    public const int MinDisbursementWholeTranches = 3;

    // ── Hợp đồng ──────────────────────────────────────────────────────────────
    /// <summary>Người đại diện Bên A ký hợp đồng, dùng khi tạo hợp đồng không ghi rõ.</summary>
    public const string ContractSideARepresentative = "CONTRACT_SIDE_A_REPRESENTATIVE";
    public const string DefaultContractSideARepresentative = "Nguyễn Kim Ánh";

    // ── Dữ liệu demo ──────────────────────────────────────────────────────────
    /// <summary>
    /// Bật/tắt bộ dữ liệu kịch bản demo (8 đề tài ở 8 bước khác nhau của quy trình).
    /// Đặt <c>false</c> TRƯỚC khi bàn giao bản chạy thật để cơ sở dữ liệu không dính đề tài giả.
    /// Tắt chỉ ngăn seed thêm — dữ liệu đã seed vẫn còn, muốn sạch thì xoá bằng tay.
    /// </summary>
    public const string DemoDataEnabled = "DEMO_DATA_ENABLED";
    public const bool DefaultDemoDataEnabled = true;
}
