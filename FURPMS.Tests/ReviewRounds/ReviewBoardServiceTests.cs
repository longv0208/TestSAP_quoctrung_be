using FURPMS.Application.DTOs.ReviewRounds;
using FURPMS.Domain.Entities.Proposals;
using FURPMS.Domain.Entities.Review;
using FURPMS.Domain.Entities.Users;
using FURPMS.Infrastructure.Repositories;
using FURPMS.Infrastructure.Services;
using FURPMS.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace FURPMS.Tests.ReviewRounds;

public class ReviewBoardServiceTests
{
    private static User MakeUser(Guid? id = null) => new()
    {
        Id = id ?? Guid.NewGuid(),
        Email = $"user-{Guid.NewGuid()}@test.com",
        FullName = "Test User",
        Status = "ACTIVE"
    };

    private static (FURPMS.Domain.Entities.Projects.Project project, Proposal proposal) MakeProjectWithProposal(
        Guid piUserId, string status = "SUBMITTED", int cycleTrackId = 1)
    {
        var project = new FURPMS.Domain.Entities.Projects.Project
        {
            Id = Guid.NewGuid(),
            CycleTrackId = cycleTrackId,
            OrderId = 1,
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

    private static ReviewRound MakeRound(int cycleTrackId = 1, string dimension = "SCIENCE") => new()
    {
        Id = Guid.NewGuid(),
        CycleTrackId = cycleTrackId,
        RoundNumber = 1,
        Sequence = 1,
        Dimension = dimension,
        RoundType = "REVIEW",
        Status = "PENDING"
    };

    private static ReviewBoardService MakeService(FURPMS.Infrastructure.Data.FURPMSDbContext db) =>
        new(new ReviewRepository(db), new ProposalRepository(db), new CycleRepository(db));

    // ── Xóa vòng ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteRound_Empty_Succeeds()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var round = MakeRound();
        db.ReviewRounds.Add(round);
        await db.SaveChangesAsync();

        var service = MakeService(db);
        await service.DeleteRoundAsync(round.Id);

        Assert.Null(await db.ReviewRounds.FindAsync(round.Id));
    }

    [Fact]
    public async Task DeleteRound_WithCouncil_Throws()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var round = MakeRound();
        db.ReviewRounds.Add(round);
        db.ReviewCouncils.Add(new ReviewCouncil
        {
            Id = Guid.NewGuid(),
            RoundId = round.Id,
            CouncilType = "REVIEW",
            CreatedBy = Guid.NewGuid()
        });
        await db.SaveChangesAsync();

        var service = MakeService(db);
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => service.DeleteRoundAsync(round.Id));
        Assert.Contains("hội đồng", ex.Message);
    }

    [Fact]
    public async Task DeleteRound_WithFinalizedProjectRound_Throws()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var round = MakeRound();
        db.ReviewRounds.Add(round);
        db.ProjectRounds.Add(new ProjectRound { ProjectId = Guid.NewGuid(), RoundId = round.Id, Status = "PASSED", FinalizedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var service = MakeService(db);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.DeleteRoundAsync(round.Id));
    }

    // ── Tạo hội đồng trọn gói ────────────────────────────────────────────────

    [Fact]
    public async Task CreateCouncilPackage_Valid_CreatesCouncilWithAssignmentsAndMembers()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var pi = MakeUser();
        var (project, proposal) = MakeProjectWithProposal(pi.Id);
        var chair = MakeUser();
        var secretary = MakeUser();
        db.Users.AddRange(pi, chair, secretary);
        db.Projects.Add(project);
        db.Proposals.Add(proposal);

        var round = MakeRound();
        db.ReviewRounds.Add(round);
        db.ProjectRounds.Add(new ProjectRound { ProjectId = project.Id, RoundId = round.Id, Status = "PENDING" });
        await db.SaveChangesAsync();

        var service = MakeService(db);
        var result = await service.CreateCouncilPackageAsync(round.Id, new CreateCouncilPackageRequest
        {
            ProjectIds = new List<Guid> { project.Id },
            Members = new List<CouncilPackageMemberRequest>
            {
                new() { UserId = chair.Id, MemberRole = "Chair" },
                new() { UserId = secretary.Id, MemberRole = "Secretary" }
            }
        }, Guid.NewGuid());

        Assert.Equal(2, result.Members.Count);
        Assert.Equal(project.Id, Assert.Single(result.ProjectIds));
        Assert.Equal(1, await db.CouncilProjectAssignments.CountAsync(a => a.CouncilId == result.Id));
    }

    [Fact]
    public async Task CreateCouncilPackage_MissingSecretary_Throws()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var pi = MakeUser();
        var (project, proposal) = MakeProjectWithProposal(pi.Id);
        var chair = MakeUser();
        db.Users.AddRange(pi, chair);
        db.Projects.Add(project);
        db.Proposals.Add(proposal);

        var round = MakeRound();
        db.ReviewRounds.Add(round);
        db.ProjectRounds.Add(new ProjectRound { ProjectId = project.Id, RoundId = round.Id, Status = "PENDING" });
        await db.SaveChangesAsync();

        var service = MakeService(db);
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.CreateCouncilPackageAsync(round.Id, new CreateCouncilPackageRequest
        {
            ProjectIds = new List<Guid> { project.Id },
            Members = new List<CouncilPackageMemberRequest> { new() { UserId = chair.Id, MemberRole = "Chair" } }
        }, Guid.NewGuid()));
        Assert.Contains("Thư ký", ex.Message);
        Assert.Equal(0, await db.ReviewCouncils.CountAsync());
    }

    [Fact]
    public async Task CreateCouncilPackage_SamePersonMultipleRoles_Throws()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var pi = MakeUser();
        var (project, proposal) = MakeProjectWithProposal(pi.Id);
        var one = MakeUser();
        db.Users.AddRange(pi, one);
        db.Projects.Add(project);
        db.Proposals.Add(proposal);

        var round = MakeRound();
        db.ReviewRounds.Add(round);
        db.ProjectRounds.Add(new ProjectRound { ProjectId = project.Id, RoundId = round.Id, Status = "PENDING" });
        await db.SaveChangesAsync();

        var service = MakeService(db);
        // 1 người vừa Chủ tịch vừa Thư ký → phải bị chặn (mỗi người chỉ 1 vị trí/hội đồng)
        var ex = await Assert.ThrowsAsync<ArgumentException>(() => service.CreateCouncilPackageAsync(round.Id, new CreateCouncilPackageRequest
        {
            ProjectIds = new List<Guid> { project.Id },
            Members = new List<CouncilPackageMemberRequest>
            {
                new() { UserId = one.Id, MemberRole = "Chair" },
                new() { UserId = one.Id, MemberRole = "Secretary" }
            }
        }, Guid.NewGuid()));
        Assert.Contains("1 vị trí", ex.Message);
        Assert.Equal(0, await db.ReviewCouncils.CountAsync());
    }

    [Fact]
    public async Task CreateCouncilPackage_PiConflictOfInterest_Throws_And_CreatesNothing()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var pi = MakeUser();
        var (project, proposal) = MakeProjectWithProposal(pi.Id);
        var chair = MakeUser();
        var secretary = MakeUser();
        db.Users.AddRange(pi, chair, secretary);
        db.Projects.Add(project);
        db.Proposals.Add(proposal);

        var round = MakeRound();
        db.ReviewRounds.Add(round);
        db.ProjectRounds.Add(new ProjectRound { ProjectId = project.Id, RoundId = round.Id, Status = "PENDING" });
        await db.SaveChangesAsync();

        // Hội đồng đủ Chair+Secretary nhưng thêm PI làm ủy viên → COI phải chặn, KHÔNG tạo nửa vời.
        var service = MakeService(db);
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateCouncilPackageAsync(round.Id, new CreateCouncilPackageRequest
        {
            ProjectIds = new List<Guid> { project.Id },
            Members = new List<CouncilPackageMemberRequest>
            {
                new() { UserId = chair.Id, MemberRole = "Chair" },
                new() { UserId = secretary.Id, MemberRole = "Secretary" },
                new() { UserId = pi.Id, MemberRole = "Member" }
            }
        }, Guid.NewGuid()));

        Assert.Equal(0, await db.ReviewCouncils.CountAsync());
    }

    [Fact]
    public async Task CreateCouncilPackage_ProjectNotJoinedRound_Throws()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var pi = MakeUser();
        var (project, proposal) = MakeProjectWithProposal(pi.Id);
        var chair = MakeUser();
        var secretary = MakeUser();
        db.Users.AddRange(pi, chair, secretary);
        db.Projects.Add(project);
        db.Proposals.Add(proposal);

        var round = MakeRound();
        db.ReviewRounds.Add(round); // KHÔNG add ProjectRound cho project này
        await db.SaveChangesAsync();

        var service = MakeService(db);
        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateCouncilPackageAsync(round.Id, new CreateCouncilPackageRequest
        {
            ProjectIds = new List<Guid> { project.Id },
            Members = new List<CouncilPackageMemberRequest>
            {
                new() { UserId = chair.Id, MemberRole = "Chair" },
                new() { UserId = secretary.Id, MemberRole = "Secretary" }
            }
        }, Guid.NewGuid()));
    }

    // ── Thêm / bỏ đề tài khỏi vòng ───────────────────────────────────────────

    [Fact]
    public async Task AddProjectToRound_DifferentTrack_Throws()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var pi = MakeUser();
        var (project, proposal) = MakeProjectWithProposal(pi.Id, cycleTrackId: 2);
        db.Users.Add(pi);
        db.Projects.Add(project);
        db.Proposals.Add(proposal);

        var round = MakeRound(cycleTrackId: 1);
        db.ReviewRounds.Add(round);
        await db.SaveChangesAsync();

        var service = MakeService(db);
        await Assert.ThrowsAsync<ArgumentException>(() => service.AddProjectToRoundAsync(round.Id, project.Id));
    }

    [Fact]
    public async Task RemoveProjectFromRound_Pending_Succeeds()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var round = MakeRound();
        var projectId = Guid.NewGuid();
        db.ReviewRounds.Add(round);
        db.ProjectRounds.Add(new ProjectRound { ProjectId = projectId, RoundId = round.Id, Status = "PENDING" });
        await db.SaveChangesAsync();

        var service = MakeService(db);
        await service.RemoveProjectFromRoundAsync(round.Id, projectId);

        Assert.False(await db.ProjectRounds.AnyAsync(pr => pr.RoundId == round.Id && pr.ProjectId == projectId));
    }

    [Fact]
    public async Task RemoveProjectFromRound_AlreadyFinalized_Throws()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var round = MakeRound();
        var projectId = Guid.NewGuid();
        db.ReviewRounds.Add(round);
        db.ProjectRounds.Add(new ProjectRound { ProjectId = projectId, RoundId = round.Id, Status = "PASSED", FinalizedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var service = MakeService(db);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.RemoveProjectFromRoundAsync(round.Id, projectId));
    }
}
