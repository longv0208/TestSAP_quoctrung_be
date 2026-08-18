namespace FURPMS.Application.Constants;

// Gom các giá trị status/enum-string rải khắp service vào 1 chỗ để tránh typo
// & dễ đổi. Chỉ là hằng chuỗi (không phải logic) — dùng cho so sánh & gán status.
// Giá trị PHẢI khớp đúng chuỗi đang lưu trong DB (UPPERCASE).

public static class ProposalStatus
{
    public const string Draft = "DRAFT";
    public const string Submitted = "SUBMITTED";
    public const string Approved = "APPROVED";
    public const string Rejected = "REJECTED";
    public const string RevisionRequired = "REVISION_REQUIRED";
}

// Vòng đời TỔNG của đề tài (Project.Status) — Review 2: Project = thực thể trung tâm.
public static class ProjectStatus
{
    public const string Proposed = "PROPOSED";           // mới tạo (proposal nháp)
    public const string UnderReview = "UNDER_REVIEW";    // đã nộp, đang xét
    public const string Approved = "APPROVED";           // duyệt đề cương — chờ ký HĐ
    public const string InProgress = "IN_PROGRESS";      // HĐ ký, đang thực hiện
    public const string Acceptance = "ACCEPTANCE";       // nộp final report, đang nghiệm thu
    public const string Completed = "COMPLETED";
    public const string Cancelled = "CANCELLED";
    public const string Terminated = "TERMINATED";
}

public static class CycleStatus
{
    public const string Planning = "PLANNING";
    public const string Open = "OPEN";
    public const string Closed = "CLOSED";
}

public static class ReviewRoundStatus
{
    public const string Pending = "PENDING";
    public const string Open = "OPEN";
    public const string Passed = "PASSED";
    public const string Failed = "FAILED";
}

public static class ReviewRoundDimension
{
    public const string Science = "SCIENCE";
    public const string Finance = "FINANCE";
}

// Kết quả close round (request.Result) — ánh xạ sang trạng thái proposal.
public static class ReviewResult
{
    public const string Approved = "APPROVED";
    public const string Rejected = "REJECTED";
    public const string RevisionRequired = "REVISION_REQUIRED";
}

public static class CouncilStatus
{
    public const string Forming = "FORMING";
    public const string Decided = "DECIDED";
}

public static class CouncilMemberStatus
{
    public const string Assigned = "ASSIGNED";   // đã gán nhưng CHƯA gửi thư mời
    public const string Invited = "INVITED";
    public const string Confirmed = "CONFIRMED";
    public const string Declined = "DECLINED";
    public const string Expired = "EXPIRED";     // quá hạn xác nhận
}

// Chức danh trong hội đồng (CouncilMember.MemberRole) — chỉ là field, không tách actor (rule #11).
public static class CouncilMemberRole
{
    public const string Chair = "Chair";        // Chủ tịch — chốt kết quả + duyệt/khoá biên bản
    public const string Secretary = "Secretary"; // Thư ký — soạn biên bản
    public const string Opponent = "Opponent";  // Phản biện
    public const string Member = "Member";      // Thành viên
}

/// <summary>
/// Hình thức họp — chỉ 2 giá trị (thầy 05/08). Nền tảng cụ thể (Meet/Teams/Zoom) không còn
/// phân biệt; dữ liệu cũ được map về ONLINE khi đọc/ghi.
/// </summary>
public static class MeetingPlatform
{
    public const string InPerson = "IN_PERSON";
    public const string Online = "ONLINE";
}

public static class MeetingStatus
{
    public const string Scheduled = "SCHEDULED";
    public const string InProgress = "IN_PROGRESS";
    public const string Completed = "COMPLETED";
}

public static class ContractStatus
{
    public const string PendingSignature = "PENDING_SIGNATURE";
    public const string Active = "ACTIVE";
    public const string UnderReview = "UNDER_REVIEW";
    // Chỉ đạt trạng thái này khi Biên bản nghiệm thu & thanh lý BM13 đã được ký.
    // Nghiệm thu đề tài Đạt chỉ làm Project = COMPLETED, chưa tự coi hợp đồng đã thanh lý.
    public const string Settled = "SETTLED";
    public const string Terminated = "TERMINATED";
}

public static class DisbursementStatus
{
    public const string Pending = "PENDING";
    public const string Disbursed = "DISBURSED";
}

// Nghiệm thu sản phẩm (ProductDeliverable.AcceptanceStatus)
public static class AcceptanceStatus
{
    public const string Pending = "PENDING";
    public const string Passed = "PASSED";
    public const string Failed = "FAILED";
}

public static class ProgressReportStatus
{
    public const string Draft = "DRAFT";
    public const string Submitted = "SUBMITTED";
    public const string Evaluated = "EVALUATED";
}

public static class FinalReportStatus
{
    public const string Submitted = "SUBMITTED";
    public const string RevisionRequired = "REVISION_REQUIRED";
    public const string Accepted = "ACCEPTED";
    public const string Archived = "ARCHIVED";
}

public static class AmendmentStatus
{
    public const string Pending = "PENDING";
    public const string Approved = "APPROVED";
    public const string Rejected = "REJECTED";
}

public static class UserStatus
{
    public const string Active = "ACTIVE";
}

// Phương thức khoán kinh phí (Proposal.FundingMethod)
public static class FundingMethod
{
    public const string Whole = "WHOLE";
    public const string Partial = "PARTIAL";
}

// Kết quả đánh giá nghiệm thu (AcceptanceEvaluation.Result)
public static class EvaluationResult
{
    public const string Pass = "PASS";
    public const string Fail = "FAIL";
}
