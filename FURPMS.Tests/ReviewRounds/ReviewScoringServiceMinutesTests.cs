using FURPMS.Tests.Reminders;
using FURPMS.Application.DTOs.ReviewScoring;
using FURPMS.Domain.Entities.Proposals;
using FURPMS.Domain.Entities.Review;
using FURPMS.Infrastructure.Data;
using FURPMS.Domain.Entities.Users;
using FURPMS.Infrastructure.Repositories;
using FURPMS.Infrastructure.Services;
using FURPMS.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace FURPMS.Tests.ReviewRounds;

// Bug đã fix: ApproveMinutesAsync trước đây chỉ cập nhật proposal/project, KHÔNG nối mạch
// project_round → 2 nguồn dữ liệu lệch nhau. Test lại đúng hành vi nối mạch + tự đóng round.
public class ReviewScoringServiceMinutesTests
{
    private static User MakeUser(string role) => new()
    {
        Id = Guid.NewGuid(),
        Email = $"{role}-{Guid.NewGuid()}@test.com",
        FullName = role,
        Status = "ACTIVE"
    };

    private static (FURPMS.Domain.Entities.Projects.Project project, Proposal proposal) MakeProjectWithProposal(Guid piUserId, int cycleTrackId = 1)
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
            Status = "SUBMITTED"
        };
        return (project, proposal);
    }

    private static ReviewScoringService MakeService(FURPMS.Infrastructure.Data.FURPMSDbContext db) =>
        new(new ReviewRepository(db), new MasterDataRepository(db), new ProposalRepository(db), new FakeClock(),
            new SystemSettingService(new MasterDataRepository(db)));

    /// <summary>
    /// Seed phiếu đã nộp cho các thành viên — cần vì QĐ543 Điều 8.3.b bắt buộc ≥2/3 thành viên
    /// dự họp (và người dự phải chấm) thì mới lưu/chốt được biên bản.
    /// </summary>
    private static void SeedBallots(FURPMSDbContext db, Guid councilId, Guid projectId, params CouncilMember[] members)
    {
        foreach (var m in members)
            db.ProposalReviewScores.Add(new ProposalReviewScore
            {
                CouncilId = councilId,
                ProjectId = projectId,
                EvaluatorMemberId = m.Id,
                TemplateId = 1,
                SubmittedAt = DateTime.UtcNow,
                IsValidBallot = true
            });
    }

    [Fact]
    public async Task ApproveMinutes_SingleProjectRound_SyncsProjectRoundAndClosesRound()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var pi = MakeUser("PI");
        var (project, proposal) = MakeProjectWithProposal(pi.Id);
        var chair = MakeUser("Chair");
        var secretary = MakeUser("Secretary");
        db.Users.AddRange(pi, chair, secretary);
        db.Projects.Add(project);
        db.Proposals.Add(proposal);

        var round = new ReviewRound { Id = Guid.NewGuid(), CycleTrackId = 1, RoundNumber = 1, Sequence = 1, Dimension = "SCIENCE", RoundType = "REVIEW", Status = "OPEN" };
        db.ReviewRounds.Add(round);
        db.ProjectRounds.Add(new ProjectRound { ProjectId = project.Id, RoundId = round.Id, Status = "PENDING" });

        var council = new ReviewCouncil { Id = Guid.NewGuid(), RoundId = round.Id, CouncilType = "REVIEW", CreatedBy = Guid.NewGuid() };
        db.ReviewCouncils.Add(council);
        db.CouncilProjectAssignments.Add(new CouncilProjectAssignment { CouncilId = council.Id, ProjectId = project.Id });
        var chairMember = new CouncilMember { Id = Guid.NewGuid(), CouncilId = council.Id, UserId = chair.Id, MemberRole = "Chair", Status = "CONFIRMED" };
        var secMember = new CouncilMember { Id = Guid.NewGuid(), CouncilId = council.Id, UserId = secretary.Id, MemberRole = "Secretary", Status = "CONFIRMED" };
        db.CouncilMembers.AddRange(chairMember, secMember);
        // QĐ543 Điều 8.3.b: phải có ít nhất 2/3 thành viên dự họp và người dự phải chấm.
        SeedBallots(db, council.Id, project.Id, chairMember, secMember);
        await db.SaveChangesAsync();

        var service = MakeService(db);
        await service.SaveMinutesAsync(council.Id, secretary.Id, new SaveMinutesRequest { Result = "APPROVED", CouncilComments = "OK" });
        await service.ApproveMinutesAsync(council.Id, chair.Id);

        var projectRound = await db.ProjectRounds.FirstAsync(pr => pr.RoundId == round.Id && pr.ProjectId == project.Id);
        Assert.Equal("PASSED", projectRound.Status);
        Assert.Equal("APPROVED", projectRound.Result);
        Assert.NotNull(projectRound.FinalizedAt);

        var updatedRound = await db.ReviewRounds.FindAsync(round.Id);
        Assert.Equal("PASSED", updatedRound!.Status);
        Assert.NotNull(updatedRound.ClosedAt);

        var updatedProposal = await db.Proposals.FindAsync(proposal.Id);
        Assert.Equal("APPROVED", updatedProposal!.Status);
    }

    // Vòng NGHIỆM THU: Đạt → đề tài COMPLETED (Process_Spec ACCEPTANCE → COMPLETED).
    // Trước đây mọi vòng đều set APPROVED nên nghiệm thu xong đề tài vẫn "Đã duyệt" → đứt mạch cuối.
    [Theory]
    [InlineData("APPROVED", "COMPLETED")]
    [InlineData("REJECTED", "IN_PROGRESS")]   // chưa đạt → quay lại làm tiếp, KHÔNG hủy đề tài
    public async Task ApproveMinutes_AcceptanceRound_SetsProjectLifecycleStatus(string result, string expectedProjectStatus)
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var pi = MakeUser("PI");
        var (project, proposal) = MakeProjectWithProposal(pi.Id);
        project.Status = "ACCEPTANCE";           // đã nộp báo cáo tổng kết, đang nghiệm thu
        var chair = MakeUser("Chair");
        var secretary = MakeUser("Secretary");
        db.Users.AddRange(pi, chair, secretary);
        db.Projects.Add(project);
        db.Proposals.Add(proposal);

        var round = new ReviewRound { Id = Guid.NewGuid(), CycleTrackId = 1, RoundNumber = 2, Sequence = 2, Dimension = "SCIENCE", RoundType = "ACCEPTANCE", Status = "OPEN" };
        db.ReviewRounds.Add(round);
        db.ProjectRounds.Add(new ProjectRound { ProjectId = project.Id, RoundId = round.Id, Status = "PENDING" });

        var council = new ReviewCouncil { Id = Guid.NewGuid(), RoundId = round.Id, CouncilType = "ACCEPTANCE", CreatedBy = Guid.NewGuid() };
        db.ReviewCouncils.Add(council);
        db.CouncilProjectAssignments.Add(new CouncilProjectAssignment { CouncilId = council.Id, ProjectId = project.Id });
        var chairMember = new CouncilMember { Id = Guid.NewGuid(), CouncilId = council.Id, UserId = chair.Id, MemberRole = "Chair", Status = "CONFIRMED" };
        var secMember = new CouncilMember { Id = Guid.NewGuid(), CouncilId = council.Id, UserId = secretary.Id, MemberRole = "Secretary", Status = "CONFIRMED" };
        db.CouncilMembers.AddRange(chairMember, secMember);
        // QĐ543 Điều 8.3.b: phải có ít nhất 2/3 thành viên dự họp và người dự phải chấm.
        SeedBallots(db, council.Id, project.Id, chairMember, secMember);
        await db.SaveChangesAsync();

        var service = MakeService(db);
        await service.SaveMinutesAsync(council.Id, secretary.Id, new SaveMinutesRequest { Result = result, CouncilComments = "Biên bản nghiệm thu" });
        await service.ApproveMinutesAsync(council.Id, chair.Id);

        var updatedProject = await db.Projects.FindAsync(project.Id);
        Assert.Equal(expectedProjectStatus, updatedProject!.Status);
    }

    [Fact]
    public async Task ApproveMinutes_OtherProjectStillPending_RoundStaysOpen()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var pi = MakeUser("PI");
        var (projectA, proposalA) = MakeProjectWithProposal(pi.Id);
        var (projectB, proposalB) = MakeProjectWithProposal(pi.Id);
        var chair = MakeUser("Chair");
        var secretary = MakeUser("Secretary");
        db.Users.AddRange(pi, chair, secretary);
        db.Projects.AddRange(projectA, projectB);
        db.Proposals.AddRange(proposalA, proposalB);

        var round = new ReviewRound { Id = Guid.NewGuid(), CycleTrackId = 1, RoundNumber = 1, Sequence = 1, Dimension = "SCIENCE", RoundType = "REVIEW", Status = "OPEN" };
        db.ReviewRounds.Add(round);
        db.ProjectRounds.Add(new ProjectRound { ProjectId = projectA.Id, RoundId = round.Id, Status = "PENDING" });
        db.ProjectRounds.Add(new ProjectRound { ProjectId = projectB.Id, RoundId = round.Id, Status = "PENDING" }); // vẫn PENDING

        var council = new ReviewCouncil { Id = Guid.NewGuid(), RoundId = round.Id, CouncilType = "REVIEW", CreatedBy = Guid.NewGuid() };
        db.ReviewCouncils.Add(council);
        db.CouncilProjectAssignments.Add(new CouncilProjectAssignment { CouncilId = council.Id, ProjectId = projectA.Id });
        var chairMember = new CouncilMember { Id = Guid.NewGuid(), CouncilId = council.Id, UserId = chair.Id, MemberRole = "Chair", Status = "CONFIRMED" };
        var secMember = new CouncilMember { Id = Guid.NewGuid(), CouncilId = council.Id, UserId = secretary.Id, MemberRole = "Secretary", Status = "CONFIRMED" };
        db.CouncilMembers.AddRange(chairMember, secMember);
        // QĐ543 Điều 8.3.b: phải có ít nhất 2/3 thành viên dự họp và người dự phải chấm.
        SeedBallots(db, council.Id, projectA.Id, chairMember, secMember);
        await db.SaveChangesAsync();

        var service = MakeService(db);
        await service.SaveMinutesAsync(council.Id, secretary.Id, new SaveMinutesRequest { ProjectId = projectA.Id, Result = "APPROVED" });
        await service.ApproveMinutesAsync(council.Id, chair.Id);

        var prA = await db.ProjectRounds.FirstAsync(pr => pr.RoundId == round.Id && pr.ProjectId == projectA.Id);
        Assert.Equal("PASSED", prA.Status);

        var updatedRound = await db.ReviewRounds.FindAsync(round.Id);
        Assert.Equal("OPEN", updatedRound!.Status); // đề tài B vẫn PENDING → round chưa tự đóng
        Assert.Null(updatedRound.ClosedAt);
    }

    // Bug fix (rule #1): REVISION_REQUIRED KHÔNG được khóa project_round thành FAILED — phải giữ
    // vòng mở để PI sửa & nộp lại; round không tự đóng; board không hiển thị "Từ chối".
    [Fact]
    public async Task ApproveMinutes_RevisionRequired_KeepsProjectRoundOpen_RoundNotClosed()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var pi = MakeUser("PI");
        var (project, proposal) = MakeProjectWithProposal(pi.Id);
        var chair = MakeUser("Chair");
        var secretary = MakeUser("Secretary");
        db.Users.AddRange(pi, chair, secretary);
        db.Projects.Add(project);
        db.Proposals.Add(proposal);

        var round = new ReviewRound { Id = Guid.NewGuid(), CycleTrackId = 1, RoundNumber = 1, Sequence = 1, Dimension = "SCIENCE", RoundType = "REVIEW", Status = "OPEN" };
        db.ReviewRounds.Add(round);
        db.ProjectRounds.Add(new ProjectRound { ProjectId = project.Id, RoundId = round.Id, Status = "PENDING" });

        var council = new ReviewCouncil { Id = Guid.NewGuid(), RoundId = round.Id, CouncilType = "REVIEW", CreatedBy = Guid.NewGuid() };
        db.ReviewCouncils.Add(council);
        db.CouncilProjectAssignments.Add(new CouncilProjectAssignment { CouncilId = council.Id, ProjectId = project.Id });
        var chairMember = new CouncilMember { Id = Guid.NewGuid(), CouncilId = council.Id, UserId = chair.Id, MemberRole = "Chair", Status = "CONFIRMED" };
        var secMember = new CouncilMember { Id = Guid.NewGuid(), CouncilId = council.Id, UserId = secretary.Id, MemberRole = "Secretary", Status = "CONFIRMED" };
        db.CouncilMembers.AddRange(chairMember, secMember);
        // QĐ543 Điều 8.3.b: phải có ít nhất 2/3 thành viên dự họp và người dự phải chấm.
        SeedBallots(db, council.Id, project.Id, chairMember, secMember);
        await db.SaveChangesAsync();

        var service = MakeService(db);
        await service.SaveMinutesAsync(council.Id, secretary.Id, new SaveMinutesRequest { Result = "REVISION_REQUIRED", CouncilComments = "Sửa mục tiêu" });
        await service.ApproveMinutesAsync(council.Id, chair.Id);

        var projectRound = await db.ProjectRounds.FirstAsync(pr => pr.RoundId == round.Id && pr.ProjectId == project.Id);
        Assert.Equal("OPEN", projectRound.Status);        // KHÔNG phải FAILED
        Assert.Equal("REVISION_REQUIRED", projectRound.Result);
        Assert.Null(projectRound.FinalizedAt);            // không khóa

        var updatedRound = await db.ReviewRounds.FindAsync(round.Id);
        Assert.Equal("OPEN", updatedRound!.Status);       // round chưa đóng
        Assert.Null(updatedRound.ClosedAt);

        var updatedProposal = await db.Proposals.FindAsync(proposal.Id);
        Assert.Equal("REVISION_REQUIRED", updatedProposal!.Status);
    }

    // Rule #1 (P0): PI nộp lại bản REVISION → ReopenAfterResubmit mở lại hội đồng đã chốt "cần sửa",
    // GIỮ điểm cũ; council về FORMING, biên bản về nháp, project_round về OPEN chờ chốt lại.
    [Fact]
    public async Task ReopenAfterResubmit_RevisionCouncil_ReopensAndKeepsScores()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var pi = MakeUser("PI");
        var (project, proposal) = MakeProjectWithProposal(pi.Id);
        var chair = MakeUser("Chair");
        var secretary = MakeUser("Secretary");
        db.Users.AddRange(pi, chair, secretary);
        db.Projects.Add(project);
        db.Proposals.Add(proposal);

        var round = new ReviewRound { Id = Guid.NewGuid(), CycleTrackId = 1, RoundNumber = 1, Sequence = 1, Dimension = "SCIENCE", RoundType = "REVIEW", Status = "OPEN" };
        db.ReviewRounds.Add(round);
        db.ProjectRounds.Add(new ProjectRound { ProjectId = project.Id, RoundId = round.Id, Status = "PENDING" });

        var council = new ReviewCouncil { Id = Guid.NewGuid(), RoundId = round.Id, CouncilType = "REVIEW", CreatedBy = Guid.NewGuid() };
        db.ReviewCouncils.Add(council);
        db.CouncilProjectAssignments.Add(new CouncilProjectAssignment { CouncilId = council.Id, ProjectId = project.Id });
        var chairMember = new CouncilMember { Id = Guid.NewGuid(), CouncilId = council.Id, UserId = chair.Id, MemberRole = "Chair", Status = "CONFIRMED" };
        var secMember2 = new CouncilMember { Id = Guid.NewGuid(), CouncilId = council.Id, UserId = secretary.Id, MemberRole = "Secretary", Status = "CONFIRMED" };
        db.CouncilMembers.AddRange(chairMember, secMember2);
        // Hội đồng 2 người ⇒ cần đủ 2 phiếu (QĐ543 Điều 8.3.b: ≥2/3, làm tròn lên).
        SeedBallots(db, council.Id, project.Id, secMember2);
        // Điểm cũ của 1 ủy viên — phải được GIỮ sau khi reopen.
        db.ProposalReviewScores.Add(new ProposalReviewScore { CouncilId = council.Id, ProjectId = project.Id, EvaluatorMemberId = chairMember.Id, TemplateId = 1, SubmittedAt = DateTime.UtcNow, IsValidBallot = true });
        await db.SaveChangesAsync();

        var scoring = MakeService(db);
        await scoring.SaveMinutesAsync(council.Id, secretary.Id, new SaveMinutesRequest { Result = "REVISION_REQUIRED", CouncilComments = "Sửa mục tiêu" });
        await scoring.ApproveMinutesAsync(council.Id, chair.Id); // → council DECIDED, biên bản khóa

        var reviewRounds = new FURPMS.Infrastructure.Services.ReviewRoundService(
            new ReviewRepository(db), new ProposalRepository(db), new NotificationRepository(db), TestNotifier.Create(db), new FakeClock());
        await reviewRounds.ReopenAfterResubmitAsync(project.Id);

        var reopenedCouncil = await db.ReviewCouncils.FindAsync(council.Id);
        Assert.Equal("FORMING", reopenedCouncil!.Status);

        var decision = await db.CouncilDecisions.FirstAsync(d => d.CouncilId == council.Id && d.ProjectId == project.Id);
        Assert.Null(decision.FinalizedAt);            // biên bản về nháp

        var pr = await db.ProjectRounds.FirstAsync(x => x.RoundId == round.Id && x.ProjectId == project.Id);
        Assert.Equal("OPEN", pr.Status);
        Assert.Null(pr.Result);

        // Reopen KHÔNG được xoá điểm — cả 2 phiếu (Chủ tịch + Thư ký) phải còn nguyên.
        Assert.Equal(2, await db.ProposalReviewScores.CountAsync(s => s.CouncilId == council.Id));
    }

    // P5 (BM04 II.1 — cách ghi Q&A): Thư ký lưu danh sách hỏi–đáp; lưu lại lần 2 THAY TOÀN BỘ (không cộng dồn).
    [Fact]
    public async Task SaveMinutes_QaEntries_PersistedAndReplacedOnResave()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var pi = MakeUser("PI");
        var (project, proposal) = MakeProjectWithProposal(pi.Id);
        var secretary = MakeUser("Secretary");
        db.Users.AddRange(pi, secretary);
        db.Projects.Add(project);
        db.Proposals.Add(proposal);

        var round = new ReviewRound { Id = Guid.NewGuid(), CycleTrackId = 1, RoundNumber = 1, Sequence = 1, Dimension = "SCIENCE", RoundType = "REVIEW", Status = "OPEN" };
        db.ReviewRounds.Add(round);
        db.ProjectRounds.Add(new ProjectRound { ProjectId = project.Id, RoundId = round.Id, Status = "PENDING" });

        var council = new ReviewCouncil { Id = Guid.NewGuid(), RoundId = round.Id, CouncilType = "REVIEW", CreatedBy = Guid.NewGuid() };
        db.ReviewCouncils.Add(council);
        db.CouncilProjectAssignments.Add(new CouncilProjectAssignment { CouncilId = council.Id, ProjectId = project.Id });
        var secOnly = new CouncilMember { Id = Guid.NewGuid(), CouncilId = council.Id, UserId = secretary.Id, MemberRole = "Secretary", Status = "CONFIRMED" };
        db.CouncilMembers.Add(secOnly);
        SeedBallots(db, council.Id, project.Id, secOnly);
        await db.SaveChangesAsync();

        var service = MakeService(db);
        await service.SaveMinutesAsync(council.Id, secretary.Id, new SaveMinutesRequest
        {
            Result = "APPROVED",
            QaEntries = new()
            {
                new QaEntryDto { AskedBy = "Chủ tịch", Question = "Tính mới ở đâu?", Answer = "Ở thuật toán X", Order = 0 },
                new QaEntryDto { AskedBy = "Phản biện", Question = "Dữ liệu lấy đâu?", Answer = "Từ đối tác", Order = 1 },
                new QaEntryDto { Question = "   " }, // rỗng → bị bỏ qua
            }
        });

        var afterFirst = await service.GetDecisionAsync(council.Id);
        Assert.Equal(2, afterFirst!.QaEntries.Count);
        Assert.Equal("Tính mới ở đâu?", afterFirst.QaEntries[0].Question);
        Assert.Equal("Ở thuật toán X", afterFirst.QaEntries[0].Answer);

        // Lưu lại với 1 mục khác → thay toàn bộ, không cộng dồn.
        await service.SaveMinutesAsync(council.Id, secretary.Id, new SaveMinutesRequest
        {
            Result = "APPROVED",
            QaEntries = new() { new QaEntryDto { AskedBy = "Thành viên", Question = "Kế hoạch triển khai?", Answer = "6 tháng", Order = 0 } }
        });

        var afterSecond = await service.GetDecisionAsync(council.Id);
        Assert.Single(afterSecond!.QaEntries);
        Assert.Equal("Kế hoạch triển khai?", afterSecond.QaEntries[0].Question);
        Assert.Equal(1, await db.CouncilQaEntries.CountAsync()); // mục cũ đã bị xóa khỏi DB
    }

    // P5 (BM04 II.1 — ý kiến TV): Thư ký ghi ý kiến từng thành viên (chuyên môn/kinh phí),
    // lưu lại thay TOÀN BỘ; mục tên rỗng bị bỏ.
    [Fact]
    public async Task SaveMinutes_MemberOpinions_PersistedAndReplacedOnResave()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var pi = MakeUser("PI");
        var (project, proposal) = MakeProjectWithProposal(pi.Id);
        var secretary = MakeUser("Secretary");
        db.Users.AddRange(pi, secretary);
        db.Projects.Add(project);
        db.Proposals.Add(proposal);

        var round = new ReviewRound { Id = Guid.NewGuid(), CycleTrackId = 1, RoundNumber = 1, Sequence = 1, Dimension = "SCIENCE", RoundType = "REVIEW", Status = "OPEN" };
        db.ReviewRounds.Add(round);
        db.ProjectRounds.Add(new ProjectRound { ProjectId = project.Id, RoundId = round.Id, Status = "PENDING" });

        var council = new ReviewCouncil { Id = Guid.NewGuid(), RoundId = round.Id, CouncilType = "REVIEW", CreatedBy = Guid.NewGuid() };
        db.ReviewCouncils.Add(council);
        db.CouncilProjectAssignments.Add(new CouncilProjectAssignment { CouncilId = council.Id, ProjectId = project.Id });
        var secOnly = new CouncilMember { Id = Guid.NewGuid(), CouncilId = council.Id, UserId = secretary.Id, MemberRole = "Secretary", Status = "CONFIRMED" };
        db.CouncilMembers.Add(secOnly);
        SeedBallots(db, council.Id, project.Id, secOnly);
        await db.SaveChangesAsync();

        var service = MakeService(db);
        await service.SaveMinutesAsync(council.Id, secretary.Id, new SaveMinutesRequest
        {
            Result = "APPROVED",
            MemberOpinions = new()
            {
                new MemberOpinionDto { MemberName = "Chủ tịch", AcademicComment = "Đề tài tốt", BudgetComment = "Dự toán hợp lý", Order = 0 },
                new MemberOpinionDto { MemberName = "Thành viên 1", AcademicComment = "Cần rõ phương pháp", Order = 1 },
                new MemberOpinionDto { MemberName = "  " }, // rỗng → bỏ qua
            }
        });

        var afterFirst = await service.GetDecisionAsync(council.Id);
        Assert.Equal(2, afterFirst!.MemberOpinions.Count);
        Assert.Equal("Dự toán hợp lý", afterFirst.MemberOpinions[0].BudgetComment);

        await service.SaveMinutesAsync(council.Id, secretary.Id, new SaveMinutesRequest
        {
            Result = "APPROVED",
            MemberOpinions = new() { new MemberOpinionDto { MemberName = "Phản biện", AcademicComment = "OK", Order = 0 } }
        });

        var afterSecond = await service.GetDecisionAsync(council.Id);
        Assert.Single(afterSecond!.MemberOpinions);
        Assert.Equal("Phản biện", afterSecond.MemberOpinions[0].MemberName);
        Assert.Equal(1, await db.CouncilMemberOpinions.CountAsync());
    }
}
