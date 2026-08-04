using FURPMS.Tests.Reminders;
using FURPMS.Domain.Entities.Contracts;
using FURPMS.Domain.Entities.Financial;
using FURPMS.Domain.Entities.MasterData;
using FURPMS.Domain.Entities.Progress;
using FURPMS.Domain.Entities.Proposals;
using FURPMS.Domain.Entities.Users;
using FURPMS.Infrastructure.Repositories;
using FURPMS.Infrastructure.Services;
using FURPMS.Tests.Helpers;

namespace FURPMS.Tests.Contracts;

public class ContractLifecycleTests
{
    // ── Shared helpers ────────────────────────────────────────────────────────

    private static User MakeUser() => new()
    {
        Id = Guid.NewGuid(),
        Email = $"u-{Guid.NewGuid()}@test.com",
        FullName = "Test User",
        Status = "ACTIVE"
    };

    private static ResearchType MakeResearchType() => new()
    {
        Code = $"RT-{Guid.NewGuid():N}"[..8],
        Name = "Applied Research",
        IsActive = true
    };

    private static (FURPMS.Domain.Entities.Projects.Project project, Proposal proposal) MakeApprovedProposal(Guid piId, int researchTypeId)
    {
        var project = new FURPMS.Domain.Entities.Projects.Project
        {
            Id = Guid.NewGuid(),
            CycleTrackId = 1,
            OrderId = 1,
            PiUserId = piId,
            HostingUnitId = 1,
            ResearchTypeId = researchTypeId,
            TitleVi = "Test Project",
            Status = "APPROVED",
            PlannedStartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            PlannedEndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(12))
        };
        var proposal = new Proposal
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            VersionNo = 1,
            IsCurrent = true,
            TitleVi = "Test Proposal",
            AbstractVi = "Abstract",
            ResearchObjectives = "Objectives",
            DurationMonths = 12,
            PlannedStartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            PlannedEndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(12)),
            Status = "APPROVED",
            FundingMethod = "WHOLE"
        };
        return (project, proposal);
    }

    private static Contract MakeContract(Guid projectId, decimal totalAmount = 1_000_000m) => new()
    {
        ProjectId = projectId,
        ContractNumber = $"CTR-{Guid.NewGuid():N}"[..12],
        TotalAmount = totalAmount,
        StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
        EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(12)),
        OriginalEndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(12)),
        MaxExtensionMonths = 6,
        Status = "ACTIVE",
        CreatedBy = Guid.NewGuid()
    };

    // ── Test 1: WHOLE → 3 tranches, equal split when no template ────────────

    [Fact]
    public async Task GenerateWhole_NoTemplate_Creates3EqualTranches()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");

        var pi = MakeUser();
        var rt = MakeResearchType();
        db.Users.Add(pi);
        db.ResearchTypes.Add(rt);
        await db.SaveChangesAsync();

        var (project, proposal) = MakeApprovedProposal(pi.Id, rt.Id);
        proposal.FundingMethod = "WHOLE";
        db.Projects.Add(project);
        db.Proposals.Add(proposal);
        await db.SaveChangesAsync();

        var contract = MakeContract(project.Id, 1_200_000m);
        contract.Project = project;
        db.Contracts.Add(contract);
        await db.SaveChangesAsync();

        var svc = new DisbursementService(new ContractRepository(db), new MasterDataRepository(db), new FakeClock(), new SystemSettingService(new MasterDataRepository(db)));
        var result = (await svc.GenerateAsync(contract.Id)).ToList();

        Assert.Equal(3, result.Count);
        Assert.All(result, t => Assert.Equal("PENDING", t.Status));
        // Equal split: floor(100/3)=33, 33, 34
        Assert.Equal(33m, result[0].Percentage);
        Assert.Equal(33m, result[1].Percentage);
        Assert.Equal(34m, result[2].Percentage);
        Assert.Equal(100m, result.Sum(r => r.Percentage));
    }

    // ── Test 2: WHOLE with templates → uses template percentages ─────────────

    [Fact]
    public async Task GenerateWhole_WithTemplate_UsesTemplatePercentages()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");

        var pi = MakeUser();
        var rt = MakeResearchType();
        db.Users.Add(pi);
        db.ResearchTypes.Add(rt);
        await db.SaveChangesAsync();

        db.DisbursementTemplates.AddRange(
            new DisbursementTemplate { ResearchTypeId = rt.Id, RoundNumber = 1, Percentage = 40m, ConditionDescription = "Start", IsActive = true },
            new DisbursementTemplate { ResearchTypeId = rt.Id, RoundNumber = 2, Percentage = 30m, ConditionDescription = "Mid",   IsActive = true },
            new DisbursementTemplate { ResearchTypeId = rt.Id, RoundNumber = 3, Percentage = 30m, ConditionDescription = "End",   IsActive = true }
        );

        var (project, proposal) = MakeApprovedProposal(pi.Id, rt.Id);
        proposal.FundingMethod = "WHOLE";
        db.Projects.Add(project);
        db.Proposals.Add(proposal);
        await db.SaveChangesAsync();

        var contract = MakeContract(project.Id, 1_000_000m);
        contract.Project = project;
        db.Contracts.Add(contract);
        await db.SaveChangesAsync();

        var svc = new DisbursementService(new ContractRepository(db), new MasterDataRepository(db), new FakeClock(), new SystemSettingService(new MasterDataRepository(db)));
        var result = (await svc.GenerateAsync(contract.Id)).ToList();

        Assert.Equal(3, result.Count);
        Assert.Equal(40m, result[0].Percentage);
        Assert.Equal(400_000m, result[0].PlannedAmount);
        Assert.Equal(30m, result[1].Percentage);
        Assert.Equal(30m, result[2].Percentage);
    }

    // ── Test 3: PARTIAL → one tranche per deliverable ────────────────────────

    [Fact]
    public async Task GeneratePartial_CreatesOneTranchePerDeliverable()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");

        var pi = MakeUser();
        var rt = MakeResearchType();
        var productCat = new ProductCategory { Code = "SOFTWARE", Name = "Software", IsActive = true };
        db.Users.Add(pi);
        db.ResearchTypes.Add(rt);
        db.ProductCategories.Add(productCat);
        await db.SaveChangesAsync();

        var (project, proposal) = MakeApprovedProposal(pi.Id, rt.Id);
        proposal.FundingMethod = "PARTIAL";
        db.Projects.Add(project);
        db.Proposals.Add(proposal);
        await db.SaveChangesAsync();

        var contract = MakeContract(project.Id, 900_000m);
        contract.Project = project;
        db.Contracts.Add(contract);
        await db.SaveChangesAsync();

        // 2 deliverables
        db.ProjectDeliverables.AddRange(
            new FURPMS.Domain.Entities.Projects.ProjectDeliverable { ProjectId = project.Id, ContractId = contract.Id, CategoryId = productCat.Id, ProductName = "Deliverable A", AcceptanceStatus = "PENDING", IsCompleted = false },
            new FURPMS.Domain.Entities.Projects.ProjectDeliverable { ProjectId = project.Id, ContractId = contract.Id, CategoryId = productCat.Id, ProductName = "Deliverable B", AcceptanceStatus = "PENDING", IsCompleted = false }
        );
        await db.SaveChangesAsync();

        var svc = new DisbursementService(new ContractRepository(db), new MasterDataRepository(db), new FakeClock(), new SystemSettingService(new MasterDataRepository(db)));
        var result = (await svc.GenerateAsync(contract.Id)).ToList();

        Assert.Equal(2, result.Count);
        Assert.All(result, t => Assert.NotNull(t.DeliverableId));
        Assert.Equal(100m, result.Sum(r => r.Percentage));
    }

    // ── Test 4: Deliverable PASSED + PARTIAL → condition_met_at set, tranche NOT DISBURSED ──

    [Fact]
    public async Task EvaluateDeliverable_Passed_SetsConditionMetAt_NotDisbursed()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");

        var pi = MakeUser();
        var rt = MakeResearchType();
        var productCat = new ProductCategory { Code = "SW2", Name = "Software", IsActive = true };
        var staffRole = new Role { Name = "Staff" };
        db.Users.Add(pi);
        db.ResearchTypes.Add(rt);
        db.ProductCategories.Add(productCat);
        db.Roles.Add(staffRole);
        await db.SaveChangesAsync();

        var (project, proposal) = MakeApprovedProposal(pi.Id, rt.Id);
        proposal.FundingMethod = "PARTIAL";
        db.Projects.Add(project);
        db.Proposals.Add(proposal);
        await db.SaveChangesAsync();

        var contract = MakeContract(project.Id, 600_000m);
        contract.Project = project;
        db.Contracts.Add(contract);
        await db.SaveChangesAsync();

        var deliverable = new FURPMS.Domain.Entities.Projects.ProjectDeliverable
        {
            ProjectId = project.Id,
            ContractId = contract.Id,
            CategoryId = productCat.Id,
            ProductName = "Product X",
            AcceptanceStatus = "PENDING",
            IsCompleted = false
        };
        db.ProjectDeliverables.Add(deliverable);
        await db.SaveChangesAsync();

        var tranche = new ContractDisbursement
        {
            ContractId = contract.Id,
            RoundNumber = 1,
            Percentage = 100m,
            PlannedAmount = 600_000m,
            ConditionDescription = "Product X delivery",
            DeliverableId = deliverable.Id,
            Status = "PENDING"
        };
        db.ContractDisbursements.Add(tranche);
        await db.SaveChangesAsync();

        var svc = new DeliverableService(new ContractRepository(db), new UserRepository(db), new NotificationRepository(db), TestNotifier.Create(db), new FakeClock());
        var staffId = Guid.NewGuid();
        var result = await svc.EvaluateAsync(deliverable.Id,
            new Application.DTOs.Contract.EvaluateDeliverableRequest
            {
                AcceptanceStatus = "PASSED",
                QualityAssessment = "Good"
            }, staffId);

        Assert.Equal("PASSED", result.AcceptanceStatus);
        Assert.True(result.IsCompleted);

        // Tranche should have ConditionMetAt set but status still PENDING (not auto-disbursed)
        var refreshed = await db.ContractDisbursements.FindAsync(tranche.Id);
        Assert.NotNull(refreshed!.ConditionMetAt);
        Assert.Equal("PENDING", refreshed.Status);
    }

    // ── Test 5: Deliverable FAILED → contract UNDER_REVIEW ───────────────────

    [Fact]
    public async Task EvaluateDeliverable_Failed_SetsContractUnderReview()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");

        var pi = MakeUser();
        var rt = MakeResearchType();
        var productCat = new ProductCategory { Code = "SW3", Name = "Software", IsActive = true };
        var staffRole = new Role { Name = "Staff" };
        db.Users.Add(pi);
        db.ResearchTypes.Add(rt);
        db.ProductCategories.Add(productCat);
        db.Roles.Add(staffRole);
        await db.SaveChangesAsync();

        var (project, proposal) = MakeApprovedProposal(pi.Id, rt.Id);
        proposal.FundingMethod = "WHOLE";
        db.Projects.Add(project);
        db.Proposals.Add(proposal);
        await db.SaveChangesAsync();

        var contract = MakeContract(project.Id);
        contract.Project = project;
        db.Contracts.Add(contract);
        await db.SaveChangesAsync();

        var deliverable = new FURPMS.Domain.Entities.Projects.ProjectDeliverable
        {
            ProjectId = project.Id,
            ContractId = contract.Id,
            CategoryId = productCat.Id,
            ProductName = "Report Z",
            AcceptanceStatus = "PENDING",
            IsCompleted = false
        };
        db.ProjectDeliverables.Add(deliverable);
        await db.SaveChangesAsync();

        var svc = new DeliverableService(new ContractRepository(db), new UserRepository(db), new NotificationRepository(db), TestNotifier.Create(db), new FakeClock());
        await svc.EvaluateAsync(deliverable.Id,
            new Application.DTOs.Contract.EvaluateDeliverableRequest
            {
                AcceptanceStatus = "FAILED",
                QualityAssessment = "Insufficient"
            }, Guid.NewGuid());

        var refreshedContract = await db.Contracts.FindAsync(contract.Id);
        Assert.Equal("UNDER_REVIEW", refreshedContract!.Status);
    }

    // ── Test 6: Extension over MaxExtensionMonths → ArgumentException (400) ──

    [Fact]
    public async Task ApproveExtensionAmendment_OverLimit_ThrowsArgumentException()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");

        var pi = MakeUser();
        db.Users.Add(pi);
        var extCat = new AmendmentCategory { Code = "EXTENSION", Name = "Gia hạn", IsActive = true };
        db.AmendmentCategories.Add(extCat);
        await db.SaveChangesAsync();

        var rt = MakeResearchType();
        db.ResearchTypes.Add(rt);
        var (project, proposal) = MakeApprovedProposal(pi.Id, rt.Id);
        db.Projects.Add(project);
        db.Proposals.Add(proposal);
        await db.SaveChangesAsync();

        var contract = MakeContract(project.Id);
        contract.Project = project;
        contract.MaxExtensionMonths = 3;
        db.Contracts.Add(contract);
        await db.SaveChangesAsync();

        var amendment = new AmendmentRequest
        {
            ContractId = contract.Id,
            CategoryId = extCat.Id,
            ChangeDescription = "Need more time",
            Justification = "Reason",
            NewValue = "6",    // 6 months, but max is 3
            RequestedBy = pi.Id,
            Status = "PENDING"
        };
        db.AmendmentRequests.Add(amendment);
        await db.SaveChangesAsync();

        var svc = new AmendmentService(new ContractRepository(db), new MasterDataRepository(db), new FakeClock());
        var staffId = Guid.NewGuid();

        await Assert.ThrowsAsync<ArgumentException>(
            () => svc.ApproveAsync(amendment.Id,
                new Application.DTOs.Contract.ReviewAmendmentRequest(), staffId));
    }

    // ── Test 7: Extension within limit → contract end_date updated ───────────

    [Fact]
    public async Task ApproveExtensionAmendment_WithinLimit_UpdatesContractEndDate()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");

        var pi = MakeUser();
        db.Users.Add(pi);
        var extCat = new AmendmentCategory { Code = "EXTENSION", Name = "Gia hạn", IsActive = true };
        db.AmendmentCategories.Add(extCat);
        await db.SaveChangesAsync();

        var rt = MakeResearchType();
        db.ResearchTypes.Add(rt);
        var (project, proposal) = MakeApprovedProposal(pi.Id, rt.Id);
        db.Projects.Add(project);
        db.Proposals.Add(proposal);
        await db.SaveChangesAsync();

        var originalEnd = new DateOnly(2027, 1, 1);
        var contract = MakeContract(project.Id);
        contract.Project = project;
        contract.MaxExtensionMonths = 6;
        contract.EndDate = originalEnd;
        contract.OriginalEndDate = originalEnd;
        db.Contracts.Add(contract);
        await db.SaveChangesAsync();

        var amendment = new AmendmentRequest
        {
            ContractId = contract.Id,
            CategoryId = extCat.Id,
            ChangeDescription = "3 month extension",
            Justification = "Reason",
            NewValue = "3",
            RequestedBy = pi.Id,
            Status = "PENDING"
        };
        db.AmendmentRequests.Add(amendment);
        await db.SaveChangesAsync();

        var svc = new AmendmentService(new ContractRepository(db), new MasterDataRepository(db), new FakeClock());
        await svc.ApproveAsync(amendment.Id,
            new Application.DTOs.Contract.ReviewAmendmentRequest(), Guid.NewGuid());

        var refreshed = await db.Contracts.FindAsync(contract.Id);
        Assert.Equal(originalEnd.AddMonths(3), refreshed!.EndDate);
    }
}
