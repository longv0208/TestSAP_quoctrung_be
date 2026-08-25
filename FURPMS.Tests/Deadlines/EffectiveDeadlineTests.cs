using FURPMS.Application.Constants;
using FURPMS.Application.Interfaces.Services;
using FURPMS.Domain.Entities.Cycles;
using FURPMS.Domain.Entities.MasterData;
using FURPMS.Domain.Entities.Proposals;
using FURPMS.Domain.Entities.Users;
using FURPMS.Infrastructure.Data;
using FURPMS.Infrastructure.Repositories;
using FURPMS.Infrastructure.Services;
using FURPMS.Tests.Helpers;
using FURPMS.Tests.Reminders; // reuse FakeClock

namespace FURPMS.Tests.Deadlines;

/// <summary>
/// Hồi quy cho lỗi thật phát hiện 25/08: <b>gia hạn đợt không có tác dụng với việc nộp</b>.
///
/// <para>Rule #19 quy định *"gia hạn deadline = LOG, không ghi đè; deadline hiệu lực = bản mới
/// nhất"*. Nhưng logic đọc bảng <c>deadline_extension</c> nằm <c>private</c> trong
/// <c>CycleService</c>, còn <c>ProposalService</c> lại so ngày với <c>SubmissionDeadline</c> thô.
/// Hậu quả: Admin gia hạn → hệ thống gửi thông báo *"đã gia hạn"* cho chủ nhiệm → chủ nhiệm bấm
/// nộp → <b>vẫn bị chặn theo hạn cũ</b>.</para>
/// </summary>
public class EffectiveDeadlineTests
{
    private static readonly DateOnly Today = new(2026, 6, 18);

    private static ProposalService MakeService(FURPMSDbContext db, FakeClock clock) => new(
        new ProposalRepository(db),
        new CycleRepository(db),
        new MasterDataRepository(db),
        new UserRepository(db),
        clock,
        new ReviewRoundService(new ReviewRepository(db), new ProposalRepository(db),
            new NotificationRepository(db), TestNotifier.Create(db), new FakeClock()),
        new ReviewRepository(db),
        new BudgetPolicyService(new ProposalRepository(db), new CycleRepository(db), new MasterDataRepository(db)),
        TestNotifier.Create(db),
        new TestAiSummaryQueue(),
        new DeadlineResolver(new CycleRepository(db)));

    private static async Task<(ResearchCycle cycle, Proposal proposal)> SeedAsync(
        FURPMSDbContext db, Guid piId, DateOnly submissionDeadline)
    {
        db.ResearchTypes.Add(new ResearchType { Id = 1, Code = "BASIC", Name = "Cơ bản", IsActive = true });
        db.ResearchTracks.Add(new ResearchTrack { Id = 1, Code = "IT", Name = "CNTT", IsActive = true });
        db.OrganizationalUnits.Add(new OrganizationalUnit
        {
            Id = 1, Code = "CNTT", Name = "Khoa CNTT", UnitType = "FACULTY", IsActive = true
        });
        await db.SaveChangesAsync();

        var cycle = new ResearchCycle
        {
            CycleYear = 2026,
            SemesterCode = "SU26",
            ResearchTypeId = 1,
            SubmissionOpenDate = Today.AddDays(-30),
            SubmissionDeadline = submissionDeadline,
            ReviewDeadline = submissionDeadline.AddDays(30),
            Status = CycleStatus.Open,
            CreatedBy = piId
        };
        db.ResearchCycles.Add(cycle);
        await db.SaveChangesAsync();

        var cycleTrack = new CycleTrack { CycleId = cycle.Id, TrackId = 1 };
        db.CycleTracks.Add(cycleTrack);
        var order = new ResearchOrder
        {
            CycleId = cycle.Id, OrderingUnitId = 1, ResearchArea = "Tự do",
            ProblemDescription = "Default", IsDefault = true, Status = "OPEN", CreatedBy = piId
        };
        db.ResearchOrders.Add(order);
        await db.SaveChangesAsync();

        var project = new FURPMS.Domain.Entities.Projects.Project
        {
            Id = Guid.NewGuid(),
            CycleTrackId = cycleTrack.Id,
            OrderId = order.Id,
            PiUserId = piId,
            HostingUnitId = 1,
            ResearchTypeId = 1,
            TitleVi = "Đề tài test",
            Status = "PROPOSED",
            PlannedStartDate = Today,
            PlannedEndDate = Today.AddMonths(12)
        };
        db.Projects.Add(project);
        var proposal = new Proposal
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            VersionNo = 1,
            IsCurrent = true,
            TitleVi = "Đề tài test",
            AbstractVi = "Tóm tắt",
            ResearchObjectives = "Mục tiêu",
            DurationMonths = 12,
            PlannedStartDate = Today,
            PlannedEndDate = Today.AddMonths(12),
            Status = ProposalStatus.Draft
        };
        db.Proposals.Add(proposal);

        // Nộp đề cương còn một cửa nữa: lý lịch khoa học phải được cập nhật gần đây (BM02).
        // Seed sẵn để test này chỉ kiểm ĐÚNG chuyện hạn nộp, không vướng cửa khác.
        db.AcademicProfiles.Add(new AcademicProfile
        {
            UserId = piId,
            UpdatedAt = Today.ToDateTime(TimeOnly.MinValue).AddDays(-1)
        });
        await db.SaveChangesAsync();

