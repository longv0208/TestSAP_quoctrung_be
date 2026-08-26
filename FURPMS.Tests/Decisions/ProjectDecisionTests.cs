using FURPMS.Application.Common;
using FURPMS.Application.Constants;
using FURPMS.Domain.Entities.Contracts;
using FURPMS.Domain.Entities.Cycles;
using FURPMS.Domain.Entities.MasterData;
using FURPMS.Domain.Entities.Projects;
using FURPMS.Domain.Entities.Proposals;
using FURPMS.Domain.Entities.Users;
using FURPMS.Infrastructure.Data;
using FURPMS.Infrastructure.Services;
using FURPMS.Tests.Helpers;
using FURPMS.Tests.Reminders; // FakeClock

namespace FURPMS.Tests.Decisions;

/// <summary>
/// Sổ quyết định của đề tài — nửa sau yêu cầu số (2) của hội đồng bảo vệ lần 2:
/// *"lưu trữ lại các quyết định liên quan đến đề tài"*.
///
/// <para>Ba tính chất phải giữ: dựng lại hồ sơ cũ được · <b>chạy lại không sinh bản trùng</b> ·
/// ghi sổ hỏng thì không được kéo đổ nghiệp vụ chính.</para>
/// </summary>
public class ProjectDecisionTests
{
    private static readonly DateTime Now = new(2026, 6, 18, 9, 0, 0, DateTimeKind.Utc);

    private static ProjectDecisionService MakeSvc(FURPMSDbContext db) =>
        new(db, new FakeClock { UtcNow = Now });

    private static async Task<(Project project, User pi)> SeedProjectAsync(FURPMSDbContext db)
    {
        var pi = new User
        {
            Id = Guid.NewGuid(),
            Email = Guid.NewGuid().ToString("N")[..12] + "@t.com",
            FullName = "Chủ nhiệm A", Status = UserStatus.Active
        };
        db.Users.Add(pi);
        db.ResearchTypes.Add(new ResearchType
        {
            Id = 1, Code = "APPLIED", Name = "Ứng dụng", MaxBudgetCap = 150_000_000m, IsActive = true
        });
        db.ResearchTracks.Add(new ResearchTrack { Id = 1, Code = "IT", Name = "CNTT", IsActive = true });
        await db.SaveChangesAsync();

        var cycle = new ResearchCycle
        {
            CycleYear = 2026, SemesterCode = "SU26", ResearchTypeId = 1,
            SubmissionOpenDate = DateOnly.FromDateTime(Now).AddDays(-60),
            SubmissionDeadline = DateOnly.FromDateTime(Now).AddDays(30),
            Status = CycleStatus.Open, CreatedBy = pi.Id
        };
        db.ResearchCycles.Add(cycle);
        await db.SaveChangesAsync();

        var track = new CycleTrack { CycleId = cycle.Id, TrackId = 1 };
        db.CycleTracks.Add(track);
        await db.SaveChangesAsync();

        var project = new Project
        {
            Id = Guid.NewGuid(), CycleTrackId = track.Id, OrderId = 1, PiUserId = pi.Id,
            HostingUnitId = 1, ResearchTypeId = 1, ProjectCode = "DT-01",
            TitleVi = "Đề tài kiểm thử sổ quyết định", Status = "IN_PROGRESS",
            PlannedStartDate = DateOnly.FromDateTime(Now),
            PlannedEndDate = DateOnly.FromDateTime(Now).AddMonths(12)
        };
        db.Projects.Add(project);
        db.Proposals.Add(new Proposal
        {
            Id = Guid.NewGuid(), ProjectId = project.Id, VersionNo = 1, IsCurrent = true,
            TitleVi = project.TitleVi, AbstractVi = "A", ResearchObjectives = "O", DurationMonths = 12,
            PlannedStartDate = DateOnly.FromDateTime(Now),
            PlannedEndDate = DateOnly.FromDateTime(Now).AddMonths(12),
            Status = ProposalStatus.Submitted,
            SubmittedAt = Now.AddDays(-30)
        });
        db.Contracts.Add(new Contract
        {
            Id = Guid.NewGuid(), ProjectId = project.Id, ContractNumber = "HĐ-TEST-01",
            TotalAmount = 120_000_000m,
            StartDate = DateOnly.FromDateTime(Now), EndDate = DateOnly.FromDateTime(Now).AddMonths(12),
            OriginalEndDate = DateOnly.FromDateTime(Now).AddMonths(12),
            Status = ContractStatus.Active,
            CreatedAt = Now.AddDays(-20),
            SignedAt = Now.AddDays(-19)
        });
        await db.SaveChangesAsync();
        return (project, pi);
    }

