using FURPMS.Application.Constants;
using FURPMS.Application.DTOs.ReviewRounds;
using FURPMS.Application.Interfaces.Services;
using FURPMS.Domain.Entities.Cycles;
using FURPMS.Domain.Entities.MasterData;
using FURPMS.Domain.Entities.Review;
using FURPMS.Infrastructure.Data;
using FURPMS.Tests.Helpers;
using FURPMS.Tests.Reminders; // FakeClock

namespace FURPMS.Tests.Deadlines;

/// <summary>
/// Hạn CHẤM của vòng — thêm 25/08 vì đây là chặng duy nhất trong vòng đời đề tài **không có hạn
/// nào cả**: vòng mở ra rồi để đấy, không ai biết bao giờ phải xong.
///
/// <para>Hai luật phải giữ: dời hạn là <b>LOG</b> chứ không ghi đè (rule #19), và quá hạn thì
/// <b>gắn cờ</b> chứ hệ thống không tự đóng vòng (rule #12 — kết luận là của Chủ tịch).</para>
/// </summary>
public class ReviewRoundDeadlineTests
{
    private static readonly DateOnly Today = new(2026, 6, 18);

    private static FakeClock ClockAtToday() => new() { UtcNow = Today.ToDateTime(TimeOnly.MinValue) };

    private static async Task<ReviewRound> SeedRoundAsync(FURPMSDbContext db, string status = "PENDING")
    {
        db.ResearchTypes.Add(new ResearchType { Id = 1, Code = "B", Name = "Cơ bản", IsActive = true });
        db.ResearchTracks.Add(new ResearchTrack { Id = 1, Code = "IT", Name = "CNTT", IsActive = true });
        await db.SaveChangesAsync();

        var cycle = new ResearchCycle
        {
            CycleYear = 2026, SemesterCode = "SU26", ResearchTypeId = 1,
            SubmissionOpenDate = Today.AddDays(-30), SubmissionDeadline = Today.AddDays(30),
            ReviewDeadline = Today.AddDays(60), Status = CycleStatus.Open, CreatedBy = Guid.NewGuid()
        };
        db.ResearchCycles.Add(cycle);
        await db.SaveChangesAsync();

        var track = new CycleTrack { CycleId = cycle.Id, TrackId = 1 };
        db.CycleTracks.Add(track);
        await db.SaveChangesAsync();

        var round = new ReviewRound
        {
            Id = Guid.NewGuid(), CycleTrackId = track.Id, RoundNumber = 1,
            Dimension = "SCIENCE", RoundType = "REVIEW", Sequence = 1, Status = status
        };
        db.ReviewRounds.Add(round);
        await db.SaveChangesAsync();
        return round;
    }

    // ── 1: mở vòng là tự đặt hạn theo cấu hình ──────────────────────────────
    [Fact]
    public async Task MoVong_TuDatHanTheoCauHinh()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var round = await SeedRoundAsync(db);
        db.SystemSettings.Add(new SystemSetting
        {
            Key = SystemSettingKeys.ScoringWindowDays, Value = "10", RecommendedValue = "15"
        });
        await db.SaveChangesAsync();

        var result = await TestServices.ReviewRounds(db, ClockAtToday()).OpenRoundAsync(round.Id);

