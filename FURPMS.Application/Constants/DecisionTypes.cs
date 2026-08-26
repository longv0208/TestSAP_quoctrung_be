namespace FURPMS.Application.Constants;

/// <summary>
/// Các loại quyết định được ghi vào sổ hồ sơ đề tài (<c>project_decisions</c>).
///
/// <para>Là hằng chuỗi chứ không phải enum: dữ liệu đã ghi phải đọc được kể cả khi mã nguồn sau này
/// thêm/bớt loại, và đây cũng là khoá i18n bên giao diện.</para>
/// </summary>
public static class DecisionTypes
{
    // ── Giai đoạn đề cương ───────────────────────────────────────────────
    public const string ProposalSubmitted = "PROPOSAL_SUBMITTED";
    public const string ProposalWithdrawn = "PROPOSAL_WITHDRAWN";
    public const string ProposalRevised = "PROPOSAL_REVISED";
    public const string ChangeRequestReviewed = "CHANGE_REQUEST_REVIEWED";

    // ── Hội đồng & chấm ──────────────────────────────────────────────────
    public const string CouncilEstablished = "COUNCIL_ESTABLISHED";
    public const string CouncilDecision = "COUNCIL_DECISION";
    public const string RoundResult = "ROUND_RESULT";
    /// <summary>Kết luận lệch điểm trung bình — nối sang nhóm 5 (cảnh báo điểm thấp mà vẫn Đạt).</summary>
    public const string ScoreDivergenceJustified = "SCORE_DIVERGENCE_JUSTIFIED";
    /// <summary>Gán ủy viên ngoài lĩnh vực chuyên môn kèm lý do — nối sang nhóm 6.</summary>
    public const string ExpertiseOverride = "EXPERTISE_OVERRIDE";

    // ── Hợp đồng ─────────────────────────────────────────────────────────
    public const string ContractCreated = "CONTRACT_CREATED";
    public const string ContractSigned = "CONTRACT_SIGNED";
    public const string ContractTerminated = "CONTRACT_TERMINATED";
    public const string AmendmentApproved = "AMENDMENT_APPROVED";

    // ── Thực hiện & nghiệm thu ───────────────────────────────────────────
    public const string ProgressEvaluated = "PROGRESS_EVALUATED";
    public const string DeliverableAccepted = "DELIVERABLE_ACCEPTED";
    public const string DisbursementConfirmed = "DISBURSEMENT_CONFIRMED";
    public const string FinalReportApproved = "FINAL_REPORT_APPROVED";
    public const string SettlementSigned = "SETTLEMENT_SIGNED";

    // ── Khác ─────────────────────────────────────────────────────────────
    public const string DeadlineExtended = "DEADLINE_EXTENDED";
    /// <summary>Kết luận của Phòng QLKH sau khi xem cảnh báo trùng lặp — nối sang nhóm 4.</summary>
    public const string DuplicateReviewed = "DUPLICATE_REVIEWED";

    /// <summary>
    /// Thứ tự trong vòng đời đề tài — dùng để xếp các quyết định <b>cùng một ngày</b>.
    ///
    /// <para><b>Vì sao không xếp thuần theo giờ:</b> một số mốc chỉ có NGÀY chứ không có giờ, vì
    /// bản chất nghiệp vụ là vậy — hợp đồng ký ngoài hệ thống, người dùng khai lại ngày ký nên
    /// <c>signed_at</c> là 00:00. Xếp thuần theo giờ thì mốc ký hợp đồng nhảy lên trước cả mốc nộp
    /// đề cương của cùng ngày hôm đó, đọc vào tưởng hệ thống ghi sai.</para>
    ///
    /// <para>Nên: ngày trước — rồi tới thứ tự vòng đời — cuối cùng mới tới giờ. Khác ngày thì thời
    /// gian thật vẫn quyết định, không có chuyện xếp lại lịch sử.</para>
    /// </summary>
    public static int LifecycleOrder(string decisionType) => decisionType switch
    {
        ProposalSubmitted => 10,
        DuplicateReviewed => 15,
        ProposalRevised => 20,
        ProposalWithdrawn => 25,
        ChangeRequestReviewed => 30,

        CouncilEstablished => 40,
        ExpertiseOverride => 45,
        RoundResult => 50,
        ScoreDivergenceJustified => 55,
        CouncilDecision => 60,

        ContractCreated => 70,
        ContractSigned => 75,
        AmendmentApproved => 80,

        ProgressEvaluated => 90,
        DeliverableAccepted => 95,
        DisbursementConfirmed => 100,

        FinalReportApproved => 110,
        SettlementSigned => 120,
        ContractTerminated => 125,

        DeadlineExtended => 130,
        _ => 200
    };

    /// <summary>Nhóm giai đoạn để giao diện xếp hồ sơ theo chặng, không phải một danh sách phẳng.</summary>
    public static string StageOf(string decisionType) => decisionType switch
    {
        ProposalSubmitted or ProposalWithdrawn or ProposalRevised
            or ChangeRequestReviewed or DuplicateReviewed => "PROPOSAL",

        CouncilEstablished or CouncilDecision or RoundResult
            or ScoreDivergenceJustified or ExpertiseOverride => "REVIEW",

        ContractCreated or ContractSigned or ContractTerminated
            or AmendmentApproved => "CONTRACT",

        ProgressEvaluated or DeliverableAccepted or DisbursementConfirmed => "EXECUTION",

        FinalReportApproved or SettlementSigned => "CLOSING",

        // Gia hạn có thể xảy ra ở BẤT KỲ chặng nào (hạn nộp đề cương, hạn chấm, hạn hợp đồng), nên
        // không nhét vừa chặng nào cả — cho nó một nhóm riêng thay vì để rơi vào "OTHER".
        DeadlineExtended => "SCHEDULE",

        // "OTHER" chỉ dành cho chuỗi lạ đọc lên từ dữ liệu cũ; mọi loại đang khai đều phải có chặng
        // (khoá bằng test MoiLoaiQuyetDinh_DeuCoChang_VaThuTuVongDoi).
        _ => "OTHER"
    };
}