        return (cycle, proposal);
    }

    private static void Extend(FURPMSDbContext db, int cycleId, DateOnly old, DateOnly @new, DateTime createdAt)
    {
        db.DeadlineExtensions.Add(new DeadlineExtension
        {
            TargetType = IDeadlineResolver.TargetTypeCycle,
            TargetId = cycleId.ToString(),
            OldDeadline = old,
            NewDeadline = @new,
            Reason = "Trùng lịch thi",
            CreatedBy = Guid.NewGuid(),
            CreatedAt = createdAt
        });
        db.SaveChanges();
    }

    // ── Đây là lỗi người dùng gặp thật ────────────────────────────────────────
    [Fact]
    public async Task Submit_SauKhiGiaHan_ThiNopDuoc()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var pi = new User
        {
            Id = Guid.NewGuid(), Email = "pi@test.com", FullName = "PI", Status = UserStatus.Active
        };
        db.Users.Add(pi);
        await db.SaveChangesAsync();

        // Hạn gốc là HÔM QUA — nếu đọc hạn thô thì chắc chắn bị chặn.
        var (cycle, proposal) = await SeedAsync(db, pi.Id, Today.AddDays(-1));
        Extend(db, cycle.Id, Today.AddDays(-1), Today.AddDays(30), DateTime.UtcNow);

        var clock = new FakeClock { UtcNow = Today.ToDateTime(TimeOnly.MinValue) };
        var result = await MakeService(db, clock).SubmitProposalAsync(proposal.Id, pi.Id);

        Assert.Equal(ProposalStatus.Submitted, result.Status);

        // Ngày gốc trên đợt KHÔNG bị ghi đè — gia hạn chỉ là log (rule #19).
        var freshCycle = await db.ResearchCycles.FindAsync(cycle.Id);
        Assert.Equal(Today.AddDays(-1), freshCycle!.SubmissionDeadline);
    }

    [Fact]
    public async Task Submit_ChuaGiaHan_VanBiChanNhuCu()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var pi = new User
        {
            Id = Guid.NewGuid(), Email = "pi@test.com", FullName = "PI", Status = UserStatus.Active
        };
        db.Users.Add(pi);
        await db.SaveChangesAsync();

        var (_, proposal) = await SeedAsync(db, pi.Id, Today.AddDays(-1));
        var clock = new FakeClock { UtcNow = Today.ToDateTime(TimeOnly.MinValue) };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => MakeService(db, clock).SubmitProposalAsync(proposal.Id, pi.Id));
        Assert.Contains("quá hạn", ex.Message);
    }

    // ── Hạn hiệu lực = bản MỚI NHẤT, không phải bản có ngày xa nhất ───────────
    [Fact]
    public async Task HanHieuLuc_LayBanMoiNhat_KeCaKhiBanSauRutNgan()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var cycle = new ResearchCycle
        {
            CycleYear = 2026, SemesterCode = "SU26", ResearchTypeId = 1,
            SubmissionOpenDate = new DateOnly(2026, 1, 1),
            SubmissionDeadline = new DateOnly(2026, 3, 1),
            ReviewDeadline = new DateOnly(2026, 6, 1),
            Status = CycleStatus.Open, CreatedBy = Guid.NewGuid()
        };
        db.ResearchCycles.Add(cycle);
        await db.SaveChangesAsync();

        var t0 = new DateTime(2026, 2, 1, 10, 0, 0, DateTimeKind.Utc);
        Extend(db, cycle.Id, new DateOnly(2026, 3, 1), new DateOnly(2026, 5, 1), t0);
        // Quyết định sau rút ngắn lại — vẫn là quyết định hành chính hợp lệ.
        Extend(db, cycle.Id, new DateOnly(2026, 5, 1), new DateOnly(2026, 4, 1), t0.AddDays(1));

        var resolver = new DeadlineResolver(new CycleRepository(db));
        var effective = await resolver.EffectiveAsync(
            IDeadlineResolver.TargetTypeCycle, cycle.Id.ToString(), cycle.SubmissionDeadline);

        Assert.Equal(new DateOnly(2026, 4, 1), effective);
    }

    [Fact]
    public async Task HanHieuLuc_ChuaGiaHan_TraVeHanGoc()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var resolver = new DeadlineResolver(new CycleRepository(db));

        var original = new DateOnly(2026, 3, 1);
        var effective = await resolver.EffectiveAsync(IDeadlineResolver.TargetTypeCycle, "999", original);

        Assert.Equal(original, effective);
    }

    [Fact]
    public async Task HanHieuLuc_TraNhieuDoiTuong_TrongMotTruyVan()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var t0 = new DateTime(2026, 2, 1, 10, 0, 0, DateTimeKind.Utc);
        Extend(db, 1, new DateOnly(2026, 3, 1), new DateOnly(2026, 4, 1), t0);
        Extend(db, 2, new DateOnly(2026, 3, 1), new DateOnly(2026, 6, 1), t0);
        Extend(db, 2, new DateOnly(2026, 6, 1), new DateOnly(2026, 5, 1), t0.AddDays(2));

        var resolver = new DeadlineResolver(new CycleRepository(db));
        var map = await resolver.EffectiveManyAsync(IDeadlineResolver.TargetTypeCycle, new[] { "1", "2", "3" });

        Assert.Equal(new DateOnly(2026, 4, 1), map["1"]);
        Assert.Equal(new DateOnly(2026, 5, 1), map["2"]);   // bản mới nhất
        Assert.False(map.ContainsKey("3"));                  // chưa gia hạn ⇒ không có khoá
    }
}
