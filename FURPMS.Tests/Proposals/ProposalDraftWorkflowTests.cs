using FURPMS.Application.Common;
using FURPMS.Application.DTOs.Proposals;
using FURPMS.Application.Constants;
using FURPMS.Domain.Entities.Cycles;
using FURPMS.Domain.Entities.MasterData;
using FURPMS.Domain.Entities.Projects;
using FURPMS.Domain.Entities.Proposals;
using FURPMS.Domain.Entities.Users;
using FURPMS.Infrastructure.Data;
using FURPMS.Infrastructure.Services;
using FURPMS.Tests.Helpers;
using FURPMS.Tests.Reminders;
using Microsoft.EntityFrameworkCore;

namespace FURPMS.Tests.Proposals;

public class ProposalDraftWorkflowTests
{
    private static readonly DateOnly Today = new(2026, 6, 18);

    private static User Pi() => new()
    {
        Id = Guid.NewGuid(), Email = $"pi-{Guid.NewGuid():N}@test.com", FullName = "PI", Status = UserStatus.Active,
        UnitId = 1
    };

    private static async Task<(FURPMSDbContext db, User pi, ResearchCycle cycle, ResearchOrder order)> SeedIntakeAsync()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var pi = Pi();
        db.Users.Add(pi);
        db.ResearchTypes.Add(new ResearchType { Id = 1, Code = "BASIC", Name = "Cơ bản", MaxBudgetCap = 100_000_000m });
        db.ResearchTracks.AddRange(
            new ResearchTrack { Id = 1, Code = "IT", Name = "CNTT" },
            new ResearchTrack { Id = 2, Code = "BIO", Name = "Sinh học" });
        db.OrganizationalUnits.Add(new OrganizationalUnit { Id = 1, Code = "UNIT", Name = "Đơn vị", UnitType = "FACULTY" });
        await db.SaveChangesAsync();

        var cycle = new ResearchCycle
        {
            CycleYear = 2026, SemesterCode = "SU26", ResearchTypeId = 1,
            SubmissionOpenDate = Today.AddDays(-10), SubmissionDeadline = Today.AddDays(10),
            ReviewDeadline = Today.AddDays(40), Status = CycleStatus.Open, CreatedBy = pi.Id
        };
        db.ResearchCycles.Add(cycle);
        await db.SaveChangesAsync();

