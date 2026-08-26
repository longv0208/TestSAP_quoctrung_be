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

    /// <summary>
    /// Cho phép chuyên viên <b>trả lời thư mời THAY</b> thành viên hội đồng.
    /// <para>
    /// Bật khi thực tế có thầy trả lời qua điện thoại/email rồi chuyên viên cần ghi nhận hộ.
    /// Mặc định tắt để trạng thái "đã chấp nhận" luôn do chính thành viên xác nhận; môi trường demo có thể
    /// bật lại trong Cài đặt mà không cần sửa code.
    /// </para>
    /// <para>
    /// Tắt khi cần <b>chỉ chấp nhận thành viên tự xác nhận</b> — lúc đó lời xác nhận mới thực sự
    /// là của người đứng tên. Dù bật, hệ thống luôn ghi lại ai đã bấm hộ
    /// (<c>council_members.responded_on_behalf_by</c>).
    /// </para>
    /// </summary>
    public const string CouncilAllowRespondOnBehalf = "COUNCIL_ALLOW_RESPOND_ON_BEHALF";
    public const bool DefaultCouncilAllowRespondOnBehalf = false;
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

    // ── Hạn của từng GIAI ĐOẠN đề tài (thêm 25/08) ────────────────────────────
    // Hội đồng bảo vệ lần 2: "cần thể hiện rõ các mốc thời gian deadline cho các giai đoạn của 1
    // đề tài". Mọi con số ngày đều để ở ĐÂY, không cắm vào code — cẩm nang chống trượt xếp hardcode
    // tham số nghiệp vụ là nguyên nhân trượt phổ biến thứ 2, và câu hỏi kinh điển của hội đồng là
    // "đổi con số này rồi demo ngay đi".

    /// <summary>Số ngày hội đồng có để chấm xong một vòng, tính từ lúc vòng được mở.</summary>
    public const string ScoringWindowDays = "SCORING_WINDOW_DAYS";
    public const int DefaultScoringWindowDays = 15;

    /// <summary>Số ngày chủ nhiệm có để nộp bản chỉnh sửa sau khi hội đồng yêu cầu sửa.</summary>
    public const string RevisionDeadlineDays = "REVISION_DEADLINE_DAYS";
    public const int DefaultRevisionDeadlineDays = 15;

    /// <summary>
    /// Báo cáo nghiệm thu phải nộp trước ngày kết thúc đề tài bao nhiêu ngày.
    /// <para><b>QĐ543 Điều 11.2.a</b> (nguyên văn): *"Chủ nhiệm đề tài phải nộp báo cáo nghiệm thu
    /// cho Phòng QLKH và Đơn vị chủ trì <b>ít nhất 30 ngày trước khi kết thúc đề tài</b>."*</para>
    /// </summary>
    public const string FinalReportLeadDays = "FINAL_REPORT_LEAD_DAYS";
    public const int DefaultFinalReportLeadDays = 30;

    /// <summary>Số ngày để ký hợp đồng sau khi hội đồng chốt duyệt đề cương.</summary>
    public const string ContractSignWindowDays = "CONTRACT_SIGN_WINDOW_DAYS";
    public const int DefaultContractSignWindowDays = 30;

    /// <summary>Số ngày để hoàn tất lưu trữ hồ sơ sau khi nộp báo cáo tổng kết.</summary>
    public const string ArchivalLeadDays = "ARCHIVAL_LEAD_DAYS";
    public const int DefaultArchivalLeadDays = 90;

    /// <summary>
    /// Hội đồng phải họp trong bao nhiêu ngày làm việc kể từ khi được lập.
    /// <para><b>QĐ543 Điều 8.3.a</b> — 15 ngày làm việc.</para>
    /// </summary>
    public const string MeetingDeadlineWorkingDays = "MEETING_DEADLINE_WORKING_DAYS";
    public const int DefaultMeetingDeadlineWorkingDays = 15;

    /// <summary>Chặn trên chung cho mọi khoá "số ngày" ở trên — Admin lỡ tay gõ 99999 thì chặn.</summary>
    public const int MaxStageWindowDays = 365;

    // ── Tài chính ─────────────────────────────────────────────────────────────
    /// <summary>Số đợt giải ngân cho đề tài cấp trọn gói (rule #6: tối thiểu 3 — đầu/giữa/cuối).</summary>

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
