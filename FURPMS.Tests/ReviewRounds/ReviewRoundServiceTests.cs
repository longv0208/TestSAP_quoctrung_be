using FURPMS.Tests.Reminders;
using FURPMS.Application.DTOs.ReviewRounds;
using FURPMS.Domain.Entities.Proposals;
using FURPMS.Domain.Entities.Review;
using FURPMS.Domain.Entities.Users;
using FURPMS.Infrastructure.Repositories;
using FURPMS.Infrastructure.Services;
using FURPMS.Tests.Helpers;

namespace FURPMS.Tests.ReviewRounds;

public class ReviewRoundServiceTests
{
    // ── helpers ────────────────────────────────────────────────────────────────

    private static User MakeUser(Guid? id = null) => new()
    {
        Id = id ?? Guid.NewGuid(),
        Email = $"user-{Guid.NewGuid()}@test.com",
        FullName = "Test User",
        Status = "ACTIVE"
    };

    private static (FURPMS.Domain.Entities.Projects.Project project, Proposal proposal) MakeProjectWithProposal(
        Guid piUserId, string status = "SUBMITTED", int orderId = 1)
    {
        var project = new FURPMS.Domain.Entities.Projects.Project
        {
            Id = Guid.NewGuid(),
            CycleTrackId = 1,
            OrderId = orderId,
            PiUserId = piUserId,
            HostingUnitId = 1,
            ResearchTypeId = 1,
            TitleVi = "Test Project",
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
            TitleVi = "Test Proposal",
            AbstractVi = "Abstract",
            ResearchObjectives = "Objectives",
            DurationMonths = 12,
            PlannedStartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            PlannedEndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(12)),
            Status = status
        };
        return (project, proposal);
    }

    private static ReviewRound MakeRound(Guid projectId, string dimension, string status, Guid? prerequisiteId = null) => new()
    {
        Id = Guid.NewGuid(),
        CycleTrackId = 1,   // Phase B: round thuộc cycle_track; project gắn qua ProjectRound
        RoundNumber = 1,
        Sequence = 1,
        Dimension = dimension,
        RoundType = "REVIEW",
        Status = status,
        PrerequisiteRoundId = prerequisiteId
    };

    // ── test 1: FINANCE round cannot be opened before SCIENCE round PASSED ────

    [Fact]
    public async Task OpenRound_Finance_Before_Science_Passed_Throws_409()
    {
        // Arrange
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var pi = MakeUser();
        var (project, proposal) = MakeProjectWithProposal(pi.Id);
        db.Users.Add(pi);
        db.Projects.Add(project);
        db.Proposals.Add(proposal);

        var scienceRound = MakeRound(project.Id, "SCIENCE", "OPEN"); // NOT PASSED
        var financeRound = MakeRound(project.Id, "FINANCE", "PENDING", scienceRound.Id);
        financeRound.RoundNumber = 2;
        financeRound.Sequence = 2;

        db.ReviewRounds.Add(scienceRound);
        db.ReviewRounds.Add(financeRound);
        await db.SaveChangesAsync();

        var service = new ReviewRoundService(new ReviewRepository(db), new ProposalRepository(db), new NotificationRepository(db), TestNotifier.Create(db), new FakeClock());

        // Act & Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.OpenRoundAsync(financeRound.Id));

        Assert.Contains("not PASSED", ex.Message);
    }

    // ── test 2: Closing a round with REJECTED → proposal.Status = REJECTED ────

    [Fact]
    public async Task CloseRound_Rejected_Sets_Proposal_Rejected()
    {
        // Arrange
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var pi = MakeUser();
        var (project, proposal) = MakeProjectWithProposal(pi.Id, "SUBMITTED");
        db.Users.Add(pi);
        db.Projects.Add(project);
        db.Proposals.Add(proposal);

        var round = MakeRound(project.Id, "SCIENCE", "OPEN");
        db.ReviewRounds.Add(round);
        db.ProjectRounds.Add(new ProjectRound { ProjectId = project.Id, RoundId = round.Id, Status = "PENDING" });
        await db.SaveChangesAsync();

        var service = new ReviewRoundService(new ReviewRepository(db), new ProposalRepository(db), new NotificationRepository(db), TestNotifier.Create(db), new FakeClock());

        // Act
        await service.CloseRoundAsync(round.Id, new CloseRoundRequest { Result = "REJECTED" });

        // Assert
        var updatedProposal = await db.Proposals.FindAsync(proposal.Id);
        Assert.Equal("REJECTED", updatedProposal!.Status);

        var updatedRound = await db.ReviewRounds.FindAsync(round.Id);
        Assert.Equal("FAILED", updatedRound!.Status);
        Assert.Equal("REJECTED", updatedRound.Result);
    }

    [Fact]
    public async Task CloseRound_Approved_Sets_Round_Passed_And_Proposal_Unchanged()
    {
        // Arrange
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var pi = MakeUser();
        var (project, proposal) = MakeProjectWithProposal(pi.Id, "SUBMITTED");
        db.Users.Add(pi);
        db.Projects.Add(project);
        db.Proposals.Add(proposal);

        var round = MakeRound(project.Id, "SCIENCE", "OPEN");
        db.ReviewRounds.Add(round);
        db.ProjectRounds.Add(new ProjectRound { ProjectId = project.Id, RoundId = round.Id, Status = "PENDING" });
        await db.SaveChangesAsync();

        var service = new ReviewRoundService(new ReviewRepository(db), new ProposalRepository(db), new NotificationRepository(db), TestNotifier.Create(db), new FakeClock());

        // Act
        await service.CloseRoundAsync(round.Id, new CloseRoundRequest { Result = "APPROVED" });

        // Assert
        var updatedRound = await db.ReviewRounds.FindAsync(round.Id);
        Assert.Equal("PASSED", updatedRound!.Status);

        var updatedProposal = await db.Proposals.FindAsync(proposal.Id);
        Assert.Equal("SUBMITTED", updatedProposal!.Status); // unchanged
    }

    [Fact]
    public async Task OpenRound_After_Prerequisite_Passed_Succeeds()
    {
        // Arrange
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var pi = MakeUser();
        var (project, proposal) = MakeProjectWithProposal(pi.Id);
        db.Users.Add(pi);
        db.Projects.Add(project);
        db.Proposals.Add(proposal);

        var scienceRound = MakeRound(project.Id, "SCIENCE", "PASSED");
        var financeRound = MakeRound(project.Id, "FINANCE", "PENDING", scienceRound.Id);
        financeRound.RoundNumber = 2;
        financeRound.Sequence = 2;

        db.ReviewRounds.Add(scienceRound);
        db.ReviewRounds.Add(financeRound);
        await db.SaveChangesAsync();

        var service = new ReviewRoundService(new ReviewRepository(db), new ProposalRepository(db), new NotificationRepository(db), TestNotifier.Create(db), new FakeClock());

        // Act
        var result = await service.OpenRoundAsync(financeRound.Id);

        // Assert
        Assert.Equal("OPEN", result.Status);
    }
}
