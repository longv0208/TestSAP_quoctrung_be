using FURPMS.Application.Constants;
using FURPMS.Domain.Entities.Cycles;
using FURPMS.Domain.Entities.Proposals;
using FURPMS.Domain.Entities.Users;
using FURPMS.Infrastructure.Data;
using FURPMS.Infrastructure.Repositories;
using FURPMS.Infrastructure.Services;
using FURPMS.Tests.Helpers;

namespace FURPMS.Tests.Review;

// Phase 3 (Applied đặt hàng): chọn 1 winner → các đề cương cạnh tranh còn lại bị loại.
public class ResearchOrderWinnerTests
{
    private static (FURPMS.Domain.Entities.Projects.Project project, Proposal proposal) MakeProposal(Guid piId, int orderId, string status)
    {
        var project = new FURPMS.Domain.Entities.Projects.Project
        {
            Id = Guid.NewGuid(),
            CycleTrackId = 1,
            OrderId = orderId,
            PiUserId = piId,
            HostingUnitId = 1,
            ResearchTypeId = 1,
            TitleVi = "P",
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
            TitleVi = "P",
            AbstractVi = "A",
            ResearchObjectives = "O",
            DurationMonths = 12,
            PlannedStartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            PlannedEndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(12)),
            Status = status
        };
        return (project, proposal);
    }

    [Fact]
    public async Task MatchWinner_Approves_Winner_And_Rejects_Competitors()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");

        var order = new ResearchOrder
        {
            CycleId = 1,
            OrderingUnitId = 1,
            ResearchArea = "AI",
            ProblemDescription = "X",
            Status = "OPEN",
            CreatedBy = Guid.NewGuid()
        };
        db.Set<ResearchOrder>().Add(order);
        await db.SaveChangesAsync();

        var (winnerProject, winner) = MakeProposal(Guid.NewGuid(), order.Id, ProposalStatus.Submitted);
        var (loser1Project, loser1) = MakeProposal(Guid.NewGuid(), order.Id, ProposalStatus.Submitted);
        var (loser2Project, loser2) = MakeProposal(Guid.NewGuid(), order.Id, ProposalStatus.Submitted);
        db.Projects.AddRange(winnerProject, loser1Project, loser2Project);
        db.Proposals.AddRange(winner, loser1, loser2);
        await db.SaveChangesAsync();

        var service = new ResearchOrderService(new CycleRepository(db), new ProposalRepository(db));
        var rejectedCount = await service.MatchWinnerAsync(order.Id, winner.Id);

        Assert.Equal(2, rejectedCount);
        Assert.Equal(ProposalStatus.Approved, (await db.Proposals.FindAsync(winner.Id))!.Status);
        Assert.Equal(ProposalStatus.Rejected, (await db.Proposals.FindAsync(loser1.Id))!.Status);
        Assert.Equal(ProposalStatus.Rejected, (await db.Proposals.FindAsync(loser2.Id))!.Status);

        var afterOrder = await db.Set<ResearchOrder>().FindAsync(order.Id);
        Assert.Equal("MATCHED", afterOrder!.Status);
        Assert.Equal(winnerProject.Id, afterOrder.MatchedProjectId);
    }

    [Fact]
    public async Task MatchWinner_Proposal_NotForOrder_Throws_400()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");

        var order = new ResearchOrder
        {
            CycleId = 1, OrderingUnitId = 1, ResearchArea = "AI",
            ProblemDescription = "X", Status = "OPEN", CreatedBy = Guid.NewGuid()
        };
        db.Set<ResearchOrder>().Add(order);
        await db.SaveChangesAsync();

        // proposal đăng ký order khác (id+99) → không thuộc order này
        var (strayProject, stray) = MakeProposal(Guid.NewGuid(), order.Id + 99, ProposalStatus.Submitted);
        db.Projects.Add(strayProject);
        db.Proposals.Add(stray);
        await db.SaveChangesAsync();

        var service = new ResearchOrderService(new CycleRepository(db), new ProposalRepository(db));
        await Assert.ThrowsAsync<ArgumentException>(() => service.MatchWinnerAsync(order.Id, stray.Id));
    }
}