    // ── 1: dựng lại hồ sơ từ dữ liệu đã có ───────────────────────────────
    [Fact]
    public async Task DungLaiHoSo_TuDuLieuDaCo()
    {
        var db = TestDbContextFactory.Create("test-" + Guid.NewGuid());
        var (project, pi) = await SeedProjectAsync(db);

        var result = await MakeSvc(db).BackfillAsync(null, dryRun: false);

        Assert.True(result.Created >= 3);   // nộp đề cương + lập HĐ + ký HĐ
        var dossier = await MakeSvc(db).GetDossierAsync(project.Id, pi.Id, Array.Empty<string>());

        Assert.Contains(dossier.Decisions, d => d.DecisionType == DecisionTypes.ProposalSubmitted);
        Assert.Contains(dossier.Decisions, d => d.DecisionType == DecisionTypes.ContractCreated);
        Assert.Contains(dossier.Decisions, d => d.DecisionType == DecisionTypes.ContractSigned);
    }

    // ── 2: chạy lại KHÔNG sinh bản trùng ─────────────────────────────────
    [Fact]
    public async Task ChayLaiLanHai_KhongSinhBanTrung()
    {
        var db = TestDbContextFactory.Create("test-" + Guid.NewGuid());
        await SeedProjectAsync(db);

        var lan1 = await MakeSvc(db).BackfillAsync(null, dryRun: false);
        var lan2 = await MakeSvc(db).BackfillAsync(null, dryRun: false);

        // Đây là tính chất quan trọng nhất: dữ liệu thật đang chạy trên Railway, và người vận hành
        // sẽ bấm lại endpoint này khi không chắc đã chạy chưa. Trùng một lần là hồ sơ hỏng vĩnh viễn.
        Assert.Equal(0, lan2.Created);
        Assert.Equal(lan1.Created, lan2.Skipped);
        Assert.Equal(lan1.Created, db.ProjectDecisions.Count());
    }

    // ── 3: chạy thử thì đếm nhưng KHÔNG ghi ──────────────────────────────
    [Fact]
    public async Task ChayThu_DemNhungKhongGhi()
    {
        var db = TestDbContextFactory.Create("test-" + Guid.NewGuid());
        await SeedProjectAsync(db);

        var thu = await MakeSvc(db).BackfillAsync(null, dryRun: true);

        Assert.True(thu.Created > 0);
        Assert.True(thu.DryRun);
        Assert.Empty(db.ProjectDecisions);
    }

    // ── 4: người ngoài không xem được hồ sơ ──────────────────────────────
    [Fact]
    public async Task NguoiNgoai_KhongXemDuocHoSo()
    {
        var db = TestDbContextFactory.Create("test-" + Guid.NewGuid());
        var (project, _) = await SeedProjectAsync(db);
        await MakeSvc(db).BackfillAsync(null, dryRun: false);

        await Assert.ThrowsAsync<ForbiddenException>(() =>
            MakeSvc(db).GetDossierAsync(project.Id, Guid.NewGuid(), Array.Empty<string>()));

        // Phòng QLKH thì xem được — họ là người giữ hồ sơ.
        var asStaff = await MakeSvc(db).GetDossierAsync(project.Id, Guid.NewGuid(), new[] { "Staff" });
        Assert.NotEmpty(asStaff.Decisions);
    }

