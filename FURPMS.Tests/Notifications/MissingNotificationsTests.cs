using FURPMS.Application.DTOs.Cycles;
using FURPMS.Domain.Entities.Cycles;
using FURPMS.Domain.Entities.MasterData;
using FURPMS.Domain.Entities.Projects;
using FURPMS.Domain.Entities.Proposals;
using FURPMS.Domain.Entities.Users;
using FURPMS.Infrastructure.Data;
using FURPMS.Infrastructure.Repositories;
using FURPMS.Infrastructure.Services;
using FURPMS.Tests.Helpers;
using FURPMS.Tests.Reminders;
using Microsoft.EntityFrameworkCore;

namespace FURPMS.Tests.Notifications;

/// <summary>
/// Bốn mốc quan trọng trước đây <b>im lặng</b> — người cần biết phải tự vào app mò xem có gì mới.
/// Khoá lại để không ai gỡ đi lúc dọn code:
/// nộp đề cương · ký hợp đồng · Chủ tịch khoá biên bản · gia hạn hạn nộp của đợt.
/// </summary>
public class MissingNotificationsTests
{
    private static User MakeUser(string name = "Người dùng") => new()
    {
        Id = Guid.NewGuid(),
        Email = $"u-{Guid.NewGuid()}@test.com",
        FullName = name,
        Status = "ACTIVE"
    };

    /// <summary>Tạo một người mang vai <paramref name="roleName"/> để nhận thông báo theo vai.</summary>
    private static async Task<User> AddUserWithRoleAsync(FURPMSDbContext db, string roleName, string name)
    {
        var role = await db.Roles.FirstOrDefaultAsync(r => r.Name == roleName);
        if (role == null)
        {
            role = new Role { Name = roleName, Description = roleName };
            db.Roles.Add(role);
            await db.SaveChangesAsync();
        }

        var user = MakeUser(name);
        db.Users.Add(user);
        await db.SaveChangesAsync();

        db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id, AssignedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();
        return user;
    }

    // ── 1: PI nộp đề cương → Phòng QLKH được báo ────────────────────────────
    [Fact]
    public async Task SubmitProposal_NotifiesStaff()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");

        var staff = await AddUserWithRoleAsync(db, "Staff", "Chuyên viên QLKH");
        var pi = MakeUser("Nguyễn Văn A");
        db.Users.Add(pi);

        var type = new ResearchType { Code = "BASIC", Name = "Nghiên cứu cơ bản", MaxBudgetCap = 100_000_000m, IsActive = true };
        db.ResearchTypes.Add(type);
        await db.SaveChangesAsync();

