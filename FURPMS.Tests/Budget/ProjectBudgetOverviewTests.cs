using FURPMS.Application.Common;
using FURPMS.Application.Constants;
using FURPMS.Domain.Entities.Contracts;
using FURPMS.Domain.Entities.MasterData;
using FURPMS.Domain.Entities.Projects;
using FURPMS.Domain.Entities.Proposals;
using FURPMS.Domain.Entities.Users;
using FURPMS.Infrastructure.Data;
using FURPMS.Infrastructure.Repositories;
using FURPMS.Infrastructure.Services;
using FURPMS.Tests.Helpers;

namespace FURPMS.Tests.Budget;

/// <summary>
/// Bức tranh kinh phí một đề tài — trả lời yêu cầu số (1) của hội đồng bảo vệ lần 2:
/// *"thể hiện rõ ngân sách tương ứng cho các đề tài"*.
///
/// <para>Trọng tâm là 4 con số phải cộng ra đúng: <b>dự toán duyệt · đã ký hợp đồng · đã đánh dấu
/// chi · còn lại</b>; cộng với việc <b>không tự suy diễn thay kế toán</b> khi Phòng Tài chính chưa
/// báo số thực chi (rule #15).</para>
/// </summary>
public class ProjectBudgetOverviewTests
{
    private static ProjectBudgetOverviewService MakeSvc(FURPMSDbContext db) =>
        new(new ProposalRepository(db), new ContractRepository(db), new ReviewRepository(db));

