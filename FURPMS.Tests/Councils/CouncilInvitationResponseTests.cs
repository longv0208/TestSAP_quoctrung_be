using FURPMS.Application.Common;
using FURPMS.Application.Constants;
using FURPMS.Domain.Entities.Review;
using FURPMS.Domain.Entities.Users;
using FURPMS.Infrastructure.Data;
using FURPMS.Infrastructure.Services;
using FURPMS.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace FURPMS.Tests.Councils;

public class CouncilInvitationResponseTests
{
    private static async Task<(FURPMSDbContext db, ReviewCouncil council, CouncilMember member, User reviewer)> SeedAsync(
        string status = CouncilMemberStatus.Invited, DateTime? expiresAt = null)
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var reviewer = new User
        {
            Id = Guid.NewGuid(), Email = $"reviewer-{Guid.NewGuid():N}@test.com", FullName = "Reviewer",
            Status = UserStatus.Active
        };
        var council = new ReviewCouncil
        {
            Id = Guid.NewGuid(), CouncilType = "REVIEW", Status = CouncilStatus.Forming, CreatedBy = Guid.NewGuid()
        };
        var member = new CouncilMember
        {
            Id = Guid.NewGuid(), CouncilId = council.Id, UserId = reviewer.Id,
            MemberRole = CouncilMemberRole.Member, Status = status,
            InvitationSentAt = DateTime.UtcNow.AddMinutes(-1),
            TokenExpiresAt = expiresAt ?? DateTime.UtcNow.AddMinutes(10)
        };
        db.Users.Add(reviewer);
        db.ReviewCouncils.Add(council);
        db.CouncilMembers.Add(member);
        await db.SaveChangesAsync();
        return (db, council, member, reviewer);
    }

    [Fact]
    public async Task Respond_Accept_ConfirmsMember()
    {
        var (db, _, member, reviewer) = await SeedAsync();

        var result = await TestServices.Councils(db).RespondToMembershipAsync(member.Id, reviewer.Id, true, null);

        Assert.Equal(CouncilMemberStatus.Confirmed, result.Status);
        var saved = await db.CouncilMembers.FindAsync(member.Id);
        Assert.NotNull(saved!.ConfirmedAt);
        Assert.Null(saved.DeclinedAt);
    }

    [Fact]
    public async Task Respond_Decline_StoresReasonAndStatus()
    {
        var (db, _, member, reviewer) = await SeedAsync();

        var result = await TestServices.Councils(db).RespondToMembershipAsync(member.Id, reviewer.Id, false, "Trùng lịch công tác");

        Assert.Equal(CouncilMemberStatus.Declined, result.Status);
        var saved = await db.CouncilMembers.FindAsync(member.Id);
        Assert.Equal("Trùng lịch công tác", saved!.DeclineReason);
        Assert.NotNull(saved.DeclinedAt);
    }

    [Fact]
    public async Task Respond_ByDifferentUser_IsForbidden()
    {
        var (db, _, member, _) = await SeedAsync();

        await Assert.ThrowsAsync<ForbiddenException>(() => TestServices.Councils(db)
            .RespondToMembershipAsync(member.Id, Guid.NewGuid(), true, null));
    }

    [Fact]
    public async Task Respond_AfterDecline_IsBlocked()
    {
        var (db, _, member, reviewer) = await SeedAsync(CouncilMemberStatus.Declined);

        await Assert.ThrowsAsync<InvalidOperationException>(() => TestServices.Councils(db)
            .RespondToMembershipAsync(member.Id, reviewer.Id, true, null));
    }

    [Fact]
    public async Task Respond_AcceptAfterExpiry_MarksInvitationExpired()
    {
        var (db, _, member, reviewer) = await SeedAsync(
            CouncilMemberStatus.Invited, DateTime.UtcNow.AddMinutes(-1));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => TestServices.Councils(db)
            .RespondToMembershipAsync(member.Id, reviewer.Id, true, null));

        Assert.Contains("quá hạn", ex.Message);
        Assert.Equal(CouncilMemberStatus.Expired, (await db.CouncilMembers.FindAsync(member.Id))!.Status);
    }

    [Fact]
    public async Task Respond_UnknownMember_IsNotFound()
    {
        var (db, _, _, reviewer) = await SeedAsync();

        await Assert.ThrowsAsync<KeyNotFoundException>(() => TestServices.Councils(db)
            .RespondToMembershipAsync(Guid.NewGuid(), reviewer.Id, true, null));
    }
}