    // ── 5: xếp theo NGÀY rồi tới thứ tự vòng đời ─────────────────────────
    [Fact]
    public async Task XepTheoNgay_RoiToiThuTuVongDoi()
    {
        var db = TestDbContextFactory.Create("test-" + Guid.NewGuid());
        var (project, pi) = await SeedProjectAsync(db);

        // Hai quyết định CÙNG NGÀY: ký hợp đồng lúc 00:00 (ngày trần — hợp đồng ký ngoài hệ thống)
        // và nộp đề cương lúc 09:00. Xếp thuần theo giờ thì "ký hợp đồng" nhảy lên trước "nộp đề
        // cương", đọc vào tưởng hệ thống ghi sai.
        var day = new DateTime(2026, 5, 10, 0, 0, 0, DateTimeKind.Utc);
        db.ProjectDecisions.AddRange(
            new ProjectDecision
            {
                ProjectId = project.Id, DecisionType = DecisionTypes.ContractSigned,
                Summary = "Ký hợp đồng", SourceEntityType = "Contract", SourceEntityId = "c1",
                DecidedAt = day, CreatedAt = Now
            },
            new ProjectDecision
            {
                ProjectId = project.Id, DecisionType = DecisionTypes.ProposalSubmitted,
                Summary = "Nộp đề cương", SourceEntityType = "Proposal", SourceEntityId = "p1",
                DecidedAt = day.AddHours(9), CreatedAt = Now
            });
        await db.SaveChangesAsync();

        var dossier = await MakeSvc(db).GetDossierAsync(project.Id, pi.Id, Array.Empty<string>());
        var sameDay = dossier.Decisions.Where(d => d.DecidedAt.Date == day.Date).ToList();

        Assert.Equal(DecisionTypes.ProposalSubmitted, sameDay[0].DecisionType);
        Assert.Equal(DecisionTypes.ContractSigned, sameDay[1].DecisionType);
    }

    // ── 6: ghi sổ hỏng KHÔNG được kéo đổ nghiệp vụ chính ─────────────────
    [Fact]
    public void GhiSoHong_KhongKeoDoNghiepVuChinh()
    {
        var db = TestDbContextFactory.Create("test-" + Guid.NewGuid());
        db.Dispose();   // ép mọi thao tác lên DbContext này ném lỗi

        var logger = TestServices.Decisions(db);

        // Kết luận Đạt/Không đạt của Chủ tịch hội đồng không được phép hỏng chỉ vì không ghi nổi
        // một dòng sổ phụ. Lỗi đi vào log, luồng chính chạy tiếp.
        var ex = Record.Exception(() => logger.Log(
            Guid.NewGuid(), DecisionTypes.CouncilDecision, "Hội đồng chốt kết luận",
            "CouncilDecision", "1"));

        Assert.Null(ex);
    }

    // ── 7: chặng của quyết định phải phủ hết loại đã khai ────────────────
    [Fact]
    public void MoiLoaiQuyetDinh_DeuCoChang_VaThuTuVongDoi()
    {
        var types = typeof(DecisionTypes)
            .GetFields()
            .Where(f => f.IsLiteral && f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue()!)
            .ToList();

        Assert.NotEmpty(types);
        // Thiếu chỗ nào thì loại đó rơi vào nhóm "OTHER" và tụt xuống cuối hồ sơ — lỗi im lặng,
        // chỉ lộ ra khi mở hồ sơ trước hội đồng. Bắt ngay ở đây.
        Assert.All(types, t => Assert.NotEqual("OTHER", DecisionTypes.StageOf(t)));
        Assert.All(types, t => Assert.NotEqual(200, DecisionTypes.LifecycleOrder(t)));
    }
}
