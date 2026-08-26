namespace FURPMS.Application.DTOs.Timeline;

/// <summary>
/// Dòng thời gian của MỘT đề tài — mọi giai đoạn kèm hạn của từng giai đoạn.
///
/// <para><b>Vì sao có (25/08):</b> hội đồng bảo vệ lần 2 yêu cầu *"thể hiện rõ các mốc thời gian
/// deadline cho các giai đoạn của 1 đề tài"*. Trước đó hạn nằm rải rác ở 9 cột khác nhau, phần lớn
/// chỉ-lưu-không-ai-đọc, và riêng giai đoạn CHẤM thì không có hạn nào cả.</para>
///
/// <para>Đây là <b>read-model</b>: không có bảng nào tên "timeline". Service lắp từ các cột đã có,
/// và <b>scanner nhắc hạn dùng lại chính nó</b> ⇒ email nhắc và màn hình không bao giờ lệch nhau.</para>
/// </summary>
public class ProjectTimelineResponse
{
    public Guid ProjectId { get; set; }
    public string? ProjectCode { get; set; }
    public string? TitleVi { get; set; }
    public string? ProjectStatus { get; set; }
    public List<ProjectStageDto> Stages { get; set; } = new();

    /// <summary>Số giai đoạn đang quá hạn — để màn danh sách gắn cờ mà không phải tải cả dòng thời gian.</summary>
    public int OverdueCount { get; set; }
}

/// <summary>
/// Một hạn sắp tới của NGƯỜI ĐANG ĐĂNG NHẬP — gộp từ mọi đề tài họ có liên quan.
/// Dùng cho thẻ "Hạn sắp tới" trên bảng điều khiển.
/// </summary>
public class UpcomingDeadlineDto
{
    public Guid ProjectId { get; set; }
    public string? ProjectTitle { get; set; }
    public ProjectStageDto Stage { get; set; } = null!;
}

public class ProjectStageDto
{
    /// <summary>Mã cố định của giai đoạn — dùng làm khoá i18n <c>timeline.stage.*</c>.</summary>
    public string Code { get; set; } = null!;
    public int Order { get; set; }

    /// <summary>Hạn phải xong. <c>null</c> = giai đoạn này không có hạn hoặc chưa ai đặt.</summary>
    public string? Deadline { get; set; }

    /// <summary>
    /// Hạn này ở đâu ra. Hai trường <see cref="DeadlineSource"/> và <see cref="DeadlineBasis"/>
    /// sinh ra <b>để trả lời hội đồng</b>: bị hỏi "hạn này lấy từ đâu" thì màn hình tự nói, không
    /// phải giở code ra tìm.
    /// </summary>
    public string DeadlineSource { get; set; } = StageDeadlineSource.NotSet;

    /// <summary>Câu tiếng Việt giải thích căn cứ, ví dụ "QĐ543 Điều 11.2.a — 30 ngày trước khi kết thúc đề tài".</summary>
    public string? DeadlineBasis { get; set; }

    /// <summary>Ngày thực tế làm xong (nộp, ký, họp…). <c>null</c> = chưa xong.</summary>
    public string? ActualDate { get; set; }

    public string Status { get; set; } = StageStatus.NotStarted;

    /// <summary>Số ngày còn lại tới hạn; âm = đã quá hạn. <c>null</c> khi không có hạn.</summary>
    public int? DaysLeft { get; set; }

    /// <summary>Hạn đã bị dời so với hạn gốc (có dòng trong <c>deadline_extension</c>).</summary>
    public bool IsExtended { get; set; }

    /// <summary>Mở đúng bản ghi gốc — ví dụ <c>ReviewRound</c> + id để FE dẫn tới màn tương ứng.</summary>
    public string? EntityType { get; set; }
    public string? EntityId { get; set; }
}

/// <summary>Hạn của giai đoạn lấy từ đâu.</summary>
public static class StageDeadlineSource
{
    /// <summary>Hạn của ĐỢT nghiên cứu (research_cycle).</summary>
    public const string Cycle = "CYCLE";
    /// <summary>Hạn gốc đã bị dời — giá trị đang dùng là bản mới nhất trong deadline_extension.</summary>
    public const string Extension = "EXTENSION";
    /// <summary>Suy từ ngày trên hợp đồng.</summary>
    public const string Contract = "CONTRACT";
    /// <summary>Quy định của trường bắt buộc, không phải cấu hình tuỳ ý.</summary>
    public const string RuleQd543 = "RULE_QD543";
    /// <summary>Tính ra từ một mốc khác + số ngày cấu hình.</summary>
    public const string Derived = "DERIVED";
    /// <summary>Giai đoạn này chưa ai đặt hạn, hoặc bản chất không có hạn.</summary>
    public const string NotSet = "NOT_SET";
}

public static class StageStatus
{
    public const string NotStarted = "NOT_STARTED";
    public const string InProgress = "IN_PROGRESS";
    public const string Done = "DONE";
    /// <summary>Còn hạn nhưng sắp hết (trong vòng 7 ngày).</summary>
    public const string AtRisk = "AT_RISK";
    public const string Overdue = "OVERDUE";
    /// <summary>Đang làm nhưng không có hạn để so — nói thẳng thay vì bịa một ngày.</summary>
    public const string NoDeadline = "NO_DEADLINE";
}

/// <summary>Mã giai đoạn — thứ tự đúng vòng đời đề tài theo QĐ543.</summary>
public static class StageCodes
{
    public const string ProposalSubmission = "PROPOSAL_SUBMISSION";
    public const string Revision = "REVISION";
    public const string Review = "REVIEW";
    public const string ReviewMeeting = "REVIEW_MEETING";
    public const string ContractSigning = "CONTRACT_SIGNING";
    public const string ProgressReport = "PROGRESS_REPORT";
    public const string Deliverable = "DELIVERABLE";
    public const string Disbursement = "DISBURSEMENT";
    public const string FinalReport = "FINAL_REPORT";
    public const string AcceptanceMeeting = "ACCEPTANCE_MEETING";
    public const string Settlement = "SETTLEMENT";
    public const string Archival = "ARCHIVAL";
}
