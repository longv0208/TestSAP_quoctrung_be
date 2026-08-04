using FURPMS.Application.Common;
using FURPMS.Application.Constants;
using FURPMS.Domain.Entities.Cycles;
using FURPMS.Domain.Entities.MasterData;
using FURPMS.Domain.Entities.Proposals;
using FURPMS.Domain.Entities.Users;
using FURPMS.Infrastructure.Data;
using FURPMS.Infrastructure.Repositories;
using FURPMS.Infrastructure.Services;
using FURPMS.Tests.Helpers;
using FURPMS.Tests.Reminders; // reuse FakeClock

namespace FURPMS.Tests.Proposals;

// Guard tests cho SubmitProposalAsync — trọng tâm là chặn nộp quá hạn (409),
// dùng FakeClock để tua thời gian thay vì phụ thuộc DateTime.UtcNow thật.
public class ProposalSubmissionTests
{
    private static readonly DateOnly Today = new(2026, 6, 18);

    private static User MakePi() => new()
    {
        Id = Guid.NewGuid(),
        Email = $"pi-{Guid.NewGuid():N}"[..20] + "@test.com",
        FullName = "PI User",
        Status = UserStatus.Active
    };

    private static async Task<(ResearchCycle cycle, Proposal proposal)> SeedAsync(
        FURPMSDbContext db, Guid piId, DateOnly submissionDeadline, string proposalStatus)
    {
        // Master data cho các navigation bắt buộc của Proposal (ResearchType/Track/Unit)
        // — GetProposalByIdAsync Include hết, thiếu row sẽ làm query InMemory rớt proposal.
        db.ResearchTypes.Add(new ResearchType { Id = 1, Code = "BASIC", Name = "Cơ bản", IsActive = true });
        db.ResearchTracks.Add(new ResearchTrack { Id = 1, Code = "IT", Name = "CNTT", IsActive = true });
        db.OrganizationalUnits.Add(new OrganizationalUnit { Id = 1, Code = "CNTT", Name = "Khoa CNTT", UnitType = "FACULTY", IsActive = true });
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

        // Project-centric: cycle_track + order mặc định + PROJECT gốc + proposal v1.
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
            Status = proposalStatus
        };
        db.Proposals.Add(proposal);
        await db.SaveChangesAsync();

