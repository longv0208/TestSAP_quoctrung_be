using FURPMS.Application.Constants;
using FURPMS.Application.DTOs.Timeline;
using FURPMS.Domain.Entities.Cycles;
using FURPMS.Domain.Entities.MasterData;
using FURPMS.Domain.Entities.Projects;
using FURPMS.Domain.Entities.Proposals;
using FURPMS.Domain.Entities.Users;
using FURPMS.Infrastructure.Data;
using FURPMS.Infrastructure.Repositories;
using FURPMS.Infrastructure.Services;
using FURPMS.Tests.Helpers;
using FURPMS.Tests.Reminders; // FakeClock

namespace FURPMS.Tests.Deadlines;

/// <summary>
/// Thẻ "Hạn sắp tới" trên bảng điều khiển (<c>GET /api/me/deadlines</c>).
///
/// <para>Thẻ này là thứ người dùng nhìn thấy ĐẦU TIÊN khi vào hệ thống, nên ba tính chất phải
/// đúng: chỉ thấy hạn của đề tài mình được phép xem · đề tài đã đóng không làm rác danh sách ·
/// việc quá hạn không bao giờ bị cửa sổ N ngày đẩy ra ngoài.</para>
/// </summary>
public class UpcomingDeadlinesTests
{
    private static readonly DateOnly Today = new(2026, 6, 18);

    private static ProjectTimelineService MakeSvc(FURPMSDbContext db) =>
        new(new ProposalRepository(db), new ContractRepository(db), new ReviewRepository(db),
            new DeadlineResolver(new CycleRepository(db)),
            new SystemSettingService(new MasterDataRepository(db)),
            new FakeClock { UtcNow = Today.ToDateTime(TimeOnly.MinValue) });

    private static User NewUser(FURPMSDbContext db, string name)
    {
        var u = new User
        {
            Id = Guid.NewGuid(),
            Email = Guid.NewGuid().ToString("N")[..12] + "@t.com",
            FullName = name,
            Status = UserStatus.Active
        };
        db.Users.Add(u);
        return u;
    }

    /// <summary>Một đề tài kèm hạn nộp đề cương đặt cách hôm nay <paramref name="dueInDays"/> ngày.</summary>
    private static async Task<Project> SeedProjectAsync(
        FURPMSDbContext db, Guid piId, int dueInDays, string status = "IN_PROGRESS", string code = "DT-01")
    {
        if (!db.ResearchTypes.Any())
        {
            db.ResearchTypes.Add(new ResearchType
            {
                Id = 1, Code = "APPLIED", Name = "Ứng dụng", MaxBudgetCap = 150_000_000m, IsActive = true
            });
            db.ResearchTracks.Add(new ResearchTrack { Id = 1, Code = "IT", Name = "CNTT", IsActive = true });
            await db.SaveChangesAsync();
        }

        var cycle = new ResearchCycle
        {
            CycleYear = 2026, SemesterCode = "SU26", ResearchTypeId = 1,
            SubmissionOpenDate = Today.AddDays(-60),
            SubmissionDeadline = Today.AddDays(dueInDays),
            Status = CycleStatus.Open, CreatedBy = piId
        };
        db.ResearchCycles.Add(cycle);
        await db.SaveChangesAsync();

        var track = new CycleTrack { CycleId = cycle.Id, TrackId = 1 };
        db.CycleTracks.Add(track);
        await db.SaveChangesAsync();

        var project = new Project
        {
            Id = Guid.NewGuid(), CycleTrackId = track.Id, OrderId = 1, PiUserId = piId,
            HostingUnitId = 1, ResearchTypeId = 1, ProjectCode = code,
            TitleVi = "Đề tài " + code, Status = status,
            PlannedStartDate = Today, PlannedEndDate = Today.AddMonths(12),
            UpdatedAt = DateTime.UtcNow
        };
        db.Projects.Add(project);
        db.Proposals.Add(new Proposal
        {
            Id = Guid.NewGuid(), ProjectId = project.Id, VersionNo = 1, IsCurrent = true,
            TitleVi = project.TitleVi, AbstractVi = "A", ResearchObjectives = "O", DurationMonths = 12,
            PlannedStartDate = Today, PlannedEndDate = Today.AddMonths(12),
            Status = ProposalStatus.Submitted
        });
        await db.SaveChangesAsync();
        return project;
    }

    // ── 1: chủ nhiệm chỉ thấy hạn của đề tài MÌNH ────────────────────────────
    [Fact]
    public async Task ChuNhiem_ChiThayHanCuaDeTaiMinh()
    {
        var db = TestDbContextFactory.Create("test-" + Guid.NewGuid());
        var mine = NewUser(db, "PI A");
        var other = NewUser(db, "PI B");
        await db.SaveChangesAsync();

        await SeedProjectAsync(db, mine.Id, dueInDays: 10, code: "DT-CUA-TOI");
        await SeedProjectAsync(db, other.Id, dueInDays: 10, code: "DT-NGUOI-KHAC");

        var result = await MakeSvc(db).GetUpcomingAsync(mine.Id, Array.Empty<string>(), days: 30);

        Assert.NotEmpty(result);
        Assert.All(result, r => Assert.Equal("Đề tài DT-CUA-TOI", r.ProjectTitle));
    }

