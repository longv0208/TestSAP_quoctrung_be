using FURPMS.Application.Interfaces.Services;
using FURPMS.Application.Interfaces;
using FURPMS.Infrastructure.Data;
using FURPMS.Infrastructure.Repositories;
using FURPMS.Infrastructure.Services;
using FURPMS.Tests.Reminders; // FakeClock
using Microsoft.Extensions.Logging.Abstractions;

namespace FURPMS.Tests.Helpers;

/// <summary>
/// Xưởng dựng service cho test.
///
/// <para><b>Vì sao có (25/08):</b> mấy service lõi được `new` thẳng ở hàng chục chỗ trong bộ test,
/// nên mỗi lần thêm một dependency là <b>8–10 file test đỏ cùng lúc</b> dù chẳng test nào sai —
/// đã dính ba lần liên tiếp khi làm nhóm deadline. Gom về đây thì thêm dependency chỉ phải sửa
/// một chỗ.</para>
///
/// <para>Chỉ đặt ở đây những service <b>nhiều test cùng dựng</b>. Service chỉ một file dùng thì cứ
/// `new` tại chỗ cho dễ đọc.</para>
/// </summary>
public static class TestServices
{
    /// <summary>
    /// Bộ ghi sổ quyết định — dùng <b>bản thật</b> chứ không phải bản giả.
    ///
    /// <para>Nó chỉ <c>Add</c> vào <c>DbContext</c> đang có nên chạy được nguyên vẹn trên
    /// <c>UseInMemoryDatabase</c>, và như vậy test nào chốt biên bản/ký hợp đồng cũng đồng thời
    /// kiểm luôn được là sổ quyết định có ghi đúng — thứ mà một bản giả sẽ giấu mất.</para>
    /// </summary>
    public static DecisionLogger Decisions(FURPMSDbContext db, IClock? clock = null) => new(
        db, clock ?? new FakeClock(), NullLogger<DecisionLogger>.Instance);

    public static ReviewRoundService ReviewRounds(FURPMSDbContext db, IClock? clock = null) => new(
        new ReviewRepository(db),
        new ProposalRepository(db),
        new NotificationRepository(db),
        TestNotifier.Create(db),
        clock ?? new FakeClock(),
        new SystemSettingService(new MasterDataRepository(db)),
        new DeadlineResolver(new CycleRepository(db)),
        new CycleRepository(db));

    public static ProposalService Proposals(FURPMSDbContext db, IClock clock) => new(
        new ProposalRepository(db),
        new CycleRepository(db),
        new MasterDataRepository(db),
        new UserRepository(db),
        clock,
        ReviewRounds(db),
        new ReviewRepository(db),
        new BudgetPolicyService(new ProposalRepository(db), new CycleRepository(db), new MasterDataRepository(db)),
        TestNotifier.Create(db),
        new TestAiSummaryQueue(),
        new DeadlineResolver(new CycleRepository(db)),
        Decisions(db, clock));

    public static DeadlineReminderScanner DeadlineScanner(
        FURPMSDbContext db, IClock clock, IEmailService email) => new(
        new ContractRepository(db),
        new NotificationRepository(db),
        clock,
        email,
        new SystemSettingService(new MasterDataRepository(db)),
        TestNotifier.Create(db),
        new ReviewRepository(db),
        new DeadlineResolver(new CycleRepository(db)));

    public static ContractService Contracts(FURPMSDbContext db, IClock clock) => new(
        new ContractRepository(db),
        new ProposalRepository(db),
        clock,
        new SystemSettingService(new MasterDataRepository(db)),
        TestNotifier.Create(db),
        new DocumentRepository(db),
        Decisions(db, clock));

    public static ReviewScoringService ReviewScoring(FURPMSDbContext db, IClock? clock = null) => new(
        new ReviewRepository(db),
        new MasterDataRepository(db),
        new ProposalRepository(db),
        clock ?? new FakeClock(),
        new SystemSettingService(new MasterDataRepository(db)),
        TestNotifier.Create(db),
        Decisions(db, clock));

    public static DeliverableService Deliverables(FURPMSDbContext db, IClock? clock = null) => new(
        new ContractRepository(db),
        new UserRepository(db),
        new NotificationRepository(db),
        TestNotifier.Create(db),
        clock ?? new FakeClock(),
        Decisions(db, clock));

    public static DisbursementService Disbursements(FURPMSDbContext db, IClock? clock = null) => new(
        new ContractRepository(db),
        new MasterDataRepository(db),
        clock ?? new FakeClock(),
        new SystemSettingService(new MasterDataRepository(db)),
        TestNotifier.Create(db),
        new DocumentRepository(db),
        Decisions(db, clock));

    public static ProgressReportService ProgressReports(FURPMSDbContext db, IClock? clock = null) => new(
        new ContractRepository(db),
        new ProposalRepository(db),
        clock ?? new FakeClock(),
        new DocumentRepository(db),
        TestNotifier.Create(db),
        Decisions(db, clock));

    public static FinalReportService FinalReports(FURPMSDbContext db, IClock? clock = null) => new(
        new ContractRepository(db),
        clock ?? new FakeClock(),
        new SystemSettingService(new MasterDataRepository(db)),
        Decisions(db, clock));

    public static ContractSettlementService Settlements(FURPMSDbContext db, IClock? clock = null) => new(
        new ContractRepository(db),
        new UserRepository(db),
        clock ?? new FakeClock(),
        Decisions(db, clock));

    public static AmendmentService Amendments(FURPMSDbContext db, IClock? clock = null) => new(
        new ContractRepository(db),
        new MasterDataRepository(db),
        clock ?? new FakeClock(),
        Decisions(db, clock));

    public static CycleService Cycles(FURPMSDbContext db) => new(
        new CycleRepository(db),
        new MasterDataRepository(db),
        new ProposalRepository(db),
        new ReviewRepository(db),
        TestNotifier.Create(db),
        new DeadlineResolver(new CycleRepository(db)));
}