    private static async Task<(Project project, Guid piId)> SeedProjectAsync(
        FURPMSDbContext db, decimal cap = 150_000_000m)
    {
        var pi = new User
        {
            Id = Guid.NewGuid(), Email = $"pi{Guid.NewGuid():N}"[..18] + "@t.com",
            FullName = "PI", Status = UserStatus.Active
        };
        db.Users.Add(pi);
        db.ResearchTypes.Add(new ResearchType
        {
            Id = 1, Code = "APPLIED", Name = "Nghiên cứu ứng dụng", MaxBudgetCap = cap, IsActive = true
        });
        await db.SaveChangesAsync();

        var project = new Project
        {
            Id = Guid.NewGuid(),
            CycleTrackId = 1,
            OrderId = 1,
            PiUserId = pi.Id,
            HostingUnitId = 1,
            ResearchTypeId = 1,
            ProjectCode = "DT-001",
            TitleVi = "Đề tài kiểm thử kinh phí",
            Status = "IN_PROGRESS",
            PlannedStartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            PlannedEndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(12))
        };
        db.Projects.Add(project);
        await db.SaveChangesAsync();
        return (project, pi.Id);
    }

    private static async Task AddCurrentProposalWithBudgetAsync(
        FURPMSDbContext db, Project project, decimal total)
    {
        var proposal = new Proposal
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            VersionNo = 1,
            IsCurrent = true,
            TitleVi = project.TitleVi,
            AbstractVi = "A",
            ResearchObjectives = "O",
            DurationMonths = 12,
            PlannedStartDate = project.PlannedStartDate,
            PlannedEndDate = project.PlannedEndDate,
            Status = ProposalStatus.Approved
        };
        db.Proposals.Add(proposal);
        db.ProposalBudgets.Add(new ProposalBudget
        {
            ProposalId = proposal.Id,
            TotalAmount = total,
            LaborAmount = total * 0.5m,
            EquipmentAmount = total * 0.3m,
            ConferenceAmount = total * 0.2m
        });
        await db.SaveChangesAsync();
    }

    private static Contract AddContract(FURPMSDbContext db, Project project, decimal amount)
    {
        var contract = new Contract
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            ContractNumber = "HĐ-TEST-01",
            TotalAmount = amount,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(12)),
            OriginalEndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(12)),
            Status = ContractStatus.Active
        };
        db.Contracts.Add(contract);
        db.SaveChanges();
        return contract;
    }

    private static void AddTranche(
        FURPMSDbContext db, Guid contractId, int round, decimal pct, decimal planned,
        string status, decimal? actual = null)
    {
        db.ContractDisbursements.Add(new ContractDisbursement
        {
            ContractId = contractId,
            RoundNumber = round,
            Percentage = pct,
            PlannedAmount = planned,
            ActualAmount = actual,
            ConditionDescription = $"Đợt {round}",
            Status = status,
            DisbursedAt = status == DisbursementStatus.Disbursed ? DateTime.UtcNow : null
        });
        db.SaveChanges();
    }

    // ── 1: bốn con số phải khớp ───────────────────────────────────────────────
    [Fact]
    public async Task TongHop_DuToan_DaKy_DaChi_ConLai()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var (project, piId) = await SeedProjectAsync(db);
        await AddCurrentProposalWithBudgetAsync(db, project, 100_000_000m);
        var contract = AddContract(db, project, 100_000_000m);

        AddTranche(db, contract.Id, 1, 30m, 30_000_000m, DisbursementStatus.Disbursed, 30_000_000m);
        AddTranche(db, contract.Id, 2, 40m, 40_000_000m, DisbursementStatus.Pending);
        AddTranche(db, contract.Id, 3, 30m, 30_000_000m, DisbursementStatus.Pending);

        var dto = await MakeSvc(db).GetAsync(project.Id, piId, Array.Empty<string>());

        Assert.Equal(100_000_000m, dto.ApprovedTotal);
        Assert.Equal(100_000_000m, dto.ContractedTotal);
        Assert.Equal(100_000_000m, dto.PlannedTotal);
        Assert.Equal(30_000_000m, dto.MarkedDisbursedTotal);
        Assert.Equal(70_000_000m, dto.RemainingTotal);

        // Đợt kế tiếp = đợt CHƯA chi có số nhỏ nhất, để trả lời ngay "sắp tới chi cái gì".
        Assert.Equal(2, dto.NextTranche!.RoundNumber);
        Assert.False(dto.CapExceeded);
    }

    // ── 2: chưa báo số thực chi thì KHÔNG được im lặng coi như đúng kế hoạch ──
    [Fact]
    public async Task ChuaBaoSoThucChi_ThiBatCo_DeGiaoDienNoiRo()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var (project, piId) = await SeedProjectAsync(db);
        await AddCurrentProposalWithBudgetAsync(db, project, 100_000_000m);
        var contract = AddContract(db, project, 100_000_000m);

        // Đã đánh dấu chi nhưng Phòng Tài chính chưa báo lại con số.
        AddTranche(db, contract.Id, 1, 50m, 50_000_000m, DisbursementStatus.Disbursed);

        var dto = await MakeSvc(db).GetAsync(project.Id, piId, Array.Empty<string>());

        Assert.Equal(50_000_000m, dto.MarkedDisbursedTotal);   // tạm lấy theo kế hoạch
        Assert.True(dto.HasUnreportedActuals);                 // nhưng phải nói rõ là tạm
    }

    // ── 3: đề tài chưa có hợp đồng — không được chia 0, không được ném ────────
    [Fact]
    public async Task ChuaCoHopDong_TraVeSoKhong_KhongNem()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var (project, piId) = await SeedProjectAsync(db);
        await AddCurrentProposalWithBudgetAsync(db, project, 80_000_000m);

        var dto = await MakeSvc(db).GetAsync(project.Id, piId, Array.Empty<string>());

        Assert.Equal(80_000_000m, dto.ApprovedTotal);
        Assert.Equal(0m, dto.ContractedTotal);
        Assert.Equal(0m, dto.MarkedDisbursedTotal);
        Assert.Equal(0m, dto.RemainingTotal);
        Assert.Null(dto.NextTranche);
        Assert.Empty(dto.Tranches);
    }

    // ── 4: dự toán 0 thì % hạng mục phải là 0, không phải NaN / chia 0 ────────
    [Fact]
    public async Task DuToanBangKhong_KhongChiaChoKhong()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var (project, piId) = await SeedProjectAsync(db);
        await AddCurrentProposalWithBudgetAsync(db, project, 0m);

        var dto = await MakeSvc(db).GetAsync(project.Id, piId, Array.Empty<string>());

        Assert.Equal(6, dto.ApprovedByHeading.Count);          // giữ đủ 06 hạng mục Điều 15
        Assert.All(dto.ApprovedByHeading, h => Assert.Equal(0m, h.Percentage));
    }

    // ── 5: vượt trần (trần bị siết SAU khi duyệt) phải bật cờ ─────────────────
    [Fact]
    public async Task VuotTran_BatCo_CapExceeded()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var (project, piId) = await SeedProjectAsync(db, cap: 100_000_000m);
        await AddCurrentProposalWithBudgetAsync(db, project, 145_000_000m);

        var dto = await MakeSvc(db).GetAsync(project.Id, piId, Array.Empty<string>());

        Assert.True(dto.CapExceeded);
        Assert.Equal(100_000_000m, dto.FundingCap);
    }

    // ── 6: người không liên quan không được xem tiền của đề tài người khác ────
    [Fact]
    public async Task NguoiKhongLienQuan_Nem_Forbidden()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var (project, _) = await SeedProjectAsync(db);
        await AddCurrentProposalWithBudgetAsync(db, project, 100_000_000m);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => MakeSvc(db).GetAsync(project.Id, Guid.NewGuid(), Array.Empty<string>()));

        // Phòng QLKH thì xem được.
        var asStaff = await MakeSvc(db).GetAsync(project.Id, Guid.NewGuid(), new[] { "Staff" });
        Assert.Equal(project.Id, asStaff.ProjectId);
    }

    // ── 7: trần = 0 nghĩa là "không đặt trần", không phải "trần bằng 0" ───────
    [Fact]
    public async Task TranBangKhong_TraVeNull_KhongBaoDongOan()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var (project, piId) = await SeedProjectAsync(db, cap: 0m);
        await AddCurrentProposalWithBudgetAsync(db, project, 50_000_000m);

        var dto = await MakeSvc(db).GetAsync(project.Id, piId, Array.Empty<string>());

        Assert.Null(dto.FundingCap);
        Assert.False(dto.CapExceeded);
    }
}