    // ── 2: Phòng QLKH thấy hạn của MỌI đề tài — họ là người đi nhắc ──────────
    [Fact]
    public async Task PhongQLKH_ThayHanCuaMoiDeTai()
    {
        var db = TestDbContextFactory.Create("test-" + Guid.NewGuid());
        var staff = NewUser(db, "Cán bộ QLKH");
        var pi1 = NewUser(db, "PI A");
        var pi2 = NewUser(db, "PI B");
        await db.SaveChangesAsync();

        await SeedProjectAsync(db, pi1.Id, dueInDays: 5, code: "DT-01");
        await SeedProjectAsync(db, pi2.Id, dueInDays: 7, code: "DT-02");

        var result = await MakeSvc(db).GetUpcomingAsync(staff.Id, new[] { "Staff" }, days: 30);

        var titles = result.Select(r => r.ProjectTitle).Distinct().ToList();
        Assert.Contains("Đề tài DT-01", titles);
        Assert.Contains("Đề tài DT-02", titles);
    }

    // ── 3: đề tài đã đóng không làm rác thẻ nhắc việc ────────────────────────
    [Fact]
    public async Task DeTaiDaDong_KhongVaoTheNhacViec()
    {
        var db = TestDbContextFactory.Create("test-" + Guid.NewGuid());
        var pi = NewUser(db, "PI");
        await db.SaveChangesAsync();

        await SeedProjectAsync(db, pi.Id, dueInDays: -3, status: ProjectStatus.Completed, code: "DT-XONG");

        var result = await MakeSvc(db).GetUpcomingAsync(pi.Id, Array.Empty<string>(), days: 30);

        Assert.Empty(result);
    }

    // ── 4: quá hạn LUÔN hiện, dù đã quá xa cửa sổ nhìn tới trước ─────────────
    [Fact]
    public async Task ViecQuaHan_LuonHien_DuCuaSoNhinChiLa1Ngay()
    {
        var db = TestDbContextFactory.Create("test-" + Guid.NewGuid());
        var pi = NewUser(db, "PI");
        await db.SaveChangesAsync();

        await SeedProjectAsync(db, pi.Id, dueInDays: -40, code: "DT-TRE");

        // Cửa sổ 1 ngày: nếu lọc thuần theo "hạn nằm trong N ngày tới" thì việc trễ 40 ngày biến mất.
        var result = await MakeSvc(db).GetUpcomingAsync(pi.Id, Array.Empty<string>(), days: 1);

        var overdue = result.Where(r => r.Stage.Status == StageStatus.Overdue).ToList();
        Assert.NotEmpty(overdue);
        Assert.All(overdue, r => Assert.True(r.Stage.DaysLeft < 0));
    }

    // ── 5: hạn còn XA nằm ngoài cửa sổ thì không chen vào ────────────────────
    [Fact]
    public async Task HanConXa_NgoaiCuaSo_KhongChenVao()
    {
        var db = TestDbContextFactory.Create("test-" + Guid.NewGuid());
        var pi = NewUser(db, "PI");
        await db.SaveChangesAsync();

        await SeedProjectAsync(db, pi.Id, dueInDays: 120, code: "DT-CON-XA");

        var result = await MakeSvc(db).GetUpcomingAsync(pi.Id, Array.Empty<string>(), days: 30);

        Assert.DoesNotContain(result, r => r.Stage.Code == StageCodes.ProposalSubmission);
    }

    // ── 6: sắp xếp — việc gấp nhất đứng trước ────────────────────────────────
    [Fact]
    public async Task SapXep_ViecGapNhatDungTruoc()
    {
        var db = TestDbContextFactory.Create("test-" + Guid.NewGuid());
        var pi = NewUser(db, "PI");
        await db.SaveChangesAsync();

        await SeedProjectAsync(db, pi.Id, dueInDays: 20, code: "DT-CHAM");
        await SeedProjectAsync(db, pi.Id, dueInDays: -2, code: "DT-QUA-HAN");
        await SeedProjectAsync(db, pi.Id, dueInDays: 3, code: "DT-GAP");

        var result = await MakeSvc(db).GetUpcomingAsync(pi.Id, Array.Empty<string>(), days: 30);

        // Thẻ trên bảng điều khiển chỉ hiện N dòng đầu, nên THỨ TỰ chính là phần trả lời —
        // xếp sai thì việc quá hạn bị đẩy xuống dưới và không ai nhìn thấy.
        var daysLeft = result.Select(r => r.Stage.DaysLeft ?? int.MaxValue).ToList();
        Assert.Equal(daysLeft.OrderBy(d => d).ToList(), daysLeft);
        Assert.True(daysLeft.First() < 0);
    }
}
