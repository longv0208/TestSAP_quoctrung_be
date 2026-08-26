using FURPMS.Application.Common;
using FURPMS.Application.Constants;
using FURPMS.Application.DTOs.Ai;
using FURPMS.Domain.Entities.AI;
using FURPMS.Domain.Entities.Cycles;
using FURPMS.Domain.Entities.MasterData;
using FURPMS.Domain.Entities.Projects;
using FURPMS.Domain.Entities.Proposals;
using FURPMS.Domain.Entities.Users;
using FURPMS.Infrastructure.Data;
using FURPMS.Tests.Helpers;
using FURPMS.Tests.Reminders; // FakeClock
using Microsoft.EntityFrameworkCore;

namespace FURPMS.Tests.Ai;

/// <summary>
/// Hành vi của <see cref="FURPMS.Infrastructure.Services.DuplicateCheckService"/> ngoài phần tính
/// cosine đã có test riêng (<see cref="DuplicateThresholdEvaluationTests"/>): cờ hàng loạt cho màn
/// danh sách, và việc chốt kết luận có báo cho chủ nhiệm hay không.
/// </summary>
public class DuplicateCheckServiceTests
{
    private static User NewUser(FURPMSDbContext db, string name)
    {
        var u = new User
        {
            Id = Guid.NewGuid(), Email = Guid.NewGuid().ToString("N")[..12] + "@t.com",
            FullName = name, Status = UserStatus.Active
        };
        db.Users.Add(u);
        return u;
    }

    /// <summary>Ba đề cương đã có vector: A và B gần nhau (giả lập trùng), C ở xa cả hai.</summary>
    private static async Task<(FURPMSDbContext db, Guid staffId, Proposal a, Proposal b, Proposal c)>
        SeedAsync()
    {
        var db = TestDbContextFactory.Create("test-" + Guid.NewGuid());
        var staff = NewUser(db, "Chuyên viên");
        var piA = NewUser(db, "Chủ nhiệm A");
        var piB = NewUser(db, "Chủ nhiệm B");
        var piC = NewUser(db, "Chủ nhiệm C");

        db.ResearchTypes.Add(new ResearchType { Id = 1, Code = "APPLIED", Name = "Ứng dụng", IsActive = true });
        db.ResearchTracks.Add(new ResearchTrack { Id = 1, Code = "IT", Name = "CNTT", IsActive = true });
        await db.SaveChangesAsync();

        var cycle = new ResearchCycle
        {
            CycleYear = 2026, SemesterCode = "SU26", ResearchTypeId = 1,
            SubmissionOpenDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-30),
            SubmissionDeadline = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(30),
            Status = CycleStatus.Open, CreatedBy = staff.Id
        };
        db.ResearchCycles.Add(cycle);
        await db.SaveChangesAsync();
        var track = new CycleTrack { CycleId = cycle.Id, TrackId = 1 };
        db.CycleTracks.Add(track);
        await db.SaveChangesAsync();

        async Task<Proposal> Seed(Guid piId, string code, string title)
        {
            var project = new Project
            {
                Id = Guid.NewGuid(), CycleTrackId = track.Id, OrderId = 1, PiUserId = piId,
                HostingUnitId = 1, ResearchTypeId = 1, ProjectCode = code, TitleVi = title,
                Status = ProjectStatus.UnderReview,
                PlannedStartDate = DateOnly.FromDateTime(DateTime.UtcNow),
                PlannedEndDate = DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(12)
            };
            db.Projects.Add(project);
            var proposal = new Proposal
            {
                Id = Guid.NewGuid(), ProjectId = project.Id, VersionNo = 1, IsCurrent = true,
                TitleVi = title, AbstractVi = "A", ResearchObjectives = "O", DurationMonths = 12,
                PlannedStartDate = DateOnly.FromDateTime(DateTime.UtcNow),
                PlannedEndDate = DateOnly.FromDateTime(DateTime.UtcNow).AddMonths(12),
                Status = ProposalStatus.Submitted, SubmittedAt = DateTime.UtcNow
            };
            db.Proposals.Add(proposal);
            await db.SaveChangesAsync();
            return proposal;
        }

        var a = await Seed(piA.Id, "DT-A", "Đề tài A");
        var b = await Seed(piB.Id, "DT-B", "Đề tài B — gần giống A");
        var c = await Seed(piC.Id, "DT-C", "Đề tài C — khác hẳn");

        // Vector giả lập: A và B gần nhau (cosine cao), C vuông góc với cả hai (cosine 0).
        void AddVector(Guid proposalId, float[] vec) => db.SemanticSearchVectors.Add(new SemanticSearchVector
        {
            EntityType = FURPMS.Infrastructure.Services.DuplicateCheckService.VectorEntityType,
            EntityId = proposalId.ToString(),
            ContentHash = "h-" + proposalId,
            Embedding = VectorMath.Serialize(vec),
            Dimensions = vec.Length,
            ModelUsed = "test"
        });
        AddVector(a.Id, new float[] { 1f, 0f });
        AddVector(b.Id, new float[] { 0.99f, 0.14f });   // ~0.995 cosine với A
        AddVector(c.Id, new float[] { 0f, 1f });          // 0 cosine với A và B
        await db.SaveChangesAsync();

