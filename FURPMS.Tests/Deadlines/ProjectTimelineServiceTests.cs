using FURPMS.Application.Common;
using FURPMS.Application.Constants;
using FURPMS.Application.DTOs.Timeline;
using FURPMS.Domain.Entities.Contracts;
using FURPMS.Domain.Entities.Cycles;
using FURPMS.Domain.Entities.MasterData;
using FURPMS.Domain.Entities.Progress;
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
/// Dòng thời gian đề tài — trả lời yêu cầu số (2) của hội đồng bảo vệ lần 2:
/// *"thể hiện rõ các mốc thời gian deadline cho các giai đoạn của 1 đề tài"*.
///
/// <para>Trọng tâm: hạn phải <b>đúng căn cứ</b> (QĐ543 / cấu hình / suy ra), và chỗ nào KHÔNG có
/// hạn thì phải nói thẳng là không có — không được bịa ra một ngày cho đẹp dòng thời gian.</para>
/// </summary>
public class ProjectTimelineServiceTests
{
    private static readonly DateOnly Today = new(2026, 6, 18);

    private static ProjectTimelineService MakeSvc(FURPMSDbContext db, FakeClock clock) =>
        new(new ProposalRepository(db), new ContractRepository(db), new ReviewRepository(db),
            new DeadlineResolver(new CycleRepository(db)),
            new SystemSettingService(new MasterDataRepository(db)), clock);

    private static FakeClock ClockAtToday() =>
        new() { UtcNow = Today.ToDateTime(TimeOnly.MinValue) };

    private static async Task<(Project project, Guid piId, ResearchCycle cycle, CycleTrack track)>
        SeedProjectAsync(FURPMSDbContext db, DateOnly submissionDeadline)
    {
        var pi = new User
        {
            Id = Guid.NewGuid(), Email = $"pi{Guid.NewGuid():N}"[..18] + "@t.com",
            FullName = "PI", Status = UserStatus.Active
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
            SubmissionOpenDate = Today.AddDays(-60),
            SubmissionDeadline = submissionDeadline,
            ReviewDeadline = submissionDeadline.AddDays(30),
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
            TitleVi = "Đề tài kiểm thử dòng thời gian", Status = "IN_PROGRESS",
            PlannedStartDate = Today, PlannedEndDate = Today.AddMonths(12)
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
        return (project, pi.Id, cycle, track);
    }

    // ── 1: hạn nộp phải là hạn HIỆU LỰC, và nói rõ là đã gia hạn ─────────────
    [Fact]
    public async Task HanNopDeCuong_LayHanHieuLuc_VaDanhDauDaGiaHan()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var (project, piId, cycle, _) = await SeedProjectAsync(db, Today.AddDays(-5));

        db.DeadlineExtensions.Add(new DeadlineExtension
        {
            TargetType = "CYCLE", TargetId = cycle.Id.ToString(),
            OldDeadline = Today.AddDays(-5), NewDeadline = Today.AddDays(20),
            CreatedBy = piId, CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var dto = await MakeSvc(db, ClockAtToday()).GetAsync(project.Id, piId, Array.Empty<string>());
        var stage = dto.Stages.Single(s => s.Code == StageCodes.ProposalSubmission);

        Assert.Equal("2026-07-08", stage.Deadline);                    // hạn MỚI, không phải hạn gốc
        Assert.True(stage.IsExtended);
        Assert.Equal(StageDeadlineSource.Extension, stage.DeadlineSource);
        Assert.Contains("gia hạn", stage.DeadlineBasis!);
    }

    // ── 2: hạn báo cáo nghiệm thu suy đúng QĐ543 Điều 11.2.a ────────────────
    [Fact]
    public async Task HanBaoCaoNghiemThu_Bang_NgayKetThucTruNgayCauHinh()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var (project, piId, _, _) = await SeedProjectAsync(db, Today.AddDays(30));

        db.Contracts.Add(new Contract
        {
            Id = Guid.NewGuid(), ProjectId = project.Id, ContractNumber = "HĐ-01",
            TotalAmount = 100_000_000m, StartDate = Today,
            EndDate = new DateOnly(2026, 12, 31),
            OriginalEndDate = new DateOnly(2026, 12, 31),
            Status = ContractStatus.Active
        });
        await db.SaveChangesAsync();

        var dto = await MakeSvc(db, ClockAtToday()).GetAsync(project.Id, piId, Array.Empty<string>());
        var stage = dto.Stages.Single(s => s.Code == StageCodes.FinalReport);

        // 31/12 − 30 ngày = 01/12
        Assert.Equal("2026-12-01", stage.Deadline);
        Assert.Equal(StageDeadlineSource.RuleQd543, stage.DeadlineSource);
        Assert.Contains("11.2.a", stage.DeadlineBasis!);
    }

    // ── 3: đổi cấu hình thì hạn đổi theo — CHỨNG MINH không hardcode ────────
    [Fact]
    public async Task DoiCauHinh_HanBaoCaoNghiemThu_DoiTheo()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var (project, piId, _, _) = await SeedProjectAsync(db, Today.AddDays(30));

        db.Contracts.Add(new Contract
        {
            Id = Guid.NewGuid(), ProjectId = project.Id, ContractNumber = "HĐ-01",
            TotalAmount = 100_000_000m, StartDate = Today,
            EndDate = new DateOnly(2026, 12, 31),
            OriginalEndDate = new DateOnly(2026, 12, 31),
            Status = ContractStatus.Active
        });
        // Trường siết chặt hơn quy định: 45 ngày thay vì 30.
        db.SystemSettings.Add(new SystemSetting
        {
            Key = SystemSettingKeys.FinalReportLeadDays, Value = "45", RecommendedValue = "30"
        });
        await db.SaveChangesAsync();

        var dto = await MakeSvc(db, ClockAtToday()).GetAsync(project.Id, piId, Array.Empty<string>());
        var stage = dto.Stages.Single(s => s.Code == StageCodes.FinalReport);

        Assert.Equal("2026-11-16", stage.Deadline);   // 31/12 − 45 ngày
    }

