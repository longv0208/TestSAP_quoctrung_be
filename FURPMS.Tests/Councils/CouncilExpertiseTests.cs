using FURPMS.Application.Constants;
using FURPMS.Application.DTOs.Councils;
using FURPMS.Domain.Entities.Cycles;
using FURPMS.Domain.Entities.MasterData;
using FURPMS.Domain.Entities.Projects;
using FURPMS.Domain.Entities.Review;
using FURPMS.Domain.Entities.Users;
using FURPMS.Infrastructure.Data;
using FURPMS.Infrastructure.Services;
using FURPMS.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace FURPMS.Tests.Councils;

/// <summary>
/// Chuyên môn của ủy viên hội đồng — góp ý miệng của hội đồng bảo vệ lần 2:
/// *"người chấm phải có chuyên môn"*.
///
/// <para><b>Tình trạng trước 26/08:</b> hệ thống <b>không có bảng nào nối người với lĩnh vực</b>.
/// Chuyên môn chỉ là hai ô text tự do trong hồ sơ khoa học, dùng để in ra Word. Khi gán ủy viên,
/// hệ thống chỉ kiểm xung đột lợi ích, trùng tên và số lượng.</para>
///
/// <para>⚠️ <b>Chặn có kiểm soát, không khoá cứng.</b> Có ca cần mời người ngoài lĩnh vực thật —
/// chuyên gia liên ngành, hoặc lĩnh vực hẹp không đủ người. Phòng QLKH vẫn gán được, nhưng phải
/// ghi rõ lý do và lý do đó vào sổ quyết định.</para>
/// </summary>
public class CouncilExpertiseTests
{
    private const int TrackAi = 11;
    private const int TrackIt = 12;

    private static User MakeUser(string name) => new()
    {
        Id = Guid.NewGuid(),
        Email = Guid.NewGuid().ToString("N")[..14] + "@t.com",
        FullName = name,
        Status = UserStatus.Active
    };

    /// <summary>Đề tài thuộc lĩnh vực AI, kèm hội đồng đã gán đề tài đó.</summary>
    private static async Task<(FURPMSDbContext db, ReviewCouncil council, Project project, User pi)>
        SeedAsync()
    {
        var db = TestDbContextFactory.Create("test-" + Guid.NewGuid());

        db.ResearchTracks.AddRange(
            new ResearchTrack { Id = TrackAi, Code = "AI", Name = "Trí tuệ nhân tạo", IsActive = true },
            new ResearchTrack { Id = TrackIt, Code = "IT", Name = "Công nghệ thông tin", IsActive = true });
        db.ResearchTypes.Add(new ResearchType
        {
            Id = 1, Code = "APPLIED", Name = "Ứng dụng", MaxBudgetCap = 150_000_000m, IsActive = true
        });

        var pi = MakeUser("Chủ nhiệm");
        db.Users.Add(pi);
        await db.SaveChangesAsync();

        var cycle = new ResearchCycle
        {
            CycleYear = 2026, SemesterCode = "SU26", ResearchTypeId = 1,
            SubmissionOpenDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-30),
            SubmissionDeadline = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(30),
            Status = CycleStatus.Open, CreatedBy = pi.Id
        };
        db.ResearchCycles.Add(cycle);
        await db.SaveChangesAsync();

        var track = new CycleTrack { CycleId = cycle.Id, TrackId = TrackAi };
        db.CycleTracks.Add(track);
        await db.SaveChangesAsync();

        var project = new Project
        {
            Id = Guid.NewGuid(), CycleTrackId = track.Id, OrderId = 1, PiUserId = pi.Id,
            HostingUnitId = 1, ResearchTypeId = 1, ProjectCode = "DT-AI",
            TitleVi = "Đề tài lĩnh vực trí tuệ nhân tạo", Status = ProjectStatus.UnderReview,
            PlannedStartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            PlannedEndDate = DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(12)
        };
        db.Projects.Add(project);

        var council = new ReviewCouncil
        {
            Id = Guid.NewGuid(), CouncilType = "SCIENCE", Status = CouncilStatus.Forming,
            CreatedBy = pi.Id, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        db.ReviewCouncils.Add(council);
        db.CouncilProjectAssignments.Add(
            new CouncilProjectAssignment { CouncilId = council.Id, ProjectId = project.Id });
        await db.SaveChangesAsync();

        return (db, council, project, pi);
    }

