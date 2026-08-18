using FURPMS.Application.Constants;
using FURPMS.Application.Interfaces.Services;
using FURPMS.Domain.Entities.AI;
using FURPMS.Domain.Entities.Contracts;
using FURPMS.Domain.Entities.Cycles;
using FURPMS.Domain.Entities.Financial;
using FURPMS.Domain.Entities.MasterData;
using FURPMS.Domain.Entities.Progress;
using FURPMS.Domain.Entities.Projects;
using FURPMS.Domain.Entities.Proposals;
using FURPMS.Domain.Entities.Review;
using FURPMS.Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FURPMS.Infrastructure.Data;

/// <summary>
/// E7 — dữ liệu kịch bản demo. Mục đích KHÔNG phải "màn nào cũng có chữ", mà là
/// <b>mỗi bước của quy trình có sẵn một đề tài đứng ngay TRƯỚC bước đó</b>, để lúc demo bấm là
/// chạy thật, không mất 20 phút dựng tiền đề trước mỗi thao tác.
///
/// Data seed phải qua được chính các chốt chặn đã thêm 06–08/08, nếu không demo gãy tại chỗ:
/// hội đồng số LẺ 3–5 (xét duyệt) / 5–7 (nghiệm thu) · quorum 2/3 làm tròn lên · nghiệm thu phải
/// có phiếu của phản biện · bộ tiêu chí phải cộng đúng <c>MaxTotalScore</c> · kỳ báo cáo tuần tự ·
/// slot chấm nằm trong khung giờ họp và không chồng nhau · sản phẩm đã PASSED thì không nộp lại.
///
/// Chạy SAU <see cref="DatabaseSeeder"/>, idempotent theo <c>ProjectCode</c>, bật/tắt bằng
/// system setting <c>DEMO_DATA_ENABLED</c> để bản bàn giao thật không dính data giả.
/// </summary>
public class DemoScenarioSeeder
{
    /// <summary>
    /// Thư mục Drive dùng chung cho MỌI link minh chứng trong dữ liệu demo.
    /// <para>
    /// Trước đây mỗi chỗ ghi một đường dẫn bịa (<c>.../1demo-san-pham-nghiem-thu/view</c>) — bấm
    /// vào ra trang lỗi của Google, nên lúc demo trông như hệ thống hỏng. Trỏ hết về một thư mục
    /// CÓ THẬT thì mọi nút "Mở link" đều mở được. Đổi link demo chỉ cần sửa đúng một chỗ này.
    /// </para>
    /// </summary>
    private const string DemoEvidenceFolderUrl =
        "https://drive.google.com/drive/folders/1_upAOnWZKIvEZ44l-4BV-K7Xyzx3p88D?usp=sharing";

    private readonly FURPMSDbContext _db;
    private readonly IFileStorage _storage;
    private readonly IDocumentExportService _export;
    private readonly ILogger<DemoScenarioSeeder> _log;

    private readonly IHostEnvironment _env;

    public DemoScenarioSeeder(
        FURPMSDbContext db, IFileStorage storage, IDocumentExportService export,
        ILogger<DemoScenarioSeeder> log, IHostEnvironment env)
    {
        _env = env;
        _db = db;
        _storage = storage;
        _export = export;
        _log = log;
    }

    // Mã đề tài của kịch bản — cũng là khoá idempotent.
    private const string S1 = "NCKH-2026-001"; // nháp
    private const string S2 = "NCKH-2026-002"; // đã nộp, chưa vào vòng
    private const string S3 = "NCKH-2026-003"; // đang chấm  ⭐ màn chính
    private const string S4 = "NCKH-2026-004"; // yêu cầu sửa
    private const string S5 = "NCKH-2026-005"; // đã duyệt, chưa có hợp đồng
    private const string S6 = "NCKH-2026-006"; // đang thực hiện, báo cáo kỳ 1 chờ duyệt
    private const string S7 = "NCKH-2026-007"; // đang nghiệm thu
    private const string S8 = "NCKH-2026-008"; // hoàn thành
    private const string S9 = "NCKH-2026-009"; // vòng 1 đã chốt không đạt
    private const string S10 = "NCKH-2026-010"; // lời mời hội đồng chấm 2 đề tài
    private const string S11 = "NCKH-2026-011"; // lời mời hội đồng chấm 2 đề tài

    private const string DocxMime = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

    public async Task SeedAsync()
    {
        if (!await IsEnabledAsync()) return;
        var ctx = await LoadContextAsync();
        if (ctx == null) return; // seeder gốc chưa chạy xong — bỏ qua, lần khởi động sau làm lại

        await TryStepAsync("đề tài nháp", () => SeedDraftAsync(ctx));
        await TryStepAsync("đề tài đã nộp", () => SeedSubmittedAsync(ctx));
        await TryStepAsync("vòng xét duyệt + 3 đề tài", async () =>
        {
            var reviewRound = await GetOrCreateReviewRoundAsync(ctx);
            await SeedUnderReviewGroupAsync(ctx, reviewRound);
            await SeedPendingMultiProjectInvitationAsync(ctx, reviewRound);
        });
        await TryStepAsync("đề tài đang thực hiện", () => SeedInProgressAsync(ctx));
        await TryStepAsync("đề tài đang nghiệm thu", () => SeedAcceptanceAsync(ctx));
        await TryStepAsync("đề tài hoàn thành", () => SeedCompletedAsync(ctx));
        await TryStepAsync("file thuyết minh đính kèm", AttachProposalFilesAsync);
    }

    /// <summary>
    /// Mỗi kịch bản chạy độc lập: cái nào vướng dữ liệu sẵn có (trùng số hợp đồng, trùng mã đợt…)
    /// thì bỏ qua đúng cái đó, các kịch bản còn lại vẫn dựng. Dữ liệu demo **không đáng** để chặn
    /// ứng dụng khởi động — trên deploy thì cả API sập theo, FE mất luôn backend.
    /// Bỏ dở giữa chừng còn để lại bản ghi rác, nên gỡ luôn phần đã theo dõi trong bộ nhớ EF.
    /// </summary>
    private async Task TryStepAsync(string step, Func<Task> run)
    {
        // Mỗi kịch bản gọi SaveChanges nhiều lần (project → thành viên → hợp đồng → phiếu…), nên
        // không có giao dịch thì lỗi ở bước cuối vẫn để lại nửa đề tài đã ghi: lần khởi động sau
        // thấy mã đề tài đã tồn tại nên bỏ qua luôn, và cái nửa vời đó nằm lại vĩnh viễn.
        var useTransaction = _db.Database.IsRelational();
        var tx = useTransaction ? await _db.Database.BeginTransactionAsync() : null;
        try
        {
            await run();
            if (tx != null) await tx.CommitAsync();
        }
        catch (Exception ex)
        {
            if (tx != null) await tx.RollbackAsync();
            _db.ChangeTracker.Clear();
            _log.LogWarning(ex, "Bỏ qua phần dữ liệu demo \"{Step}\" vì lỗi — ứng dụng vẫn chạy bình thường.", step);
        }
        finally
        {
            if (tx != null) await tx.DisposeAsync();
        }
    }

    // ── Bối cảnh dùng chung ──────────────────────────────────────────────────

    private sealed record Ctx(
        User Admin, User Staff, User Pi1, User Pi2, List<User> Reviewers,
        OrganizationalUnit Unit, ProductCategory Category,
        ResearchType Applied, ResearchType Basic,
        CycleTrack OpenTrack, CycleTrack ClosedTrack,
        ResearchOrder OpenOrder, ResearchOrder ClosedOrder,
        RubricTemplate ReviewRubric);

