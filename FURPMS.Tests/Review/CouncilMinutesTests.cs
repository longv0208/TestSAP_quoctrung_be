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
using Microsoft.EntityFrameworkCore;

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
        new(new ReviewRepository(db), new MasterDataRepository(db), new ProposalRepository(db), new FakeClock(),
            new SystemSettingService(new MasterDataRepository(db)));

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

        var chairMember = new CouncilMember { Id = Guid.NewGuid(), CouncilId = council.Id, UserId = chair.Id, MemberRole = CouncilMemberRole.Chair, Status = CouncilMemberStatus.Confirmed };
        var secMember = new CouncilMember { Id = Guid.NewGuid(), CouncilId = council.Id, UserId = sec.Id, MemberRole = CouncilMemberRole.Secretary, Status = CouncilMemberStatus.Confirmed };
        var plainMember = new CouncilMember { Id = Guid.NewGuid(), CouncilId = council.Id, UserId = member.Id, MemberRole = CouncilMemberRole.Member, Status = CouncilMemberStatus.Confirmed };
        db.Set<CouncilMember>().AddRange(chairMember, secMember, plainMember);

        // QĐ543 Điều 8.3.b: họp phải có ít nhất 2/3 thành viên dự và người dự phải chấm.
        // Hội đồng 3 người ⇒ cần ≥ 2 phiếu, không thì không lưu/chốt được biên bản.
        db.ProposalReviewScores.AddRange(
            new ProposalReviewScore { CouncilId = council.Id, ProjectId = project.Id, EvaluatorMemberId = chairMember.Id, TemplateId = 1, SubmittedAt = DateTime.UtcNow, IsValidBallot = true },
            new ProposalReviewScore { CouncilId = council.Id, ProjectId = project.Id, EvaluatorMemberId = plainMember.Id, TemplateId = 1, SubmittedAt = DateTime.UtcNow, IsValidBallot = true });

        await db.SaveChangesAsync();
        return (council, chair.Id, sec.Id, member.Id, proposal);
    }

    // ── QĐ543 Điều 8.3.b: chưa đủ 2/3 thành viên chấm thì KHÔNG lưu/chốt được biên bản ──
    [Fact]
    public async Task SaveMinutes_NotEnoughBallots_Throws()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var (council, _, secId, _, _) = await SeedAsync(db);

        // Hội đồng 3 người ⇒ cần ≥2 phiếu. Bỏ bớt 1 để còn 1 phiếu.
        var one = await db.ProposalReviewScores.FirstAsync(s => s.CouncilId == council.Id);
        db.ProposalReviewScores.Remove(one);
        await db.SaveChangesAsync();

        var svc = MakeService(db);
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.SaveMinutesAsync(council.Id, secId, new SaveMinutesRequest
            {
                Result = ReviewResult.Approved,
                CouncilComments = "OK"
            }));
        Assert.Contains("1/3", ex.Message);
    }

    // Chủ tịch cũng không chốt được biên bản khi thiếu phiếu — chặn ở cả hai đầu.
    [Fact]
    public async Task ApproveMinutes_NotEnoughBallots_Throws()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var (council, chairId, secId, _, _) = await SeedAsync(db);
        var svc = MakeService(db);

        // Lưu nháp khi còn đủ phiếu…
        await svc.SaveMinutesAsync(council.Id, secId, new SaveMinutesRequest
        {
            Result = ReviewResult.Approved,
            CouncilComments = "OK"
        });

        // …rồi một phiếu bị gỡ (vd Staff thay người) — chốt phải bị chặn.
        var one = await db.ProposalReviewScores.FirstAsync(s => s.CouncilId == council.Id);
        db.ProposalReviewScores.Remove(one);
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.ApproveMinutesAsync(council.Id, chairId));

        var decision = await db.CouncilDecisions.FirstAsync(d => d.CouncilId == council.Id);
        Assert.Null(decision.FinalizedAt);   // chặn mà vẫn khoá thì coi như không chặn
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

    /// <summary>
    /// Vòng NGHIỆM THU không chấm điểm (BM11 chỉ Đạt/Không đạt) nên phiếu nằm ở
    /// <c>acceptance_evaluations</c>, không phải <c>review_scores</c>. Trước đây quorum chỉ đếm bảng
    /// điểm ⇒ hội đồng nghiệm thu dù đủ phiếu vẫn bị báo "mới có 0/3 phiếu" và không ai chốt được
    /// biên bản nghiệm thu.
    /// </summary>
    private static async Task<Guid> MakeAcceptanceRoundAsync(FURPMSDbContext db, ReviewCouncil council)
    {
        var round = new ReviewRound
        {
            Id = Guid.NewGuid(),
            CycleTrackId = 1,
            RoundNumber = 1,
            Dimension = ReviewRoundDimension.Science,
            RoundType = "ACCEPTANCE",
            Sequence = 1,
            Status = ReviewRoundStatus.Open
        };
        db.ReviewRounds.Add(round);
        council.RoundId = round.Id;
        await db.SaveChangesAsync();
        return round.Id;
    }

    [Fact]
    public async Task SaveMinutes_AcceptanceCouncil_CountsPassFailBallotsTowardQuorum()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var (council, _, secId, _, proposal) = await SeedAsync(db);
        await MakeAcceptanceRoundAsync(db, council);

        // Nghiệm thu không có phiếu điểm — thay bằng phiếu Đạt/Không đạt của cùng 2 thành viên.
        var projectId = db.Proposals.First(p => p.Id == proposal.Id).ProjectId;
        db.ProposalReviewScores.RemoveRange(db.ProposalReviewScores.Where(s => s.CouncilId == council.Id));
        var members = db.Set<CouncilMember>().Where(m => m.CouncilId == council.Id).ToList();
        var opponent = members.First(m => m.MemberRole == CouncilMemberRole.Member);
        opponent.MemberRole = CouncilMemberRole.Opponent;   // Điều 12.3.b: phản biện phải dự
        db.AcceptanceEvaluations.AddRange(
            new AcceptanceEvaluation { CouncilId = council.Id, ProjectId = projectId, EvaluatorMemberId = opponent.Id, Result = EvaluationResult.Pass, IsValidBallot = true, SubmittedAt = DateTime.UtcNow },
            new AcceptanceEvaluation { CouncilId = council.Id, ProjectId = projectId, EvaluatorMemberId = members.First(m => m.MemberRole == CouncilMemberRole.Chair).Id, Result = EvaluationResult.Pass, IsValidBallot = true, SubmittedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var svc = MakeService(db);
        var decision = await svc.SaveMinutesAsync(council.Id, secId, new SaveMinutesRequest
        {
            Result = ReviewResult.Approved,
            CouncilComments = "Nghiệm thu Đạt"
        });

        // BM12 mục 10.1: phiếu Đạt/Không đạt phải vào ô "thu về / hợp lệ"; nghiệm thu không có điểm TB.
        Assert.Equal(2, decision.ValidBallots);
        Assert.Equal(0, decision.InvalidBallots);
        Assert.Null(decision.AverageScore);
    }

    [Fact]
    public async Task SaveMinutes_AcceptanceCouncil_WithoutOpponentBallot_Throws()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var (council, _, secId, _, proposal) = await SeedAsync(db);
        await MakeAcceptanceRoundAsync(db, council);

        var projectId = db.Proposals.First(p => p.Id == proposal.Id).ProjectId;
        db.ProposalReviewScores.RemoveRange(db.ProposalReviewScores.Where(s => s.CouncilId == council.Id));
        var members = db.Set<CouncilMember>().Where(m => m.CouncilId == council.Id).ToList();
        members.First(m => m.MemberRole == CouncilMemberRole.Member).MemberRole = CouncilMemberRole.Opponent;

        // Đủ 2/3 phiếu nhưng KHÔNG có phiếu của phản biện ⇒ vẫn phải chặn (Điều 12.3.b).
        foreach (var m in members.Where(m => m.MemberRole != CouncilMemberRole.Opponent))
        {
            db.AcceptanceEvaluations.Add(new AcceptanceEvaluation
            {
                CouncilId = council.Id, ProjectId = projectId, EvaluatorMemberId = m.Id,
                Result = EvaluationResult.Pass, IsValidBallot = true, SubmittedAt = DateTime.UtcNow
            });
        }
        await db.SaveChangesAsync();

        var svc = MakeService(db);
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            svc.SaveMinutesAsync(council.Id, secId, new SaveMinutesRequest { Result = ReviewResult.Approved }));
        Assert.Contains("phản biện", ex.Message);
    }
}