        var cycle = new ResearchCycle
        {
            CycleYear = 2026, SemesterCode = "CB26", ResearchTypeId = type.Id, Status = "OPEN",
            SubmissionOpenDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-10)),
            SubmissionDeadline = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),
            CreatedBy = staff.Id
        };
        db.ResearchCycles.Add(cycle);
        await db.SaveChangesAsync();

        var track = new ResearchTrack { Code = "AI", Name = "Trí tuệ nhân tạo", IsActive = true };
        db.ResearchTracks.Add(track);
        await db.SaveChangesAsync();

        var cycleTrack = new CycleTrack { CycleId = cycle.Id, TrackId = track.Id };
        db.CycleTracks.Add(cycleTrack);
        await db.SaveChangesAsync();

        var project = new Project
        {
            Id = Guid.NewGuid(), CycleTrackId = cycleTrack.Id, OrderId = 1, PiUserId = pi.Id,
            HostingUnitId = 1, ResearchTypeId = type.Id, TitleVi = "Đề tài kiểm thử thông báo",
            Status = "PROPOSED",
            PlannedStartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            PlannedEndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(12))
        };
        var proposal = new Proposal
        {
            Id = Guid.NewGuid(), ProjectId = project.Id, VersionNo = 1, IsCurrent = true,
            TitleVi = "Đề tài kiểm thử thông báo", AbstractVi = "Tóm tắt", ResearchObjectives = "Mục tiêu",
            DurationMonths = 12,
            PlannedStartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            PlannedEndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(12)),
            Status = "DRAFT"
        };
        db.Projects.Add(project);
        db.Proposals.Add(proposal);
        db.ProposalBudgets.Add(new ProposalBudget { ProposalId = proposal.Id, TotalAmount = 80_000_000m });
        // CV mới cập nhật để không vướng cửa nhắc CV.
        db.AcademicProfiles.Add(new AcademicProfile { UserId = pi.Id, UpdatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var summaryQueue = new TestAiSummaryQueue();
        var svc = new ProposalService(
            new ProposalRepository(db), new CycleRepository(db), new MasterDataRepository(db),
            new UserRepository(db), new FakeClock(),
            TestServices.ReviewRounds(db),
            new ReviewRepository(db),
            new BudgetPolicyService(new ProposalRepository(db), new CycleRepository(db), new MasterDataRepository(db)),
            TestNotifier.Create(db),
            summaryQueue,
            new DeadlineResolver(new CycleRepository(db)),
            TestServices.Decisions(db));

        await svc.SubmitProposalAsync(proposal.Id, pi.Id);

        var sent = await db.Notifications.Where(n => n.UserId == staff.Id).ToListAsync();
        Assert.Single(sent);
        Assert.Equal("PROPOSAL_SUBMITTED", sent[0].NotificationType);

        // Thầy góp ý 05/08 + 14/08: tóm tắt AI phải có SẴN khi người chấm mở đề tài, không để
        // họ bấm rồi chờ 30–60 giây đúng lúc hội đồng đang ngồi nhìn. Nộp xong phải xếp hàng
        // sinh tóm tắt — và chỉ XẾP HÀNG, không gọi Gemini ngay trong lời gọi nộp (bắt PI chờ
        // một phút cho việc họ không cần).
        Assert.Single(summaryQueue.Enqueued);
        Assert.Equal(proposal.Id, summaryQueue.Enqueued[0].ProposalId);
        Assert.Equal(pi.Id, summaryQueue.Enqueued[0].OnBehalfOfUserId);
        // Chuyên viên phải biết AI nộp và nộp cái gì, không chỉ "có đề cương mới".
        Assert.Contains("Nguyễn Văn A", sent[0].Body);
        Assert.Contains("Đề tài kiểm thử thông báo", sent[0].Body);
    }

    // ── 2: gia hạn hạn nộp của đợt → mọi chủ nhiệm trong đợt được báo ───────
    [Fact]
    public async Task ExtendCycleDeadline_NotifiesEveryPiInCycle()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");

        var admin = MakeUser("Quản trị");
        var pi1 = MakeUser("Chủ nhiệm 1");
        var pi2 = MakeUser("Chủ nhiệm 2");
        var piOther = MakeUser("Chủ nhiệm đợt khác");
        db.Users.AddRange(admin, pi1, pi2, piOther);

        var type = new ResearchType { Code = "BASIC", Name = "Nghiên cứu cơ bản", MaxBudgetCap = 100_000_000m, IsActive = true };
        db.ResearchTypes.Add(type);
        await db.SaveChangesAsync();

        var track = new ResearchTrack { Code = "AI", Name = "Trí tuệ nhân tạo", IsActive = true };
        db.ResearchTracks.Add(track);
        await db.SaveChangesAsync();

        async Task<CycleTrack> MakeCycleTrackAsync(string code)
        {
            var c = new ResearchCycle
            {
                CycleYear = 2026, SemesterCode = code, ResearchTypeId = type.Id, Status = "OPEN",
                SubmissionOpenDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-10)),
                SubmissionDeadline = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(5)),
                CreatedBy = admin.Id
            };
            db.ResearchCycles.Add(c);
            await db.SaveChangesAsync();
            var ct = new CycleTrack { CycleId = c.Id, TrackId = track.Id };
            db.CycleTracks.Add(ct);
            await db.SaveChangesAsync();
            return ct;
        }

        var ctA = await MakeCycleTrackAsync("CB26");
        var ctB = await MakeCycleTrackAsync("UD26");

        void AddProposal(CycleTrack ct, Guid piId, string title)
        {
            var project = new Project
            {
                Id = Guid.NewGuid(), CycleTrackId = ct.Id, OrderId = 1, PiUserId = piId,
                HostingUnitId = 1, ResearchTypeId = type.Id, TitleVi = title, Status = "PROPOSED",
                PlannedStartDate = DateOnly.FromDateTime(DateTime.UtcNow),
                PlannedEndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(12))
            };
            db.Projects.Add(project);
            db.Proposals.Add(new Proposal
            {
                Id = Guid.NewGuid(), ProjectId = project.Id, VersionNo = 1, IsCurrent = true,
                TitleVi = title, AbstractVi = "Tóm tắt", ResearchObjectives = "Mục tiêu", DurationMonths = 12,
                PlannedStartDate = DateOnly.FromDateTime(DateTime.UtcNow),
                PlannedEndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(12)),
                Status = "SUBMITTED"
            });
        }

        AddProposal(ctA, pi1.Id, "Đề tài 1");
        AddProposal(ctA, pi2.Id, "Đề tài 2");
        AddProposal(ctB, piOther.Id, "Đề tài đợt khác");
        await db.SaveChangesAsync();

        var svc = new CycleService(new CycleRepository(db), new MasterDataRepository(db),
            new ProposalRepository(db), new ReviewRepository(db), TestNotifier.Create(db),
            new DeadlineResolver(new CycleRepository(db)));

        var newDeadline = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(20));
        await svc.ExtendCycleDeadlineAsync(ctA.CycleId,
            new ExtendDeadlineRequest { NewDeadline = newDeadline.ToString("yyyy-MM-dd"), Reason = "Trùng lịch thi" },
            admin.Id);

        var notifs = await db.Notifications.Where(n => n.NotificationType == "CYCLE_DEADLINE_EXTENDED").ToListAsync();
        var recipients = notifs.Select(n => n.UserId).ToHashSet();

        Assert.Contains(pi1.Id, recipients);
        Assert.Contains(pi2.Id, recipients);
        // Chủ nhiệm ở ĐỢT KHÁC không được nhận — gia hạn là chuyện riêng của từng đợt.
        Assert.DoesNotContain(piOther.Id, recipients);
        // Nội dung phải nêu cả hạn cũ lẫn hạn mới để khỏi phải đi tra lại.
        Assert.Contains(newDeadline.ToString("dd/MM/yyyy"), notifs[0].Body);
        Assert.Contains("Trùng lịch thi", notifs[0].Body);
    }
}