        return (db, staff.Id, a, b, c);
    }

    // ── Cờ hàng loạt cho màn danh sách ────────────────────────────────────
    [Fact]
    public async Task GetFlags_CapGanNhau_BaoCanXem_CapXaNhau_KhongBao()
    {
        var (db, _, a, b, c) = await SeedAsync();
        var svc = TestServices.DuplicateChecks(db);

        var flags = await svc.GetFlagsAsync(new[] { a.Id, b.Id, c.Id });

        Assert.True(flags[a.Id].Indexed);
        Assert.NotNull(flags[a.Id].MaxSeverity);   // A gần B → phải cảnh báo
        Assert.NotNull(flags[b.Id].MaxSeverity);

        // C ở xa cả A lẫn B (cosine 0) → không có gì vượt ngưỡng, MaxSeverity phải là null chứ
        // không phải "LOW" — cột trên danh sách chỉ vẽ badge khi có MaxSeverity, "không có gì để
        // báo" phải là một trạng thái rõ ràng.
        Assert.Null(flags[c.Id].MaxSeverity);
    }

    [Fact]
    public async Task GetFlags_ChuaVectorHoa_TraIndexedFalse()
    {
        var db = TestDbContextFactory.Create("test-" + Guid.NewGuid());
        var svc = TestServices.DuplicateChecks(db);

        var flags = await svc.GetFlagsAsync(new[] { Guid.NewGuid() });

        Assert.False(flags.Single().Value.Indexed);
    }

    [Fact]
    public async Task GetFlags_DanhSachRong_TraVeRong()
    {
        var db = TestDbContextFactory.Create("test-" + Guid.NewGuid());
        var svc = TestServices.DuplicateChecks(db);

        var flags = await svc.GetFlagsAsync(Array.Empty<Guid>());

        Assert.Empty(flags);
    }

    // ── Chốt kết luận phải báo cho chủ nhiệm ──────────────────────────────
    [Fact]
    public async Task ReviewAsync_ChotKetLuan_BaoChoChuNhiem()
    {
        var (db, staffId, a, _, _) = await SeedAsync();
        var svc = TestServices.DuplicateChecks(db);
        var piId = (await db.Projects.FindAsync(a.ProjectId))!.PiUserId;

        await svc.ReviewAsync(
            a.Id,
            new ReviewDuplicateRequest { Verdict = DuplicateVerdict.NeedsRevision, Note = "Trùng chủ đề với đề tài B, cần làm rõ khác biệt." },
            staffId, new[] { "Staff" });

        // Trước 27/08 kết luận chỉ nằm phía Staff — chủ nhiệm không hề biết đề cương mình có bị
        // đối chiếu, kể cả khi kết luận là "cần chỉnh sửa" và họ phải làm gì đó ngay.
        var sent = await db.Notifications.Where(n => n.UserId == piId).ToListAsync();
        Assert.Contains(sent, n => n.NotificationType == "DUPLICATE_CHECK_REVIEWED"
            && n.Body.Contains("cần chỉnh sửa"));
    }

    [Fact]
    public async Task ReviewAsync_KetLuanKhongTrung_VanBaoChoChuNhiem()
    {
        var (db, staffId, a, _, _) = await SeedAsync();
        var svc = TestServices.DuplicateChecks(db);
        var piId = (await db.Projects.FindAsync(a.ProjectId))!.PiUserId;

        await svc.ReviewAsync(
            a.Id, new ReviewDuplicateRequest { Verdict = DuplicateVerdict.NotDuplicate },
            staffId, new[] { "Staff" });

        // Minh bạch cả hai chiều: không chỉ báo tin xấu, "đã xem và không có vấn đề gì" cũng đáng
        // được báo — chủ nhiệm chưa từng thấy cảnh báo nên không tự suy ra được điều đó.
        Assert.Contains(await db.Notifications.Where(n => n.UserId == piId).ToListAsync(),
            n => n.NotificationType == "DUPLICATE_CHECK_REVIEWED");
    }

    [Fact]
    public async Task ReviewAsync_GhiVaoSoQuyetDinh()
    {
        var (db, staffId, a, _, _) = await SeedAsync();
        var svc = TestServices.DuplicateChecks(db);

        await svc.ReviewAsync(
            a.Id, new ReviewDuplicateRequest { Verdict = DuplicateVerdict.NotDuplicate },
            staffId, new[] { "Staff" });

        Assert.True(await db.ProjectDecisions.AnyAsync(
            d => d.ProjectId == a.ProjectId && d.DecisionType == DecisionTypes.DuplicateReviewed));
    }
}