        Assert.Equal("2026-06-28", result.ScoringDeadline);   // hôm nay + 10 ngày
        Assert.False(result.IsScoringOverdue);
    }

    // ── 2: mở lại vòng đã có hạn thì KHÔNG ghi đè hạn cũ ────────────────────
    [Fact]
    public async Task MoLaiVong_KhongGhiDeHanDaCo()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var round = await SeedRoundAsync(db);
        round.ScoringDeadline = new DateOnly(2026, 7, 1);
        await db.SaveChangesAsync();

        var result = await TestServices.ReviewRounds(db, ClockAtToday()).OpenRoundAsync(round.Id);

        Assert.Equal("2026-07-01", result.ScoringDeadline);
    }

    // ── 3: đặt hạn lần đầu — ghi thẳng, chưa có gì để "dời" ─────────────────
    [Fact]
    public async Task DatHanLanDau_GhiThangVaoVong_KhongSinhLog()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var round = await SeedRoundAsync(db, ReviewRoundStatus.Open);

        var result = await TestServices.ReviewRounds(db, ClockAtToday())
            .SetRoundDeadlineAsync(round.Id, new SetRoundDeadlineRequest
            {
                ScoringDeadline = "2026-07-15"
            }, Guid.NewGuid());

        Assert.Equal("2026-07-15", result.ScoringDeadline);
        Assert.Empty(db.DeadlineExtensions);
    }

    // ── 4: DỜI hạn đã có ⇒ sinh log, ngày GỐC giữ nguyên (rule #19) ─────────
    [Fact]
    public async Task DoiHan_SinhLog_VaGiuNguyenNgayGoc()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var round = await SeedRoundAsync(db, ReviewRoundStatus.Open);
        round.ScoringDeadline = new DateOnly(2026, 7, 1);
        await db.SaveChangesAsync();

        var actor = Guid.NewGuid();
        var result = await TestServices.ReviewRounds(db, ClockAtToday())
            .SetRoundDeadlineAsync(round.Id, new SetRoundDeadlineRequest
            {
                ScoringDeadline = "2026-07-20", Reason = "Một ủy viên đi công tác"
            }, actor);

        Assert.Equal("2026-07-20", result.ScoringDeadline);   // hạn HIỆU LỰC

        var fresh = await db.ReviewRounds.FindAsync(round.Id);
        Assert.Equal(new DateOnly(2026, 7, 1), fresh!.ScoringDeadline);   // ngày GỐC không đổi

        var log = Assert.Single(db.DeadlineExtensions);
        Assert.Equal(IDeadlineResolver.TargetTypeReviewRound, log.TargetType);
        Assert.Equal(new DateOnly(2026, 7, 1), log.OldDeadline);
        Assert.Equal(new DateOnly(2026, 7, 20), log.NewDeadline);
        Assert.Equal("Một ủy viên đi công tác", log.Reason);
        Assert.Equal(actor, log.CreatedBy);
    }

    // ── 5: dời hạn mà không nêu lý do ⇒ chặn ────────────────────────────────
    [Fact]
    public async Task DoiHan_KhongCoLyDo_ThiChan()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var round = await SeedRoundAsync(db, ReviewRoundStatus.Open);
        round.ScoringDeadline = new DateOnly(2026, 7, 1);
        await db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            TestServices.ReviewRounds(db, ClockAtToday())
                .SetRoundDeadlineAsync(round.Id, new SetRoundDeadlineRequest
                {
                    ScoringDeadline = "2026-07-20"
                }, Guid.NewGuid()));

        Assert.Contains("lý do", ex.Message);
        Assert.Empty(db.DeadlineExtensions);
    }

    // ── 6: đặt hạn vào quá khứ ⇒ chặn ───────────────────────────────────────
    [Fact]
    public async Task DatHanVaoQuaKhu_ThiChan()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var round = await SeedRoundAsync(db, ReviewRoundStatus.Open);

        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            TestServices.ReviewRounds(db, ClockAtToday())
                .SetRoundDeadlineAsync(round.Id, new SetRoundDeadlineRequest
                {
                    ScoringDeadline = "2026-06-01"
                }, Guid.NewGuid()));

        Assert.Contains("quá khứ", ex.Message);
    }

    // ── 7: vòng đã chốt kết quả thì đặt hạn không còn ý nghĩa ───────────────
    [Fact]
    public async Task VongDaChot_ThiKhongDatHanNua()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        var round = await SeedRoundAsync(db, ReviewRoundStatus.Passed);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            TestServices.ReviewRounds(db, ClockAtToday())
                .SetRoundDeadlineAsync(round.Id, new SetRoundDeadlineRequest
                {
                    ScoringDeadline = "2026-07-20"
                }, Guid.NewGuid()));
    }

    // ── 8: quá hạn thì GẮN CỜ, KHÔNG tự đóng vòng (rule #12) ────────────────
    [Fact]
    public async Task QuaHan_GanCo_NhungKhongTuDongVong()
    {
        var db = TestDbContextFactory.Create($"test-{Guid.NewGuid()}");
        // Vòng còn chờ mở nhưng hạn chấm đã được đặt từ trước và đã trôi qua.
        var round = await SeedRoundAsync(db);
        round.ScoringDeadline = Today.AddDays(-3);
        await db.SaveChangesAsync();

        var result = await TestServices.ReviewRounds(db, ClockAtToday()).OpenRoundAsync(round.Id);

        Assert.True(result.IsScoringOverdue);                  // có gắn cờ
        Assert.Equal("2026-06-15", result.ScoringDeadline);    // và KHÔNG bị đặt lại hạn mới

        var fresh = await db.ReviewRounds.FindAsync(round.Id);
        Assert.Equal(ReviewRoundStatus.Open, fresh!.Status);   // vẫn MỞ, hệ thống không tự chốt
        Assert.Null(fresh.Result);                              // và không tự gán kết quả (rule #12)
    }
}