    private static User AddExpert(FURPMSDbContext db, string name, params int[] trackIds)
    {
        var u = MakeUser(name);
        db.Users.Add(u);
        db.SaveChanges();
        foreach (var t in trackIds)
            db.UserResearchTracks.Add(new UserResearchTrack { UserId = u.Id, TrackId = t });
        db.SaveChanges();
        return u;
    }

    private static AddCouncilMemberRequest Req(Guid userId, bool accept = false, string? note = null) => new()
    {
        UserId = userId,
        MemberRole = CouncilMemberRole.Member,
        AcceptWithoutExpertise = accept,
        ExpertiseNote = note
    };

    // ── 1: đúng lĩnh vực → gán bình thường ───────────────────────────────
    [Fact]
    public async Task DungLinhVuc_GanBinhThuong()
    {
        var (db, council, _, _) = await SeedAsync();
        var expert = AddExpert(db, "Chuyên gia AI", TrackAi);

        var member = await TestServices.Councils(db).AddMemberAsync(council.Id, Req(expert.Id));

        Assert.Equal(expert.Id, member.UserId);
    }

    // ── 2: khác lĩnh vực, không lý do → chặn ─────────────────────────────
    [Fact]
    public async Task KhacLinhVuc_KhongLyDo_ThiChan()
    {
        var (db, council, _, _) = await SeedAsync();
        var outsider = AddExpert(db, "Chuyên gia IT", TrackIt);

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            TestServices.Councils(db).AddMemberAsync(council.Id, Req(outsider.Id)));

