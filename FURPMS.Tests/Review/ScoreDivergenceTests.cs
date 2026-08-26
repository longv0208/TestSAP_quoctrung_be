using FURPMS.Application.Constants;
using FURPMS.Application.DTOs.ReviewScoring;
using FURPMS.Domain.Entities.MasterData;
using FURPMS.Domain.Entities.Proposals;
using FURPMS.Domain.Entities.Review;
using FURPMS.Domain.Entities.Users;
using FURPMS.Infrastructure.Data;
using FURPMS.Infrastructure.Repositories;
using FURPMS.Infrastructure.Services;
using FURPMS.Tests.Helpers;
using Microsoft.EntityFrameworkCore;

namespace FURPMS.Tests.Review;

/// <summary>
/// Cảnh báo khi kết luận của hội đồng <b>lệch</b> với điểm chấm — góp ý miệng của hội đồng bảo vệ
/// lần 2: *"warning khi điểm thấp mà vẫn đạt"*.
///
/// <para><b>Tình trạng trước 25/08:</b> <c>AverageScore</c> và <c>Result</c> được gán ở hai dòng
/// liền kề trong <c>SaveMinutesAsync</c> mà không một phép so sánh nào. Đề tài trung bình 35/100
/// vẫn chốt được "Đạt", chuyển sang HOÀN THÀNH và mở khoá giải ngân đợt cuối, không một tiếng
/// cảnh báo.</para>
///
/// <para>⚠️ Rule #12 giữ nguyên: hệ thống <b>không</b> tự kết luận Đạt/Không đạt. Nó chỉ đòi hội
/// đồng ghi rõ lý do khi kết luận khác điểm.</para>
/// </summary>
public class ScoreDivergenceTests
{
    private static User MakeUser() => new()
    {
        Id = Guid.NewGuid(),
        Email = Guid.NewGuid().ToString("N")[..14] + "@t.com",
        FullName = "U",
        Status = UserStatus.Active
    };

    private static ReviewScoringService MakeService(FURPMSDbContext db) =>
        TestServices.ReviewScoring(db);

