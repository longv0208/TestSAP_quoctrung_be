using FURPMS.Application.DTOs.Meetings;
using FURPMS.Domain.Entities.Review;
using FURPMS.Domain.Entities.Users;
using FURPMS.Infrastructure.Repositories;
using FURPMS.Infrastructure.Services;
using FURPMS.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace FURPMS.Tests.Councils;

public class CouncilMeetingConflictTests
{
    private static User NewUser(string name) => new()
    {
        Id = Guid.NewGuid(), Email = $"{Guid.NewGuid():N}@test.com", FullName = name, Status = "ACTIVE"
    };

    private static ReviewCouncil NewCouncil(Guid createdBy) => new()
    {
        Id = Guid.NewGuid(), CouncilType = "REVIEW", Status = "FORMING", CreatedBy = createdBy
    };

    private static ScheduleMeetingRequest Request(DateTime at, int minutes = 60) => new()
    {
        Title = "Lịch mới", Platform = "ONLINE", MeetingLink = "https://meet.test/room",
        ScheduledAt = at, DurationMinutes = minutes
    };

    [Fact]
    public async Task Schedule_SameCouncilOverlaps_Blocked()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var creator = NewUser("Người tạo");
        var council = NewCouncil(creator.Id);
        var at = DateTime.UtcNow.AddDays(2);
        db.Users.Add(creator);
        db.ReviewCouncils.Add(council);
        db.CouncilMeetings.Add(new CouncilMeeting
        {
            Id = Guid.NewGuid(), CouncilId = council.Id, Title = "Lịch đã có", Platform = "ONLINE",
            ScheduledAt = at, DurationMinutes = 60, Status = "SCHEDULED"
        });
        await db.SaveChangesAsync();

        var service = new CouncilMeetingService(new ReviewRepository(db));
        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ScheduleAsync(council.Id, Request(at.AddMinutes(30))));

        Assert.Contains("đã có lịch", error.Message);
        Assert.Equal(1, await db.CouncilMeetings.CountAsync());
    }

    [Fact]
    public async Task Schedule_SharedMemberHasOtherCouncilMeeting_Blocked()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var creator = NewUser("Người tạo");
        var reviewer = NewUser("PGS.TS. Trùng Lịch");
        var first = NewCouncil(creator.Id);
        var second = NewCouncil(creator.Id);
        var at = DateTime.UtcNow.AddDays(2);
        db.Users.AddRange(creator, reviewer);
        db.ReviewCouncils.AddRange(first, second);
        db.CouncilMembers.AddRange(
            new CouncilMember { Id = Guid.NewGuid(), CouncilId = first.Id, UserId = reviewer.Id, MemberRole = "Member", Status = "CONFIRMED" },
            new CouncilMember { Id = Guid.NewGuid(), CouncilId = second.Id, UserId = reviewer.Id, MemberRole = "Member", Status = "CONFIRMED" });
        db.CouncilMeetings.Add(new CouncilMeeting
        {
            Id = Guid.NewGuid(), CouncilId = first.Id, Title = "Hội đồng thứ nhất", Platform = "ONLINE",
            ScheduledAt = at, DurationMinutes = 90, Status = "SCHEDULED"
        });
        await db.SaveChangesAsync();

        var service = new CouncilMeetingService(new ReviewRepository(db));
        var error = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.ScheduleAsync(second.Id, Request(at.AddMinutes(15))));

        Assert.Contains(reviewer.FullName, error.Message);
        Assert.Equal(1, await db.CouncilMeetings.CountAsync());
    }

    [Fact]
    public async Task Schedule_StartsWhenPreviousEnds_Allowed()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var creator = NewUser("Người tạo");
        var council = NewCouncil(creator.Id);
        var at = DateTime.UtcNow.AddDays(2);
        db.Users.Add(creator);
        db.ReviewCouncils.Add(council);
        db.CouncilMeetings.Add(new CouncilMeeting
        {
            Id = Guid.NewGuid(), CouncilId = council.Id, Title = "Lịch trước", Platform = "ONLINE",
            ScheduledAt = at, DurationMinutes = 60, Status = "SCHEDULED"
        });
        await db.SaveChangesAsync();

        var service = new CouncilMeetingService(new ReviewRepository(db));
        var created = await service.ScheduleAsync(council.Id, Request(at.AddMinutes(60)));

        Assert.NotEqual(Guid.Empty, created.Id);
        Assert.Equal(2, await db.CouncilMeetings.CountAsync());
    }
}