        // Câu báo phải nói RÕ lĩnh vực nào, không thì Phòng QLKH không biết mình thiếu gì.
        Assert.Contains("Trí tuệ nhân tạo", ex.Message);
        Assert.Contains("8.2", ex.Message);
        Assert.Empty(db.CouncilMembers.Where(m => m.CouncilId == council.Id));
    }

    // ── 3: chưa khai chuyên môn ≠ khác lĩnh vực ──────────────────────────
    [Fact]
    public async Task ChuaKhaiChuyenMon_BaoDungThucTe()
    {
        var (db, council, _, _) = await SeedAsync();
        var unknown = AddExpert(db, "Chưa khai gì");   // không truyền track nào

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            TestServices.Councils(db).AddMemberAsync(council.Id, Req(unknown.Id)));

        // Nói "không khai lĩnh vực X" cho người chưa khai gì cả là mô tả sai sự việc — hệ thống
        // đang KHÔNG BIẾT, chứ không phải đã xác định là sai chuyên môn.
        Assert.Contains("chưa khai lĩnh vực chuyên môn", ex.Message);
    }

    // ── 4: bật cờ mà thiếu lý do → vẫn chặn ──────────────────────────────
    [Fact]
    public async Task BatCoMaThieuLyDo_VanChan()
    {
        var (db, council, _, _) = await SeedAsync();
        var outsider = AddExpert(db, "Chuyên gia IT", TrackIt);

        // Mục đích là buộc giải trình, không phải dựng thêm một ô tick cho người ta bấm qua.
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            TestServices.Councils(db).AddMemberAsync(council.Id, Req(outsider.Id, accept: true)));

        Assert.Contains("lý do", ex.Message);
    }

    // ── 5: có cờ + có lý do → gán được, và ghi vào sổ quyết định ─────────
    [Fact]
    public async Task CoCoVaCoLyDo_GanDuoc_VaGhiVaoSoQuyetDinh()
    {
        var (db, council, project, _) = await SeedAsync();
        var outsider = AddExpert(db, "Chuyên gia IT", TrackIt);

        const string lyDo = "Đề tài có cấu phần hệ thống lớn, cần chuyên gia kiến trúc phần mềm.";
        await TestServices.Councils(db).AddMemberAsync(
            council.Id, Req(outsider.Id, accept: true, note: lyDo));

        Assert.Single(db.CouncilMembers.Where(m => m.CouncilId == council.Id));

        var row = await db.ProjectDecisions.FirstOrDefaultAsync(
            d => d.ProjectId == project.Id && d.DecisionType == DecisionTypes.ExpertiseOverride);
        Assert.NotNull(row);
        Assert.Equal(lyDo, row!.Reason);
        Assert.Contains("Chuyên gia IT", row.Summary);
    }

    // ── 6: xung đột lợi ích vẫn chặn như cũ ──────────────────────────────
    [Fact]
    public async Task XungDotLoiIch_VanChanNhuCu()
    {
        var (db, council, _, pi) = await SeedAsync();
        db.UserResearchTracks.Add(new UserResearchTrack { UserId = pi.Id, TrackId = TrackAi });
        await db.SaveChangesAsync();

        // Chủ nhiệm đúng lĩnh vực của chính đề tài mình — nhưng vẫn không được ngồi hội đồng chấm
        // nó. Thêm luật chuyên môn không được làm yếu đi hàng rào cũ.
        await Assert.ThrowsAsync<ArgumentException>(() =>
            TestServices.Councils(db).AddMemberAsync(council.Id, Req(pi.Id)));
    }

    // ── 7: hội đồng chưa gán đề tài → không có lĩnh vực để so ────────────
    [Fact]
    public async Task HoiDongChuaGanDeTai_ThiKhongDoiChuyenMon()
    {
        var (db, _, _, pi) = await SeedAsync();
        var empty = new ReviewCouncil
        {
            Id = Guid.NewGuid(), CouncilType = "SCIENCE", Status = CouncilStatus.Forming,
            CreatedBy = pi.Id, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        db.ReviewCouncils.Add(empty);
        await db.SaveChangesAsync();
        var unknown = AddExpert(db, "Chưa khai gì");

        // Không bịa ra một phán quyết từ chỗ không có dữ liệu.
        var member = await TestServices.Councils(db).AddMemberAsync(empty.Id, Req(unknown.Id));
        Assert.Equal(unknown.Id, member.UserId);
    }

    // ══ Danh sách ứng viên đã xếp hạng ════════════════════════════════════

    [Fact]
    public async Task UngVien_DungLinhVucXepTruoc_VaGanCoDayDu()
    {
        var (db, council, project, pi) = await SeedAsync();

        var faculty = await db.Roles.FirstOrDefaultAsync(r => r.Name == "Faculty");
        if (faculty is null)
        {
            faculty = new Role { Name = "Faculty" };
            db.Roles.Add(faculty);
            await db.SaveChangesAsync();
        }

        var dungNganh = AddExpert(db, "A đúng ngành", TrackAi);
        var khacNganh = AddExpert(db, "B khác ngành", TrackIt);
        var chuaKhai = AddExpert(db, "C chưa khai");
        foreach (var u in new[] { dungNganh, khacNganh, chuaKhai, pi })
            db.UserRoles.Add(new UserRole { UserId = u.Id, RoleId = faculty.Id, AssignedAt = DateTime.UtcNow });
        db.UserResearchTracks.Add(new UserResearchTrack { UserId = pi.Id, TrackId = TrackAi });
        await db.SaveChangesAsync();

        var svc = new CouncilCandidateService(db);
        var result = await svc.GetCandidatesAsync(project.Id, null, council.Id);

        Assert.Equal(TrackAi, result.TrackId);
        Assert.Equal("Trí tuệ nhân tạo", result.TrackName);

        // Chủ nhiệm cũng khai AI nên cũng "đúng lĩnh vực", nhưng vướng xung đột lợi ích ⇒ phải bị
        // đẩy xuống DƯỚI CÙNG, kể cả dưới người chưa khai chuyên môn: bấm vào cũng không chọn được
        // nên không được chiếm mấy dòng đầu.
        var first = result.Candidates[0];
        Assert.Equal("A đúng ngành", first.FullName);
        Assert.True(first.MatchesTrack);

        var piRow = result.Candidates.First(c => c.UserId == pi.Id);
        Assert.True(piRow.HasConflictOfInterest);
        Assert.Equal(result.Candidates.Count - 1, result.Candidates.IndexOf(piRow));

        var unknownRow = result.Candidates.First(c => c.FullName == "C chưa khai");
        Assert.True(unknownRow.ExpertiseUnknown);
        Assert.False(unknownRow.MatchesTrack);

        // Vẫn HIỆN người không chọn được — giấu đi thì Phòng QLKH không hiểu vì sao tìm mãi
        // không thấy một cái tên.
        Assert.Equal(4, result.TotalCount);
        Assert.Equal(2, result.MatchingCount);
    }
}
