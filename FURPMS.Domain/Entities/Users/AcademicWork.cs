namespace FURPMS.Domain.Entities.Users;

/// <summary>
/// Một dòng trong lý lịch khoa học (<b>QĐ543 — Biểu mẫu 02</b>): công trình đã công bố, sách,
/// bằng sở hữu trí tuệ, sản phẩm ứng dụng, đề tài đã chủ trì/tham gia, giải thưởng, hoặc
/// nghiên cứu sinh/học viên đã hướng dẫn.
///
/// <para>
/// <b>Vì sao có bảng này.</b> BM02 yêu cầu <b>cả hai</b>: số lượng <i>và</i> danh sách chi tiết.
/// Hệ thống trước đây chỉ làm phần số (mục 14.1–14.5, 16.1–16.2, 19.1–19.3) và bỏ trắng toàn bộ
/// phần liệt kê (14.6, 16.3, 17, 19.4) — tức là hồ sơ nộp lên <b>thiếu so với biểu mẫu</b>.
/// Nguyên văn BM02 mục <b>14.6</b>: <i>"Liệt kê đầy đủ các công bố nêu trên từ trước đến nay theo
/// thứ tự thời gian… (tên tác giả, năm xuất bản, tên công trình, tên tạp chí, volume, trang số)"</i>.
/// Thầy góp ý ở buổi demo 14/08 đúng chỗ này: <i>"đừng có đếm số mà phải xem được nguồn"</i>.
/// </para>
///
/// <para>
/// <b>Quan hệ với các ô đếm cũ.</b> Các ô đếm trong <see cref="AcademicProfile"/> vẫn còn nhưng
/// chuyển thành <b>số suy ra</b> — tính lại từ bảng này sau mỗi lần thêm/sửa/xoá. Nhờ vậy mục
/// 14.1–14.5 và 14.6 của biểu mẫu <b>không bao giờ mâu thuẫn nhau</b>, thứ mà khai tay hai chỗ
/// riêng biệt gần như chắc chắn sẽ lệch.
/// </para>
/// </summary>
public class AcademicWork
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }

    /// <summary>Ứng với <b>mục nào của BM02</b> — xem <see cref="WorkTypes"/>.</summary>
    public string WorkType { get; set; } = WorkTypes.Publication;

    /// <summary>
    /// Phân loại trong mục — <b>thứ quyết định dòng này được đếm vào ô thống kê nào</b>.
    /// Xem <see cref="WorkCategories"/>.
    /// </summary>
    public string Category { get; set; } = WorkCategories.JournalDomestic;

    /// <summary>
    /// Tên công trình · tên sách · tên và nội dung văn bằng · tên sản phẩm ·
    /// tên nhiệm vụ · nội dung giải thưởng · tên luận án/luận văn.
    /// </summary>
    public string Title { get; set; } = null!;

    /// <summary>
    /// Nơi công bố / nơi cấp — <b>phần thầy nhấn mạnh phải xem được</b>. Tuỳ mục mà là:
    /// tên tạp chí hoặc hội nghị (14.6) · nhà xuất bản (13) · nơi cấp văn bằng (15) ·
    /// cơ quan quản lý nhiệm vụ, thuộc Chương trình (17) · tổ chức tặng thưởng (18) ·
    /// cơ quan công tác của NCS/học viên (19.4).
    /// </summary>
    public string? Venue { get; set; }

    /// <summary>
    /// Tên tác giả theo đúng thứ tự in trên công trình (13, 14.6) —
    /// hoặc <b>họ tên nghiên cứu sinh/thạc sĩ</b> ở mục 19.4.
    /// </summary>
    public string? Authors { get; set; }

    /// <summary>
    /// Vai trò của chủ hồ sơ. Xem <see cref="WorkRoles"/>. Mục 17 tách chủ trì (17.1) khỏi
    /// tham gia (17.2); mục 19.4 đòi ghi rõ <i>"vai trò hướng dẫn (chính hay phụ)"</i>.
    /// </summary>
    public string? Role { get; set; }

    /// <summary>Năm xuất bản / năm cấp / năm tặng thưởng / năm kết thúc nhiệm vụ.</summary>
    public int? Year { get; set; }

    /// <summary>Năm bắt đầu — mục 17 và 19.4 ghi <i>khoảng thời gian</i> chứ không phải một mốc.</summary>
    public int? StartYear { get; set; }

    /// <summary>
    /// Mã tra cứu — <b>thứ khiến công trình kiểm chứng được</b>: DOI/ISSN (14.6) ·
    /// ISBN (13) · <i>"Số, Ký mã hiệu"</i> của văn bằng (15) · mã số nhiệm vụ (17).
    /// </summary>
    public string? Identifier { get; set; }

    /// <summary>Số quyển của tạp chí — BM02 mục 14.6 đòi đích danh <i>"volume"</i>.</summary>
    public string? Volume { get; set; }

    /// <summary>Số trang — BM02 mục 14.6 đòi đích danh <i>"trang số"</i>.</summary>
    public string? Pages { get; set; }

    /// <summary>
    /// Tình trạng nhiệm vụ — BM02 mục 17 đòi ghi rõ
    /// <i>"đã nghiệm thu / chưa nghiệm thu / không hoàn thành"</i>. Xem <see cref="WorkStatuses"/>.
    /// </summary>
    public string? Status { get; set; }

    /// <summary>Đường dẫn tra cứu trực tiếp (trang bài báo, DOI, thư viện số…).</summary>
    public string? Url { get; set; }

    /// <summary>
    /// Ghi chú. Mục 16.3 dùng ô này cho <i>"thời gian, hình thức, quy mô, địa chỉ áp dụng"</i>
    /// và <i>"công dụng"</i> của sản phẩm; chỗ khác dùng ghi xếp hạng tạp chí (Q1/Q2)…
    /// </summary>
    public string? Note { get; set; }

    /// <summary>
    /// Thứ tự hiển thị trong cùng một mục. BM02 mục 14.6 yêu cầu
    /// <i>"ưu tiên các dòng đầu cho 5 công trình tiêu biểu, xuất sắc nhất"</i> — người khai tự
    /// xếp, hệ thống không đoán hộ.
    /// </summary>
    public int SortOrder { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
}

