using FURPMS.Domain.Entities.Projects;
using FURPMS.Domain.Entities.Proposals;
using FURPMS.Domain.Entities.Review;
using FURPMS.Domain.Entities.Users;
using FURPMS.Infrastructure.Data;
using FURPMS.Infrastructure.Repositories;
using FURPMS.Infrastructure.Services;
using FURPMS.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace FURPMS.Tests.Councils;

/// <summary>
/// Ai xem được lịch họp nào.
///
/// <para>
/// Lỗi thật phát hiện khi quét 38 màn ngày 14/08: menu <b>"Lịch họp"</b> hiện cho cả vai Hội đồng,
/// nhưng màn đó gọi <c>GET /api/meetings</c> — endpoint khoá cứng <c>Roles = "Admin,Staff"</c> ⇒
/// người chấm bấm vào là ăn <b>403</b> và màn báo lỗi đỏ. Mà họ mới chính là người cần biết họp
/// lúc nào, ở đâu.
/// </para>
/// <para>
/// Nay phạm vi tuỳ vai: quản lý thấy tất cả, thành viên hội đồng chỉ thấy hội đồng mình.
/// </para>
/// </summary>
public class MeetingVisibilityTests
{
    private static async Task<(CouncilMeetingService Svc, Guid MemberId, Guid OutsiderId, Guid MyCouncilId)>
        SeedTwoCouncilsAsync(FURPMSDbContext db)
    {
        var pi = NewUser();
        var member = NewUser();      // ở TRONG hội đồng A
        var outsider = NewUser();    // không ở hội đồng nào
        db.Users.AddRange(pi, member, outsider);

        var project = new Project
        {
            Id = Guid.NewGuid(), CycleTrackId = 1, OrderId = 1, PiUserId = pi.Id, HostingUnitId = 1,
            ResearchTypeId = 1, TitleVi = "Đề tài kiểm thử",
            Status = "SUBMITTED",
            PlannedStartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            PlannedEndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(12))
        };
        db.Projects.Add(project);
        db.Proposals.Add(new Proposal
        {
            Id = Guid.NewGuid(), ProjectId = project.Id, VersionNo = 1, IsCurrent = true,
            TitleVi = "Đề tài kiểm thử", DurationMonths = 12, Status = "SUBMITTED",
            AbstractVi = "Tóm tắt", ResearchObjectives = "Mục tiêu"
        });
        await db.SaveChangesAsync();

        // Hội đồng A — có `member`. Hội đồng B — không liên quan tới ai trong bài test.
        var councilA = NewCouncil(pi.Id);
        var councilB = NewCouncil(pi.Id);
        db.ReviewCouncils.AddRange(councilA, councilB);
        await db.SaveChangesAsync();

        db.CouncilMembers.Add(new CouncilMember
        {
            Id = Guid.NewGuid(), CouncilId = councilA.Id, UserId = member.Id,
            MemberRole = "Member", Status = "CONFIRMED"
        });
        db.CouncilMeetings.AddRange(
            NewMeeting(councilA.Id, "Họp hội đồng A"),
            NewMeeting(councilB.Id, "Họp hội đồng B"));
        await db.SaveChangesAsync();

        return (new CouncilMeetingService(new ReviewRepository(db)), member.Id, outsider.Id, councilA.Id);
    }

    private static User NewUser() => new()
    {
        Id = Guid.NewGuid(), Email = $"u-{Guid.NewGuid():N}"[..20] + "@t.com",
        FullName = "Người kiểm thử", Status = "ACTIVE"
    };

    private static ReviewCouncil NewCouncil(Guid createdBy) => new()
    {
        Id = Guid.NewGuid(), CouncilType = "SCIENCE", Status = "FORMING", CreatedBy = createdBy,
        MinMembersRequired = 3, MaxMembersAllowed = 5, QuorumNumerator = 2, QuorumDenominator = 3
    };

    private static CouncilMeeting NewMeeting(Guid councilId, string title) => new()
    {
        Id = Guid.NewGuid(), CouncilId = councilId, Title = title,
        ScheduledAt = DateTime.UtcNow.AddDays(3), Status = "SCHEDULED", Location = "P.301"
    };

    [Fact]
    public async Task Thanh_vien_hoi_dong_XEM_DUOC_lich_hop_cua_hoi_dong_minh()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var (svc, memberId, _, _) = await SeedTwoCouncilsAsync(db);

        var meetings = (await svc.GetForCouncilMemberAsync(memberId)).ToList();

        Assert.Single(meetings);
        Assert.Equal("Họp hội đồng A", meetings[0].Title);
    }

    [Fact]
    public async Task KHONG_thay_lich_hop_cua_hoi_dong_minh_khong_tham_gia()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var (svc, _, outsiderId, _) = await SeedTwoCouncilsAsync(db);

        var meetings = await svc.GetForCouncilMemberAsync(outsiderId);

        Assert.Empty(meetings);
    }

    [Fact]
    public async Task Quan_ly_van_thay_TAT_CA_lich_hop()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var (svc, _, _, _) = await SeedTwoCouncilsAsync(db);

        var all = await svc.GetAllAsync();

        Assert.Equal(2, all.Count());
    }
}