        return (cycle, proposal);
    }

    [Fact]
    public async Task GetProposalById_NonOwnerNonStaff_ThrowsForbidden()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var pi = MakePi();
        db.Users.Add(pi);
        await db.SaveChangesAsync();
        var (_, proposal) = await SeedAsync(db, pi.Id, Today.AddDays(30), "SUBMITTED");
        var service = MakeService(db, new FakeClock());

        // Người lạ (không PI, không thành viên hội đồng, không Staff/Admin) → 403 (chống IDOR)
        await Assert.ThrowsAsync<ForbiddenException>(
            () => service.GetProposalByIdAsync(proposal.Id, Guid.NewGuid(), Array.Empty<string>()));

        // PI chủ đề cương → xem được
        var mine = await service.GetProposalByIdAsync(proposal.Id, pi.Id, Array.Empty<string>());
        Assert.Equal(proposal.Id, mine.Id);

        // Staff → xem được
        var asStaff = await service.GetProposalByIdAsync(proposal.Id, Guid.NewGuid(), new[] { "Staff" });
        Assert.Equal(proposal.Id, asStaff.Id);
    }

    private static ProposalService MakeService(FURPMSDbContext db, FakeClock clock) => new(
        new ProposalRepository(db),
        new CycleRepository(db),
        new MasterDataRepository(db),
        new UserRepository(db),
        clock,
        new ReviewRoundService(new ReviewRepository(db), new ProposalRepository(db), new NotificationRepository(db), TestNotifier.Create(db), new FakeClock()),
        new ReviewRepository(db));

    // ── 1: nộp quá hạn → 409 (InvalidOperationException) ─────────────────────
    [Fact]
    public async Task Submit_AfterDeadline_Throws_409()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var pi = MakePi();
        db.Users.Add(pi);
        await db.SaveChangesAsync();

        // hạn nộp = hôm qua → hôm nay đã quá hạn
        var (_, proposal) = await SeedAsync(db, pi.Id, Today.AddDays(-1), ProposalStatus.Draft);
        var clock = new FakeClock { UtcNow = Today.ToDateTime(TimeOnly.MinValue) };
        var service = MakeService(db, clock);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.SubmitProposalAsync(proposal.Id, pi.Id));
        Assert.Contains("quá hạn", ex.Message);

        // status KHÔNG đổi
        var after = await db.Proposals.FindAsync(proposal.Id);
        Assert.Equal(ProposalStatus.Draft, after!.Status);
    }

    // ── 2: nộp trước hạn → SUBMITTED + SubmittedAt được set ──────────────────
    [Fact]
    public async Task Submit_BeforeDeadline_Succeeds()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var pi = MakePi();
        db.Users.Add(pi);
        await db.SaveChangesAsync();

        var (_, proposal) = await SeedAsync(db, pi.Id, Today.AddDays(5), ProposalStatus.Draft);
        var clock = new FakeClock { UtcNow = Today.ToDateTime(TimeOnly.MinValue) };
        var service = MakeService(db, clock);

        var result = await service.SubmitProposalAsync(proposal.Id, pi.Id, confirmCvUpToDate: true);

        Assert.Equal(ProposalStatus.Submitted, result.Status);
        var after = await db.Proposals.FindAsync(proposal.Id);
        Assert.Equal(ProposalStatus.Submitted, after!.Status);
        Assert.NotNull(after.SubmittedAt);
    }

    // ── 3: nộp ĐÚNG ngày hạn → vẫn được (check là today > deadline, strict) ──
    [Fact]
    public async Task Submit_OnDeadlineDay_Succeeds()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var pi = MakePi();
        db.Users.Add(pi);
        await db.SaveChangesAsync();

        var (_, proposal) = await SeedAsync(db, pi.Id, Today, ProposalStatus.Draft);
        var clock = new FakeClock { UtcNow = Today.ToDateTime(TimeOnly.MinValue) };
        var service = MakeService(db, clock);

        var result = await service.SubmitProposalAsync(proposal.Id, pi.Id, confirmCvUpToDate: true);
        Assert.Equal(ProposalStatus.Submitted, result.Status);
    }

    // ── 4: chỉ PI mới được nộp → người khác = 401 ────────────────────────────
    [Fact]
    public async Task Submit_ByNonPi_Throws_403()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var pi = MakePi();
        db.Users.Add(pi);
        await db.SaveChangesAsync();

        var (_, proposal) = await SeedAsync(db, pi.Id, Today.AddDays(5), ProposalStatus.Draft);
        var clock = new FakeClock { UtcNow = Today.ToDateTime(TimeOnly.MinValue) };
        var service = MakeService(db, clock);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => service.SubmitProposalAsync(proposal.Id, Guid.NewGuid()));
    }

    // ── 5: chỉ DRAFT mới được nộp → SUBMITTED rồi = 409 ──────────────────────
    [Fact]
    public async Task Submit_NonDraft_Throws_409()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var pi = MakePi();
        db.Users.Add(pi);
        await db.SaveChangesAsync();

        var (_, proposal) = await SeedAsync(db, pi.Id, Today.AddDays(5), ProposalStatus.Submitted);
        var clock = new FakeClock { UtcNow = Today.ToDateTime(TimeOnly.MinValue) };
        var service = MakeService(db, clock);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.SubmitProposalAsync(proposal.Id, pi.Id));
    }

    // ── 6: CV thiếu/cũ + chưa xác nhận → 409, status giữ DRAFT ────────────────
    [Fact]
    public async Task Submit_MissingCv_NoConfirm_Throws_409()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var pi = MakePi();
        db.Users.Add(pi);
        await db.SaveChangesAsync();

        var (_, proposal) = await SeedAsync(db, pi.Id, Today.AddDays(5), ProposalStatus.Draft);
        var clock = new FakeClock { UtcNow = Today.ToDateTime(TimeOnly.MinValue) };
        var service = MakeService(db, clock);

        // Không seed AcademicProfile + confirmCvUpToDate=false → bị chặn nhắc cập nhật CV.
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.SubmitProposalAsync(proposal.Id, pi.Id, confirmCvUpToDate: false));
        Assert.Contains("CV", ex.Message);

        var after = await db.Proposals.FindAsync(proposal.Id);
        Assert.Equal(ProposalStatus.Draft, after!.Status);
    }
}
