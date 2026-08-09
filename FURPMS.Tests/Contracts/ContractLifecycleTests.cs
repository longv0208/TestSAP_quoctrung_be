using FURPMS.Application.Constants;
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

    // ── Đề tài đã đóng thì không điều chỉnh được nữa (thầy 05/08) ────────────

    /// <summary>
    /// Thầy bắt lúc demo: *"sản phẩm đã nghiệm thu rồi nhưng vẫn có thể tiếp tục gia hạn thêm
    /// thời gian làm"*. Nghiệm thu ĐẠT đưa project sang COMPLETED ⇒ gia hạn cho một đề tài đã
    /// hoàn thành là vô nghĩa. `ChangeRequestService` (BM07) chặn đúng như vậy từ trước.
    /// </summary>
    [Theory]
    [InlineData(ProjectStatus.Completed)]
    [InlineData(ProjectStatus.Cancelled)]
    [InlineData(ProjectStatus.Terminated)]
    public async Task ApproveExtensionAmendment_ProjectClosed_Throws(string closedStatus)
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
        project.Status = closedStatus;
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
            ChangeDescription = "Xin gia hạn thêm 3 tháng",
            Justification = "Lý do",
            NewValue = "3",
            RequestedBy = pi.Id,
            Status = "PENDING"
        };
        db.AmendmentRequests.Add(amendment);
        await db.SaveChangesAsync();

        var svc = new AmendmentService(new ContractRepository(db), new MasterDataRepository(db), new FakeClock());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.ApproveAsync(amendment.Id, new Application.DTOs.Contract.ReviewAmendmentRequest(), Guid.NewGuid()));

        // Hạn hợp đồng phải giữ nguyên — chặn mà vẫn dời hạn thì coi như không chặn.
        var refreshed = await db.Contracts.FindAsync(contract.Id);
        Assert.Equal(originalEnd, refreshed!.EndDate);
    }

    /// <summary>Không chỉ chặn lúc duyệt — PI cũng không gửi được đơn mới cho đề tài đã đóng.</summary>
    [Fact]
    public async Task CreateAmendment_ProjectCompleted_Throws()
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
        project.Status = ProjectStatus.Completed;
        db.Projects.Add(project);
        db.Proposals.Add(proposal);
        await db.SaveChangesAsync();

        var contract = MakeContract(project.Id);
        contract.Project = project;
        db.Contracts.Add(contract);
        await db.SaveChangesAsync();

        var svc = new AmendmentService(new ContractRepository(db), new MasterDataRepository(db), new FakeClock());

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.CreateAsync(contract.Id, new Application.DTOs.Contract.CreateAmendmentRequest
            {
                CategoryId = extCat.Id,
                ChangeDescription = "Xin gia hạn",
                Justification = "Lý do",
                NewValue = "3"
            }, pi.Id));
    }

    // ── P5: giải ngân phải có sản phẩm minh chứng đã nghiệm thu ──────────────

    /// <summary>Dựng 1 hợp đồng + 1 đợt giải ngân, tuỳ chọn gắn sản phẩm ở trạng thái cho trước.</summary>
    private static async Task<(FURPMS.Infrastructure.Data.FURPMSDbContext db, ContractDisbursement tranche, int deliverableId)>
        SeedTrancheAsync(string? deliverableStatus)
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");

        var pi = MakeUser();
        var rt = MakeResearchType();
        var cat = new ProductCategory { Code = "SW-P5", Name = "Software", IsActive = true };
        db.Users.Add(pi);
        db.ResearchTypes.Add(rt);
        db.ProductCategories.Add(cat);
        await db.SaveChangesAsync();

        var (project, proposal) = MakeApprovedProposal(pi.Id, rt.Id);
        proposal.FundingMethod = "WHOLE";   // P5: WHOLE cũng gắn được sản phẩm
        db.Projects.Add(project);
        db.Proposals.Add(proposal);
        await db.SaveChangesAsync();

        var contract = MakeContract(project.Id);
        db.Contracts.Add(contract);
        await db.SaveChangesAsync();

        int deliverableId = 0;
        if (deliverableStatus != null)
        {
            var deliverable = new FURPMS.Domain.Entities.Projects.ProjectDeliverable
            {
                ProjectId = project.Id,
                ContractId = contract.Id,
                CategoryId = cat.Id,
                ProductName = "Bao cao chuyen de",
                AcceptanceStatus = deliverableStatus,
                IsCompleted = deliverableStatus == "PASSED"
            };
            db.ProjectDeliverables.Add(deliverable);
            await db.SaveChangesAsync();
            deliverableId = deliverable.Id;
        }

        var tranche = new ContractDisbursement
        {
            ContractId = contract.Id,
            RoundNumber = 1,
            Percentage = 100m,
            PlannedAmount = 1_000_000m,
            ConditionDescription = "Dot 1",
            DeliverableId = deliverableStatus == null ? null : deliverableId,
            Status = "PENDING"
        };
        db.ContractDisbursements.Add(tranche);
        await db.SaveChangesAsync();

        return (db, tranche, deliverableId);
    }

    private static DisbursementService MakeDisbursementService(FURPMS.Infrastructure.Data.FURPMSDbContext db) =>
        new(new ContractRepository(db), new MasterDataRepository(db), new FakeClock(),
            new SystemSettingService(new MasterDataRepository(db)));

    [Theory]
    [InlineData("PENDING")]
    [InlineData("FAILED")]
    public async Task Confirm_DeliverableNotPassed_Throws(string deliverableStatus)
    {
        var (db, tranche, _) = await SeedTrancheAsync(deliverableStatus);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            MakeDisbursementService(db).ConfirmAsync(
                tranche.Id, new Application.DTOs.Contract.ConfirmDisbursementRequest(), Guid.NewGuid()));

        Assert.Contains("chua nghiem thu Dat", RemoveDiacritics(ex.Message));

        // Không được đánh dấu nửa vời: đợt vẫn PENDING.
        Assert.Equal("PENDING", (await db.ContractDisbursements.FindAsync(tranche.Id))!.Status);
    }

    /// <summary>Đợt KHÔNG gắn sản phẩm (vd tạm ứng khởi động) vẫn đánh dấu được — không chặn oan.</summary>
    [Fact]
    public async Task Confirm_NoDeliverableLinked_Succeeds()
    {
        var (db, tranche, _) = await SeedTrancheAsync(null);

        var result = await MakeDisbursementService(db).ConfirmAsync(
            tranche.Id, new Application.DTOs.Contract.ConfirmDisbursementRequest(), Guid.NewGuid());

        Assert.Equal("DISBURSED", result.Status);
        Assert.False(result.IsBlockedByDeliverable);
    }

    [Fact]
    public async Task Confirm_DeliverablePassed_Succeeds()
    {
        var (db, tranche, _) = await SeedTrancheAsync("PASSED");

        var result = await MakeDisbursementService(db).ConfirmAsync(
            tranche.Id, new Application.DTOs.Contract.ConfirmDisbursementRequest(), Guid.NewGuid());

        Assert.Equal("DISBURSED", result.Status);
        Assert.Equal("Bao cao chuyen de", result.DeliverableName);
    }

    // ── C6: đợt giải ngân CUỐI chỉ mở sau khi nghiệm thu Đạt (BM05 Điều 4.2) ──

    /// <summary>Dựng hợp đồng 3 đợt để có "đợt cuối" thật sự, trạng thái đề tài truyền vào.</summary>
    private static async Task<(FURPMS.Infrastructure.Data.FURPMSDbContext db, List<ContractDisbursement> tranches)>
        SeedThreeTranchesAsync(string projectStatus)
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var pi = MakeUser();
        var rt = MakeResearchType();
        db.Users.Add(pi);
        db.ResearchTypes.Add(rt);
        await db.SaveChangesAsync();

        var (project, proposal) = MakeApprovedProposal(pi.Id, rt.Id);
        project.Status = projectStatus;
        db.Projects.Add(project);
        db.Proposals.Add(proposal);
        await db.SaveChangesAsync();

        var contract = MakeContract(project.Id);
        db.Contracts.Add(contract);
        await db.SaveChangesAsync();

        var tranches = Enumerable.Range(1, 3).Select(n => new ContractDisbursement
        {
            ContractId = contract.Id,
            RoundNumber = n,
            Percentage = n == 3 ? 20m : 40m,
            PlannedAmount = n == 3 ? 200_000m : 400_000m,
            ConditionDescription = $"Dot {n}",
            Status = "PENDING"
        }).ToList();
        db.ContractDisbursements.AddRange(tranches);
        await db.SaveChangesAsync();
        return (db, tranches);
    }

    /// <summary>
    /// BM05 Điều 4.2 — đợt cuối chỉ chi sau khi đề tài được công nhận Đạt. Không chặn thì chi hết
    /// tiền xong mới họp nghiệm thu, mất luôn đòn bẩy cuối cùng của mốc giải ngân.
    /// </summary>
    [Theory]
    [InlineData("IN_PROGRESS")]
    [InlineData("ACCEPTANCE")]
    public async Task Confirm_FinalTranche_BeforeAcceptancePassed_Throws(string projectStatus)
    {
        var (db, tranches) = await SeedThreeTranchesAsync(projectStatus);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            MakeDisbursementService(db).ConfirmAsync(
                tranches[2].Id, new Application.DTOs.Contract.ConfirmDisbursementRequest(), Guid.NewGuid()));

        Assert.Contains("dot giai ngan CUOI", RemoveDiacritics(ex.Message));
        Assert.Equal("PENDING", (await db.ContractDisbursements.FindAsync(tranches[2].Id))!.Status);
    }

    /// <summary>Các đợt TRƯỚC đợt cuối vẫn chi bình thường — luật chỉ gác đúng đợt cuối.</summary>
    [Fact]
    public async Task Confirm_EarlierTranche_BeforeAcceptance_Succeeds()
    {
        var (db, tranches) = await SeedThreeTranchesAsync("IN_PROGRESS");

        var result = await MakeDisbursementService(db).ConfirmAsync(
            tranches[0].Id, new Application.DTOs.Contract.ConfirmDisbursementRequest(), Guid.NewGuid());

        Assert.Equal("DISBURSED", result.Status);
    }

    [Fact]
    public async Task Confirm_FinalTranche_AfterAcceptancePassed_Succeeds()
    {
        var (db, tranches) = await SeedThreeTranchesAsync("COMPLETED");

        var result = await MakeDisbursementService(db).ConfirmAsync(
            tranches[2].Id, new Application.DTOs.Contract.ConfirmDisbursementRequest(), Guid.NewGuid());

        Assert.Equal("DISBURSED", result.Status);
    }

    /// <summary>Không cho lấy sản phẩm của hợp đồng khác làm minh chứng.</summary>
    [Fact]
    public async Task LinkDeliverable_FromAnotherContract_Throws()
    {
        var (db, tranche, _) = await SeedTrancheAsync(null);

        var otherProject = (await db.Projects.FindAsync(
            (await db.Contracts.FindAsync(tranche.ContractId))!.ProjectId))!;
        var otherContract = MakeContract(otherProject.Id);
        db.Contracts.Add(otherContract);
        await db.SaveChangesAsync();

        var foreign = new FURPMS.Domain.Entities.Projects.ProjectDeliverable
        {
            ProjectId = otherProject.Id,
            ContractId = otherContract.Id,
            CategoryId = db.ProductCategories.First().Id,
            ProductName = "San pham HD khac",
            AcceptanceStatus = "PASSED"
        };
        db.ProjectDeliverables.Add(foreign);
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<ArgumentException>(() =>
            MakeDisbursementService(db).LinkDeliverableAsync(
                tranche.Id, new Application.DTOs.Contract.LinkDeliverableRequest { DeliverableId = foreign.Id }));
    }

    /// <summary>Gắn sản phẩm ĐÃ nghiệm thu ⇒ điều kiện đạt ngay, không phải chấm lại.</summary>
    [Fact]
    public async Task LinkDeliverable_AlreadyPassed_SetsConditionMet()
    {
        var (db, tranche, deliverableId) = await SeedTrancheAsync("PASSED");
        tranche.DeliverableId = null;               // giả lập đợt WHOLE chưa gắn gì
        await db.SaveChangesAsync();

        var result = await MakeDisbursementService(db).LinkDeliverableAsync(
            tranche.Id, new Application.DTOs.Contract.LinkDeliverableRequest { DeliverableId = deliverableId });

        Assert.Equal(deliverableId, result.DeliverableId);
        Assert.NotNull(result.ConditionMetAt);
        Assert.False(result.IsBlockedByDeliverable);
    }

    private static string RemoveDiacritics(string text)
    {
        var normalized = text.Normalize(System.Text.NormalizationForm.FormD);
        var chars = normalized.Where(c =>
            System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c)
                != System.Globalization.UnicodeCategory.NonSpacingMark);
        return new string(chars.ToArray()).Replace('đ', 'd').Replace('Đ', 'D');
    }
}