/// <summary>Mỗi giá trị ứng với đúng một mục của BM02 — đặt tên theo mục để đối chiếu biểu mẫu.</summary>
public static class WorkTypes
{
    /// <summary>BM02 mục 13 — Sách, sách chuyên khảo, giáo trình.</summary>
    public const string Book = "BOOK";

    /// <summary>BM02 mục 14.6 — Các công trình khoa học đã công bố.</summary>
    public const string Publication = "PUBLICATION";

    /// <summary>BM02 mục 15 — Bằng sở hữu trí tuệ đã được cấp.</summary>
    public const string Patent = "PATENT";

    /// <summary>BM02 mục 16.3 — Sản phẩm được ứng dụng, chuyển giao.</summary>
    public const string Application = "APPLICATION";

    /// <summary>BM02 mục 17 — Các đề tài KH&amp;CN các cấp đã chủ trì hoặc tham gia.</summary>
    public const string Project = "PROJECT";

    /// <summary>BM02 mục 18 — Giải thưởng về KH&amp;CN trong và ngoài nước.</summary>
    public const string Award = "AWARD";

    /// <summary>BM02 mục 19.4 — Quá trình tham gia đào tạo sau đại học.</summary>
    public const string Supervision = "SUPERVISION";

    public static readonly string[] All =
        [Book, Publication, Patent, Application, Project, Award, Supervision];
}

