using FURPMS.Application.DTOs.Budget;
using FURPMS.Domain.Entities.MasterData;
using FURPMS.Domain.Entities.Proposals;
using FURPMS.Domain.Entities.Users;
using FURPMS.Infrastructure.Repositories;
using FURPMS.Infrastructure.Services;
using FURPMS.Tests.Helpers;

namespace FURPMS.Tests.Budget;

public class ProposalBudgetServiceTests
{
    private static User MakeUser() => new()
    {
        Id = Guid.NewGuid(),
        Email = $"user-{Guid.NewGuid()}@test.com",
        FullName = "Test User",
        Status = "ACTIVE"
    };

    private static (FURPMS.Domain.Entities.Projects.Project project, Proposal proposal) MakeProposal(Guid piId)
    {
        var project = new FURPMS.Domain.Entities.Projects.Project
        {
            Id = Guid.NewGuid(),
            CycleTrackId = 1,
            OrderId = 1,
            PiUserId = piId,
            HostingUnitId = 1,
            ResearchTypeId = 1,
            TitleVi = "Test",
            Status = "UNDER_REVIEW",
            PlannedStartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            PlannedEndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(12))
        };
        var proposal = new Proposal
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            VersionNo = 1,
            IsCurrent = true,
            TitleVi = "Test",
            AbstractVi = "Abstract",
            ResearchObjectives = "Objectives",
            DurationMonths = 12,
            PlannedStartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            PlannedEndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(12)),
            Status = "SUBMITTED"
        };
        return (project, proposal);
    }

    private static ProposalBudget MakeBudget(Guid proposalId) => new()
    {
        ProposalId = proposalId,
        TotalAmount = 0,
        LaborAmount = 0,
        EquipmentAmount = 0,
        ExternalServiceAmount = 0,
        ConferenceAmount = 0,
        OfficeSuppliesAmount = 0,
        IncidentalIpAmount = 0
    };

    private static BudgetExpenseCategory MakeCategory(string code = "LABOR", int seq = 1) => new()
    {
        Code = code,
        Name = code,
        Sequence = seq,
        IsActive = true
    };

    // ── test 1: budget sum mismatch → ArgumentException (→ 400) ──────────────

    [Fact]
    public async Task UpdateBudget_SumMismatch_Throws_400()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");

        var pi = MakeUser();
        var (project, proposal) = MakeProposal(pi.Id);
        var budget = MakeBudget(proposal.Id);
        var cat = MakeCategory();

        db.Users.Add(pi);
        db.Projects.Add(project);
        db.Proposals.Add(proposal);
        db.ProposalBudgets.Add(budget);
        db.BudgetExpenseCategories.Add(cat);
        await db.SaveChangesAsync();

        var service = new ProposalBudgetService(new ProposalRepository(db), new MasterDataRepository(db));

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => service.UpdateBudgetAsync(proposal.Id, new UpdateBudgetRequest
            {
                TotalAmount = 1_000_000m,
                Items = new List<BudgetItemDto>
                {
                    new() { CategoryId = cat.Id, Amount = 600_000m, Sequence = 1 },
                    new() { CategoryId = cat.Id, Amount = 300_000m, Sequence = 2 }
                    // sum = 900_000, but totalAmount = 1_000_000 → mismatch
                }
            }));

        Assert.Contains("does not equal", ex.Message);
    }

    // ── test 2: budget sum matches → items saved, TotalAmount updated ────────

    [Fact]
    public async Task UpdateBudget_SumMatches_Saves_Items()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");

        var pi = MakeUser();
        var (project, proposal) = MakeProposal(pi.Id);
        var budget = MakeBudget(proposal.Id);
        var cat1 = MakeCategory("LABOR", 1);
        var cat2 = MakeCategory("MATERIALS", 2);

        db.Users.Add(pi);
        db.Projects.Add(project);
        db.Proposals.Add(proposal);
        db.ProposalBudgets.Add(budget);
        db.BudgetExpenseCategories.AddRange(cat1, cat2);
        await db.SaveChangesAsync();

        var service = new ProposalBudgetService(new ProposalRepository(db), new MasterDataRepository(db));

        var result = await service.UpdateBudgetAsync(proposal.Id, new UpdateBudgetRequest
        {
            TotalAmount = 1_000_000m,
            Items = new List<BudgetItemDto>
            {
                new() { CategoryId = cat1.Id, Amount = 700_000m, Sequence = 1 },
                new() { CategoryId = cat2.Id, Amount = 300_000m, Sequence = 2 }
            }
        });

        Assert.Equal(1_000_000m, result.TotalAmount);
        Assert.Equal(2, result.Items.Count());

        var savedBudget = await db.ProposalBudgets.FindAsync(budget.Id);
        Assert.Equal(1_000_000m, savedBudget!.TotalAmount);
    }

    // ── test 3: labor computation dailyRate = coefficient × BASE_DAILY_SALARY ─

    [Fact]
    public async Task UpdateLaborDetail_ComputesDailyRate_Correctly()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");

        var pi = MakeUser();
        var (project, proposal) = MakeProposal(pi.Id);
        var budget = MakeBudget(proposal.Id);

        var member = new FURPMS.Domain.Entities.Projects.ProjectMember
        {
            ProjectId = project.Id,
            FullName = "Nguyen Van A",
            WorkContent = "Research",
            WorkMonths = 6,
            IsPi = false,
            IsSecretary = false,
            Sequence = 1
        };

        var config = new SystemFinancialConfig
        {
            Code = "BASE_DAILY_SALARY",
            Value = 1_490_000m,
            EffectiveDate = DateOnly.FromDateTime(DateTime.UtcNow),
            IsActive = true
        };

        db.Users.Add(pi);
        db.Projects.Add(project);
        db.Proposals.Add(proposal);
        db.ProposalBudgets.Add(budget);
        db.ProjectMembers.Add(member);
        db.SystemFinancialConfigs.Add(config);
        await db.SaveChangesAsync();

        var detail = new ProposalBudgetLaborDetail
        {
            ProposalId = proposal.Id,
            ProjectMemberId = member.Id,
            TotalResearchHours = 80m,
            HourlyRate = 50_000m,
            Sequence = 1
        };
        db.ProposalBudgetLaborDetails.Add(detail);
        await db.SaveChangesAsync();

        var service = new ProposalBudgetService(new ProposalRepository(db), new MasterDataRepository(db));

        // Act: workDays=10, coefficient=0.79 → dailyRate = 0.79 × 1,490,000 = 1,177,100
        var result = await service.UpdateLaborDetailAsync(proposal.Id, detail.Id, new UpdateLaborDetailRequest
        {
            TotalResearchHours = 80m,
            HourlyRate = 50_000m,
            WorkDays = 10m,
            Coefficient = 0.79m,
            Sequence = 1
        });

        Assert.Equal(0.79m, result.Coefficient);
        Assert.Equal(10m, result.WorkDays);
        Assert.Equal(1_177_100m, result.DailyRate);
        Assert.Equal(11_771_000m, result.ComputedDailyTotal); // 10 × 1,177,100
    }
}