        var order = new ResearchOrder
        {
            CycleId = cycle.Id, OrderingUnitId = 1, ResearchArea = "AI", ProblemDescription = "Problem",
            IsDefault = false, Status = "OPEN", CreatedBy = pi.Id
        };
        db.ResearchOrders.Add(order);
        await db.SaveChangesAsync();
        return (db, pi, cycle, order);
    }

    private static CreateProposalRequest Request(int? cycleId, int? orderId = null, string title = "Đề tài mới") => new()
    {
        CycleId = cycleId, OrderId = orderId, TrackId = "1", TitleVI = title,
        DurationMonths = 12, Objectives = "Mục tiêu nghiên cứu"
    };

    private static async Task<(FURPMSDbContext db, User pi, Proposal proposal, Project project)> SeedProposalAsync(string status)
    {
        var (db, pi, cycle, order) = await SeedIntakeAsync();
        var track = new CycleTrack { CycleId = cycle.Id, TrackId = 1 };
        db.CycleTracks.Add(track);
        await db.SaveChangesAsync();
        var project = new Project
        {
            Id = Guid.NewGuid(), CycleTrackId = track.Id, OrderId = order.Id, PiUserId = pi.Id,
            HostingUnitId = 1, ResearchTypeId = 1, TitleVi = "Đề tài cũ", Status = ProjectStatus.Proposed,
            PlannedStartDate = Today, PlannedEndDate = Today.AddMonths(12)
        };
        var proposal = new Proposal
        {
            Id = Guid.NewGuid(), ProjectId = project.Id, VersionNo = 1, IsCurrent = true,
            TitleVi = "Đề tài cũ", AbstractVi = "Tóm tắt", ResearchObjectives = "Mục tiêu",
            DurationMonths = 12, PlannedStartDate = Today, PlannedEndDate = Today.AddMonths(12), Status = status
        };
        db.Projects.Add(project);
        db.Proposals.Add(proposal);
        await db.SaveChangesAsync();
        db.ProposalBudgets.Add(new ProposalBudget { ProposalId = proposal.Id, TotalAmount = 0m });
        await db.SaveChangesAsync();
        return (db, pi, proposal, project);
    }

    [Fact]
    public async Task Create_BasicDraft_CreatesProjectProposalAndDefaultOrder()
    {
        var (db, pi, cycle, _) = await SeedIntakeAsync();
        var service = TestServices.Proposals(db, new FakeClock { UtcNow = Today.ToDateTime(TimeOnly.MinValue) });

        var result = await service.CreateProposalAsync(Request(cycle.Id), pi.Id);

        Assert.Equal(ProposalStatus.Draft, result.Status);
        Assert.Equal(ProjectStatus.Proposed, result.ProjectStatus);
        Assert.Equal(1, result.VersionNo);
        var project = await db.Projects.SingleAsync(p => p.Id == result.ProjectId);
        Assert.Equal(pi.Id, project.PiUserId);
        Assert.NotEqual(0, project.OrderId);
        Assert.True(await db.ProposalBudgets.AnyAsync(b => b.ProposalId == result.Id));
        Assert.True(await db.ResearchOrders.AnyAsync(o => o.CycleId == cycle.Id && o.IsDefault));
    }

    [Fact]
    public async Task Create_WithRequestedOrder_UsesSelectedOrder()
    {
        var (db, pi, cycle, order) = await SeedIntakeAsync();
        var service = TestServices.Proposals(db);

        var result = await service.CreateProposalAsync(Request(cycle.Id, order.Id, "Đề tài đặt hàng"), pi.Id);

        var project = await db.Projects.SingleAsync(p => p.Id == result.ProjectId);
        Assert.Equal(order.Id, project.OrderId);
        Assert.Equal(1, result.ResearchTypeId);
    }

    [Theory]
    [InlineData("", 12)]
    [InlineData("Đề tài", 0)]
    public async Task Create_InvalidRequiredFields_ThrowsArgumentException(string title, int duration)
    {
        var (db, pi, cycle, _) = await SeedIntakeAsync();
        var request = Request(cycle.Id, title: title);
        request.DurationMonths = duration;

        await Assert.ThrowsAsync<ArgumentException>(() => TestServices.Proposals(db).CreateProposalAsync(request, pi.Id));
    }

    [Fact]
    public async Task Update_Draft_PersistsFieldsAndProjectTrack()
    {
        var (db, pi, proposal, project) = await SeedProposalAsync(ProposalStatus.Draft);
        var service = TestServices.Proposals(db, new FakeClock { UtcNow = Today.ToDateTime(TimeOnly.MinValue) });

        var result = await service.UpdateProposalAsync(proposal.Id, new CreateProposalRequest
        {
            TrackId = "2", TitleVI = "Tên mới", DurationMonths = 6, Objectives = "Mục tiêu mới"
        }, pi.Id);

        Assert.Equal("Tên mới", result.TitleVI);
        Assert.Equal("2", result.TrackId);
        var updated = await db.Projects.FindAsync(project.Id);
        Assert.Equal("Tên mới", updated!.TitleVi);
        Assert.Equal(6, (await db.Proposals.FindAsync(proposal.Id))!.DurationMonths);
    }

    [Fact]
    public async Task Update_SubmittedOrByOtherUser_IsBlocked()
    {
        var (db, pi, proposal, _) = await SeedProposalAsync(ProposalStatus.Submitted);
        var request = Request(null, title: "Không được sửa");
        var service = TestServices.Proposals(db, new FakeClock { UtcNow = Today.ToDateTime(TimeOnly.MinValue) });

        await Assert.ThrowsAsync<ForbiddenException>(() => service.UpdateProposalAsync(proposal.Id, request, Guid.NewGuid()));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.UpdateProposalAsync(proposal.Id, request, pi.Id));
    }

    [Fact]
    public async Task Withdraw_SubmittedProposal_ReturnsDraftAndLogsDecision()
    {
        var (db, pi, proposal, project) = await SeedProposalAsync(ProposalStatus.Submitted);
        project.Status = ProjectStatus.UnderReview;
        await db.SaveChangesAsync();

        var result = await TestServices.Proposals(db).WithdrawProposalAsync(proposal.Id, pi.Id);

        Assert.Equal(ProposalStatus.Draft, result.Status);
        Assert.Equal(ProjectStatus.Proposed, (await db.Projects.FindAsync(project.Id))!.Status);
        var stored = await db.Proposals.FindAsync(proposal.Id);
        Assert.Null(stored!.SubmittedAt);
        Assert.True(await db.ProjectDecisions.AnyAsync(d => d.ProjectId == project.Id && d.DecisionType == DecisionTypes.ProposalWithdrawn));
    }

    [Theory]
    [InlineData(ProposalStatus.Draft)]
    [InlineData(ProposalStatus.Approved)]
    [InlineData(ProposalStatus.RevisionRequired)]
    [InlineData(ProposalStatus.Rejected)]
    public async Task Withdraw_NonSubmittedProposal_IsBlocked(string status)
    {
        var (db, pi, proposal, _) = await SeedProposalAsync(status);

        await Assert.ThrowsAsync<InvalidOperationException>(() => TestServices.Proposals(db).WithdrawProposalAsync(proposal.Id, pi.Id));
    }

    [Fact]
    public async Task Withdraw_ByNonOwner_IsForbidden()
    {
        var (db, _, proposal, _) = await SeedProposalAsync(ProposalStatus.Submitted);

        await Assert.ThrowsAsync<ForbiddenException>(() => TestServices.Proposals(db).WithdrawProposalAsync(proposal.Id, Guid.NewGuid()));
    }
}
