using FURPMS.Application.Common;
using FURPMS.Application.Constants;
using FURPMS.Tests.Reminders;
using FURPMS.Application.DTOs.Councils;
using FURPMS.Domain.Entities.Proposals;
using FURPMS.Domain.Entities.Review;
using FURPMS.Domain.Entities.Users;
using FURPMS.Infrastructure.Repositories;
using FURPMS.Infrastructure.Services;
using FURPMS.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace FURPMS.Tests.Councils;

public class CouncilServiceTests
{
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

    private static ReviewCouncil MakeCouncil(Guid projectId, Guid createdBy, Guid? roundId = null) => new()
    {
        Id = Guid.NewGuid(),
        RoundId = roundId,
        CouncilType = "SCIENCE",
        Status = "FORMING",
        CreatedBy = createdBy,
        MinMembersRequired = 3,
        MaxMembersAllowed = 5,
        QuorumNumerator = 2,
        QuorumDenominator = 3,
        CreatedAt = DateTime.UtcNow,
        UpdatedAt = DateTime.UtcNow
    };

    [Fact]
    public async Task GetMyMemberships_MultiProjectCouncil_ReturnsEveryAssignedProject()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var pi = MakeUser();
        var reviewer = MakeUser();
        var staff = MakeUser();
        var (first, firstProposal) = MakeProjectWithProposal(pi.Id);
        first.TitleVi = "Đề tài thứ nhất";
        firstProposal.TitleVi = first.TitleVi;
        var (second, secondProposal) = MakeProjectWithProposal(pi.Id);
        second.TitleVi = "Đề tài thứ hai";
        secondProposal.TitleVi = second.TitleVi;
        var council = MakeCouncil(first.Id, staff.Id);
        db.Users.AddRange(pi, reviewer, staff);
        db.Projects.AddRange(first, second);
        db.Proposals.AddRange(firstProposal, secondProposal);
        db.ReviewCouncils.Add(council);
        db.CouncilMembers.Add(new CouncilMember
        {
            Id = Guid.NewGuid(), CouncilId = council.Id, UserId = reviewer.Id,
            MemberRole = "Member", Status = "CONFIRMED"
        });
        db.CouncilProjectAssignments.AddRange(
            new CouncilProjectAssignment { CouncilId = council.Id, ProjectId = first.Id },
            new CouncilProjectAssignment { CouncilId = council.Id, ProjectId = second.Id });
        await db.SaveChangesAsync();

        var service = TestServices.Councils(db);

        var memberships = (await service.GetMyMembershipsAsync(reviewer.Id)).ToList();

