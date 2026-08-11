using FURPMS.Domain.Entities.Cycles;
using FURPMS.Domain.Entities.MasterData;
using FURPMS.Domain.Entities.Proposals;
using FURPMS.Domain.Entities.Users;
using FURPMS.Infrastructure.Repositories;
using FURPMS.Infrastructure.Services;
using FURPMS.Tests.Helpers;

namespace FURPMS.Tests.Budget;

/// <summary>
/// Trần kinh phí đề tài — QĐ543 <b>Điều 14</b>: cơ bản ≤ 100tr, ứng dụng/triển khai ≤ 150tr.
/// Điều 14.3 cho phép vượt trần nhưng phải do Hiệu trưởng quyết định ⇒ hệ thống chặn theo trần
/// trong master data, muốn vượt thì Phòng QLKH nâng trần chứ không tự do.
/// </summary>
public class BudgetCapPolicyTests
{
    private static (FURPMS.Domain.Entities.Projects.Project project, Proposal proposal) MakeProposal(
        Guid piId, int researchTypeId, int orderId)
    {
        var project = new FURPMS.Domain.Entities.Projects.Project
        {
            Id = Guid.NewGuid(),
            CycleTrackId = 1,
            OrderId = orderId,
            PiUserId = piId,
            HostingUnitId = 1,
            ResearchTypeId = researchTypeId,
            TitleVi = "Đề tài kiểm thử trần kinh phí",
            Status = "DRAFT",
            PlannedStartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            PlannedEndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(12))
        };
        var proposal = new Proposal
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            VersionNo = 1,
            IsCurrent = true,
            TitleVi = "Đề tài kiểm thử trần kinh phí",
            AbstractVi = "Abstract",
            ResearchObjectives = "Objectives",
            DurationMonths = 12,
            PlannedStartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            PlannedEndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(12)),
            Status = "DRAFT"
        };
        return (project, proposal);
    }

    private static async Task<(BudgetPolicyService svc, Guid proposalId)> SetupAsync(
        decimal typeCap, decimal? orderCap = null)
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");

        var pi = new User { Id = Guid.NewGuid(), Email = $"pi-{Guid.NewGuid()}@test.com", FullName = "PI", Status = "ACTIVE" };
        var type = new ResearchType { Code = "BASIC", Name = "Nghiên cứu cơ bản", MaxBudgetCap = typeCap, IsActive = true };
        db.Users.Add(pi);
        db.ResearchTypes.Add(type);
        await db.SaveChangesAsync();

        var order = new ResearchOrder
        {
            CycleId = 1,
            OrderingUnitId = 1,
            ResearchArea = "Trí tuệ nhân tạo",
            ProblemDescription = "Đơn đặt hàng kiểm thử trần kinh phí",
            BudgetCap = orderCap,
            CreatedBy = pi.Id
        };
        db.ResearchOrders.Add(order);
        await db.SaveChangesAsync();

        var (project, proposal) = MakeProposal(pi.Id, type.Id, order.Id);
        db.Projects.Add(project);
        db.Proposals.Add(proposal);
        await db.SaveChangesAsync();

        var svc = new BudgetPolicyService(
            new ProposalRepository(db), new CycleRepository(db), new MasterDataRepository(db));
        return (svc, proposal.Id);
    }

    // ── 1: đúng trần thì qua ─────────────────────────────────────────────────
    [Fact]
    public async Task WithinCap_Passes()
    {
        var (svc, id) = await SetupAsync(typeCap: 100_000_000m);
        await svc.AssertWithinCapAsync(id, 100_000_000m); // bằng đúng trần vẫn hợp lệ
    }

    // ── 2: vượt trần loại đề tài → 400, lỗi phải nói RÕ trần bao nhiêu ───────
    [Fact]
    public async Task OverTypeCap_Throws_400_WithCapInMessage()
    {
        var (svc, id) = await SetupAsync(typeCap: 100_000_000m);

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => svc.AssertWithinCapAsync(id, 190_000_000m));

        // PI phải đọc được cần cắt xuống bao nhiêu, không phải "vượt trần" chung chung.
        Assert.Contains("100.000.000", ex.Message.Replace(",", "."));
        Assert.Contains("Điều 14", ex.Message);
    }

    // ── 3: đơn đặt hàng siết chặt hơn → lấy trần NGHIÊM NGẶT hơn ─────────────
    [Fact]
    public async Task OrderCapStricter_Wins()
    {
        var (svc, id) = await SetupAsync(typeCap: 150_000_000m, orderCap: 80_000_000m);

        var cap = await svc.GetCapAsync(id);
        Assert.Equal(80_000_000m, cap.EffectiveCap);

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => svc.AssertWithinCapAsync(id, 100_000_000m));
        Assert.Contains("đơn đặt hàng", ex.Message);
    }

    // ── 4: đơn đặt hàng nới hơn KHÔNG được phá trần Điều 14 ──────────────────
    [Fact]
    public async Task OrderCapLooser_DoesNotOverrideRegulation()
    {
        var (svc, id) = await SetupAsync(typeCap: 100_000_000m, orderCap: 500_000_000m);

        var cap = await svc.GetCapAsync(id);
        Assert.Equal(100_000_000m, cap.EffectiveCap);
        await Assert.ThrowsAsync<ArgumentException>(() => svc.AssertWithinCapAsync(id, 200_000_000m));
    }

    // ── 5: chưa cấu hình trần ⇒ KHÔNG chặn (đừng khoá ở 0) ───────────────────
    [Fact]
    public async Task NoCapConfigured_DoesNotBlock()
    {
        var (svc, id) = await SetupAsync(typeCap: 0m);

        var cap = await svc.GetCapAsync(id);
        Assert.Null(cap.EffectiveCap);
        await svc.AssertWithinCapAsync(id, 999_000_000m);
    }

    // ── 6: Điều 15 — vượt tỷ lệ hạng mục thì chặn, lỗi phải chỉ đích danh hạng mục ────
    [Fact]
    public async Task OverCategoryPercentage_Throws_400_NamingTheCategory()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var conference = new BudgetExpenseCategory
        {
            Code = "CONFERENCE", Name = "Hội nghị/hội thảo/seminar", MaxPercentage = 30m, Sequence = 4, IsActive = true
        };
        var labor = new BudgetExpenseCategory
        {
            Code = "LABOR", Name = "Thù lao nghiên cứu", MaxPercentage = 100m, Sequence = 1, IsActive = true
        };
        db.BudgetExpenseCategories.AddRange(conference, labor);
        await db.SaveChangesAsync();

        var svc = new BudgetPolicyService(
            new ProposalRepository(db), new CycleRepository(db), new MasterDataRepository(db));

        // Tổng 100tr: hội thảo 40tr = 40% > trần 30%; thù lao 60tr = 60% ≤ 100% (hợp lệ).
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => svc.AssertCategoryLimitsAsync(
            new Dictionary<int, decimal> { [conference.Id] = 40_000_000m, [labor.Id] = 60_000_000m },
            100_000_000m));

        Assert.Contains("Điều 15", ex.Message);
        Assert.Contains("Hội nghị", ex.Message);
        // Hạng mục hợp lệ KHÔNG được lôi vào danh sách vi phạm.
        Assert.DoesNotContain("Thù lao", ex.Message);
    }

    // ── 7: đúng trần % thì qua; hạng mục ngoài Điều 15 (MaxPercentage null) bỏ qua ───
    [Fact]
    public async Task WithinCategoryPercentage_AndLegacyCategories_Pass()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var conference = new BudgetExpenseCategory
        {
            Code = "CONFERENCE", Name = "Hội nghị/hội thảo/seminar", MaxPercentage = 30m, Sequence = 4, IsActive = true
        };
        // Hạng mục cũ ngoài quy định (vd "Chi đoàn ra") — không có tỷ lệ nên không soi.
        var legacy = new BudgetExpenseCategory
        {
            Code = "OVERSEAS_TRAVEL", Name = "Chi đoàn ra", MaxPercentage = null, Sequence = 10, IsActive = false
        };
        db.BudgetExpenseCategories.AddRange(conference, legacy);
        await db.SaveChangesAsync();

        var svc = new BudgetPolicyService(
            new ProposalRepository(db), new CycleRepository(db), new MasterDataRepository(db));

        await svc.AssertCategoryLimitsAsync(
            new Dictionary<int, decimal> { [conference.Id] = 30_000_000m, [legacy.Id] = 70_000_000m },
            100_000_000m);
    }

    // ── 8: tổng = 0 thì không có tỷ lệ nào để tính, đừng chia cho 0 ──────────────────
    [Fact]
    public async Task ZeroTotal_DoesNotThrow()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var svc = new BudgetPolicyService(
            new ProposalRepository(db), new CycleRepository(db), new MasterDataRepository(db));

        await svc.AssertCategoryLimitsAsync(new Dictionary<int, decimal> { [1] = 0m }, 0m);
    }
}
