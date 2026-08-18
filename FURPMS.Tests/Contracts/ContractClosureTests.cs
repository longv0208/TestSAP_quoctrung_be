using FURPMS.Application.Constants;
using FURPMS.Application.DTOs.Settlements;
using FURPMS.Domain.Entities.Contracts;
using FURPMS.Domain.Entities.MasterData;
using FURPMS.Domain.Entities.Projects;
using FURPMS.Domain.Entities.Users;
using FURPMS.Infrastructure.Data;
using FURPMS.Infrastructure.Repositories;
using FURPMS.Infrastructure.Services;
using FURPMS.Tests.Helpers;
using FURPMS.Tests.Reminders;

namespace FURPMS.Tests.Contracts;

/// <summary>
/// QĐ543 BM13: nghiệm thu Đạt là điều kiện lập hồ sơ; chỉ khi tiền + tài sản đã được xác nhận
/// và BM13 được ký thì hợp đồng mới thực sự SETTLED.
/// </summary>
public class ContractClosureTests
{
    private static async Task<(ContractSettlementService service, FURPMSDbContext db, Contract contract, User staff)>
        SetupAsync(string projectStatus = ProjectStatus.Completed)
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var pi = new User { Id = Guid.NewGuid(), Email = $"pi-{Guid.NewGuid()}@test.com", FullName = "PI", Status = "ACTIVE" };
        var staff = new User { Id = Guid.NewGuid(), Email = $"staff-{Guid.NewGuid()}@test.com", FullName = "Staff", Status = "ACTIVE" };
        var type = new ResearchType { Code = $"RT-{Guid.NewGuid():N}"[..8], Name = "Test", IsActive = true };
        db.Users.AddRange(pi, staff);
        db.ResearchTypes.Add(type);
        await db.SaveChangesAsync();

        var project = new Project
        {
            Id = Guid.NewGuid(), CycleTrackId = 1, OrderId = 1, PiUserId = pi.Id, HostingUnitId = 1,
            ResearchTypeId = type.Id, TitleVi = "Đề tài đóng hợp đồng", Status = projectStatus,
            PlannedStartDate = new DateOnly(2026, 1, 1), PlannedEndDate = new DateOnly(2026, 12, 31)
        };
        var contract = new Contract
        {
            Id = Guid.NewGuid(), ProjectId = project.Id, ContractNumber = "HD-CLOSE-01",
            Status = ContractStatus.Active, TotalAmount = 100_000_000m,
            StartDate = new DateOnly(2026, 1, 1), EndDate = new DateOnly(2026, 12, 31),
            OriginalEndDate = new DateOnly(2026, 12, 31), MaxExtensionMonths = 6, CreatedBy = staff.Id
        };
        db.Projects.Add(project);
        db.Contracts.Add(contract);
        await db.SaveChangesAsync();

        var service = new ContractSettlementService(
            new ContractRepository(db), new UserRepository(db), new FakeClock());
        return (service, db, contract, staff);
    }

    [Fact]
    public async Task Create_BeforeAcceptancePassed_IsBlocked()
    {
        var (service, _, contract, _) = await SetupAsync(ProjectStatus.InProgress);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.CreateAsync(
            contract.Id,
            new CreateSettlementRequest { TotalContractedAmount = contract.TotalAmount }));

        Assert.Contains("nghiệm thu", ex.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Đạt", ex.Message);
    }

    [Fact]
    public async Task Sign_BeforeAccountingAndAssetsCleared_IsBlocked()
    {
        var (service, _, contract, staff) = await SetupAsync();
        var settlement = await service.CreateAsync(contract.Id,
            new CreateSettlementRequest { TotalContractedAmount = contract.TotalAmount });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.SignAsync(
            settlement.Id, new SignSettlementRequest { SideASigneeId = staff.Id }));

        Assert.Contains("quyết toán kinh phí", ex.Message);
        Assert.Contains("tài sản", ex.Message);
    }

    [Fact]
    public async Task Sign_AfterAllClearance_SetsContractSettled()
    {
        var (service, db, contract, staff) = await SetupAsync();
        var settlement = await service.CreateAsync(contract.Id,
            new CreateSettlementRequest { TotalContractedAmount = contract.TotalAmount });
        await service.MarkAccountingClearedAsync(settlement.Id, new DateOnly(2026, 8, 19));
        await service.MarkAssetsClearedAsync(settlement.Id, new DateOnly(2026, 8, 19));

        var signed = await service.SignAsync(settlement.Id,
            new SignSettlementRequest { SideASigneeId = staff.Id });

        Assert.NotNull(signed.SettlementSignedAt);
        Assert.Equal(ContractStatus.Settled, (await db.Contracts.FindAsync(contract.Id))!.Status);
    }
}
