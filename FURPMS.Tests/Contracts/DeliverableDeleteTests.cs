using FURPMS.Application.Constants;
using FURPMS.Application.DTOs.Contract;
using FURPMS.Domain.Entities.Contracts;
using FURPMS.Domain.Entities.MasterData;
using FURPMS.Domain.Entities.Projects;
using FURPMS.Domain.Entities.Proposals;
using FURPMS.Domain.Entities.Users;
using FURPMS.Infrastructure.Data;
using FURPMS.Infrastructure.Repositories;
using FURPMS.Infrastructure.Services;
using FURPMS.Tests.Helpers;
using FURPMS.Tests.Reminders;

namespace FURPMS.Tests.Contracts;

/// <summary>
/// Rà CRUD 05/08: sản phẩm có thêm mà **không có sửa/xoá**. Thêm vào rồi thì phải chặn đúng chỗ —
/// xoá một sản phẩm đang là điều kiện của đợt giải ngân là làm đợt đó mất căn cứ mở, và sau này
/// không ai truy lại được vì sao tiền đã chi.
/// </summary>
public class DeliverableDeleteTests
{
    private static DeliverableService MakeService(FURPMSDbContext db) =>
        new(new ContractRepository(db), new UserRepository(db), new NotificationRepository(db),
            TestNotifier.Create(db), new FakeClock());

    private static async Task<(FURPMSDbContext db, ProjectDeliverable deliverable, Contract contract)>
        SeedAsync(string? acceptanceStatus = null, bool submitted = false, bool linkedToTranche = false)
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");

        var pi = new User
        {
            Id = Guid.NewGuid(),
            Email = $"pi-{Guid.NewGuid():N}"[..18] + "@t.com",
            FullName = "PI",
            Status = UserStatus.Active
        };
        var rt = new ResearchType { Code = $"RT{Guid.NewGuid():N}"[..8], Name = "Loai", IsActive = true };
        db.Users.Add(pi);
        db.ResearchTypes.Add(rt);
        await db.SaveChangesAsync();

        var project = new Project
        {
            Id = Guid.NewGuid(),
            CycleTrackId = 1,
            OrderId = 1,
            PiUserId = pi.Id,
            HostingUnitId = 1,
            ResearchTypeId = rt.Id,
            TitleVi = "De tai",
            Status = ProjectStatus.InProgress,
            PlannedStartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            PlannedEndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(12))
        };
        db.Projects.Add(project);
        db.Proposals.Add(new Proposal
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            VersionNo = 1,
            IsCurrent = true,
            TitleVi = "De tai",
            AbstractVi = "A",
            ResearchObjectives = "O",
            DurationMonths = 12,
            PlannedStartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            PlannedEndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(12)),
            Status = ProposalStatus.Approved
        });

        var contract = new Contract
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            ContractNumber = $"CTR-{Guid.NewGuid():N}"[..12],
            TotalAmount = 1_000_000m,
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(12)),
            OriginalEndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(12)),
            Status = ContractStatus.Active,
            CreatedBy = pi.Id
        };
        db.Contracts.Add(contract);
        await db.SaveChangesAsync();

        var deliverable = new ProjectDeliverable
        {
            ProjectId = project.Id,
            ContractId = contract.Id,
            ProductName = "Bao cao chuyen de",
            AcceptanceStatus = acceptanceStatus,
            SubmittedAt = submitted ? DateTime.UtcNow : null,
            Sequence = 1
        };
        db.ProjectDeliverables.Add(deliverable);
        await db.SaveChangesAsync();

        if (linkedToTranche)
        {
            db.ContractDisbursements.Add(new ContractDisbursement
            {
                ContractId = contract.Id,
                RoundNumber = 2,
                Percentage = 40m,
                PlannedAmount = 400_000m,
                ConditionDescription = "Sau khi nghiem thu san pham",
                Status = DisbursementStatus.Pending,
                DeliverableId = deliverable.Id
            });
            await db.SaveChangesAsync();
        }

        return (db, deliverable, contract);
    }

    [Fact]
    public async Task Delete_Untouched_Succeeds()
    {
        var (db, deliverable, _) = await SeedAsync();

        await MakeService(db).DeleteAsync(deliverable.Id);

        Assert.Null(await db.ProjectDeliverables.FindAsync(deliverable.Id));
    }

    [Fact]
    public async Task Delete_AlreadyPassed_Throws()
    {
        var (db, deliverable, _) = await SeedAsync(acceptanceStatus: AcceptanceStatus.Passed);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => MakeService(db).DeleteAsync(deliverable.Id));

        Assert.Contains("đã nghiệm thu ĐẠT", ex.Message);
        Assert.NotNull(await db.ProjectDeliverables.FindAsync(deliverable.Id));
    }

    [Fact]
    public async Task Delete_AlreadySubmitted_Throws()
    {
        var (db, deliverable, _) = await SeedAsync(submitted: true);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => MakeService(db).DeleteAsync(deliverable.Id));

        Assert.Contains("đã được nộp minh chứng", ex.Message);
    }

    /// <summary>Cửa khoá nặng nhất: sản phẩm đang là điều kiện mở một đợt giải ngân.</summary>
    [Fact]
    public async Task Delete_LinkedToDisbursement_Throws()
    {
        var (db, deliverable, _) = await SeedAsync(linkedToTranche: true);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => MakeService(db).DeleteAsync(deliverable.Id));

        Assert.Contains("đợt giải ngân 2", ex.Message);
        Assert.NotNull(await db.ProjectDeliverables.FindAsync(deliverable.Id));
    }

    [Fact]
    public async Task Update_AlreadyPassed_Throws()
    {
        var (db, deliverable, _) = await SeedAsync(acceptanceStatus: AcceptanceStatus.Passed);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => MakeService(db).UpdateAsync(deliverable.Id,
                new CreateDeliverableRequest { ProductName = "Doi ten" }));
    }

    [Fact]
    public async Task Update_Pending_ChangesNameAndDueDate()
    {
        var (db, deliverable, _) = await SeedAsync(acceptanceStatus: AcceptanceStatus.Pending);

        var result = await MakeService(db).UpdateAsync(deliverable.Id, new CreateDeliverableRequest
        {
            ProductName = "Bai bao tap chi trong nuoc",
            DueDate = "2027-01-31"
        });

        Assert.Equal("Bai bao tap chi trong nuoc", result.ProductName);
        Assert.Equal(new DateOnly(2027, 1, 31),
            (await db.ProjectDeliverables.FindAsync(deliverable.Id))!.DueDate);
    }
}