        Assert.Equal(2, memberships.Count);
        Assert.Equal(2, memberships.Select(x => x.ProjectId).Distinct().Count());
        Assert.All(memberships, x => Assert.Equal(council.Id, x.CouncilId));
    }

    // ── test 3: COI check — PI cannot be added as council member ─────────────

    [Fact]
    public async Task AddMember_PI_As_Member_Throws_400()
    {
        // Arrange
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");

        var pi = MakeUser();
        var staff = MakeUser();
        var (project, proposal) = MakeProjectWithProposal(pi.Id);
        var council = MakeCouncil(project.Id, staff.Id);

        db.Users.Add(pi);
        db.Users.Add(staff);
        db.Projects.Add(project);
        db.Proposals.Add(proposal);
        db.ReviewCouncils.Add(council);
        db.CouncilProjectAssignments.Add(new CouncilProjectAssignment { CouncilId = council.Id, ProjectId = project.Id });
        await db.SaveChangesAsync();

        var service = TestServices.Councils(db);

        // Act & Assert — adding the PI (COI) should throw ArgumentException → maps to 400
        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => service.AddMemberAsync(council.Id, new AddCouncilMemberRequest
            {
                UserId = pi.Id,      // <-- COI: same user as PI
                MemberRole = "MEMBER",
                IsExternal = false
            }));

        Assert.Contains("Conflict of interest", ex.Message);
    }

    [Fact]
    public async Task AddMember_NonPI_Succeeds()
    {
        // Arrange
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");

        var pi = MakeUser();
        var reviewer = MakeUser();
        var staff = MakeUser();
        var (project, proposal) = MakeProjectWithProposal(pi.Id);
        var council = MakeCouncil(project.Id, staff.Id);

        db.Users.Add(pi);
        db.Users.Add(reviewer);
        db.Users.Add(staff);
        db.Projects.Add(project);
        db.Proposals.Add(proposal);
        db.ReviewCouncils.Add(council);
        db.CouncilProjectAssignments.Add(new CouncilProjectAssignment { CouncilId = council.Id, ProjectId = project.Id });
        await db.SaveChangesAsync();

        var service = TestServices.Councils(db);

        // Act — adding a non-PI reviewer should succeed
        var result = await service.AddMemberAsync(council.Id, new AddCouncilMemberRequest
        {
            UserId = reviewer.Id,
            MemberRole = "MEMBER",
            IsExternal = false
        });

        // Assert — mới gán thì ở trạng thái ASSIGNED (chưa gửi thư mời)
        Assert.Equal(reviewer.Id, result.UserId);
        Assert.Equal("ASSIGNED", result.Status);
    }

    [Fact]
    public async Task DeleteCouncil_NoScores_RemovesCouncilMembersAndAssignments()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var pi = MakeUser();
        var reviewer = MakeUser();
        var staff = MakeUser();
        var (project, proposal) = MakeProjectWithProposal(pi.Id);
        var council = MakeCouncil(project.Id, staff.Id);

        db.Users.AddRange(pi, reviewer, staff);
        db.Projects.Add(project);
        db.Proposals.Add(proposal);
        db.ReviewCouncils.Add(council);
        db.CouncilProjectAssignments.Add(new CouncilProjectAssignment { CouncilId = council.Id, ProjectId = project.Id });
        await db.SaveChangesAsync();

        var service = TestServices.Councils(db);
        await service.AddMemberAsync(council.Id, new AddCouncilMemberRequest { UserId = reviewer.Id, MemberRole = "MEMBER", IsExternal = false });

        await service.DeleteCouncilAsync(council.Id);

        Assert.Equal(0, await db.ReviewCouncils.CountAsync());
        Assert.Equal(0, await db.CouncilMembers.CountAsync());
        Assert.Equal(0, await db.CouncilProjectAssignments.CountAsync());
    }

    [Fact]
    public async Task ConfirmMemberOnBehalf_SetsConfirmed()
    {
        // Arrange — Staff xác nhận thay reviewer (reviewer đồng ý ngoài hệ thống / tiện demo)
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");

        var pi = MakeUser();
        var reviewer = MakeUser();
        var staff = MakeUser();
        var (project, proposal) = MakeProjectWithProposal(pi.Id);
        var council = MakeCouncil(project.Id, staff.Id);

        db.Users.AddRange(pi, reviewer, staff);
        db.Projects.Add(project);
        db.Proposals.Add(proposal);
        db.ReviewCouncils.Add(council);
        db.CouncilProjectAssignments.Add(new CouncilProjectAssignment { CouncilId = council.Id, ProjectId = project.Id });
        db.SystemSettings.Add(new FURPMS.Domain.Entities.MasterData.SystemSetting
        {
            Key = SystemSettingKeys.CouncilAllowRespondOnBehalf,
            Value = "true",
            RecommendedValue = "false"
        });
        await db.SaveChangesAsync();

        var service = TestServices.Councils(db);
        var member = await service.AddMemberAsync(council.Id, new AddCouncilMemberRequest { UserId = reviewer.Id, MemberRole = "MEMBER", IsExternal = false });

        // Phải GỬI THƯ MỜI trước — chưa gửi mà đã ghi nhận trả lời là hồ sơ tự mâu thuẫn.
        var entity = await db.CouncilMembers.FirstAsync(m => m.Id == member.Id);
        entity.InvitationSentAt = DateTime.UtcNow;
        await db.SaveChangesAsync();

        // Act
        var confirmed = await service.RespondOnBehalfAsync(member.Id, staff.Id, accept: true, declineReason: null);

        // Assert — chuyển sang CONFIRMED + có mốc thời gian, không cần đăng nhập tài khoản reviewer
        Assert.Equal("CONFIRMED", confirmed.Status);
        Assert.NotNull(confirmed.ConfirmedAt);

        // …và LƯU LẠI ai đã bấm hộ. Không có dấu vết này thì về sau không phân biệt được thành
        // viên thật sự đồng ý hay chuyên viên bấm thay.
        var saved = await db.CouncilMembers.FirstAsync(m => m.Id == member.Id);
        Assert.Equal(staff.Id, saved.RespondedOnBehalfBy);
    }

    /// <summary>
    /// Nút "Đánh dấu từ chối" của chuyên viên phải CHẠY ĐƯỢC.
    /// <para>
    /// Trước 14/08 giao diện gọi nhầm sang endpoint dành cho chính thành viên, nên chuyên viên
    /// luôn nhận 403 "Bạn chỉ trả lời được thư mời gửi cho chính mình" — câu vô nghĩa với người
    /// đang ghi nhận hộ. Nhánh XÁC NHẬN đã được chuyển sang endpoint riêng từ trước, nhánh TỪ
    /// CHỐI thì bị bỏ sót.
    /// </para>
    /// </summary>
    [Fact]
    public async Task RespondOnBehalf_Decline_Works_AndRecordsWhoDidIt()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var (service, staffId, memberId) = await SeedInvitedMemberAsync(db);

        var result = await service.RespondOnBehalfAsync(memberId, staffId, accept: false, declineReason: "Bận công tác");

        Assert.Equal("DECLINED", result.Status);
        var saved = await db.CouncilMembers.FirstAsync(m => m.Id == memberId);
        Assert.Equal("Bận công tác", saved.DeclineReason);
        Assert.Equal(staffId, saved.RespondedOnBehalfBy);
    }

    /// <summary>Chưa gửi thư mời mà đã ghi nhận trả lời là hồ sơ tự mâu thuẫn — không có thư nào để trả lời.</summary>
    [Fact]
    public async Task RespondOnBehalf_BeforeInvitationSent_Throws()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var (service, staffId, memberId) = await SeedInvitedMemberAsync(db, sendInvitation: false);

        var ex = await Assert.ThrowsAsync<AppException>(
            () => service.RespondOnBehalfAsync(memberId, staffId, accept: true, declineReason: null));
        Assert.Contains("Chưa gửi thư mời", ex.Message);
    }

    /// <summary>Tắt công tắc thì chỉ thành viên tự trả lời — chuyên viên không ghi nhận hộ được nữa.</summary>
    [Fact]
    public async Task RespondOnBehalf_WhenSettingOff_Throws()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var (service, staffId, memberId) = await SeedInvitedMemberAsync(db, allowRespondOnBehalf: false);

        var ex = await Assert.ThrowsAsync<AppException>(
            () => service.RespondOnBehalfAsync(memberId, staffId, accept: true, declineReason: null));
        Assert.Contains("TỰ trả lời", ex.Message);
    }

    /// <summary>Đã từ chối rồi thì không "ghi nhận lại" — phải gán người thay.</summary>
    [Fact]
    public async Task RespondOnBehalf_AfterDeclined_Throws()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var (service, staffId, memberId) = await SeedInvitedMemberAsync(db);

        await service.RespondOnBehalfAsync(memberId, staffId, accept: false, declineReason: "x");

        await Assert.ThrowsAsync<AppException>(
            () => service.RespondOnBehalfAsync(memberId, staffId, accept: true, declineReason: null));
    }

    /// <summary>Dựng sẵn một hội đồng có 1 thành viên ĐÃ ĐƯỢC GỬI thư mời.</summary>
    private static async Task<(CouncilService Service, Guid StaffId, Guid MemberId)> SeedInvitedMemberAsync(
        FURPMS.Infrastructure.Data.FURPMSDbContext db, bool sendInvitation = true,
        bool allowRespondOnBehalf = true)
    {
        var pi = MakeUser();
        var reviewer = MakeUser();
        var staff = MakeUser();
        var (project, proposal) = MakeProjectWithProposal(pi.Id);
        var council = MakeCouncil(project.Id, staff.Id);

        db.Users.AddRange(pi, reviewer, staff);
        db.Projects.Add(project);
        db.Proposals.Add(proposal);
        db.ReviewCouncils.Add(council);
        db.CouncilProjectAssignments.Add(new CouncilProjectAssignment { CouncilId = council.Id, ProjectId = project.Id });
        db.SystemSettings.Add(new FURPMS.Domain.Entities.MasterData.SystemSetting
        {
            Key = SystemSettingKeys.CouncilAllowRespondOnBehalf,
            Value = allowRespondOnBehalf ? "true" : "false",
            RecommendedValue = "false"
        });
        await db.SaveChangesAsync();

        var service = TestServices.Councils(db);
        var member = await service.AddMemberAsync(council.Id,
            new AddCouncilMemberRequest { UserId = reviewer.Id, MemberRole = "MEMBER", IsExternal = false });

        if (sendInvitation)
        {
            var entity = await db.CouncilMembers.FirstAsync(m => m.Id == member.Id);
            entity.InvitationSentAt = DateTime.UtcNow;
            await db.SaveChangesAsync();
        }

        return (service, staff.Id, member.Id);
    }

    [Fact]
    public async Task AddMember_TeamMember_Throws_400_COI()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");

        var pi = MakeUser();
        var teamUser = MakeUser();
        var staff = MakeUser();
        var (project, proposal) = MakeProjectWithProposal(pi.Id);

        db.Users.Add(pi);
        db.Users.Add(teamUser);
        db.Users.Add(staff);
        db.Projects.Add(project);
        db.Proposals.Add(proposal);
        db.ProjectMembers.Add(new FURPMS.Domain.Entities.Projects.ProjectMember
        {
            ProjectId = project.Id,
            UserId = teamUser.Id,        // <-- thành viên đề tài có liên kết user
            FullName = "Team Member",
            WorkContent = "X",
            WorkMonths = 3
        });
        var council = MakeCouncil(project.Id, staff.Id);
        db.ReviewCouncils.Add(council);
        db.CouncilProjectAssignments.Add(new CouncilProjectAssignment { CouncilId = council.Id, ProjectId = project.Id });
        await db.SaveChangesAsync();

        var service = TestServices.Councils(db);

        var ex = await Assert.ThrowsAsync<ArgumentException>(
            () => service.AddMemberAsync(council.Id, new AddCouncilMemberRequest
            {
                UserId = teamUser.Id,    // COI: thành viên đề tài
                MemberRole = "Member",
                IsExternal = false
            }));
        Assert.Contains("Conflict of interest", ex.Message);
    }

    [Fact]
    public async Task SendInvitations_SetsInvitedAndDeadline()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");

        var pi = MakeUser();
        var reviewer = MakeUser();
        var staff = MakeUser();
        var (project, proposal) = MakeProjectWithProposal(pi.Id);
        var council = MakeCouncil(project.Id, staff.Id);

        db.Users.Add(pi);
        db.Users.Add(reviewer);
        db.Users.Add(staff);
        db.Projects.Add(project);
        db.Proposals.Add(proposal);
        db.ReviewCouncils.Add(council);
        db.CouncilProjectAssignments.Add(new CouncilProjectAssignment { CouncilId = council.Id, ProjectId = project.Id });
        await db.SaveChangesAsync();

        var service = TestServices.Councils(db);

        // Gate (rule tuần 10): cần Chủ tịch + Thư ký + lịch họp mới gửi được thư mời.
        var chair = MakeUser();
        var secretary = MakeUser();
        db.Users.AddRange(chair, secretary);
        db.CouncilMeetings.Add(new CouncilMeeting { CouncilId = council.Id, ScheduledAt = DateTime.UtcNow.AddDays(3), DurationMinutes = 60, Location = "P.A" });
        await db.SaveChangesAsync();

        await service.AddMemberAsync(council.Id, new AddCouncilMemberRequest { UserId = reviewer.Id, MemberRole = "Member", IsExternal = false });
        await service.AddMemberAsync(council.Id, new AddCouncilMemberRequest { UserId = chair.Id, MemberRole = "Chair", IsExternal = false });
        await service.AddMemberAsync(council.Id, new AddCouncilMemberRequest { UserId = secretary.Id, MemberRole = "Secretary", IsExternal = false });

        // Mô phỏng request HTTP mới: không còn relationship fix-up từ context đã dùng để gán đề tài.
        // Regression: SendInvitations từng chỉ Include Members nên luôn tưởng ProjectAssignments = 0.
        db.ChangeTracker.Clear();

        var deadline = DateTime.UtcNow.AddDays(5);
        var count = await service.SendInvitationsAsync(council.Id, deadline);

        Assert.Equal(3, count);
        var members = await service.GetMembersAsync(council.Id);
        Assert.Equal(3, members.Count());
        Assert.All(members, m => Assert.Equal("INVITED", m.Status));
    }

    // Gate (rule tuần 10): thiếu Chủ tịch/Thư ký hoặc chưa có lịch họp → không cho gửi thư mời.
    [Fact]
    public async Task SendInvitations_MissingChairOrMeeting_Throws()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var round = MakeRound();
        var council = MakeCouncil(Guid.NewGuid(), Guid.NewGuid(), round.Id);
        var member = MakeUser();
        db.Users.Add(member);
        db.ReviewRounds.Add(round);
        db.ReviewCouncils.Add(council);
        await db.SaveChangesAsync();

        var service = MakeSvc(db);
        await service.AddMemberAsync(council.Id, new AddCouncilMemberRequest { UserId = member.Id, MemberRole = "Member", IsExternal = false });

        // Chưa có Chủ tịch/Thư ký + chưa có lịch họp → chặn.
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.SendInvitationsAsync(council.Id, null));
    }

    [Fact]
    public async Task CreateCouncil_Requires_Valid_RoundId()
    {
        // Arrange
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");

        var pi = MakeUser();
        var staff = MakeUser();
        var (project, proposal) = MakeProjectWithProposal(pi.Id);

        db.Users.Add(pi);
        db.Users.Add(staff);
        db.Projects.Add(project);
        db.Proposals.Add(proposal);
        await db.SaveChangesAsync();

        var service = TestServices.Councils(db);

        // Act & Assert — non-existent roundId should throw KeyNotFoundException
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => service.CreateCouncilAsync(new CreateCouncilRequest
            {
                ProposalId = proposal.Id,
                RoundId = Guid.NewGuid(), // does not exist
                CouncilType = "SCIENCE",
                MinMembersRequired = 3,
                MaxMembersAllowed = 5
            }, staff.Id));
    }

    // ── Gán / gỡ đề tài vào hội đồng có sẵn (dropdown ở màn Hội đồng & Chấm) ──

    private static CouncilService MakeSvc(FURPMS.Infrastructure.Data.FURPMSDbContext db) =>
        TestServices.Councils(db);

    private static ReviewRound MakeRound(int cycleTrackId = 1) => new()
    {
        Id = Guid.NewGuid(), CycleTrackId = cycleTrackId, RoundNumber = 1, Sequence = 1,
        Dimension = "SCIENCE", RoundType = "REVIEW", Status = "OPEN"
    };

    [Fact]
    public async Task AssignProjectToCouncil_ProjectInRound_Succeeds()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var pi = MakeUser();
        var (project, proposal) = MakeProjectWithProposal(pi.Id);
        var round = MakeRound();
        var council = MakeCouncil(project.Id, Guid.NewGuid(), round.Id);
        db.Users.Add(pi);
        db.Projects.Add(project);
        db.Proposals.Add(proposal);
        db.ReviewRounds.Add(round);
        db.ProjectRounds.Add(new ProjectRound { ProjectId = project.Id, RoundId = round.Id, Status = "PENDING" });
        db.ReviewCouncils.Add(council);
        await db.SaveChangesAsync();

        await MakeSvc(db).AssignProjectToCouncilAsync(council.Id, project.Id);

        Assert.True(await db.CouncilProjectAssignments.AnyAsync(a => a.CouncilId == council.Id && a.ProjectId == project.Id));
    }

    [Fact]
    public async Task AssignProjectToCouncil_ProjectNotInRound_Throws()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var round = MakeRound();
        var council = MakeCouncil(Guid.NewGuid(), Guid.NewGuid(), round.Id);
        db.ReviewRounds.Add(round);
        db.ReviewCouncils.Add(council);
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<ArgumentException>(
            () => MakeSvc(db).AssignProjectToCouncilAsync(council.Id, Guid.NewGuid()));
    }

    [Fact]
    public async Task AssignProjectToCouncil_MovesFromOtherCouncilInSameRound()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var pi = MakeUser();
        var (project, proposal) = MakeProjectWithProposal(pi.Id);
        var round = MakeRound();
        var councilA = MakeCouncil(project.Id, Guid.NewGuid(), round.Id);
        var councilB = MakeCouncil(project.Id, Guid.NewGuid(), round.Id);
        db.Users.Add(pi);
        db.Projects.Add(project);
        db.Proposals.Add(proposal);
        db.ReviewRounds.Add(round);
        db.ProjectRounds.Add(new ProjectRound { ProjectId = project.Id, RoundId = round.Id, Status = "PENDING" });
        db.ReviewCouncils.AddRange(councilA, councilB);
        db.CouncilProjectAssignments.Add(new CouncilProjectAssignment { CouncilId = councilA.Id, ProjectId = project.Id });
        await db.SaveChangesAsync();

        await MakeSvc(db).AssignProjectToCouncilAsync(councilB.Id, project.Id);

        Assert.False(await db.CouncilProjectAssignments.AnyAsync(a => a.CouncilId == councilA.Id && a.ProjectId == project.Id));
        Assert.True(await db.CouncilProjectAssignments.AnyAsync(a => a.CouncilId == councilB.Id && a.ProjectId == project.Id));
    }

    [Fact]
    public async Task AssignProjectToCouncil_MemberIsPi_ThrowsCoi()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var pi = MakeUser();
        var (project, proposal) = MakeProjectWithProposal(pi.Id);
        var round = MakeRound();
        var council = MakeCouncil(project.Id, Guid.NewGuid(), round.Id);
        db.Users.Add(pi);
        db.Projects.Add(project);
        db.Proposals.Add(proposal);
        db.ReviewRounds.Add(round);
        db.ProjectRounds.Add(new ProjectRound { ProjectId = project.Id, RoundId = round.Id, Status = "PENDING" });
        db.ReviewCouncils.Add(council);
        db.CouncilMembers.Add(new CouncilMember { Id = Guid.NewGuid(), CouncilId = council.Id, UserId = pi.Id, MemberRole = "Member", Status = "ASSIGNED" });
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<ArgumentException>(
            () => MakeSvc(db).AssignProjectToCouncilAsync(council.Id, project.Id));
    }

    [Fact]
    public async Task RemoveProjectFromCouncil_NoScore_Succeeds()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var council = MakeCouncil(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());
        var projectId = Guid.NewGuid();
        db.ReviewCouncils.Add(council);
        db.CouncilProjectAssignments.Add(new CouncilProjectAssignment { CouncilId = council.Id, ProjectId = projectId });
        await db.SaveChangesAsync();

        await MakeSvc(db).RemoveProjectFromCouncilAsync(council.Id, projectId);

        Assert.False(await db.CouncilProjectAssignments.AnyAsync(a => a.CouncilId == council.Id && a.ProjectId == projectId));
    }

    // Rule tuần 10: 1 giảng viên ở 2 hội đồng họp GIAO GIỜ → báo trùng lịch.
    [Fact]
    public async Task GetScheduleConflicts_SharedMemberOverlappingMeetings_ReturnsConflict()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var shared = MakeUser();
        var c1 = MakeCouncil(Guid.NewGuid(), Guid.NewGuid());
        var c2 = MakeCouncil(Guid.NewGuid(), Guid.NewGuid());
        db.Users.Add(shared);
        db.ReviewCouncils.AddRange(c1, c2);
        db.CouncilMembers.Add(new CouncilMember { Id = Guid.NewGuid(), CouncilId = c1.Id, UserId = shared.Id, MemberRole = "Member", Status = "CONFIRMED" });
        db.CouncilMembers.Add(new CouncilMember { Id = Guid.NewGuid(), CouncilId = c2.Id, UserId = shared.Id, MemberRole = "Member", Status = "CONFIRMED" });
        var at = DateTime.UtcNow.AddDays(3);
        db.CouncilMeetings.Add(new CouncilMeeting { CouncilId = c1.Id, ScheduledAt = at, DurationMinutes = 60 });
        db.CouncilMeetings.Add(new CouncilMeeting { CouncilId = c2.Id, ScheduledAt = at.AddMinutes(30), DurationMinutes = 60 }); // giao giờ
        await db.SaveChangesAsync();

        var conflicts = (await MakeSvc(db).GetScheduleConflictsAsync(c1.Id)).ToList();
        Assert.Single(conflicts);
        Assert.Equal(shared.Id, conflicts[0].MemberUserId);
        Assert.Equal(c2.Id, conflicts[0].OtherCouncilId);
    }

    // Rule tuần 10: gán slot (khung giờ con) cho từng đề tài trong buổi họp; GetSlots trả theo thứ tự.
    [Fact]
    public async Task SaveAndGetSlots_SetsSlotAndMeetingId_OrderedBySlotOrder()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var pi = MakeUser();
        var (p1, prop1) = MakeProjectWithProposal(pi.Id);
        var (p2, prop2) = MakeProjectWithProposal(pi.Id);
        var round = MakeRound();
        var council = MakeCouncil(p1.Id, Guid.NewGuid(), round.Id);
        db.Users.Add(pi);
        db.Projects.AddRange(p1, p2);
        db.Proposals.AddRange(prop1, prop2);
        db.ReviewRounds.Add(round);
        db.ReviewCouncils.Add(council);
        db.CouncilProjectAssignments.Add(new CouncilProjectAssignment { CouncilId = council.Id, ProjectId = p1.Id });
        db.CouncilProjectAssignments.Add(new CouncilProjectAssignment { CouncilId = council.Id, ProjectId = p2.Id });
        var meeting = new CouncilMeeting { CouncilId = council.Id, ScheduledAt = DateTime.UtcNow.AddDays(3), DurationMinutes = 120 };
        db.CouncilMeetings.Add(meeting);
        await db.SaveChangesAsync();

        var svc = MakeSvc(db);
        await svc.SaveCouncilSlotsAsync(council.Id, new SaveSlotsRequest
        {
            Entries = new()
            {
                new SlotEntryDto { ProjectId = p2.Id, SlotStartAt = meeting.ScheduledAt.AddMinutes(60), SlotDurationMinutes = 60, SlotOrder = 1 },
                new SlotEntryDto { ProjectId = p1.Id, SlotStartAt = meeting.ScheduledAt, SlotDurationMinutes = 60, SlotOrder = 0 },
            }
        });

        var slots = (await svc.GetCouncilSlotsAsync(council.Id)).ToList();
        Assert.Equal(2, slots.Count);
        Assert.Equal(p1.Id, slots[0].ProjectId);   // SlotOrder 0 trước
        Assert.Equal(p2.Id, slots[1].ProjectId);
        Assert.Equal(meeting.Id, slots[0].MeetingId);
        Assert.Equal(60, slots[0].SlotDurationMinutes);
    }

    [Fact]
    public async Task GetScheduleConflicts_NonOverlappingMeetings_ReturnsNone()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var shared = MakeUser();
        var c1 = MakeCouncil(Guid.NewGuid(), Guid.NewGuid());
        var c2 = MakeCouncil(Guid.NewGuid(), Guid.NewGuid());
        db.Users.Add(shared);
        db.ReviewCouncils.AddRange(c1, c2);
        db.CouncilMembers.Add(new CouncilMember { Id = Guid.NewGuid(), CouncilId = c1.Id, UserId = shared.Id, MemberRole = "Member", Status = "CONFIRMED" });
        db.CouncilMembers.Add(new CouncilMember { Id = Guid.NewGuid(), CouncilId = c2.Id, UserId = shared.Id, MemberRole = "Member", Status = "CONFIRMED" });
        var at = DateTime.UtcNow.AddDays(3);
        db.CouncilMeetings.Add(new CouncilMeeting { CouncilId = c1.Id, ScheduledAt = at, DurationMinutes = 60 });
        db.CouncilMeetings.Add(new CouncilMeeting { CouncilId = c2.Id, ScheduledAt = at.AddHours(2), DurationMinutes = 60 }); // cách 2h
        await db.SaveChangesAsync();

        Assert.Empty(await MakeSvc(db).GetScheduleConflictsAsync(c1.Id));
    }
}
