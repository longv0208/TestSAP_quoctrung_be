using FURPMS.Tests.Reminders;
using FURPMS.Application.Common;
using FURPMS.Application.Constants;
using FURPMS.Application.DTOs.ReviewScoring;
using FURPMS.Domain.Entities.Proposals;
using FURPMS.Domain.Entities.Review;
using FURPMS.Domain.Entities.Users;
using FURPMS.Infrastructure.Data;
using FURPMS.Infrastructure.Repositories;
using FURPMS.Infrastructure.Services;
using FURPMS.Tests.Helpers;

namespace FURPMS.Tests.Review;

// Phase 1: biên bản hội đồng — Thư ký soạn nháp → Chủ tịch duyệt = khóa + cập nhật status đề tài.
// Hệ thống KHÔNG tự đếm phiếu; kết quả do Chủ tịch chốt khi duyệt.
public class CouncilMinutesTests
{
    private static User MakeUser() => new()
    {
        Id = Guid.NewGuid(),
        Email = $"u-{Guid.NewGuid():N}"[..18] + "@t.com",
        FullName = "U",
        Status = UserStatus.Active
    };

    private static ReviewScoringService MakeService(FURPMSDbContext db) =>
        new(new ReviewRepository(db), new MasterDataRepository(db), new ProposalRepository(db), new FakeClock());