    /// <summary>Hội đồng 3 người đã chấm xong, mỗi phiếu <paramref name="score"/> trên thang 100.</summary>
    private static async Task<(ReviewCouncil council, Guid chairId, Guid secId, Guid projectId)>
        SeedAsync(FURPMSDbContext db, decimal score)
    {
        var pi = MakeUser();
        var chair = MakeUser();
        var sec = MakeUser();
        var member = MakeUser();
        db.Users.AddRange(pi, chair, sec, member);

        var project = new FURPMS.Domain.Entities.Projects.Project
        {
            Id = Guid.NewGuid(), CycleTrackId = 1, OrderId = 1, PiUserId = pi.Id,
            HostingUnitId = 1, ResearchTypeId = 1, TitleVi = "Đề tài kiểm thử ngưỡng điểm",
            Status = ProjectStatus.UnderReview,
            PlannedStartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            PlannedEndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(12))
        };
        db.Projects.Add(project);
        db.Proposals.Add(new Proposal
        {
            Id = Guid.NewGuid(), ProjectId = project.Id, VersionNo = 1, IsCurrent = true,
            TitleVi = project.TitleVi, AbstractVi = "A", ResearchObjectives = "O", DurationMonths = 12,
            PlannedStartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            PlannedEndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(12)),
            Status = ProposalStatus.Submitted
        });

        var council = new ReviewCouncil
        {
            Id = Guid.NewGuid(), CouncilType = "SCIENCE", Status = CouncilStatus.Forming,
            CreatedBy = chair.Id, CreatedAt = DateTime.UtcNow, UpdatedAt = DateTime.UtcNow
        };
        db.ReviewCouncils.Add(council);
        db.CouncilProjectAssignments.Add(
            new CouncilProjectAssignment { CouncilId = council.Id, ProjectId = project.Id });

        var chairM = new CouncilMember { Id = Guid.NewGuid(), CouncilId = council.Id, UserId = chair.Id, MemberRole = CouncilMemberRole.Chair, Status = CouncilMemberStatus.Confirmed };
        var secM = new CouncilMember { Id = Guid.NewGuid(), CouncilId = council.Id, UserId = sec.Id, MemberRole = CouncilMemberRole.Secretary, Status = CouncilMemberStatus.Confirmed };
        var memberM = new CouncilMember { Id = Guid.NewGuid(), CouncilId = council.Id, UserId = member.Id, MemberRole = CouncilMemberRole.Member, Status = CouncilMemberStatus.Confirmed };
        db.CouncilMembers.AddRange(chairM, secM, memberM);
        await db.SaveChangesAsync();

        TestBallots.Add(db, council.Id, project.Id, chairM.Id, score);
        TestBallots.Add(db, council.Id, project.Id, memberM.Id, score);

        return (council, chair.Id, sec.Id, project.Id);
    }

    private static SaveMinutesRequest Minutes(string result, string? justification = null) => new()
    {
        Result = result,
        CouncilComments = "Biên bản họp hội đồng",
        ResultJustification = justification
    };

    // ── 1: điểm thấp mà kết luận Đạt, không lý do → chặn ─────────────────
    [Fact]
    public async Task DiemThapMaDat_KhongLyDo_ThiChan()
    {
        var db = TestDbContextFactory.Create("test-" + Guid.NewGuid());
        var (council, _, secId, _) = await SeedAsync(db, TestBallots.LowScore);

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            MakeService(db).SaveMinutesAsync(council.Id, secId, Minutes(ReviewResult.Approved)));

        // Câu báo lỗi phải nói ra CON SỐ, không thì Thư ký không biết mình lệch ở đâu.
        Assert.Contains("35", ex.Message);
        Assert.Contains("100", ex.Message);
        Assert.Contains("lý do", ex.Message);

        // Chặn mà vẫn lưu thì coi như không chặn.
        Assert.Empty(db.CouncilDecisions.Where(d => d.CouncilId == council.Id));
    }

    // ── 2: có lý do thì lưu được, và lý do được giữ lại ──────────────────
    [Fact]
    public async Task DiemThapMaDat_CoLyDo_ThiLuoDuoc()
    {
        var db = TestDbContextFactory.Create("test-" + Guid.NewGuid());
        var (council, _, secId, _) = await SeedAsync(db, TestBallots.LowScore);

        const string lyDo = "Đề tài có tính ứng dụng cao cho đơn vị đặt hàng, hội đồng thống nhất thông qua.";
        var dto = await MakeService(db).SaveMinutesAsync(council.Id, secId,
            Minutes(ReviewResult.Approved, lyDo));

        Assert.Equal(lyDo, dto.ResultJustification);
        Assert.True(dto.ResultDivergesFromScore);
        Assert.Equal(100m, dto.RubricTotal);
        Assert.Equal(SystemSettingKeys.DefaultReviewPassThresholdPct, dto.PassThresholdPct);
    }

    // ── 3: đối xứng — điểm cao mà kết luận Không đạt cũng phải giải trình ─
    [Fact]
    public async Task DiemCaoMaKhongDat_CungPhaiGiaiTrinh()
    {
        var db = TestDbContextFactory.Create("test-" + Guid.NewGuid());
        var (council, _, secId, _) = await SeedAsync(db, TestBallots.PassingScore);

        // Chỉ bắt một chiều thì thành ra hệ thống nghi ngờ hội đồng khi họ rộng tay nhưng im lặng
        // khi họ chặt tay — không công bằng với một quyết định chuyên môn.
        await Assert.ThrowsAsync<ArgumentException>(() =>
            MakeService(db).SaveMinutesAsync(council.Id, secId, Minutes(ReviewResult.Rejected)));

        var dto = await MakeService(db).SaveMinutesAsync(council.Id, secId,
            Minutes(ReviewResult.Rejected, "Sản phẩm không đủ số lượng theo hợp đồng."));
        Assert.True(dto.ResultDivergesFromScore);
    }

    // ── 4: kết luận KHỚP điểm thì không đòi gì cả ────────────────────────
    [Fact]
    public async Task KetLuanKhopDiem_ThiKhongDoiGiCa()
    {
        var db = TestDbContextFactory.Create("test-" + Guid.NewGuid());
        var (council, _, secId, _) = await SeedAsync(db, TestBallots.PassingScore);

        var dto = await MakeService(db).SaveMinutesAsync(council.Id, secId, Minutes(ReviewResult.Approved));

        Assert.False(dto.ResultDivergesFromScore);
        Assert.Null(dto.ResultJustification);
    }

    // ── 5: "yêu cầu chỉnh sửa" hợp lý ở cả hai phía ngưỡng ───────────────
    [Fact]
    public async Task YeuCauChinhSua_KhongBaoGioBiCoiLaLech()
    {
        var db = TestDbContextFactory.Create("test-" + Guid.NewGuid());
        var (council, _, secId, _) = await SeedAsync(db, TestBallots.LowScore);

        var dto = await MakeService(db).SaveMinutesAsync(council.Id, secId,
            Minutes(ReviewResult.RevisionRequired));

        Assert.False(dto.ResultDivergesFromScore);
    }

    // ── 6: đổi ngưỡng ở Cài đặt thì cảnh báo đổi theo ────────────────────
    [Fact]
    public async Task DoiNguongOCaiDat_ThiCanhBaoDoiTheo()
    {
        var db = TestDbContextFactory.Create("test-" + Guid.NewGuid());
        var (council, _, secId, _) = await SeedAsync(db, TestBallots.LowScore);

        // 35/100 đang là "thấp" với ngưỡng mặc định 50%. Hạ ngưỡng xuống 30% thì 35 thành "đạt"
        // và kết luận Đạt hết lệch — đây chính là câu trả lời cho "đổi con số này rồi demo ngay đi".
        db.SystemSettings.Add(new SystemSetting
        {
            Key = SystemSettingKeys.ReviewPassThresholdPct,
            Value = "30",
            RecommendedValue = "50"
        });
        await db.SaveChangesAsync();

        var dto = await MakeService(db).SaveMinutesAsync(council.Id, secId, Minutes(ReviewResult.Approved));

        Assert.False(dto.ResultDivergesFromScore);
        Assert.Equal(30, dto.PassThresholdPct);
    }

    // ── 7: chốt biên bản lệch → sinh một dòng trong sổ quyết định ────────
    [Fact]
    public async Task ChotBienBanLech_SinhDongTrongSoQuyetDinh()
    {
        var db = TestDbContextFactory.Create("test-" + Guid.NewGuid());
        var (council, chairId, secId, projectId) = await SeedAsync(db, TestBallots.LowScore);
        var svc = MakeService(db);

        const string lyDo = "Hội đồng đánh giá cao tính mới dù điểm hình thức thấp.";
        await svc.SaveMinutesAsync(council.Id, secId, Minutes(ReviewResult.Approved, lyDo));
        await svc.ApproveMinutesAsync(council.Id, chairId);

        // Không ghi lại thì hồ sơ chỉ nói "Đạt" mà không nói vì sao Đạt với 35/100 — đọc lại
        // không hiểu nổi, và cũng mất luôn bằng chứng có người chịu trách nhiệm.
        var row = await db.ProjectDecisions
            .FirstOrDefaultAsync(d => d.ProjectId == projectId
                                      && d.DecisionType == DecisionTypes.ScoreDivergenceJustified);

        Assert.NotNull(row);
        Assert.Equal(lyDo, row!.Reason);
        Assert.Equal("Chủ tịch hội đồng", row.DecidedByRole);
        Assert.Contains("35", row.Summary);
    }

    // ── 8: kết luận KHỚP điểm thì KHÔNG sinh dòng giải trình ─────────────
    [Fact]
    public async Task KetLuanKhopDiem_KhongSinhDongGiaiTrinh()
    {
        var db = TestDbContextFactory.Create("test-" + Guid.NewGuid());
        var (council, chairId, secId, projectId) = await SeedAsync(db, TestBallots.PassingScore);
        var svc = MakeService(db);

        await svc.SaveMinutesAsync(council.Id, secId, Minutes(ReviewResult.Approved));
        await svc.ApproveMinutesAsync(council.Id, chairId);

        Assert.False(await db.ProjectDecisions
            .AnyAsync(d => d.ProjectId == projectId
                           && d.DecisionType == DecisionTypes.ScoreDivergenceJustified));
    }

    // ── 9: sửa từ lệch sang khớp thì lý do cũ bị xoá ─────────────────────
    [Fact]
    public async Task SuaTuLechSangKhop_ThiXoaLyDoCu()
    {
        var db = TestDbContextFactory.Create("test-" + Guid.NewGuid());
        var (council, _, secId, _) = await SeedAsync(db, TestBallots.LowScore);
        var svc = MakeService(db);

        await svc.SaveMinutesAsync(council.Id, secId,
            Minutes(ReviewResult.Approved, "Lý do của bản nháp trước."));

        // Thư ký đổi sang "Không đạt" — khớp với điểm 35/100 nên hết lệch. Giữ lại lý do cũ thì
        // biên bản mâu thuẫn với chính nó: kết luận đúng điểm mà vẫn kèm câu giải trình vì sao khác.
        var dto = await svc.SaveMinutesAsync(council.Id, secId, Minutes(ReviewResult.Rejected));

        Assert.False(dto.ResultDivergesFromScore);
        Assert.Null(dto.ResultJustification);
    }
}