    // ── 4: giải ngân KHÔNG có hạn — phải nói thẳng, không bịa ngày ──────────
    [Fact]
    public async Task GiaiNgan_KhongCoHan_ThiBaoKhongCoHan()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var (project, piId, _, _) = await SeedProjectAsync(db, Today.AddDays(30));

        var contract = new Contract
        {
            Id = Guid.NewGuid(), ProjectId = project.Id, ContractNumber = "HĐ-01",
            TotalAmount = 100_000_000m, StartDate = Today, EndDate = Today.AddMonths(12),
            OriginalEndDate = Today.AddMonths(12), Status = ContractStatus.Active
        };
        db.Contracts.Add(contract);
        await db.SaveChangesAsync();

        db.ContractDisbursements.Add(new ContractDisbursement
        {
            ContractId = contract.Id, RoundNumber = 1, Percentage = 100m,
            PlannedAmount = 100_000_000m, ConditionDescription = "Sau khi ký hợp đồng",
            Status = DisbursementStatus.Pending
        });
        await db.SaveChangesAsync();

        var dto = await MakeSvc(db, ClockAtToday()).GetAsync(project.Id, piId, Array.Empty<string>());
        var stage = dto.Stages.Single(s => s.Code == StageCodes.Disbursement);

        Assert.Null(stage.Deadline);
        Assert.Equal(StageDeadlineSource.NotSet, stage.DeadlineSource);
        Assert.Equal(StageStatus.NoDeadline, stage.Status);
        // Vẫn phải nói được ĐIỀU KIỆN mở khoá, nếu không thì dòng này vô nghĩa.
        Assert.Equal("Sau khi ký hợp đồng", stage.DeadlineBasis);
    }

    // ── 5: quá hạn / sắp tới hạn / đã xong ──────────────────────────────────
    [Fact]
    public async Task TrangThai_QuaHan_SapToiHan_DaXong()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var (project, piId, _, _) = await SeedProjectAsync(db, Today.AddDays(30));

        var contract = new Contract
        {
            Id = Guid.NewGuid(), ProjectId = project.Id, ContractNumber = "HĐ-01",
            TotalAmount = 100_000_000m, StartDate = Today, EndDate = Today.AddMonths(12),
            OriginalEndDate = Today.AddMonths(12), Status = ContractStatus.Active
        };
        db.Contracts.Add(contract);
        await db.SaveChangesAsync();

        db.ProjectDeliverables.AddRange(
            new ProjectDeliverable
            {
                ProjectId = project.Id, ProductName = "Quá hạn",
                DueDate = Today.AddDays(-10)          // hạn đã qua, chưa nộp
            },
            new ProjectDeliverable
            {
                ProjectId = project.Id, ProductName = "Sắp tới hạn",
                DueDate = Today.AddDays(3)            // trong 7 ngày
            },
            new ProjectDeliverable
            {
                ProjectId = project.Id, ProductName = "Đã nộp",
                DueDate = Today.AddDays(-20), SubmittedAt = Today.AddDays(-25).ToDateTime(TimeOnly.MinValue)
            });
        await db.SaveChangesAsync();

        var dto = await MakeSvc(db, ClockAtToday()).GetAsync(project.Id, piId, Array.Empty<string>());
        var byName = dto.Stages.Where(s => s.Code == StageCodes.Deliverable)
            .ToDictionary(s => s.DeadlineBasis!, s => s);

        Assert.Equal(StageStatus.Overdue, byName["Quá hạn"].Status);
        Assert.Equal(-10, byName["Quá hạn"].DaysLeft);
        Assert.Equal(StageStatus.AtRisk, byName["Sắp tới hạn"].Status);
        // Nộp rồi thì hạn không còn ý nghĩa — dù hạn đã qua vẫn là XONG, không phải quá hạn.
        Assert.Equal(StageStatus.Done, byName["Đã nộp"].Status);
        Assert.Equal(1, dto.OverdueCount);
    }

    // ── 6: người ngoài không xem được tiến trình đề tài người khác ──────────
    [Fact]
    public async Task NguoiKhongLienQuan_Nem_Forbidden()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var (project, _, _, _) = await SeedProjectAsync(db, Today.AddDays(30));

        await Assert.ThrowsAsync<ForbiddenException>(
            () => MakeSvc(db, ClockAtToday()).GetAsync(project.Id, Guid.NewGuid(), Array.Empty<string>()));

        var asStaff = await MakeSvc(db, ClockAtToday())
            .GetAsync(project.Id, Guid.NewGuid(), new[] { "Staff" });
        Assert.Equal(project.Id, asStaff.ProjectId);
    }

    // ── 7: câu giải thích phải là SỐ NGÀY, không phải tên khoá cấu hình ─────
    [Fact]
    public async Task CanCuHan_KhongDuocLoTenKhoaCauHinh()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var (project, piId, _, _) = await SeedProjectAsync(db, Today.AddDays(30));

        db.Contracts.Add(new Contract
        {
            Id = Guid.NewGuid(), ProjectId = project.Id, ContractNumber = "HĐ-01",
            TotalAmount = 100_000_000m, StartDate = Today, EndDate = Today.AddMonths(12),
            OriginalEndDate = Today.AddMonths(12), Status = ContractStatus.Active
        });
        await db.SaveChangesAsync();

        var dto = await MakeSvc(db, ClockAtToday()).GetAsync(project.Id, piId, Array.Empty<string>());

        // Đã dính thật khi chạy thử 25/08: màn hình in ra "CONTRACT_SIGN_WINDOW_DAYS ngày kể từ…"
        // vì nội suy nhầm hằng KHOÁ thay vì GIÁ TRỊ.
        foreach (var stage in dto.Stages)
        {
            Assert.DoesNotContain("_DAYS", stage.DeadlineBasis ?? "");
        }
    }
}