    private async Task<Ctx?> LoadContextAsync()
    {
        var admin = await _db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Email == "admin@furpms.edu.vn");
        var staff = await _db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Email == "staff.demo@furpms.edu.vn");
        var pi1 = await _db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Email == "pi.demo@furpms.edu.vn");
        var pi2 = await _db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Email == "pi2.demo@furpms.edu.vn");
        var unit = await _db.OrganizationalUnits.FirstOrDefaultAsync(u => u.Code == "DEMO-UNIT");
        var applied = await _db.ResearchTypes.FirstOrDefaultAsync(t => t.Code == "APPLIED");
        var basic = await _db.ResearchTypes.FirstOrDefaultAsync(t => t.Code == "BASIC");
        var track = await _db.ResearchTracks.FirstOrDefaultAsync(t => t.Code == "AI");
        if (admin == null || staff == null || pi1 == null || pi2 == null ||
            unit == null || applied == null || basic == null || track == null)
            return null;

        var rubric = await GetOrCreateValidReviewRubricAsync();

        var reviewers = await LoadReviewersAsync();
        if (reviewers.Count < 5) return null;

        var category = await _db.ProductCategories.FirstOrDefaultAsync(c => c.Code == "ARTICLE");
        if (category == null)
        {
            category = new ProductCategory { Code = "ARTICLE", Name = "Bài báo khoa học", IsActive = true };
            _db.ProductCategories.Add(category);
            await _db.SaveChangesAsync();
        }

        // Rule #7: 1 đợt = đúng 1 loại đề tài ⇒ hai đợt độc lập, không gộp.
        var openCycle = await GetOrCreateCycleAsync(
            "UD26", applied.Id, admin.Id, CycleStatus.Open,
            "Đợt 2026 — Nghiên cứu ứng dụng (đang nhận đề cương)",
            openOffsetDays: -20, deadlineOffsetDays: 25);
        var closedCycle = await GetOrCreateCycleAsync(
            "CB26", basic.Id, admin.Id, CycleStatus.Closed,
            "Đợt 2026 — Nghiên cứu cơ bản (đã đóng nhận, đang xét duyệt)",
            openOffsetDays: -180, deadlineOffsetDays: -90);

        var openTrack = await GetOrCreateCycleTrackAsync(openCycle.Id, track.Id);
        var closedTrack = await GetOrCreateCycleTrackAsync(closedCycle.Id, track.Id);
        var openOrder = await GetOrCreateOrderAsync(openCycle.Id, unit.Id, admin.Id);
        var closedOrder = await GetOrCreateOrderAsync(closedCycle.Id, unit.Id, admin.Id);

        return new Ctx(admin, staff, pi1, pi2, reviewers, unit, category,
            applied, basic, openTrack, closedTrack, openOrder, closedOrder, rubric);
    }

    /// <summary>
    /// Không tin bộ tiêu chí có sẵn: cơ sở dữ liệu đang chạy (deploy) còn bộ **125 điểm** cũ, mà
    /// bộ không cộng đúng <c>MaxTotalScore</c> thì **không nộp phiếu chấm được** (chốt chặn A11)
    /// ⇒ vòng chấm của kịch bản demo sẽ gãy dù local chạy ngon. Nên chọn bộ REVIEW **cộng đủ**;
    /// không có thì tạo mới đúng BM03 (10+20+40+20+10 = "Cộng 100").
    /// Bộ cũ **giữ nguyên**, không sửa, không tắt — đó là dữ liệu của người dùng.
    /// </summary>
    private async Task<RubricTemplate> GetOrCreateValidReviewRubricAsync()
    {
        var templates = await _db.RubricTemplates
            .Where(t => t.TemplateType == "REVIEW" && t.IsActive)
            .ToListAsync();
        foreach (var t in templates)
        {
            var sum = await _db.RubricCriteria
                .Where(c => c.TemplateId == t.Id && c.IsActive)
                .SumAsync(c => (decimal?)c.MaxScore) ?? 0m;
            if (sum > 0 && sum == t.MaxTotalScore) return t;
        }

        var template = new RubricTemplate
        {
            TemplateType = "REVIEW",
            Name = "Phiếu đánh giá thẩm định đề cương (BM03)",
            MaxTotalScore = 100m,
            IsActive = true
        };
        _db.RubricTemplates.Add(template);
        await _db.SaveChangesAsync();

        _db.RubricCriteria.AddRange(
            new RubricCriterion { TemplateId = template.Id, CriterionName = "Mục đích, ý nghĩa khoa học và thực tiễn của đề tài", MaxScore = 10m, Sequence = 1, IsActive = true },
            new RubricCriterion { TemplateId = template.Id, CriterionName = "Phương pháp nghiên cứu",                              MaxScore = 20m, Sequence = 2, IsActive = true },
            new RubricCriterion { TemplateId = template.Id, CriterionName = "Nội dung nghiên cứu và kết quả dự kiến",              MaxScore = 40m, Sequence = 3, IsActive = true },
            new RubricCriterion { TemplateId = template.Id, CriterionName = "Năng lực của chủ nhiệm đề tài và nhóm nghiên cứu",    MaxScore = 20m, Sequence = 4, IsActive = true },
            new RubricCriterion { TemplateId = template.Id, CriterionName = "Tính hợp lý của dự toán kinh phí",                    MaxScore = 10m, Sequence = 5, IsActive = true }
        );
        await _db.SaveChangesAsync();
        return template;
    }

    private async Task<List<User>> LoadReviewersAsync()
    {
        var emails = new[]
        {
            "reviewer1.demo@furpms.edu.vn", "reviewer2.demo@furpms.edu.vn", "reviewer3.demo@furpms.edu.vn",
            "reviewer4.demo@furpms.edu.vn", "reviewer5.demo@furpms.edu.vn"
        };
        var found = await _db.Users.IgnoreQueryFilters()
            .Where(u => emails.Contains(u.Email))
            .ToListAsync();
        return emails.Select(e => found.FirstOrDefault(u => u.Email == e))
            .Where(u => u != null).Select(u => u!).ToList();
    }

    /// <summary>
    /// Có đổ dữ liệu demo không.
    ///
    /// <para>
    /// <b>Mặc định phụ thuộc MÔI TRƯỜNG</b>, không phải một hằng số:
    /// </para>
    /// <list type="bullet">
    ///   <item><b>Development</b> — bật. Máy dev cần sẵn 10 đề tài ở 10 bước quy trình để đi thử.</item>
    ///   <item><b>Mọi môi trường khác</b> — TẮT. Trước đây mặc định bật ở mọi nơi, nghĩa là lần
    ///   khởi động đầu tiên trên máy chủ là DB thật có ngay 10 đề tài giả, hợp đồng giả, tài khoản
    ///   giả — và không ai bấm gì sai cả, chỉ cần deploy.</item>
    /// </list>
    /// <para>
    /// Vẫn bật lại được trên máy chủ bằng cách đặt <c>DEMO_DATA_ENABLED = true</c> trong Cài đặt —
    /// hữu ích khi cần dựng bản demo cho hội đồng xem.
    /// </para>
    /// </summary>
    private async Task<bool> IsEnabledAsync()
    {
        var setting = await _db.SystemSettings
            .FirstOrDefaultAsync(s => s.Key == SystemSettingKeys.DemoDataEnabled);

        if (setting != null)
            return string.Equals(setting.Value?.Trim(), "true", StringComparison.OrdinalIgnoreCase);

        var enabled = _env.IsDevelopment();
        if (!enabled)
            _log.LogInformation(
                "Bỏ qua dữ liệu demo vì đang chạy ở môi trường {Env}. " +
                "Đặt DEMO_DATA_ENABLED = true trong Cài đặt nếu thực sự muốn đổ dữ liệu mẫu.",
                _env.EnvironmentName);
        return enabled;
    }

    private async Task<ResearchCycle> GetOrCreateCycleAsync(
        string semesterCode, int researchTypeId, Guid createdBy, string status,
        string description, int openOffsetDays, int deadlineOffsetDays)
    {
        var cycle = await _db.ResearchCycles.FirstOrDefaultAsync(c => c.SemesterCode == semesterCode);
        if (cycle != null) return cycle;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        cycle = new ResearchCycle
        {
            CycleYear = 2026,                       // E2: QĐ543 dùng NĂM DƯƠNG LỊCH, không có "năm học"
            SemesterCode = semesterCode,
            ResearchTypeId = researchTypeId,
            SubmissionOpenDate = today.AddDays(openOffsetDays),
            SubmissionDeadline = today.AddDays(deadlineOffsetDays),
            ReviewDeadline = today.AddDays(deadlineOffsetDays + 45),
            Status = status,
            Description = description,
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.ResearchCycles.Add(cycle);
        await _db.SaveChangesAsync();
        return cycle;
    }

    private async Task<CycleTrack> GetOrCreateCycleTrackAsync(int cycleId, int trackId)
    {
        var ct = await _db.CycleTracks.FirstOrDefaultAsync(x => x.CycleId == cycleId && x.TrackId == trackId);
        if (ct != null) return ct;
        ct = new CycleTrack { CycleId = cycleId, TrackId = trackId, IsOpen = true };
        _db.CycleTracks.Add(ct);
        await _db.SaveChangesAsync();
        return ct;
    }

    private async Task<ResearchOrder> GetOrCreateOrderAsync(int cycleId, int unitId, Guid createdBy)
    {
        var order = await _db.ResearchOrders.FirstOrDefaultAsync(o => o.CycleId == cycleId && o.IsDefault);
        if (order != null) return order;
        order = new ResearchOrder
        {
            CycleId = cycleId,
            OrderingUnitId = unitId,
            ResearchArea = "Nghiên cứu tự do (đề xuất của chủ nhiệm)",
            ProblemDescription = "Danh mục mặc định của đợt — gom các đề tài do chủ nhiệm tự đề xuất.",
            IsDefault = true,
            Status = "OPEN",
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow
        };
        _db.ResearchOrders.Add(order);
        await _db.SaveChangesAsync();
        return order;
    }

    // ── Dựng đề tài ──────────────────────────────────────────────────────────

    private sealed record Spec(
        string Code, string TitleVi, string TitleEn, string Abstract,
        string Objectives, string Methodology, string ExpectedOutput,
        int DurationMonths, decimal Budget, string[] Contents, string[] Products);

    private async Task<(Project Project, Proposal Proposal, List<ProposalActivity> Activities)> BuildAsync(
        Ctx ctx, Spec spec, User pi, CycleTrack cycleTrack, ResearchOrder order, ResearchType type,
        string projectStatus, string proposalStatus, DateOnly start, DateTime createdAt)
    {
        var project = new Project
        {
            Id = Guid.NewGuid(),
            ProjectCode = spec.Code,
            CycleTrackId = cycleTrack.Id,
            OrderId = order.Id,
            PiUserId = pi.Id,
            HostingUnitId = ctx.Unit.Id,
            ResearchTypeId = type.Id,
            TitleVi = spec.TitleVi,
            TitleEn = spec.TitleEn,
            Status = projectStatus,
            PlannedStartDate = start,
            PlannedEndDate = start.AddMonths(spec.DurationMonths),
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };
        _db.Projects.Add(project);

        var proposal = new Proposal
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            VersionNo = 1,
            IsCurrent = true,
            TitleVi = spec.TitleVi,
            TitleEn = spec.TitleEn,
            AbstractVi = spec.Abstract,
            ResearchObjectives = spec.Objectives,
            Methodology = spec.Methodology,
            ExpectedOutput = spec.ExpectedOutput,
            DurationMonths = spec.DurationMonths,
            PlannedStartDate = start,
            PlannedEndDate = start.AddMonths(spec.DurationMonths),
            FundingMethod = FundingMethod.Partial,
            Status = proposalStatus,
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };
        _db.Proposals.Add(proposal);
        await _db.SaveChangesAsync();

        _db.ProjectMembers.AddRange(
            new ProjectMember
            {
                ProjectId = project.Id, UserId = pi.Id, FullName = pi.FullName, AcademicTitle = "TS",
                UnitName = "Khoa Công nghệ Thông tin", WorkContent = "Chủ nhiệm đề tài, điều phối chung",
                WorkMonths = spec.DurationMonths, IsPi = true, MemberRoleCode = "CNNV",
                SalaryCoefficient = 0.79m, Sequence = 1
            },
            new ProjectMember
            {
                ProjectId = project.Id, FullName = "ThS. Trịnh Thu Trang", AcademicTitle = "ThS",
                UnitName = "Khoa Công nghệ Thông tin", WorkContent = "Thư ký khoa học, xử lý dữ liệu",
                WorkMonths = spec.DurationMonths - 2, IsSecretary = true, MemberRoleCode = "TKKH",
                SalaryCoefficient = 0.49m, Sequence = 2
            },
            new ProjectMember
            {
                ProjectId = project.Id, FullName = "KS. Ngô Bảo Long", AcademicTitle = "KS",
                UnitName = "Trung tâm Nghiên cứu ứng dụng", WorkContent = "Lập trình, triển khai hệ thống",
                WorkMonths = spec.DurationMonths - 4, MemberRoleCode = "TVC",
                SalaryCoefficient = 0.49m, Sequence = 3
            });

        await SeedBudgetAsync(proposal.Id, spec.Budget);

        var activities = new List<ProposalActivity>();
        var monthsPerContent = Math.Max(1, spec.DurationMonths / spec.Contents.Length);
        for (var i = 0; i < spec.Contents.Length; i++)
        {
            var content = new ProposalResearchContent
            {
                ProposalId = proposal.Id,
                ContentNumber = i + 1,
                Title = spec.Contents[i],
                Description = $"Nội dung {i + 1} của đề tài: {spec.Contents[i].ToLowerInvariant()}.",
                Sequence = i + 1
            };
            _db.ProposalResearchContents.Add(content);
            await _db.SaveChangesAsync();

            var activity = new ProposalActivity
            {
                ContentId = content.Id,
                ProposalId = proposal.Id,
                ActivityName = spec.Contents[i],
                ExpectedResult = $"Hoàn thành {spec.Contents[i].ToLowerInvariant()}, có báo cáo chuyên đề kèm theo.",
                StartMonth = i * monthsPerContent + 1,
                EndMonth = Math.Min(spec.DurationMonths, (i + 1) * monthsPerContent),
                Sequence = i + 1,
                EstimatedCost = Math.Round(spec.Budget / spec.Contents.Length, 0)
            };
            _db.ProposalActivities.Add(activity);
            activities.Add(activity);
        }

        for (var i = 0; i < spec.Products.Length; i++)
        {
            _db.ProjectDeliverables.Add(new ProjectDeliverable
            {
                ProjectId = project.Id,
                CategoryId = ctx.Category.Id,
                ProductName = spec.Products[i],
                ScientificRequirements = "Đầy đủ, chính xác, có giá trị khoa học và khả năng ứng dụng.",
                Sequence = i + 1
            });
        }
        await _db.SaveChangesAsync();

        return (project, proposal, activities);
    }

    // ── #1 — nháp: PI đang soạn dở ───────────────────────────────────────────
    // Demo: wizard nộp đề cương + upload file để AI trích xuất (cố ý KHÔNG đính file sẵn).
    private async Task SeedDraftAsync(Ctx ctx)
    {
        if (await ExistsAsync(S1)) return;
        var now = DateTime.UtcNow;
        await BuildAsync(ctx, new Spec(
            S1,
            "[TEST-NHÁP] Ứng dụng học sâu phát hiện đạo văn trong bài báo khoa học tiếng Việt",
            "Deep Learning for Plagiarism Detection in Vietnamese Scientific Papers",
            "Đề tài xây dựng công cụ phát hiện đạo văn cho bài báo khoa học tiếng Việt dựa trên biểu diễn ngữ nghĩa, khắc phục hạn chế của các công cụ đối sánh chuỗi hiện hành khi văn bản đã được diễn đạt lại.",
            "1. Xây dựng kho ngữ liệu bài báo khoa học tiếng Việt có gán nhãn đạo văn.\n2. Phát triển mô hình đối sánh ngữ nghĩa đạt độ chính xác ≥ 0,85.\n3. Triển khai công cụ kiểm tra dùng được cho Phòng QLKH.",
            "Học sâu với mô hình ngôn ngữ tiếng Việt (PhoBERT), đối sánh vector ngữ nghĩa, đánh giá trên tập kiểm thử do chuyên gia gán nhãn.",
            "01 bài báo hội nghị trong nước; 01 công cụ kiểm tra đạo văn tích hợp cổng nộp bài.",
            12, 148_000_000m,
            new[] { "Xây dựng kho ngữ liệu và gán nhãn", "Phát triển mô hình đối sánh ngữ nghĩa", "Triển khai và đánh giá công cụ" },
            new[] { "01 bài báo hội nghị trong nước", "Công cụ kiểm tra đạo văn" }),
            ctx.Pi1, ctx.OpenTrack, ctx.OpenOrder, ctx.Applied,
            ProjectStatus.Proposed, ProposalStatus.Draft,
            DateOnly.FromDateTime(now).AddMonths(2), now.AddDays(-3));
    }

    // ── #2 — đã nộp, chưa vào vòng nào ───────────────────────────────────────
    // Demo: Staff tạo vòng chấm rồi gán đề tài vào.
    private async Task SeedSubmittedAsync(Ctx ctx)
    {
        if (await ExistsAsync(S2)) return;
        var now = DateTime.UtcNow;
        var (_, proposal, _) = await BuildAsync(ctx, new Spec(
            S2,
            "[TEST-CHỜ VÀO VÒNG 1] Hệ thống giám sát chất lượng môi trường khuôn viên trường bằng cảm biến IoT",
            "IoT-based Campus Environment Monitoring System",
            "Đề tài thiết kế mạng cảm biến IoT giám sát nhiệt độ, độ ẩm, bụi mịn và tiếng ồn trong khuôn viên trường, cung cấp dữ liệu thời gian thực phục vụ công tác quản trị cơ sở vật chất.",
            "1. Thiết kế mạng cảm biến chi phí thấp phủ 6 điểm trọng yếu.\n2. Xây dựng nền tảng thu thập và cảnh báo thời gian thực.\n3. Đánh giá độ tin cậy số liệu so với thiết bị chuẩn.",
            "Thiết kế phần cứng dựa trên vi điều khiển ESP32, truyền dữ liệu MQTT, hiệu chuẩn cảm biến bằng thiết bị đo chuẩn.",
            "01 bài báo tạp chí trong nước; 01 hệ thống giám sát vận hành thực tế tại cơ sở Hoà Lạc.",
            12, 135_000_000m,
            new[] { "Thiết kế và chế tạo nút cảm biến", "Xây dựng nền tảng thu thập dữ liệu", "Hiệu chuẩn và đánh giá độ tin cậy" },
            new[] { "01 bài báo tạp chí trong nước", "Hệ thống giám sát môi trường", "Bộ số liệu quan trắc 6 tháng" }),
            ctx.Pi2, ctx.OpenTrack, ctx.OpenOrder, ctx.Applied,
            ProjectStatus.UnderReview, ProposalStatus.Submitted,
            DateOnly.FromDateTime(now).AddMonths(2), now.AddDays(-12));

        proposal.SubmittedAt = now.AddDays(-6);
        await _db.SaveChangesAsync();
    }

    // ── Vòng xét duyệt dùng chung cho #3, #4, #5 ─────────────────────────────
    private async Task<ReviewRound> GetOrCreateReviewRoundAsync(Ctx ctx)
    {
        var round = await _db.ReviewRounds
            .FirstOrDefaultAsync(r => r.CycleTrackId == ctx.ClosedTrack.Id && r.RoundType == "REVIEW");
        if (round != null) return round;

        round = new ReviewRound
        {
            Id = Guid.NewGuid(),
            CycleTrackId = ctx.ClosedTrack.Id,
            RoundNumber = 1,
            Dimension = ReviewRoundDimension.Science,
            RoundType = "REVIEW",
            RubricTemplateId = ctx.ReviewRubric.Id,   // rule #13: pin bộ tiêu chí tại thời điểm mở vòng
            Sequence = 1,
            Status = ReviewRoundStatus.Open,
            OpenedAt = DateTime.UtcNow.AddDays(-14)
        };
        _db.ReviewRounds.Add(round);
        await _db.SaveChangesAsync();
        return round;
    }

    /// <summary>
    /// #3 + #4 + #5 dùng CHUNG một hội đồng 5 người, một buổi họp, ba slot con — nếu mỗi hội đồng
    /// chỉ chấm 1 đề tài thì màn "lịch chấm" không có gì để xem.
    /// #3 để dở giữa chừng (4/5 phiếu, chưa có biên bản) ⇒ demo chấm → soạn biên bản → chốt chạy live.
    /// </summary>
    private async Task SeedUnderReviewGroupAsync(Ctx ctx, ReviewRound round)
    {
        if (await ExistsAsync(S3)) return;
        var now = DateTime.UtcNow;
        var start = new DateOnly(2026, 9, 1);

        var (p3, _, _) = await BuildAsync(ctx, new Spec(
            S3,
            "[TEST-VÒNG 1 THIẾU PHIẾU] Mô hình dự báo nguy cơ bỏ học của sinh viên bằng học máy tổ hợp",
            "Ensemble Machine Learning for Student Dropout Risk Prediction",
            "Đề tài xây dựng mô hình dự báo sớm nguy cơ bỏ học dựa trên dữ liệu học tập và tương tác của sinh viên, giúp cố vấn học tập can thiệp kịp thời thay vì xử lý khi đã quá muộn.",
            "1. Phân tích các yếu tố ảnh hưởng tới quyết định bỏ học.\n2. Xây dựng mô hình tổ hợp đạt AUC ≥ 0,85 trên dữ liệu thực.\n3. Đề xuất quy trình can thiệp sớm cho cố vấn học tập.",
            "Học máy tổ hợp (Random Forest, XGBoost, LightGBM), giải thích mô hình bằng SHAP, kiểm định chéo theo khoá học.",
            "01 bài báo tạp chí trong nước; 01 mô hình dự báo và bảng điều khiển cảnh báo sớm.",
            18, 98_000_000m,
            new[] { "Thu thập và làm sạch dữ liệu học tập", "Xây dựng và tinh chỉnh mô hình tổ hợp", "Xây dựng bảng điều khiển cảnh báo sớm" },
            new[] { "01 bài báo tạp chí trong nước", "Mô hình dự báo nguy cơ bỏ học", "Bảng điều khiển cảnh báo sớm" }),
            ctx.Pi1, ctx.ClosedTrack, ctx.ClosedOrder, ctx.Basic,
            ProjectStatus.UnderReview, ProposalStatus.Submitted, start, now.AddDays(-100));

        var (p4, prop4, _) = await BuildAsync(ctx, new Spec(
            S4,
            "[TEST-YÊU CẦU SỬA] Nền tảng học liệu thích ứng theo năng lực người học",
            "Adaptive Learning Material Platform",
            "Đề tài xây dựng nền tảng tự động điều chỉnh độ khó và thứ tự học liệu theo năng lực từng sinh viên, dựa trên lý thuyết ứng đáp câu hỏi kết hợp mô hình theo vết tri thức.",
            "1. Xây dựng ngân hàng học liệu có gán mức độ khó.\n2. Cài đặt thuật toán điều phối học liệu thích ứng.\n3. Thử nghiệm đối chứng trên 2 lớp học phần.",
            "Lý thuyết ứng đáp câu hỏi (IRT), mô hình theo vết tri thức (knowledge tracing), thử nghiệm đối chứng có nhóm chứng.",
            "01 bài báo hội nghị quốc tế; 01 nền tảng học liệu thích ứng.",
            12, 92_000_000m,
            new[] { "Xây dựng ngân hàng học liệu", "Cài đặt thuật toán điều phối thích ứng", "Thử nghiệm đối chứng" },
            new[] { "01 bài báo hội nghị quốc tế", "Nền tảng học liệu thích ứng" }),
            ctx.Pi1, ctx.ClosedTrack, ctx.ClosedOrder, ctx.Basic,
            ProjectStatus.UnderReview, ProposalStatus.RevisionRequired, start, now.AddDays(-100));

        var (p5, prop5, _) = await BuildAsync(ctx, new Spec(
            S5,
            "[TEST-ĐẠT VÒNG 1] Đánh giá chất lượng giảng dạy từ phản hồi sinh viên bằng xử lý ngôn ngữ tự nhiên",
            "NLP-based Teaching Quality Assessment from Student Feedback",
            "Đề tài xây dựng công cụ phân tích tự động hàng chục nghìn phản hồi mở của sinh viên mỗi học kỳ, trích xuất khía cạnh và mức độ hài lòng thay cho việc đọc thủ công.",
            "1. Xây dựng bộ dữ liệu phản hồi có gán nhãn khía cạnh.\n2. Phát triển mô hình phân tích cảm xúc theo khía cạnh đạt F1 ≥ 0,80.\n3. Xây dựng báo cáo tự động cho lãnh đạo khoa.",
            "Phân tích cảm xúc theo khía cạnh (ABSA) trên mô hình ngôn ngữ tiếng Việt, đánh giá bằng đối chiếu với chuyên gia.",
            "01 bài báo tạp chí trong nước; 01 công cụ phân tích phản hồi tích hợp hệ thống khảo sát.",
            12, 88_000_000m,
            new[] { "Xây dựng bộ dữ liệu phản hồi có gán nhãn", "Phát triển mô hình phân tích theo khía cạnh", "Xây dựng báo cáo tự động" },
            new[] { "01 bài báo tạp chí trong nước", "Công cụ phân tích phản hồi sinh viên" }),
            ctx.Pi2, ctx.ClosedTrack, ctx.ClosedOrder, ctx.Basic,
            ProjectStatus.Approved, ProposalStatus.Approved, start, now.AddDays(-100));

        var (p9, prop9, _) = await BuildAsync(ctx, new Spec(
            S9,
            "[TEST-KHÔNG ĐẠT VÒNG 1] Phân loại rác tái chế bằng thị giác máy tính",
            "Computer Vision for Recyclable Waste Classification",
            "Đề tài thử nghiệm phân loại rác tái chế từ ảnh chụp tại điểm thu gom trong khuôn viên.",
            "1. Thu thập dữ liệu ảnh.\n2. Huấn luyện mô hình phân loại.\n3. Đánh giá trong môi trường thực tế.",
            "Học sâu cho phân loại ảnh và đánh giá chéo trên dữ liệu thực địa.",
            "01 bộ dữ liệu ảnh; 01 mô hình thử nghiệm.",
            12, 75_000_000m,
            new[] { "Thu thập dữ liệu", "Huấn luyện mô hình", "Đánh giá thực địa" },
            new[] { "Bộ dữ liệu ảnh", "Mô hình thử nghiệm" }),
            ctx.Pi2, ctx.ClosedTrack, ctx.ClosedOrder, ctx.Basic,
            ProjectStatus.Cancelled, ProposalStatus.Rejected, start, now.AddDays(-100));

        foreach (var p in new[] { p3, p4, p5, p9 })
            _db.ProjectRounds.Add(new ProjectRound { ProjectId = p.Id, RoundId = round.Id, Status = "PENDING" });

        prop4.SubmittedAt = now.AddDays(-80);
        prop4.ReviewedAt = now.AddDays(-20);
        prop4.RevisionRequestedAt = now.AddDays(-20);
        prop4.RevisionDeadline = now.AddDays(10);
        prop5.SubmittedAt = now.AddDays(-80);
        prop5.ReviewedAt = now.AddDays(-20);
        prop5.ApprovedAt = now.AddDays(-20);
        prop5.ApprovedBy = ctx.Admin.Id;
        prop9.SubmittedAt = now.AddDays(-80);
        prop9.ReviewedAt = now.AddDays(-20);
        var prop3 = await _db.Proposals.FirstAsync(x => x.ProjectId == p3.Id);
        prop3.SubmittedAt = now.AddDays(-80);
        await _db.SaveChangesAsync();

        // Hội đồng xét duyệt: 5 người (Điều 8.2 cho 3–5, thầy chốt phải LẺ để có chênh lệch phiếu).
        var council = await CreateCouncilAsync(ctx, round, "REVIEW", minMembers: 3, maxMembers: 5,
            meetingTitle: "Họp Hội đồng xét duyệt đề cương — đợt Cơ bản 2026",
            location: "Phòng họp A203, Toà Alpha, Cơ sở Hoà Lạc",
            scheduledAt: now.Date.AddDays(2).AddHours(2), durationMinutes: 180);

        // Cache bảo hiểm cho buổi demo. Nút "Chạy lại" vẫn gọi Gemini thật và ghi đè khi thành công.
        await SeedReviewerAiCacheAsync(prop3.Id, council.Council.Id, ctx.ReviewRubric.Id, now);

        // Slot con theo từng đề tài — phải NẰM TRONG khung giờ họp và không chồng nhau.
        var slot = council.Meeting.ScheduledAt;
        var order = 1;
        foreach (var p in new[] { p3, p4, p5, p9 })
        {
            _db.CouncilProjectAssignments.Add(new CouncilProjectAssignment
            {
                CouncilId = council.Council.Id,
                ProjectId = p.Id,
                MeetingId = council.Meeting.Id,
                SlotStartAt = slot,
                SlotDurationMinutes = 45,
                SlotOrder = order++
            });
            slot = slot.AddMinutes(45);
        }
        await _db.SaveChangesAsync();

        // #3 — 4/5 phiếu (đủ quorum 2/3, thiếu đúng phiếu Chủ tịch để demo chấm live), CHƯA có biên bản.
        await AddScoreBallotsAsync(ctx, council.Council, council.Members, p3.Id,
            skipMemberIndexes: new[] { 0 }, baseQuality: 0.86m, submittedAt: now.AddDays(-2));

        // #4 — đủ 5 phiếu, biên bản đã chốt với kết quả "yêu cầu chỉnh sửa".
        await AddScoreBallotsAsync(ctx, council.Council, council.Members, p4.Id,
            skipMemberIndexes: Array.Empty<int>(), baseQuality: 0.62m, submittedAt: now.AddDays(-22));
        await AddDecisionAsync(council.Council, council.Members, p4.Id, ReviewResult.RevisionRequired,
            "Hội đồng đánh giá hướng nghiên cứu phù hợp nhưng phần thử nghiệm đối chứng còn sơ sài, dự toán kinh phí thuê khoán chuyên môn chưa thuyết minh rõ.",
            "Bổ sung thiết kế thử nghiệm đối chứng và thuyết minh lại mục thuê khoán chuyên môn trước khi nộp lại.",
            now.AddDays(-20));
        await SetProjectRoundAsync(p4.Id, round.Id, "REVISION", ReviewResult.RevisionRequired, now.AddDays(-20));

        // #5 — đủ 5 phiếu, biên bản đã chốt "đạt" ⇒ sẵn sàng lập hợp đồng.
        await AddScoreBallotsAsync(ctx, council.Council, council.Members, p5.Id,
            skipMemberIndexes: Array.Empty<int>(), baseQuality: 0.88m, submittedAt: now.AddDays(-22));
        await AddDecisionAsync(council.Council, council.Members, p5.Id, ReviewResult.Approved,
            "Đề tài có ý nghĩa thực tiễn rõ ràng, phương pháp phù hợp, sản phẩm khả thi. Hội đồng nhất trí thông qua.",
            "Rà soát lại tiến độ nội dung 3 cho khớp thời gian thực hiện.",
            now.AddDays(-20));
        await SetProjectRoundAsync(p5.Id, round.Id, "PASSED", ReviewResult.Approved, now.AddDays(-20));

        // #9 — đủ phiếu và đã chốt KHÔNG ĐẠT, dùng để thử bộ lọc trạng thái và lịch sử ca xấu.
        await AddScoreBallotsAsync(ctx, council.Council, council.Members, p9.Id,
            skipMemberIndexes: Array.Empty<int>(), baseQuality: 0.42m, submittedAt: now.AddDays(-22));
        await AddDecisionAsync(council.Council, council.Members, p9.Id, ReviewResult.Rejected,
            "Hội đồng kết luận dữ liệu thử nghiệm chưa đại diện và phương pháp đánh giá chưa chứng minh được tính khả thi.",
            "Xây dựng lại thiết kế nghiên cứu và nộp trong đợt phù hợp khác nếu tiếp tục triển khai.",
            now.AddDays(-20));
        await SetProjectRoundAsync(p9.Id, round.Id, ReviewRoundStatus.Failed, ReviewResult.Rejected, now.AddDays(-20));
    }

    // ── #10 + #11 — MỘT lời mời hội đồng, phạm vi gồm HAI đề tài ─────────────
    // Reviewer phải thấy cả hai tên trước khi nhận lời. Sau khi nhận, API my-memberships trả hai dòng
    // công việc dùng chung memberId; đây chính là ca từng bị FirstOrDefault() nuốt mất đề tài thứ hai.
    private async Task SeedPendingMultiProjectInvitationAsync(Ctx ctx, ReviewRound round)
    {
        if (await ExistsAsync(S10) || await ExistsAsync(S11)) return;
        var now = DateTime.UtcNow;
        var start = new DateOnly(2026, 10, 1);

        var (p10, prop10, _) = await BuildAsync(ctx, new Spec(
            S10,
            "[TEST-LỜI MỜI 2 ĐỀ TÀI-A] Phát hiện phòng học sử dụng điện bất thường",
            "Detecting Abnormal Classroom Energy Usage",
            "Đề tài xây dựng mô hình phát hiện bất thường từ dữ liệu công tơ thông minh của phòng học.",
            "1. Chuẩn hoá dữ liệu.\n2. Xây dựng mô hình.\n3. Đánh giá cảnh báo.",
            "Phát hiện bất thường trên chuỗi thời gian.",
            "01 mô hình cảnh báo và báo cáo thử nghiệm.",
            12, 82_000_000m,
            new[] { "Chuẩn hoá dữ liệu", "Xây dựng mô hình", "Đánh giá cảnh báo" },
            new[] { "Mô hình cảnh báo", "Báo cáo thử nghiệm" }),
            ctx.Pi1, ctx.ClosedTrack, ctx.ClosedOrder, ctx.Basic,
            ProjectStatus.UnderReview, ProposalStatus.Submitted, start, now.AddDays(-40));

        var (p11, prop11, _) = await BuildAsync(ctx, new Spec(
            S11,
            "[TEST-LỜI MỜI 2 ĐỀ TÀI-B] Trợ lý hỏi đáp quy chế đào tạo",
            "Academic Regulation Question Answering Assistant",
            "Đề tài xây dựng trợ lý tra cứu quy chế đào tạo có dẫn nguồn cho sinh viên và cán bộ.",
            "1. Chuẩn hoá văn bản.\n2. Xây dựng hệ hỏi đáp.\n3. Đánh giá độ chính xác.",
            "Truy hồi tăng cường sinh và đánh giá bởi chuyên gia.",
            "01 bộ dữ liệu và 01 trợ lý hỏi đáp thử nghiệm.",
            12, 86_000_000m,
            new[] { "Chuẩn hoá văn bản", "Xây dựng hệ hỏi đáp", "Đánh giá chuyên gia" },
            new[] { "Bộ dữ liệu", "Trợ lý hỏi đáp" }),
            ctx.Pi2, ctx.ClosedTrack, ctx.ClosedOrder, ctx.Basic,
            ProjectStatus.UnderReview, ProposalStatus.Submitted, start, now.AddDays(-40));

        prop10.SubmittedAt = now.AddDays(-35);
        prop11.SubmittedAt = now.AddDays(-35);
        _db.ProjectRounds.AddRange(
            new ProjectRound { ProjectId = p10.Id, RoundId = round.Id, Status = ReviewRoundStatus.Pending },
            new ProjectRound { ProjectId = p11.Id, RoundId = round.Id, Status = ReviewRoundStatus.Pending });
        await _db.SaveChangesAsync();

        var council = await CreateCouncilAsync(ctx, round, "REVIEW", minMembers: 3, maxMembers: 5,
            meetingTitle: "[TEST] Hội đồng nhận một lời mời chấm hai đề tài",
            location: "Phòng họp A205, Toà Alpha, Cơ sở Hoà Lạc",
            scheduledAt: now.Date.AddDays(6).AddHours(2), durationMinutes: 120,
            memberStatus: CouncilMemberStatus.Invited);

        var slot = council.Meeting.ScheduledAt;
        foreach (var (project, order) in new[] { (p10, 1), (p11, 2) })
        {
            _db.CouncilProjectAssignments.Add(new CouncilProjectAssignment
            {
                CouncilId = council.Council.Id,
                ProjectId = project.Id,
                MeetingId = council.Meeting.Id,
                SlotStartAt = slot.AddMinutes((order - 1) * 60),
                SlotDurationMinutes = 60,
                SlotOrder = order
            });
        }
        await _db.SaveChangesAsync();
    }

    // ── #6 — hợp đồng đang chạy, báo cáo kỳ 1 đã nộp CHƯA duyệt ──────────────
    // Demo: Staff duyệt báo cáo tiến độ · PI xin gia hạn.
    private async Task SeedInProgressAsync(Ctx ctx)
    {
        if (await ExistsAsync(S6)) return;
        var now = DateTime.UtcNow;
        var start = new DateOnly(2026, 3, 1);

        var (project, proposal, activities) = await BuildAsync(ctx, new Spec(
            S6,
            "[TEST-TIẾN ĐỘ CHỜ DUYỆT] Tối ưu hoá lịch thi học kỳ bằng thuật toán di truyền",
            "Exam Timetabling Optimization using Genetic Algorithms",
            "Đề tài xây dựng bộ giải bài toán xếp lịch thi nhiều ràng buộc, giảm thời gian lập lịch thủ công của Phòng Khảo thí từ vài ngày xuống dưới một giờ.",
            "1. Mô hình hoá ràng buộc lịch thi thực tế của Trường.\n2. Cài đặt thuật toán di truyền lai tìm kiếm cục bộ.\n3. So sánh với lịch thi lập thủ công trên 2 học kỳ.",
            "Thuật toán di truyền kết hợp tìm kiếm cục bộ, đánh giá bằng hàm phạt trên tập ràng buộc cứng/mềm.",
            "01 bài báo hội nghị trong nước; 01 phần mềm xếp lịch thi.",
            12, 95_000_000m,
            new[] { "Mô hình hoá ràng buộc lịch thi", "Cài đặt thuật toán di truyền lai", "Thử nghiệm và so sánh với lịch thủ công" },
            new[] { "01 bài báo hội nghị trong nước", "Phần mềm xếp lịch thi" }),
            ctx.Pi1, ctx.ClosedTrack, ctx.ClosedOrder, ctx.Basic,
            ProjectStatus.InProgress, ProposalStatus.Approved, start, now.AddDays(-200));

        proposal.SubmittedAt = now.AddDays(-190);
        proposal.ReviewedAt = now.AddDays(-160);
        proposal.ApprovedAt = now.AddDays(-160);
        proposal.ApprovedBy = ctx.Admin.Id;

        var contract = await CreateContractAsync(ctx, project, "HĐ-2026-006", 95_000_000m,
            start, start.AddMonths(12), ContractStatus.Active, now.AddDays(-150), maxExtensionMonths: 6);

        await AttachDeliverablesToContractAsync(project.Id, contract, start.AddMonths(6), start.AddMonths(12));

        // Kỳ 1 đã nộp, CHƯA đánh giá — Staff bấm duyệt ngay trên sân khấu.
        var report = new ProgressReport
        {
            ContractId = contract.Id,
            ReportRound = 1,
            RoundName = "Kỳ 1 — 6 tháng đầu",
            ReportingPeriodStart = start,
            ReportingPeriodEnd = start.AddMonths(6),
            CompletedContent = "Đã mô hình hoá đầy đủ ràng buộc lịch thi của Trường và cài đặt phiên bản đầu của thuật toán di truyền. Bộ giải chạy được trên dữ liệu học kỳ Thu 2026 với 412 học phần.",
            PendingContent = "Chưa hoàn thiện phần tìm kiếm cục bộ; thời gian chạy còn 22 phút, mục tiêu dưới 10 phút.",
            OverallCompletionPct = 45m,
            ExpenditureToDate = 38_000_000m,
            NextPeriodPlan = "Tối ưu hiệu năng, chạy đối chứng trên học kỳ Xuân 2027 và viết bài báo hội nghị.",
            PiRecommendations = "Đề nghị Phòng Khảo thí cung cấp thêm dữ liệu lịch sử 2 học kỳ để mở rộng tập kiểm thử.",
            ReportFileUrl = DemoEvidenceFolderUrl,
            SubmittedAt = now.AddDays(-5),
            Status = ProgressReportStatus.Submitted,
            DueDate = DateOnly.FromDateTime(now).AddDays(-2),
            CreatedAt = now.AddDays(-8),
            UpdatedAt = now.AddDays(-5)
        };
        _db.ProgressReports.Add(report);
        await _db.SaveChangesAsync();

        var rates = new[] { 60m, 45m, 20m };
        for (var i = 0; i < activities.Count; i++)
        {
            _db.ProgressReportItems.Add(new ProgressReportItem
            {
                ReportId = report.Id,
                ActivityId = activities[i].Id,
                CompletionRate = rates[Math.Min(i, rates.Length - 1)],
                CompletionStatus = i == 0 ? "IN_PROGRESS" : i == 1 ? "IN_PROGRESS" : "NOT_STARTED",
                EvidenceDescription = i == 0
                    ? "Tài liệu đặc tả ràng buộc, biên bản làm việc với Phòng Khảo thí."
                    : "Mã nguồn bộ giải trên kho lưu trữ nội bộ."
            });
        }
        await _db.SaveChangesAsync();
    }

    // ── #7 — sản phẩm đã nộp, vòng NGHIỆM THU đang mở ────────────────────────
    // Demo: hội đồng nghiệm thu bỏ phiếu Đạt/Không đạt (BM11) + hồ sơ nghiệm thu.
    private async Task SeedAcceptanceAsync(Ctx ctx)
    {
        if (await ExistsAsync(S7)) return;
        var now = DateTime.UtcNow;
        var start = new DateOnly(2025, 9, 1);

        var (project, proposal, activities) = await BuildAsync(ctx, new Spec(
            S7,
            "[TEST-NGHIỆM THU THIẾU PHIẾU] Nhận dạng chữ viết tay tiếng Việt trên biểu mẫu hành chính",
            "Vietnamese Handwriting Recognition on Administrative Forms",
            "Đề tài xây dựng mô hình nhận dạng chữ viết tay tiếng Việt có dấu trên biểu mẫu hành chính, phục vụ số hoá hồ sơ giấy đang tồn đọng của các phòng ban.",
            "1. Xây dựng tập dữ liệu chữ viết tay tiếng Việt trên biểu mẫu thực tế.\n2. Phát triển mô hình nhận dạng đạt độ chính xác ký tự ≥ 0,92.\n3. Tích hợp vào quy trình số hoá hồ sơ.",
            "Mạng nơ-ron tích chập kết hợp mô hình chuỗi (CRNN + CTC), tăng cường dữ liệu, đánh giá trên tập biểu mẫu thật.",
            "01 bài báo tạp chí trong nước; 01 mô hình nhận dạng và công cụ số hoá biểu mẫu.",
            12, 100_000_000m,
            new[] { "Xây dựng tập dữ liệu chữ viết tay", "Huấn luyện mô hình nhận dạng", "Tích hợp công cụ số hoá biểu mẫu" },
            new[] { "01 bài báo tạp chí trong nước", "Mô hình nhận dạng chữ viết tay", "Công cụ số hoá biểu mẫu" }),
            ctx.Pi1, ctx.ClosedTrack, ctx.ClosedOrder, ctx.Basic,
            ProjectStatus.Acceptance, ProposalStatus.Approved, start, now.AddDays(-380));

        proposal.SubmittedAt = now.AddDays(-370);
        proposal.ReviewedAt = now.AddDays(-350);
        proposal.ApprovedAt = now.AddDays(-350);
        proposal.ApprovedBy = ctx.Admin.Id;

        var contract = await CreateContractAsync(ctx, project, "HĐ-2025-007", 100_000_000m,
            start, start.AddMonths(12), ContractStatus.Active, now.AddDays(-340), maxExtensionMonths: 6);
        var deliverables = await AttachDeliverablesToContractAsync(
            project.Id, contract, start.AddMonths(6), start.AddMonths(12));

        // Sản phẩm ĐÃ NỘP nhưng để PENDING — sản phẩm đã PASSED thì không nộp lại được,
        // demo "nộp/đánh giá sản phẩm" sẽ tắc.
        foreach (var d in deliverables)
        {
            d.SubmittedAt = now.AddDays(-20);
            d.FileUrl = DemoEvidenceFolderUrl;
            d.IsCompleted = true;
            d.AcceptanceStatus = AcceptanceStatus.Pending;
        }

        // Kỳ 1 đã đánh giá — kỳ báo cáo phải tuần tự, kỳ trước chưa duyệt thì kỳ sau không nộp được.
        var report = new ProgressReport
        {
            ContractId = contract.Id,
            ReportRound = 1,
            RoundName = "Kỳ 1 — 6 tháng đầu",
            ReportingPeriodStart = start,
            ReportingPeriodEnd = start.AddMonths(6),
            CompletedContent = "Hoàn thành thu thập 12.400 mẫu chữ viết tay trên 8 loại biểu mẫu và huấn luyện mô hình cơ sở.",
            OverallCompletionPct = 55m,
            ExpenditureToDate = 50_000_000m,
            NextPeriodPlan = "Tinh chỉnh mô hình và tích hợp công cụ số hoá.",
            SubmittedAt = now.AddDays(-180),
            Status = ProgressReportStatus.Evaluated,
            EvaluatedBy = ctx.Staff.Id,
            EvaluatedAt = now.AddDays(-175),
            EvaluationResult = "PASS",
            EvaluationComments = "Tiến độ đạt yêu cầu, số liệu minh chứng đầy đủ.",
            CreatedAt = now.AddDays(-185),
            UpdatedAt = now.AddDays(-175)
        };
        _db.ProgressReports.Add(report);
        await _db.SaveChangesAsync();

        foreach (var a in activities)
        {
            _db.ProgressReportItems.Add(new ProgressReportItem
            {
                ReportId = report.Id, ActivityId = a.Id, CompletionRate = 55m,
                CompletionStatus = "IN_PROGRESS", EvidenceDescription = "Báo cáo chuyên đề và mã nguồn kèm theo."
            });
        }

        _db.FinalReports.Add(new FinalReport
        {
            ProjectId = project.Id,
            ReportFileUrl = DemoEvidenceFolderUrl,
            Language = "VI",
            SubmittedAt = now.AddDays(-18),
            Deadline = DateOnly.FromDateTime(now).AddDays(12),
            Status = FinalReportStatus.Submitted
        });
        await _db.SaveChangesAsync();

        // LỊCH SỬ VÒNG 1 trước đã — đề tài không thể tới nghiệm thu mà chưa qua xét duyệt.
        await SeedPriorReviewHistoryAsync(ctx, project, await GetOrCreateReviewRoundAsync(ctx),
            approvedAt: now.AddDays(-350), meetingRoom: "Phòng họp A302, Toà Alpha, Cơ sở Hoà Lạc");

        // Vòng nghiệm thu + hội đồng 5 người (Điều 12.2 cho 5–7), có phản biện.
        var round = new ReviewRound
        {
            Id = Guid.NewGuid(),
            CycleTrackId = ctx.ClosedTrack.Id,
            RoundNumber = 2,
            Dimension = ReviewRoundDimension.Science,
            RoundType = "ACCEPTANCE",
            Sequence = 2,
            Status = ReviewRoundStatus.Open,
            OpenedAt = now.AddDays(-10)
        };
        _db.ReviewRounds.Add(round);
        await _db.SaveChangesAsync();

        _db.ProjectRounds.Add(new ProjectRound { ProjectId = project.Id, RoundId = round.Id, Status = "PENDING" });
        await _db.SaveChangesAsync();

        var council = await CreateCouncilAsync(ctx, round, "ACCEPTANCE", minMembers: 5, maxMembers: 7,
            meetingTitle: "Họp Hội đồng nghiệm thu đề tài cấp Trường",
            location: "Phòng họp B105, Toà Beta, Cơ sở Hoà Lạc",
            scheduledAt: now.Date.AddDays(3).AddHours(2), durationMinutes: 120);

        _db.CouncilProjectAssignments.Add(new CouncilProjectAssignment
        {
            CouncilId = council.Council.Id,
            ProjectId = project.Id,
            MeetingId = council.Meeting.Id,
            SlotStartAt = council.Meeting.ScheduledAt,
            SlotDurationMinutes = 90,
            SlotOrder = 1
        });

        // 3/5 phiếu Đạt — trong đó CÓ phiếu của phản biện (Điều 12.3.b). Còn thiếu 1 phiếu nữa
        // mới đủ quorum 4/5 ⇒ demo bỏ phiếu live rồi mới soạn/chốt biên bản được.
        var voters = new[] { council.Members[2], council.Members[3], council.Members[4] }; // phản biện + 2 thành viên
        foreach (var m in voters)
        {
            _db.AcceptanceEvaluations.Add(new AcceptanceEvaluation
            {
                CouncilId = council.Council.Id,
                ProjectId = project.Id,
                EvaluatorMemberId = m.Id,
                Result = EvaluationResult.Pass,
                IsValidBallot = true,
                SubmittedAt = now.AddDays(-1)
            });
        }
        await _db.SaveChangesAsync();
    }

    // ── #8 — hoàn thành trọn vòng đời ────────────────────────────────────────
    // Demo: timeline mốc, giải ngân đủ đợt, quyết toán — trả lời "xong rồi thì nhìn thế nào".
    private async Task SeedCompletedAsync(Ctx ctx)
    {
        if (await ExistsAsync(S8)) return;
        var now = DateTime.UtcNow;
        var start = new DateOnly(2025, 3, 1);

        var (project, proposal, activities) = await BuildAsync(ctx, new Spec(
            S8,
            "[TEST-HOÀN THÀNH] Hệ thống khuyến nghị môn học tự chọn theo lộ trình nghề nghiệp",
            "Career-oriented Elective Course Recommendation System",
            "Đề tài xây dựng hệ thống gợi ý môn tự chọn dựa trên mục tiêu nghề nghiệp và kết quả học tập, giảm tình trạng sinh viên chọn môn theo cảm tính rồi phải học lại.",
            "1. Mô hình hoá lộ trình nghề nghiệp và bản đồ năng lực môn học.\n2. Xây dựng thuật toán khuyến nghị lai.\n3. Triển khai thử nghiệm cho 3 chuyên ngành.",
            "Hệ khuyến nghị lai (lọc cộng tác + dựa trên nội dung), đánh giá offline bằng NDCG và khảo sát người dùng.",
            "01 bài báo tạp chí trong nước; 01 hệ thống khuyến nghị tích hợp cổng đăng ký môn học.",
            12, 96_000_000m,
            new[] { "Xây dựng bản đồ năng lực môn học", "Phát triển thuật toán khuyến nghị lai", "Triển khai và khảo sát người dùng" },
            new[] { "01 bài báo tạp chí trong nước", "Hệ thống khuyến nghị môn học" }),
            ctx.Pi2, ctx.ClosedTrack, ctx.ClosedOrder, ctx.Basic,
            ProjectStatus.Completed, ProposalStatus.Approved, start, now.AddDays(-560));

        proposal.SubmittedAt = now.AddDays(-550);
        proposal.ReviewedAt = now.AddDays(-530);
        proposal.ApprovedAt = now.AddDays(-530);
        proposal.ApprovedBy = ctx.Admin.Id;

        var contract = await CreateContractAsync(ctx, project, "HĐ-2025-008", 96_000_000m,
            start, start.AddMonths(12), ContractStatus.Active, now.AddDays(-520), maxExtensionMonths: 6);
        var deliverables = await AttachDeliverablesToContractAsync(
            project.Id, contract, start.AddMonths(6), start.AddMonths(12));

        foreach (var d in deliverables)
        {
            d.SubmittedAt = now.AddDays(-200);
            d.FileUrl = DemoEvidenceFolderUrl;
            d.TrialEvidenceUrl = DemoEvidenceFolderUrl;
            d.IsCompleted = true;
            d.AcceptanceStatus = AcceptanceStatus.Passed;
            d.QualityAssessment = "Đạt yêu cầu khoa học, đã được hội đồng nghiệm thu thông qua.";
        }

        // Giải ngân đủ đợt, tất cả đã có minh chứng (rule #15: chỉ theo dõi mốc, không quản tiền).
        var disbursements = await _db.ContractDisbursements
            .Where(d => d.ContractId == contract.Id).OrderBy(d => d.RoundNumber).ToListAsync();
        // Ngày chi phải NẰM SAU ngày điều kiện được thoả: đợt cuối gắn với nghiệm thu nên đặt ở cuối
        // hợp đồng. Trước đây rải theo công thức cố định nên đề tài cơ bản (1 đợt "sau nghiệm thu")
        // lại hiện ngày chi giữa kỳ — mở timeline ra là thấy sai thứ tự ngay.
        var acceptedAt = new DateTime(contract.EndDate.Year, contract.EndDate.Month, contract.EndDate.Day,
            0, 0, 0, DateTimeKind.Utc).AddDays(-20);
        var signedAt = contract.SignedAt ?? acceptedAt.AddMonths(-12);
        var span = (acceptedAt - signedAt).TotalDays;
        foreach (var d in disbursements)
        {
            d.Status = DisbursementStatus.Disbursed;
            d.ActualAmount = d.PlannedAmount;
            d.DisbursedAt = disbursements.Count == 1
                ? acceptedAt
                : signedAt.AddDays(span * (d.RoundNumber - 1) / (disbursements.Count - 1));
            d.BankReference = $"FT2025{d.RoundNumber:00}00{d.RoundNumber}";
        }

        for (var round = 1; round <= 2; round++)
        {
            var report = new ProgressReport
            {
                ContractId = contract.Id,
                ReportRound = round,
                RoundName = round == 1 ? "Kỳ 1 — 6 tháng đầu" : "Kỳ 2 — 6 tháng cuối",
                ReportingPeriodStart = start.AddMonths((round - 1) * 6),
                ReportingPeriodEnd = start.AddMonths(round * 6),
                CompletedContent = round == 1
                    ? "Hoàn thành bản đồ năng lực môn học cho 3 chuyên ngành và bản thử nghiệm thuật toán khuyến nghị."
                    : "Hoàn thành triển khai hệ thống, khảo sát 620 sinh viên, hoàn thiện bài báo tạp chí.",
                OverallCompletionPct = round == 1 ? 50m : 100m,
                ExpenditureToDate = round == 1 ? 48_000_000m : 96_000_000m,
                SubmittedAt = now.AddDays(round == 1 ? -380 : -210),
                Status = ProgressReportStatus.Evaluated,
                EvaluatedBy = ctx.Staff.Id,
                EvaluatedAt = now.AddDays(round == 1 ? -375 : -205),
                EvaluationResult = "PASS",
                EvaluationComments = "Tiến độ đạt yêu cầu.",
                CreatedAt = now.AddDays(round == 1 ? -385 : -215),
                UpdatedAt = now.AddDays(round == 1 ? -375 : -205)
            };
            _db.ProgressReports.Add(report);
            await _db.SaveChangesAsync();

            foreach (var a in activities)
            {
                _db.ProgressReportItems.Add(new ProgressReportItem
                {
                    ReportId = report.Id, ActivityId = a.Id,
                    CompletionRate = round == 1 ? 50m : 100m,
                    CompletionStatus = round == 1 ? "IN_PROGRESS" : "COMPLETED",
                    EvidenceDescription = "Báo cáo chuyên đề, mã nguồn và số liệu khảo sát kèm theo."
                });
            }
        }

        _db.FinalReports.Add(new FinalReport
        {
            ProjectId = project.Id,
            ReportFileUrl = DemoEvidenceFolderUrl,
            SummaryFileUrl = DemoEvidenceFolderUrl,
            Language = "VI",
            SubmittedAt = now.AddDays(-200),
            FinalSubmittedAt = now.AddDays(-150),
            ArchivedAt = now.AddDays(-140),
            Status = FinalReportStatus.Archived
        });

        _db.ContractSettlements.Add(new ContractSettlement
        {
            ContractId = contract.Id,
            TotalContractedAmount = 96_000_000m,
            TotalDisbursedAmount = 96_000_000m,
            TotalReturnedAmount = 0m,
            ProductsSubmittedSummary = "01 bài báo tạp chí trong nước (đã đăng); 01 hệ thống khuyến nghị môn học đã bàn giao.",
            AccountingClearedAt = DateOnly.FromDateTime(now).AddDays(-145),
            AssetsClearedAt = DateOnly.FromDateTime(now).AddDays(-145),
            SettlementSignedAt = now.AddDays(-140),
            SideASigneeId = ctx.Admin.Id,
            Notes = "Đã quyết toán đủ, không có khoản phải hoàn trả."
        });
        contract.Status = ContractStatus.Settled;
        await _db.SaveChangesAsync();

        // Một PHỤ LỤC GIA HẠN đã duyệt — trước đây bảng `amendment_requests` rỗng hoàn toàn nên
        // tab "Điều chỉnh" của mọi hợp đồng đều trống, không demo được luồng xin gia hạn.
        // 3 tháng nằm trong trần 1/2 thời gian thực hiện (QĐ543 Điều 10.4: 12 tháng ⇒ tối đa 6).
        var extensionCat = await _db.AmendmentCategories.FirstOrDefaultAsync(c => c.Code == "EXTENSION");
        if (extensionCat != null && !await _db.AmendmentRequests.AnyAsync(a => a.ContractId == contract.Id))
        {
            _db.AmendmentRequests.Add(new AmendmentRequest
            {
                Id = Guid.NewGuid(),
                ContractId = contract.Id,
                CategoryId = extensionCat.Id,
                ChangeDescription = "Gia hạn thời gian thực hiện đề tài thêm 03 tháng.",
                OldValue = "12",
                NewValue = "3",
                Justification = "Việc thu thập dữ liệu khảo sát người dùng phụ thuộc lịch đăng ký môn học của sinh viên, "
                              + "rơi đúng kỳ nghỉ nên chậm hơn kế hoạch khoảng một quý.",
                RequestedBy = ctx.Pi2.Id,
                RequestedAt = now.AddDays(-300),
                RequiresRectorApproval = false,
                ReviewedBy = ctx.Staff.Id,
                ReviewedAt = now.AddDays(-295),
                Status = "APPROVED",
                ReviewerComments = "Đồng ý gia hạn 03 tháng; đề nghị chủ nhiệm bám sát mốc mới, không gia hạn tiếp."
            });
            await _db.SaveChangesAsync();
        }

        // LỊCH SỬ VÒNG 1 — đề tài hoàn thành phải xem lại được cả chặng xét duyệt đầu tiên.
        await SeedPriorReviewHistoryAsync(ctx, project, await GetOrCreateReviewRoundAsync(ctx),
            approvedAt: now.AddDays(-530), meetingRoom: "Phòng họp A201, Toà Alpha, Cơ sở Hoà Lạc");

        // Hội đồng nghiệm thu đã chốt "Đạt" — để tab nghiệm thu của đề tài hoàn thành không trống.
        var round8 = new ReviewRound
        {
            Id = Guid.NewGuid(),
            CycleTrackId = ctx.ClosedTrack.Id,
            RoundNumber = 3,
            Dimension = ReviewRoundDimension.Science,
            RoundType = "ACCEPTANCE",
            Sequence = 3,
            Status = ReviewRoundStatus.Passed,
            OpenedAt = now.AddDays(-160),
            ClosedAt = now.AddDays(-150),
            Result = ReviewResult.Approved
        };
        _db.ReviewRounds.Add(round8);
        await _db.SaveChangesAsync();

        _db.ProjectRounds.Add(new ProjectRound
        {
            ProjectId = project.Id, RoundId = round8.Id,
            Status = "PASSED", Result = ReviewResult.Approved, FinalizedAt = now.AddDays(-150)
        });
        await _db.SaveChangesAsync();

        var council = await CreateCouncilAsync(ctx, round8, "ACCEPTANCE", minMembers: 5, maxMembers: 7,
            meetingTitle: "Họp Hội đồng nghiệm thu — Hệ thống khuyến nghị môn học",
            location: "Phòng họp B105, Toà Beta, Cơ sở Hoà Lạc",
            scheduledAt: now.Date.AddDays(-152).AddHours(2), durationMinutes: 120,
            meetingStatus: MeetingStatus.Completed, actuallyAttended: true);

        _db.CouncilProjectAssignments.Add(new CouncilProjectAssignment
        {
            CouncilId = council.Council.Id, ProjectId = project.Id,
            MeetingId = council.Meeting.Id, SlotStartAt = council.Meeting.ScheduledAt,
            SlotDurationMinutes = 90, SlotOrder = 1
        });

        foreach (var m in council.Members)
        {
            _db.AcceptanceEvaluations.Add(new AcceptanceEvaluation
            {
                CouncilId = council.Council.Id, ProjectId = project.Id, EvaluatorMemberId = m.Id,
                Result = EvaluationResult.Pass, IsValidBallot = true, SubmittedAt = now.AddDays(-152)
            });
        }
        await _db.SaveChangesAsync();

        await AddDecisionAsync(council.Council, council.Members, project.Id, ReviewResult.Approved,
            "Đề tài hoàn thành đầy đủ nội dung và sản phẩm đã đăng ký. Hội đồng nhất trí nghiệm thu Đạt với 5/5 phiếu.",
            "Đề nghị chủ nhiệm nộp bản lưu trữ báo cáo tổng kết về Phòng QLKH.",
            now.AddDays(-150));

        council.Council.Status = CouncilStatus.Decided;
        await _db.SaveChangesAsync();
    }

    // ── Khối dùng lại ────────────────────────────────────────────────────────

    private Task<bool> ExistsAsync(string code) =>
        _db.Projects.IgnoreQueryFilters().AnyAsync(p => p.ProjectCode == code);


    /// <summary>
    /// Dựng <b>lịch sử vòng XÉT DUYỆT đã kết thúc</b> cho một đề tài đang/đã qua nghiệm thu.
    ///
    /// <para>
    /// Trước 17/08 kịch bản #7 (đang nghiệm thu) và #8 (đã hoàn thành) <b>chỉ có vòng ACCEPTANCE</b>
    /// — không vòng REVIEW, không hội đồng xét duyệt, không biên bản. Tức là hai đề tài tự nhiên
    /// xuất hiện ở nghiệm thu mà chưa từng được duyệt đề cương: mở màn "Hội đồng &amp; Chấm" ra là
    /// thấy trống, và không ai giải thích được vì sao chúng có hợp đồng.
    /// </para>
    /// <para>
    /// Dùng CHUNG vòng REVIEW của lĩnh vực (một vòng phủ nhiều đề tài — đúng mô hình Phase B),
    /// nhưng lập <b>hội đồng riêng</b> đã họp xong trong quá khứ: đủ 5 người, phiếu chấm đầy đủ,
    /// biên bản đã chốt, kết quả ĐẠT. Nhờ vậy Staff mở vòng 1 ra thấy đúng "đã xong, ai chấm, họp
    /// ngày nào", còn dòng thời gian của đề tài liền mạch từ xét duyệt → hợp đồng → nghiệm thu.
    /// </para>
    /// </summary>
    private async Task SeedPriorReviewHistoryAsync(
        Ctx ctx, Project project, ReviewRound reviewRound, DateTime approvedAt, string meetingRoom)
    {
        // Đã có kết quả vòng 1 rồi thì thôi — hàm chạy lại nhiều lần vẫn an toàn.
        if (await _db.ProjectRounds.AnyAsync(pr => pr.ProjectId == project.Id && pr.RoundId == reviewRound.Id))
            return;

        _db.ProjectRounds.Add(new ProjectRound
        {
            ProjectId = project.Id,
            RoundId = reviewRound.Id,
            Status = ReviewRoundStatus.Passed
        });
        await _db.SaveChangesAsync();

        // Họp TRƯỚC ngày phê duyệt — mốc thời gian phải xuôi, nếu không timeline đọc ra vô lý.
        var council = await CreateCouncilAsync(ctx, reviewRound, "REVIEW", minMembers: 3, maxMembers: 5,
            meetingTitle: "Họp Hội đồng xét duyệt đề cương đề tài cấp Trường",
            location: meetingRoom,
            scheduledAt: approvedAt.AddDays(-7), durationMinutes: 90,
            meetingStatus: MeetingStatus.Completed, actuallyAttended: true);

        _db.CouncilProjectAssignments.Add(new CouncilProjectAssignment
        {
            CouncilId = council.Council.Id,
            ProjectId = project.Id,
            MeetingId = council.Meeting.Id,
            SlotStartAt = council.Meeting.ScheduledAt,
            SlotDurationMinutes = 90,
            SlotOrder = 1
        });
        await _db.SaveChangesAsync();

        // Cả 5 thành viên đều chấm (Điều 8.3.b: mọi thành viên dự họp đánh giá theo BM03).
        await AddScoreBallotsAsync(ctx, council.Council, council.Members, project.Id,
            skipMemberIndexes: Array.Empty<int>(), baseQuality: 0.85m, submittedAt: approvedAt.AddDays(-7));

        await AddDecisionAsync(council.Council, council.Members, project.Id,
            ReviewResult.Approved,
            "Hội đồng nhất trí thông qua đề cương. Mục tiêu rõ ràng, phương pháp phù hợp, sản phẩm đăng ký khả thi trong thời gian và kinh phí đề xuất.",
            "Đề nghị chủ nhiệm bổ sung mốc kiểm tra giữa kỳ và làm rõ tiêu chí nghiệm thu cho từng sản phẩm.",
            approvedAt);

        await SetProjectRoundAsync(project.Id, reviewRound.Id,
            ReviewRoundStatus.Passed, ReviewResult.Approved, approvedAt);
    }

    private sealed record CouncilBundle(ReviewCouncil Council, List<CouncilMember> Members, CouncilMeeting Meeting);

    /// <summary>
    /// Hội đồng 5 người, đủ Chủ tịch/Thư ký/Phản biện, tất cả đã XÁC NHẬN lời mời, và đã có
    /// buổi họp — thiếu bất kỳ mảnh nào thì nút "Gửi thư mời" / màn chấm điểm đều không mở được.
    /// </summary>
    private async Task<CouncilBundle> CreateCouncilAsync(
        Ctx ctx, ReviewRound round, string councilType, int minMembers, int maxMembers,
        string meetingTitle, string location, DateTime scheduledAt, int durationMinutes,
        string meetingStatus = MeetingStatus.Scheduled, bool actuallyAttended = false,
        string memberStatus = CouncilMemberStatus.Confirmed)
    {
        var now = DateTime.UtcNow;
        var council = new ReviewCouncil
        {
            Id = Guid.NewGuid(),
            CouncilType = councilType,
            RoundId = round.Id,
            EstablishmentDecisionNo = $"QĐ-{Random.Shared.Next(100, 999)}/QĐ-ĐHFPT",
            EstablishedAt = DateOnly.FromDateTime(scheduledAt.AddDays(-14)),
            MeetingDeadline = DateOnly.FromDateTime(scheduledAt.AddDays(7)),
            MinMembersRequired = minMembers,
            MaxMembersAllowed = maxMembers,
            Status = CouncilStatus.Forming,
            CreatedBy = ctx.Staff.Id,
            CreatedAt = now.AddDays(-15),
            UpdatedAt = now
        };
        _db.ReviewCouncils.Add(council);
        await _db.SaveChangesAsync();

        var roles = new[]
        {
            CouncilMemberRole.Chair, CouncilMemberRole.Secretary, CouncilMemberRole.Opponent,
            CouncilMemberRole.Member, CouncilMemberRole.Member
        };
        var members = new List<CouncilMember>();
        for (var i = 0; i < roles.Length; i++)
        {
            var m = new CouncilMember
            {
                Id = Guid.NewGuid(),
                CouncilId = council.Id,
                UserId = ctx.Reviewers[i].Id,
                MemberRole = roles[i],
                InvitationSentAt = memberStatus == CouncilMemberStatus.Assigned ? null : now.AddDays(-12),
                ConfirmedAt = memberStatus == CouncilMemberStatus.Confirmed ? now.AddDays(-11) : null,
                TokenExpiresAt = memberStatus == CouncilMemberStatus.Invited ? now.AddDays(10) : null,
                Status = memberStatus
            };
            members.Add(m);
        }
        _db.CouncilMembers.AddRange(members);

        var meeting = new CouncilMeeting
        {
            Id = Guid.NewGuid(),
            CouncilId = council.Id,
            Title = meetingTitle,
            Platform = MeetingPlatform.InPerson,
            Location = location,
            ScheduledAt = scheduledAt,
            DurationMinutes = durationMinutes,
            Agenda = "1. Chủ tịch tuyên bố lý do, giới thiệu thành phần.\n2. Thư ký công bố quyết định thành lập Hội đồng.\n3. Chủ nhiệm trình bày.\n4. Phản biện và thành viên nêu ý kiến.\n5. Hội đồng họp riêng, bỏ phiếu.\n6. Chủ tịch công bố kết luận.",
            Status = meetingStatus,
            CreatedAt = now.AddDays(-12)
        };
        _db.CouncilMeetings.Add(meeting);
        await _db.SaveChangesAsync();

        foreach (var m in members)
        {
            _db.MeetingAttendances.Add(new MeetingAttendance
            {
                MeetingId = meeting.Id,
                MemberId = m.Id,
                RsvpStatus = memberStatus == CouncilMemberStatus.Confirmed ? "ACCEPTED" : "PENDING",
                RsvpAt = memberStatus == CouncilMemberStatus.Confirmed ? now.AddDays(-10) : null,
                ActuallyAttended = actuallyAttended ? true : null
            });
        }
        await _db.SaveChangesAsync();

        return new CouncilBundle(council, members, meeting);
    }

    /// <summary>
    /// Phiếu chấm theo bộ tiêu chí thật của vòng. Điểm mỗi tiêu chí là SỐ NGUYÊN (setting
    /// <c>SCORE_DECIMAL_PLACES</c> mặc định 0) và không vượt trần của chính tiêu chí đó.
    /// </summary>
    private async Task AddScoreBallotsAsync(
        Ctx ctx, ReviewCouncil council, List<CouncilMember> members, Guid projectId,
        int[] skipMemberIndexes, decimal baseQuality, DateTime submittedAt)
    {
        var criteria = await _db.RubricCriteria
            .Where(c => c.TemplateId == ctx.ReviewRubric.Id && c.IsActive)
            .OrderBy(c => c.Sequence).ToListAsync();
        if (criteria.Count == 0) return;

        for (var i = 0; i < members.Count; i++)
        {
            if (skipMemberIndexes.Contains(i)) continue;

            var score = new ProposalReviewScore
            {
                CouncilId = council.Id,
                ProjectId = projectId,
                EvaluatorMemberId = members[i].Id,
                TemplateId = ctx.ReviewRubric.Id,
                GeneralComments = baseQuality >= 0.8m
                    ? "Đề tài có cơ sở khoa học rõ ràng, phương pháp phù hợp, sản phẩm đăng ký khả thi."
                    : "Hướng nghiên cứu phù hợp nhưng phần thiết kế thử nghiệm và dự toán còn cần làm rõ thêm.",
                OtherRecommendations = baseQuality >= 0.8m
                    ? "Đề nghị bám sát tiến độ đã đăng ký."
                    : "Đề nghị chỉnh sửa và bổ sung theo góp ý của phản biện.",
                IsValidBallot = true,
                SubmittedAt = submittedAt
            };
            _db.ProposalReviewScores.Add(score);
            await _db.SaveChangesAsync();

            // Chênh lệch nhỏ giữa các thành viên để bảng điểm không giống hệt nhau.
            var drift = (i - 2) * 0.02m;
            foreach (var c in criteria)
            {
                var given = Math.Round(c.MaxScore * Math.Clamp(baseQuality + drift, 0.4m, 0.98m), 0,
                    MidpointRounding.AwayFromZero);
                _db.ReviewScoreDetails.Add(new ReviewScoreDetail
                {
                    ScoreId = score.Id,
                    CriterionId = c.Id,
                    GivenScore = Math.Min(given, c.MaxScore)
                });
            }
            await _db.SaveChangesAsync();
        }
    }

    private async Task SeedReviewerAiCacheAsync(
        Guid proposalId, Guid councilId, int rubricTemplateId, DateTime now)
    {
        var entityId = proposalId.ToString();
        if (!await _db.LlmOutputs.AnyAsync(x => x.EntityType == "Proposal"
                                              && x.EntityId == entityId
                                              && x.OutputType == "SUMMARY"
                                              && x.IsActive))
        {
            var summary = new
            {
                title = "Mô hình dự báo nguy cơ bỏ học của sinh viên bằng học máy tổ hợp",
                summary = "Đề tài xây dựng mô hình cảnh báo sớm nguy cơ bỏ học từ dữ liệu học tập và tương tác của sinh viên.",
                strengths = new[]
                {
                    "Mục tiêu có chỉ tiêu định lượng AUC ≥ 0,85 và gắn với nhu cầu can thiệp sớm thực tế.",
                    "Phương pháp tổ hợp và SHAP phù hợp với bài toán dự báo có yêu cầu giải thích."
                },
                weaknesses = new[]
                {
                    "Cần mô tả rõ cách bảo vệ dữ liệu cá nhân và chiến lược xử lý mất cân bằng lớp.",
                    "Kế hoạch đánh giá cần nêu cách tránh rò rỉ dữ liệu giữa các khóa học."
                },
                source = "file+form",
                sourceFileName = "ThuyetMinh_NCKH-2026-003.docx"
            };
            _db.LlmOutputs.Add(new LlmOutput
            {
                EntityType = "Proposal",
                EntityId = entityId,
                OutputType = "SUMMARY",
                ModelUsed = "demo-cache",
                PromptVersion = "v2",
                Content = System.Text.Json.JsonSerializer.Serialize(summary),
                GeneratedAt = now.AddMinutes(-10),
                IsActive = true
            });
        }

        var scoreOutput = $"SCORE_SUGGESTION:{councilId}";
        if (!await _db.LlmOutputs.AnyAsync(x => x.EntityType == "Proposal"
                                              && x.EntityId == entityId
                                              && x.OutputType == scoreOutput
                                              && x.IsActive))
        {
            var criteria = await _db.RubricCriteria
                .Where(x => x.TemplateId == rubricTemplateId && x.IsActive)
                .OrderBy(x => x.Sequence)
                .ToListAsync();
            var suggestions = criteria.Select(x => new
            {
                criterionId = x.Id,
                criterionName = x.CriterionName,
                maxScore = x.MaxScore,
                suggestedScore = Math.Round(x.MaxScore * 0.82m, 0, MidpointRounding.AwayFromZero),
                comment = "Gợi ý AI dựa trên bản thuyết minh; thành viên hội đồng cần đọc hồ sơ và tự quyết định điểm cuối."
            });
            _db.LlmOutputs.Add(new LlmOutput
            {
                EntityType = "Proposal",
                EntityId = entityId,
                OutputType = scoreOutput,
                ModelUsed = "demo-cache",
                PromptVersion = "score-v2",
                Content = System.Text.Json.JsonSerializer.Serialize(suggestions),
                GeneratedAt = now.AddMinutes(-10),
                IsActive = true
            });
        }

        await _db.SaveChangesAsync();
    }

    private async Task AddDecisionAsync(
        ReviewCouncil council, List<CouncilMember> members, Guid projectId,
        string result, string comments, string recommendations, DateTime finalizedAt)
    {
        var scores = await _db.ProposalReviewScores
            .Where(s => s.CouncilId == council.Id && s.ProjectId == projectId && s.SubmittedAt != null)
            .ToListAsync();
        var totals = await _db.ReviewScoreDetails
            .Where(d => scores.Select(s => s.Id).Contains(d.ScoreId))
            .GroupBy(d => d.ScoreId)
            .Select(g => g.Sum(x => x.GivenScore))
            .ToListAsync();

        var chair = members.FirstOrDefault(m => m.MemberRole == CouncilMemberRole.Chair);
        var secretary = members.FirstOrDefault(m => m.MemberRole == CouncilMemberRole.Secretary);

        _db.CouncilDecisions.Add(new CouncilDecision
        {
            CouncilId = council.Id,
            ProjectId = projectId,
            TotalMembers = members.Count,
            AttendingMembers = members.Count,
            ValidBallots = Math.Max(scores.Count, members.Count),
            InvalidBallots = 0,
            AverageScore = totals.Count > 0 ? Math.Round(totals.Average(), 2) : null,
            Result = result,
            CouncilComments = comments,
            Recommendations = recommendations,
            ChairUserId = chair?.UserId,
            SecretaryUserId = secretary?.UserId,
            FinalizedAt = finalizedAt   // rule #12: Chủ tịch chốt = KHOÁ biên bản
        });
        await _db.SaveChangesAsync();
    }

    private async Task SetProjectRoundAsync(Guid projectId, Guid roundId, string status, string result, DateTime at)
    {
        var pr = await _db.ProjectRounds.FirstOrDefaultAsync(x => x.ProjectId == projectId && x.RoundId == roundId);
        if (pr == null) return;
        pr.Status = status;
        pr.Result = result;
        pr.FinalizedAt = at;
        await _db.SaveChangesAsync();
    }

    private async Task<Contract> CreateContractAsync(
        Ctx ctx, Project project, string number, decimal amount,
        DateOnly start, DateOnly end, string status, DateTime signedAt, int maxExtensionMonths)
    {
        // Số hợp đồng là unique index. Trên cơ sở dữ liệu đang chạy có thể đã có người đặt trùng
        // số này bằng tay ⇒ né sang hậu tố thay vì để cả mẻ seed đổ.
        var candidate = number;
        for (var i = 2; await _db.Contracts.AnyAsync(c => c.ContractNumber == candidate); i++)
            candidate = $"{number}-{i}";

        var contract = new Contract
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            ContractNumber = candidate,
            ScopeTitle = "Toàn bộ nội dung đề tài",
            Status = status,
            SignedAt = signedAt,
            TotalAmount = amount,
            StartDate = start,
            EndDate = end,
            OriginalEndDate = end,
            MaxExtensionMonths = maxExtensionMonths,   // QĐ543 Điều 10.4: tối đa 1/2 thời gian thực hiện
            SideARepresentative = SystemSettingKeys.DefaultContractSideARepresentative,
            CreatedBy = ctx.Staff.Id,
            CreatedAt = signedAt.AddDays(-3),
            UpdatedAt = signedAt
        };
        _db.Contracts.Add(contract);
        await _db.SaveChangesAsync();
        return contract;
    }

    /// <summary>
    /// Dự toán mẫu tách theo <b>06 hạng mục QĐ543 Điều 15</b>, tỷ lệ chọn sẵn nằm dưới trần:
    /// thù lao 62% (≤100) · thiết bị 18% (≤60) · hội thảo 10% (≤30) · VPP 7% (≤20) · SHTT 3% (≤10).
    /// <para>
    /// Trước đây demo chỉ ghi một con số tổng, nên mở bản xem lại ra là bảng dự toán trống trơn —
    /// đúng phần hội đồng soi kỹ nhất khi thẩm định kinh phí.
    /// </para>
    /// </summary>
    private async Task SeedBudgetAsync(Guid proposalId, decimal total)
    {
        // Làm tròn tới NGHÌN cho số đẹp. .NET không nhận decimals âm như một số ngôn ngữ khác
        // (Math.Round(x, -3) ném ArgumentOutOfRange) nên phải chia–làm tròn–nhân lại.
        decimal Part(decimal pct) => Math.Round(total * pct / 100m / 1000m, 0) * 1000m;

        var labor = Part(62m);
        var equipment = Part(18m);
        var conference = Part(10m);
        var office = Part(7m);
        var ip = total - labor - equipment - conference - office;   // phần dư dồn vào SHTT (~3%)

        _db.ProposalBudgets.Add(new ProposalBudget
        {
            ProposalId = proposalId,
            TotalAmount = total,
            LaborAmount = labor,
            EquipmentAmount = equipment,
            ConferenceAmount = conference,
            OfficeSuppliesAmount = office,
            IncidentalIpAmount = ip
        });
        await _db.SaveChangesAsync();

        var byCode = await _db.BudgetExpenseCategories
            .Where(c => c.IsActive)
            .ToDictionaryAsync(c => c.Code, c => c.Id);

        void AddItem(string code, decimal amount, int seq)
        {
            if (amount <= 0 || !byCode.TryGetValue(code, out var id)) return;
            _db.ProposalBudgetItems.Add(new ProposalBudgetItem
            {
                ProposalId = proposalId, CategoryId = id, Amount = amount, Sequence = seq
            });
        }

        AddItem("LABOR", labor, 1);
        AddItem("EQUIPMENT", equipment, 2);
        AddItem("CONFERENCE", conference, 4);
        AddItem("OFFICE_OTHER", office, 5);
        AddItem("INCIDENTAL_IP", ip, 6);
        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Gắn sản phẩm đã cam kết vào hợp đồng theo 2 giai đoạn + dựng 3 đợt giải ngân
    /// (rule #15: chỉ là MỐC theo tiến độ, hệ thống không quản tiền).
    /// </summary>
    private async Task<List<ProjectDeliverable>> AttachDeliverablesToContractAsync(
        Guid projectId, Contract contract, DateOnly midDate, DateOnly endDate)
    {
        var phase1 = new ContractPhase
        {
            ContractId = contract.Id, PhaseNo = 1, Name = "Giai đoạn 1 — Nghiên cứu và xây dựng",
            StartDate = contract.StartDate, EndDate = midDate,
            Amount = Math.Round(contract.TotalAmount * 0.6m, 0), Status = "IN_PROGRESS"
        };
        var phase2 = new ContractPhase
        {
            ContractId = contract.Id, PhaseNo = 2, Name = "Giai đoạn 2 — Hoàn thiện và nghiệm thu",
            StartDate = midDate, EndDate = endDate,
            Amount = contract.TotalAmount - Math.Round(contract.TotalAmount * 0.6m, 0), Status = "PLANNED"
        };
        _db.ContractPhases.AddRange(phase1, phase2);
        await _db.SaveChangesAsync();

        var deliverables = await _db.ProjectDeliverables
            .Where(d => d.ProjectId == projectId).OrderBy(d => d.Sequence).ToListAsync();
        for (var i = 0; i < deliverables.Count; i++)
        {
            deliverables[i].ContractId = contract.Id;
            deliverables[i].ContractPhaseId = i == 0 ? phase1.Id : phase2.Id;
            deliverables[i].DueDate = i == 0 ? midDate : endDate;
        }
        await _db.SaveChangesAsync();

        // Mốc giải ngân lấy từ bảng mốc chuẩn của LOẠI ĐỀ TÀI (QĐ543 Điều 16) — trước đây demo cắm
        // cứng 40–40–20 nên hội đồng mở màn hình ra là thấy lệch quy định ngay.
        var project = await _db.Projects.FirstAsync(p => p.Id == projectId);
        var templates = await _db.DisbursementTemplates
            .Where(t => t.ResearchTypeId == project.ResearchTypeId && t.IsActive)
            .OrderBy(t => t.RoundNumber)
            .ToListAsync();
        if (templates.Count == 0)
            templates = new List<DisbursementTemplate>
            {
                new() { RoundNumber = 1, Percentage = 100m, ConditionDescription = "Sau khi Hội đồng nghiệm thu đánh giá \"Đạt\"" }
            };

        decimal allocated = 0;
        for (var i = 0; i < templates.Count; i++)
        {
            var t = templates[i];
            var isLast = i == templates.Count - 1;
            var amount = isLast
                ? contract.TotalAmount - allocated
                : Math.Round(contract.TotalAmount * t.Percentage / 100m, 0);
            allocated += amount;

            // Đợt đầu (tạm ứng sau ký) đã chi thật; các đợt sau còn chờ điều kiện — riêng đề tài cơ
            // bản chỉ có 1 đợt và nó nằm SAU nghiệm thu nên không được đánh dấu đã chi trước.
            var advancePaid = i == 0 && templates.Count > 1;

            _db.ContractDisbursements.Add(new ContractDisbursement
            {
                ContractId = contract.Id,
                RoundNumber = t.RoundNumber,
                PhaseId = isLast ? phase2.Id : phase1.Id,
                Percentage = t.Percentage,
                PlannedAmount = amount,
                ActualAmount = advancePaid ? amount : null,
                ConditionDescription = t.ConditionDescription,
                Status = advancePaid ? DisbursementStatus.Disbursed : DisbursementStatus.Pending,
                DisbursedAt = advancePaid ? contract.SignedAt?.AddDays(5) : null,
                BankReference = advancePaid
                    ? "FT" + contract.ContractNumber.Replace("HĐ-", "").Replace("-", "")
                    : null,
                DeliverableId = isLast ? deliverables.LastOrDefault()?.Id : deliverables.FirstOrDefault()?.Id
            });
        }
        await _db.SaveChangesAsync();

        return deliverables;
    }

    /// <summary>
    /// Sinh file Word thuyết minh KHỚP nội dung từng đề tài rồi đính kèm — để demo "AI đọc file →
    /// tóm tắt → đối chiếu biểu mẫu" chạy thật, không phải upload tay trước mặt hội đồng.
    /// Cố ý KHÔNG đính cho đề tài nháp (#1) vì chính thao tác upload là thứ đem đi demo.
    /// </summary>
    private async Task AttachProposalFilesAsync()
    {
        var codes = new[] { S2, S3, S4, S5, S6, S7 };
        var proposals = await _db.Proposals
            .Include(p => p.Project)
            .Where(p => p.Project.ProjectCode != null && codes.Contains(p.Project.ProjectCode))
            .ToListAsync();

        foreach (var proposal in proposals)
        {
            var already = await _db.Documents.AnyAsync(d =>
                d.EntityType == "Proposal" && d.EntityId == proposal.Id.ToString() && !d.IsDeleted);
            if (already) continue;

            try
            {
                var (content, fileName) = await _export.ExportScientificDocAsync(proposal.Id);
                var blobName = $"proposals/{proposal.Id}/{Guid.NewGuid():N}.docx";
                await using var stream = new MemoryStream(content);
                await _storage.SaveAsync(blobName, stream, DocxMime);

                var doc = new Document
                {
                    EntityType = "Proposal",
                    EntityId = proposal.Id.ToString(),
                    DocumentCategory = "PROPOSAL",
                    OriginalFileName = fileName,
                    FileSizeBytes = content.LongLength,
                    MimeType = DocxMime,
                    StorageContainer = _storage.Description,
                    StorageBlobName = blobName,
                    UploadedBy = proposal.Project.PiUserId,
                    UploadedAt = proposal.SubmittedAt ?? DateTime.UtcNow
                };
                doc.StorageUrl = $"/api/proposals/{proposal.Id}/documents/{doc.Id}/download";
                _db.Documents.Add(doc);
                await _db.SaveChangesAsync();
            }
            catch
            {
                // Không có file đính kèm thì kịch bản vẫn dùng được — không chặn khởi động ứng dụng.
            }
        }
    }
}