/// <summary>
/// Phân loại trong từng mục. Tên bám sát các ô đếm của BM02 để việc tính số suy ra là
/// ánh xạ một-một, không phải suy đoán.
/// </summary>
public static class WorkCategories
{
    // ── Mục 14: 5 loại công bố, đúng thứ tự 14.1 → 14.5 ──
    /// <summary>14.1 — tạp chí quốc tế thuộc ISI/SCOPUS.</summary>
    public const string IsiScopus = "ISI_SCOPUS";
    /// <summary>14.2 — tạp chí quốc tế KHÔNG thuộc ISI/SCOPUS.</summary>
    public const string JournalIntl = "JOURNAL_INTL";
    /// <summary>14.3 — tạp chí chuyên ngành trong nước.</summary>
    public const string JournalDomestic = "JOURNAL_DOMESTIC";
    /// <summary>14.4 — báo cáo hội nghị khoa học quốc tế.</summary>
    public const string ConferenceIntl = "CONFERENCE_INTL";
    /// <summary>14.5 — báo cáo hội nghị khoa học trong nước.</summary>
    public const string ConferenceDomestic = "CONFERENCE_DOMESTIC";

    // ── Mục 13 ──
    public const string BookMonograph = "BOOK_MONOGRAPH";   // sách chuyên khảo
    public const string BookTextbook = "BOOK_TEXTBOOK";     // giáo trình

    // ── Mục 15 ──
    public const string PatentGranted = "PATENT_GRANTED";

    // ── Mục 16: 16.1 nước ngoài · 16.2 trong nước ──
    public const string AppliedAbroad = "APPLIED_ABROAD";
    public const string AppliedDomestic = "APPLIED_DOMESTIC";

    // ── Mục 17: 17.1 chủ trì · 17.2 tham gia. BM02 tách theo VAI TRÒ, không theo cấp. ──
    public const string ProjectLead = "PROJECT_LEAD";
    public const string ProjectMember = "PROJECT_MEMBER";

    // ── Mục 18 ──
    public const string AwardGeneral = "AWARD_GENERAL";

    // ── Mục 19: 19.1 tiến sĩ đã đào tạo · 19.3 thạc sĩ đã đào tạo ──
    public const string PhdSupervision = "PHD";
    public const string MasterSupervision = "MASTER";

    public static readonly string[] Publication =
        [IsiScopus, JournalIntl, JournalDomestic, ConferenceIntl, ConferenceDomestic];

    public static readonly string[] Book = [BookMonograph, BookTextbook];
    public static readonly string[] Patent = [PatentGranted];
    public static readonly string[] Application = [AppliedAbroad, AppliedDomestic];
    public static readonly string[] Project = [ProjectLead, ProjectMember];
    public static readonly string[] Award = [AwardGeneral];
    public static readonly string[] Supervision = [PhdSupervision, MasterSupervision];

    /// <summary>Phân loại nào hợp lệ với mục nào — dùng để chặn dữ liệu vô nghĩa khi ghi.</summary>
    public static string[] For(string workType) => workType switch
    {
        WorkTypes.Publication => Publication,
        WorkTypes.Book => Book,
        WorkTypes.Patent => Patent,
        WorkTypes.Application => Application,
        WorkTypes.Project => Project,
        WorkTypes.Award => Award,
        WorkTypes.Supervision => Supervision,
        _ => []
    };
}

/// <summary>Vai trò của chủ hồ sơ trong công trình.</summary>
public static class WorkRoles
{
    // Công bố / sách
    public const string MainAuthor = "MAIN_AUTHOR";
    public const string CoAuthor = "CO_AUTHOR";
    public const string Corresponding = "CORRESPONDING";

    // Đề tài (mục 17)
    public const string Lead = "LEAD";
    public const string Member = "MEMBER";

    // Hướng dẫn SĐH (mục 19.4 — "chính hay phụ")
    public const string MainSupervisor = "MAIN_SUPERVISOR";
    public const string CoSupervisor = "CO_SUPERVISOR";

    public static readonly string[] All =
        [MainAuthor, CoAuthor, Corresponding, Lead, Member, MainSupervisor, CoSupervisor];
}

/// <summary>Tình trạng nhiệm vụ — nguyên văn 3 giá trị BM02 mục 17 liệt kê.</summary>
public static class WorkStatuses
{
    public const string Accepted = "ACCEPTED";       // đã nghiệm thu
    public const string InProgress = "IN_PROGRESS";  // chưa nghiệm thu
    public const string Failed = "FAILED";           // không hoàn thành

    public static readonly string[] All = [Accepted, InProgress, Failed];
}
