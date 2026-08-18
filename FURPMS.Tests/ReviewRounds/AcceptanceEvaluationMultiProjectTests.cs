using FURPMS.Application.DTOs.ReviewScoring;
using FURPMS.Domain.Entities.Projects;
using FURPMS.Domain.Entities.Review;
using FURPMS.Domain.Entities.Users;
using FURPMS.Infrastructure.Repositories;
using FURPMS.Infrastructure.Services;
using FURPMS.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace FURPMS.Tests.ReviewRounds;

public class AcceptanceEvaluationMultiProjectTests
{
    [Fact]
    public async Task SameReviewer_CanSubmitSeparateBallotForEveryAssignedProject()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var reviewer = new User
        {
            Id = Guid.NewGuid(), Email = "reviewer@test.com", FullName = "Người phản biện", Status = "ACTIVE"
        };
        var council = new ReviewCouncil
        {
            Id = Guid.NewGuid(), CouncilType = "ACCEPTANCE", Status = "FORMING", CreatedBy = reviewer.Id
        };
        var member = new CouncilMember
        {
            Id = Guid.NewGuid(), CouncilId = council.Id, UserId = reviewer.Id,
            MemberRole = "Opponent", Status = "CONFIRMED"
        };
        var first = NewProject(reviewer.Id, "Đề tài 1");
        var second = NewProject(reviewer.Id, "Đề tài 2");
        db.Users.Add(reviewer);
        db.ReviewCouncils.Add(council);
        db.CouncilMembers.Add(member);
        db.Projects.AddRange(first, second);
        db.CouncilProjectAssignments.AddRange(
            new CouncilProjectAssignment { CouncilId = council.Id, ProjectId = first.Id },
            new CouncilProjectAssignment { CouncilId = council.Id, ProjectId = second.Id });
        await db.SaveChangesAsync();

        var service = new AcceptanceEvaluationService(new ReviewRepository(db));
        await service.SubmitAsync(council.Id, reviewer.Id, new SubmitAcceptanceRequest
        {
            ProjectId = first.Id, Result = "PASS"
        });
        await service.SubmitAsync(council.Id, reviewer.Id, new SubmitAcceptanceRequest
        {
            ProjectId = second.Id, Result = "FAIL", FailReason = "Chưa đạt sản phẩm"
        });

        var ballots = await db.AcceptanceEvaluations.OrderBy(x => x.ProjectId).ToListAsync();
        Assert.Equal(2, ballots.Count);
        Assert.Contains(ballots, x => x.ProjectId == first.Id && x.Result == "PASS");
        Assert.Contains(ballots, x => x.ProjectId == second.Id && x.Result == "FAIL");

        var mySecond = await service.GetMyAsync(council.Id, second.Id, reviewer.Id);
        Assert.NotNull(mySecond);
        Assert.Equal(second.Id, mySecond!.ProjectId);
        Assert.Equal("FAIL", mySecond.Result);
    }

    [Fact]
    public async Task Submit_ProjectOutsideCouncil_Blocked()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var reviewer = new User
        {
            Id = Guid.NewGuid(), Email = "reviewer@test.com", FullName = "Người phản biện", Status = "ACTIVE"
        };
        var council = new ReviewCouncil
        {
            Id = Guid.NewGuid(), CouncilType = "ACCEPTANCE", Status = "FORMING", CreatedBy = reviewer.Id
        };
        var outside = NewProject(reviewer.Id, "Đề tài ngoài hội đồng");
        db.Users.Add(reviewer);
        db.ReviewCouncils.Add(council);
        db.CouncilMembers.Add(new CouncilMember
        {
            Id = Guid.NewGuid(), CouncilId = council.Id, UserId = reviewer.Id,
            MemberRole = "Member", Status = "CONFIRMED"
        });
        db.Projects.Add(outside);
        await db.SaveChangesAsync();

        var service = new AcceptanceEvaluationService(new ReviewRepository(db));
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SubmitAsync(
            council.Id, reviewer.Id,
            new SubmitAcceptanceRequest { ProjectId = outside.Id, Result = "PASS" }));
        Assert.Empty(await db.AcceptanceEvaluations.ToListAsync());
    }

    private static Project NewProject(Guid piId, string title) => new()
    {
        Id = Guid.NewGuid(), CycleTrackId = 1, OrderId = 1, PiUserId = piId, HostingUnitId = 1,
        ResearchTypeId = 1, TitleVi = title, Status = "ACCEPTANCE",
        PlannedStartDate = DateOnly.FromDateTime(DateTime.UtcNow),
        PlannedEndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(1))
    };
}
