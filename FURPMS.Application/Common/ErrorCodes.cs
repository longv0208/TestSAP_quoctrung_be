namespace FURPMS.Application.Common;

/// <summary>
/// Mã lỗi ổn định trả kèm mọi phản hồi thất bại.
///
/// <para>
/// <b>Vì sao cần.</b> Trước đây máy chủ chỉ trả một câu chữ, giao diện hiện nguyên văn. Cách đó
/// có ba điểm yếu:
/// </para>
/// <list type="number">
///   <item>Ngôn ngữ bị khoá cứng ở máy chủ. Dự án có bản tiếng Anh, nhưng thông báo lỗi thì
///         luôn ra thứ tiếng mà máy chủ viết sẵn.</item>
///   <item>Giao diện không thể <b>phản ứng</b> theo loại lỗi (vd hết phiên thì đăng xuất, xung đột
///         thì mời tải lại) vì phải đoán qua chuỗi chữ — sửa một dấu chấm là hỏng.</item>
///   <item>Đổi câu chữ cho dễ đọc hơn là có nguy cơ làm gãy chỗ khác đang so khớp chuỗi.</item>
/// </list>
///
/// <para>
/// <b>Cách dùng.</b> Máy chủ vẫn trả <c>message</c> tiếng Việt như hiện nay — đó là phương án dự
/// phòng khi giao diện chưa dịch mã đó. Giao diện ưu tiên tra <c>errors.&lt;mã&gt;</c> trong bảng
/// dịch, không có thì hiện <c>message</c>. Nhờ vậy chuyển đổi được <b>từng phần</b>, không phải
/// sửa 175 chỗ trong một lần rồi cầu mong không gãy gì.
/// </para>
/// </summary>
public static class ErrorCodes
{
    // ── Xác thực & phân quyền ───────────────────────────────────────────────
    /// <summary>Sai email/mật khẩu.</summary>
    public const string InvalidCredentials = "AUTH_INVALID_CREDENTIALS";
    /// <summary>Token hỏng/thiếu — giao diện nên đăng xuất.</summary>
    public const string SessionInvalid = "AUTH_SESSION_INVALID";
    public const string AccountInactive = "AUTH_ACCOUNT_INACTIVE";
    public const string PasswordIncorrect = "AUTH_PASSWORD_INCORRECT";
    public const string ResetTokenInvalid = "AUTH_RESET_TOKEN_INVALID";

    /// <summary>Đăng nhập rồi nhưng không đủ quyền với tài nguyên này.</summary>
    public const string Forbidden = "PERM_FORBIDDEN";
    public const string NotOwner = "PERM_NOT_OWNER";
    public const string NotCouncilMember = "PERM_NOT_COUNCIL_MEMBER";
    public const string ChairOnly = "PERM_CHAIR_ONLY";
    public const string SecretaryOnly = "PERM_SECRETARY_ONLY";

    // ── Không tìm thấy ──────────────────────────────────────────────────────
    public const string NotFound = "NOT_FOUND";

    // ── Dữ liệu không hợp lệ ────────────────────────────────────────────────
    public const string ValidationFailed = "VALIDATION_FAILED";
    public const string BudgetCapExceeded = "BUDGET_CAP_EXCEEDED";
    public const string BudgetCategoryCapExceeded = "BUDGET_CATEGORY_CAP_EXCEEDED";
    public const string DeadlineBeforeOpenDate = "CYCLE_DEADLINE_BEFORE_OPEN";
    public const string DuplicateCycle = "CYCLE_DUPLICATE";

    // ── Xung đột trạng thái ─────────────────────────────────────────────────
    public const string Conflict = "CONFLICT";
    public const string AlreadyLocked = "MINUTES_ALREADY_LOCKED";
    public const string SubmissionClosed = "SUBMISSION_CLOSED";
    public const string ContractAlreadySigned = "CONTRACT_ALREADY_SIGNED";
    public const string ContractNeedsSignedCopy = "CONTRACT_NEEDS_SIGNED_COPY";
    public const string ConflictOfInterest = "COUNCIL_CONFLICT_OF_INTEREST";
    public const string QuorumNotMet = "COUNCIL_QUORUM_NOT_MET";

    // ── Ngoài dự kiến ───────────────────────────────────────────────────────
    public const string Unexpected = "UNEXPECTED";
}