    private static async Task<(ReviewCouncil council, Guid chairId, Guid secId, Guid memberId, Proposal proposal)>
        SeedAsync(FURPMSDbContext db)
    {
        var pi = MakeUser();
        var chair = MakeUser();
        var sec = MakeUser();
        var member = MakeUser();
        db.Users.AddRange(pi, chair, sec, member);

        var project = new FURPMS.Domain.Entities.Projects.Project
        {
            Id = Guid.NewGuid(),
            CycleTrackId = 1,
            OrderId = 1,
            PiUserId = pi.Id,
            HostingUnitId = 1,
            ResearchTypeId = 1,
            TitleVi = "P",
            Status = ProjectStatus.UnderReview,
            PlannedStartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            PlannedEndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(12))
        };
        db.Projects.Add(project);
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
            Status = ProposalStatus.Submitted
        };
        db.Proposals.Add(proposal);

        var council = new ReviewCouncil
        {
            Id = Guid.NewGuid(),
            CouncilType = "SCIENCE",
            Status = CouncilStatus.Forming,
            CreatedBy = chair.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        db.ReviewCouncils.Add(council);
        db.CouncilProjectAssignments.Add(new CouncilProjectAssignment { CouncilId = council.Id, ProjectId = project.Id });

        db.Set<CouncilMember>().AddRange(
            new CouncilMember { Id = Guid.NewGuid(), CouncilId = council.Id, UserId = chair.Id, MemberRole = CouncilMemberRole.Chair, Status = CouncilMemberStatus.Confirmed },
            new CouncilMember { Id = Guid.NewGuid(), CouncilId = council.Id, UserId = sec.Id, MemberRole = CouncilMemberRole.Secretary, Status = CouncilMemberStatus.Confirmed },
            new CouncilMember { Id = Guid.NewGuid(), CouncilId = council.Id, UserId = member.Id, MemberRole = CouncilMemberRole.Member, Status = CouncilMemberStatus.Confirmed });

        await db.SaveChangesAsync();
        return (council, chair.Id, sec.Id, member.Id, proposal);
    }

    // ── 1: Thư ký soạn nháp → proposal CHƯA đổi, council vẫn FORMING, chưa khóa ──
    [Fact]
    public async Task SaveMinutes_BySecretary_DraftsOnly_NoStatusChange()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var (council, _, secId, _, proposal) = await SeedAsync(db);
        var svc = MakeService(db);

        var dto = await svc.SaveMinutesAsync(council.Id, secId, new SaveMinutesRequest
        {
            Result = ReviewResult.Approved,
            CouncilComments = "Biên bản nháp"
        });

        Assert.Null(dto.FinalizedAt);
        var afterProposal = await db.Proposals.FindAsync(proposal.Id);
        Assert.Equal(ProposalStatus.Submitted, afterProposal!.Status);  // CHƯA đổi
        var afterCouncil = await db.ReviewCouncils.FindAsync(council.Id);
        Assert.Equal(CouncilStatus.Forming, afterCouncil!.Status);
    }

    // ── 2: thành viên thường soạn biên bản → 401 ───────────────────────────────
    [Fact]
    public async Task SaveMinutes_ByNonSecretary_Throws_403()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var (council, _, _, memberId, _) = await SeedAsync(db);
        var svc = MakeService(db);

        await Assert.ThrowsAsync<ForbiddenException>(
            () => svc.SaveMinutesAsync(council.Id, memberId, new SaveMinutesRequest { Result = ReviewResult.Approved }));
    }

    // ── 3: Chủ tịch duyệt → khóa + proposal = APPROVED + council = DECIDED ──────
    [Fact]
    public async Task ApproveMinutes_ByChair_Locks_And_UpdatesProposal()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var (council, chairId, secId, _, proposal) = await SeedAsync(db);
        var svc = MakeService(db);

        await svc.SaveMinutesAsync(council.Id, secId, new SaveMinutesRequest { Result = ReviewResult.Approved });
        var dto = await svc.ApproveMinutesAsync(council.Id, chairId);

        Assert.NotNull(dto.FinalizedAt);
        var afterProposal = await db.Proposals.FindAsync(proposal.Id);
        Assert.Equal(ProposalStatus.Approved, afterProposal!.Status);
        var afterCouncil = await db.ReviewCouncils.FindAsync(council.Id);
        Assert.Equal(CouncilStatus.Decided, afterCouncil!.Status);
    }

    // ── 4: thành viên thường duyệt biên bản → 401 ─────────────────────────────
    [Fact]
    public async Task ApproveMinutes_ByNonChair_Throws_403()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var (council, _, secId, memberId, _) = await SeedAsync(db);
        var svc = MakeService(db);

        await svc.SaveMinutesAsync(council.Id, secId, new SaveMinutesRequest { Result = ReviewResult.Approved });
        await Assert.ThrowsAsync<ForbiddenException>(
            () => svc.ApproveMinutesAsync(council.Id, memberId));
    }

    // ── 5: duyệt khi chưa có biên bản nháp → 409 ──────────────────────────────
    [Fact]
    public async Task ApproveMinutes_NoDraft_Throws_409()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var (council, chairId, _, _, _) = await SeedAsync(db);
        var svc = MakeService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => svc.ApproveMinutesAsync(council.Id, chairId));
    }

    // ── 6: có tên trong hội đồng nhưng CHƯA xác nhận lời mời → không được hành động ──
    // Người đã TỪ CHỐI mà vẫn soạn/duyệt được biên bản thì hội đồng mất giá trị pháp lý.
    [Theory]
    [InlineData(CouncilMemberStatus.Declined)]
    [InlineData(CouncilMemberStatus.Invited)]
    [InlineData(CouncilMemberStatus.Assigned)]
    [InlineData(CouncilMemberStatus.Expired)]
    public async Task SaveMinutes_BySecretaryNotConfirmed_Throws_403(string status)
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var (council, _, secId, _, _) = await SeedAsync(db);

        var sec = db.Set<CouncilMember>().First(m => m.CouncilId == council.Id && m.UserId == secId);
        sec.Status = status;
        await db.SaveChangesAsync();

        var svc = MakeService(db);
        await Assert.ThrowsAsync<ForbiddenException>(
            () => svc.SaveMinutesAsync(council.Id, secId, new SaveMinutesRequest { Result = ReviewResult.Approved }));
    }

    [Fact]
    public async Task ApproveMinutes_ByChairNotConfirmed_Throws_403()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var (council, chairId, secId, _, _) = await SeedAsync(db);
        var svc = MakeService(db);
        await svc.SaveMinutesAsync(council.Id, secId, new SaveMinutesRequest { Result = ReviewResult.Approved });

        var chair = db.Set<CouncilMember>().First(m => m.CouncilId == council.Id && m.UserId == chairId);
        chair.Status = CouncilMemberStatus.Declined;
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<ForbiddenException>(() => svc.ApproveMinutesAsync(council.Id, chairId));
    }
}
