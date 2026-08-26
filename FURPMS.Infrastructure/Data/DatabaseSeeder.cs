using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using FURPMS.Application.Constants;
using FURPMS.Domain.Entities.Contracts;
using FURPMS.Domain.Entities.Cycles;
using FURPMS.Domain.Entities.Financial;
using FURPMS.Domain.Entities.MasterData;
using FURPMS.Domain.Entities.Projects;
using FURPMS.Domain.Entities.Proposals;
using FURPMS.Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;

namespace FURPMS.Infrastructure.Data;

public class DatabaseSeeder
{
    /// <summary>
    /// E8 — MỘT mật khẩu cho mọi tài khoản demo. Trước đây mỗi vai một chuỗi khác nhau
    /// (<c>Staff@123456</c>, <c>Reviewer@123456</c>…), đứng trước hội đồng mà gõ nhầm là mất nhịp.
    /// <para>
    /// ⚠️ Đây là mật khẩu DEMO, cố tình dễ. Việc đặt lại mật khẩu chỉ chạy khi
    /// <c>DEMO_DATA_ENABLED</c> đang bật — tắt setting đó trước khi bàn giao bản chạy thật thì
    /// seeder không đụng tới mật khẩu nữa. **Đổi mật khẩu admin trên bản deploy công khai.**
    /// </para>
    /// </summary>
    public const string DemoPassword = "password";

    private readonly FURPMSDbContext _db;

    private readonly IHostEnvironment _env;
    private readonly IConfiguration _config;
    private readonly ILogger<DatabaseSeeder> _log;

    public DatabaseSeeder(
        FURPMSDbContext db, IHostEnvironment env, IConfiguration config, ILogger<DatabaseSeeder> log)
    {
        _db = db;
        _env = env;
        _config = config;
        _log = log;
    }

    /// <summary>
    /// Dựng dữ liệu nền khi khởi động.
    ///
    /// <para>
    /// Chia làm hai phần rõ ràng:
    /// </para>
    /// <list type="bullet">
    ///   <item><b>Bắt buộc</b> — vai trò, quản trị viên, danh mục, cấu hình, bộ tiêu chí. Thiếu là
    ///   hệ thống không chạy được, nên chạy ở MỌI môi trường.</item>
    ///   <item><b>Demo</b> — tài khoản mẫu, đề tài mẫu, kịch bản đã duyệt. Chỉ chạy ở
    ///   <b>Development</b>.</item>
    /// </list>
    ///
    /// <para>
    /// <b>Vì sao phải tách.</b> Trước 14/08 tất cả chạy chung ở mọi môi trường ⇒ deploy lên máy chủ
    /// là DB thật có ngay <b>9 tài khoản demo</b>, và <c>ResetDemoPasswordsAsync</c> đặt tất cả về
    /// mật khẩu <c>password</c> — <b>kể cả admin@furpms.edu.vn</b>. Danh sách email đó nằm sẵn
    /// trong tài liệu của repo, nên ai đọc repo cũng đăng nhập được bằng quyền quản trị. Không ai
    /// bấm gì sai cả; chỉ cần deploy.
    /// </para>
    /// </summary>
    public async Task SeedAsync()
    {
        // ── Bắt buộc ở mọi môi trường ───────────────────────────────────────
        await SeedRolesAsync();
        await SeedAdminAsync();
        await SeedPersonnelRoleTypesAsync();
        await SeedBudgetExpenseCategoriesAsync();
        await SeedAmendmentCategoriesAsync();
        await NormalizeContractClosureStatusesAsync();
        await SeedSystemFinancialConfigsAsync();
        await SeedSystemSettingsAsync();
        await SeedDisbursementTemplatesAsync();
        await RemoveObsoleteSettingsAsync();
        await FixAppliedOrderingUnitFlagAsync();
        await SeedRubricCriteriaAsync();

        // ── Chỉ môi trường phát triển ───────────────────────────────────────
        if (!_env.IsDevelopment()) return;

        await SeedDemoProposalAsync();
        await EnsureDemoCycleOpenAsync();
        await SeedDemoAccountsAsync();
        await SeedAcademicProfilesAsync();
        await SeedApprovedScenarioAsync();
        await ResetDemoPasswordsAsync();
    }

    /// <summary>
    /// Đặt lại mật khẩu các tài khoản DEMO về <see cref="DemoPassword"/>.
    /// <para>
    /// Cần bước riêng vì <c>SeedDemoAccountsAsync</c> chỉ đặt mật khẩu lúc TẠO — cơ sở dữ liệu đã
    /// chạy từ trước thì đổi hằng số cũng không có tác dụng gì, đứng trước hội đồng mới phát hiện
    /// mật khẩu vẫn là chuỗi cũ.
    /// </para>
    /// <para>
    /// Chỉ chạy khi <c>DEMO_DATA_ENABLED</c> bật, và **chỉ chạm 9 tài khoản demo** — không đụng tài
    /// khoản người dùng thật. Tắt setting đó trước khi bàn giao là seeder không sờ vào mật khẩu nữa.
    /// </para>
    /// </summary>
    private async Task ResetDemoPasswordsAsync()
    {
        var enabled = await _db.SystemSettings
            .Where(x => x.Key == SystemSettingKeys.DemoDataEnabled)
            .Select(x => x.Value)
            .FirstOrDefaultAsync();
        if (string.Equals(enabled?.Trim(), "false", StringComparison.OrdinalIgnoreCase)) return;

        var demoEmails = new[]
        {
            "admin@furpms.edu.vn", "staff.demo@furpms.edu.vn", "pi.demo@furpms.edu.vn",
            "pi2.demo@furpms.edu.vn", "reviewer1.demo@furpms.edu.vn", "reviewer2.demo@furpms.edu.vn",
            "reviewer3.demo@furpms.edu.vn", "reviewer4.demo@furpms.edu.vn", "reviewer5.demo@furpms.edu.vn"
        };

        var users = await _db.Users.IgnoreQueryFilters()
            .Where(u => demoEmails.Contains(u.Email))
            .ToListAsync();

        var changed = false;
        foreach (var u in users)
        {
            // Đã đúng mật khẩu rồi thì bỏ qua — băm lại mỗi lần khởi động vừa tốn vừa vô ích.
            if (BCrypt.Net.BCrypt.Verify(DemoPassword, u.PasswordHash)) continue;
            u.PasswordHash = BCrypt.Net.BCrypt.HashPassword(DemoPassword, workFactor: 12);
            u.UpdatedAt = DateTime.UtcNow;
            changed = true;
        }
        if (changed) await _db.SaveChangesAsync();
    }

    // Đảm bảo chu kỳ demo ở trạng thái OPEN để PI có thể tạo/nộp đề xuất khi test.
    private async Task EnsureDemoCycleOpenAsync()
    {
        var cycle = await _db.ResearchCycles
            .FirstOrDefaultAsync(c => c.CycleYear == 2026 && c.SemesterCode == "SU26");
        if (cycle == null) return;

        var changed = false;
        if (cycle.Status != CycleStatus.Open && cycle.Status != CycleStatus.Closed)
        {
            cycle.Status = CycleStatus.Open;
            changed = true;
        }
        // Seed lại cửa sổ nộp về hiện tại nếu hạn đã qua (tránh nhìn "quá khứ").
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        if (cycle.SubmissionDeadline < today)
        {
            cycle.SubmissionOpenDate = today.AddDays(-7);
            cycle.SubmissionDeadline = today.AddDays(60);
            cycle.ReviewDeadline = today.AddDays(120);
            changed = true;
        }
        if (changed)
        {
            cycle.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync();
        }
    }

    // Tài khoản mẫu cho 4 vai trò (idempotent theo email). Mật khẩu in trong README/đăng nhập.
    private async Task SeedDemoAccountsAsync()
    {
        var unit = await GetOrCreateDemoUnitAsync();

        // Tên phải là tên người thật-nghĩa: hội đồng "PGS.TS Lê Phản Biện" in ra biên bản trước mặt
        // hội đồng chấm là lộ ngay dữ liệu chưa chuẩn bị. Cần 5 reviewer vì hội đồng phải đủ 5 người
        // (QĐ543 Điều 8.2 / 12.2) và phải LẺ để có chênh lệch phiếu.
        var accounts = new[]
        {
            (Email: "staff.demo@furpms.edu.vn",     Name: "Trần Thị Mai Lan staff",      Role: "Staff"),
            (Email: "reviewer1.demo@furpms.edu.vn", Name: "PGS.TS. Lê Quang Minh rv1", Role: "ReviewCommittee"),
            (Email: "reviewer2.demo@furpms.edu.vn", Name: "TS. Phạm Thu Hương rv2",    Role: "ReviewCommittee"),
            (Email: "reviewer3.demo@furpms.edu.vn", Name: "TS. Vũ Đình Nam rv3",       Role: "ReviewCommittee"),
            (Email: "reviewer4.demo@furpms.edu.vn", Name: "TS. Đặng Hoài Anh rv4",     Role: "ReviewCommittee"),
            (Email: "reviewer5.demo@furpms.edu.vn", Name: "ThS. Bùi Thanh Hà rv5",     Role: "ReviewCommittee"),
            (Email: "pi2.demo@furpms.edu.vn",       Name: "Hoàng Văn Bình pi2",        Role: "Faculty"),
        };

        // DB cũ đã có tài khoản với tên placeholder → đổi tên, không tạo trùng.
        // Tên demo nay kèm mã vai trong ngoặc — nhìn danh sách là biết đang đóng ai, khỏi phải nhớ
        // "PGS.TS. Lê Quang Minh" là rv mấy. Tên cũ liệt ở đây để DB đã triển khai tự đổi theo.
        var placeholders = new Dictionary<string, string>
        {
            ["staff.demo@furpms.edu.vn"] = "Trần Thị Mai Lan",
            ["reviewer1.demo@furpms.edu.vn"] = "PGS.TS. Lê Quang Minh",
            ["reviewer2.demo@furpms.edu.vn"] = "TS. Phạm Thu Hương",
            ["reviewer3.demo@furpms.edu.vn"] = "TS. Vũ Đình Nam",
            ["reviewer4.demo@furpms.edu.vn"] = "TS. Đặng Hoài Anh",
            ["reviewer5.demo@furpms.edu.vn"] = "ThS. Bùi Thanh Hà",
            ["pi2.demo@furpms.edu.vn"] = "Hoàng Văn Bình",
            ["reviewer1.demo@furpms.edu.vn"] = "PGS.TS Lê Phản Biện",
            ["reviewer2.demo@furpms.edu.vn"] = "TS. Phạm Hội Đồng",
            ["reviewer3.demo@furpms.edu.vn"] = "TS. Vũ Thẩm Định"
        };

        foreach (var a in accounts)
        {
            var existing = await _db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Email == a.Email);
            if (existing != null)
            {
                if (placeholders.TryGetValue(a.Email, out var old) && existing.FullName == old)
                {
                    existing.FullName = a.Name;
                    existing.UpdatedAt = DateTime.UtcNow;
                    await _db.SaveChangesAsync();
                }
                continue;
            }

            var role = await _db.Roles.FirstAsync(r => r.Name == a.Role);
            var user = new User
            {
                Id = Guid.NewGuid(),
                Email = a.Email,
                FullName = a.Name,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(DemoPassword, workFactor: 12),
                Status = UserStatus.Active,
                UnitId = unit.Id,
                IsExternal = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            _db.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = role.Id, AssignedAt = DateTime.UtcNow });
            await _db.SaveChangesAsync();
        }

        await SeedReviewerExpertiseAsync();
        await SeedMultiRoleAccountAsync();
        await RenameDemoAccountsAsync();
    }

    /// <summary>
    /// Gán thêm vai <b>Giảng viên</b> cho tài khoản Phòng QLKH demo — để rule #23 (đa vai, đổi vai ở
    /// dropdown header) <b>có tài khoản mà demo</b>. Trước đây không tài khoản demo nào có 2 vai nên
    /// dropdown đổi vai không bao giờ hiện ra, muốn thử phải tự đi gán tay trước.
    /// <para>
    /// Cũng là bộ dữ liệu để kiểm ranh giới hiển thị theo vai: đang ở vai Giảng viên thì gõ thẳng
    /// <c>/contracts</c> phải bị chặn, dù người đó thật sự có vai Phòng QLKH.
    /// </para>
    /// </summary>
    /// <summary>
    /// Đổi tên tài khoản demo sang dạng <b>có mã vai ở cuối</b> — "… pi1", "… rv3", "… staff" — để
    /// nhìn một danh sách người dùng là biết ai đóng vai gì, khỏi phải nhớ "Lê Quang Minh là rv mấy".
    /// <para>
    /// Chạy mỗi lần khởi động và <b>đặt lại đúng tên chuẩn</b> (không chỉ đổi từ một tên cũ cố định):
    /// xoá sạch DB rồi seed lại phải ra đúng bộ tên này, mà DB đang chạy cũng hội tụ về đó. Chỉ đụng
    /// 9 email demo — tài khoản thật do Admin tạo không bao giờ bị chạm.
    /// </para>
    /// </summary>
    private async Task RenameDemoAccountsAsync()
    {
        var renames = new Dictionary<string, string>
        {
            ["admin@furpms.edu.vn"] = "System Administrator admin",
            ["staff.demo@furpms.edu.vn"] = "Trần Thị Mai Lan staff",
            ["pi.demo@furpms.edu.vn"] = "Nguyễn Văn An pi1",
            ["pi2.demo@furpms.edu.vn"] = "Hoàng Văn Bình pi2",
            ["reviewer1.demo@furpms.edu.vn"] = "PGS.TS. Lê Quang Minh rv1",
            ["reviewer2.demo@furpms.edu.vn"] = "TS. Phạm Thu Hương rv2",
            ["reviewer3.demo@furpms.edu.vn"] = "TS. Vũ Đình Nam rv3",
            ["reviewer4.demo@furpms.edu.vn"] = "TS. Đặng Hoài Anh rv4",
            ["reviewer5.demo@furpms.edu.vn"] = "ThS. Bùi Thanh Hà rv5",
        };

        var changed = false;
        foreach (var (email, newName) in renames)
        {
            var u = await _db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(x => x.Email == email);
            if (u == null || u.FullName == newName) continue;
            u.FullName = newName;
            u.UpdatedAt = DateTime.UtcNow;
            changed = true;
        }
        if (changed) await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Gán lĩnh vực chuyên môn cho các tài khoản chấm demo (QĐ543 Điều 8.2).
    ///
    /// <para>Cố ý <b>không</b> cho ai cũng đủ mọi lĩnh vực: rv5 để trống để demo được cả ca "chưa
    /// khai chuyên môn", và rv4 chỉ có IT để demo ca "khác lĩnh vực" khi đề tài thuộc AI. Gán đều
    /// hết thì màn hình lúc nào cũng xanh, chẳng chứng minh được gì.</para>
    /// </summary>
    private async Task SeedReviewerExpertiseAsync()
    {
        var expertise = new (string Email, string[] TrackCodes)[]
        {
            ("reviewer1.demo@furpms.edu.vn", new[] { "AI", "IT" }),
            ("reviewer2.demo@furpms.edu.vn", new[] { "AI" }),
            ("reviewer3.demo@furpms.edu.vn", new[] { "AI" }),
            ("reviewer4.demo@furpms.edu.vn", new[] { "IT" }),
            // rv5: cố ý bỏ trống — ca "chưa khai chuyên môn".
        };

        foreach (var (email, codes) in expertise)
        {
            var userId = await _db.Users.IgnoreQueryFilters()
                .Where(u => u.Email == email).Select(u => (Guid?)u.Id).FirstOrDefaultAsync();
            if (userId is null) continue;

            foreach (var code in codes)
            {
                var trackId = await _db.ResearchTracks
                    .Where(t => t.Code == code).Select(t => (int?)t.Id).FirstOrDefaultAsync();
                if (trackId is null) continue;

                var exists = await _db.UserResearchTracks
                    .AnyAsync(x => x.UserId == userId.Value && x.TrackId == trackId.Value);
                if (exists) continue;

                _db.UserResearchTracks.Add(new UserResearchTrack
                {
                    UserId = userId.Value,
                    TrackId = trackId.Value,
                    Note = "Dữ liệu demo"
                });
            }
        }
        await _db.SaveChangesAsync();
    }

    private async Task SeedMultiRoleAccountAsync()
    {
        var staff = await _db.Users.IgnoreQueryFilters()
            .FirstOrDefaultAsync(u => u.Email == "staff.demo@furpms.edu.vn");
        if (staff == null) return;

        var faculty = await _db.Roles.FirstOrDefaultAsync(r => r.Name == "Faculty");
        if (faculty == null) return;

        if (await _db.UserRoles.AnyAsync(ur => ur.UserId == staff.Id && ur.RoleId == faculty.Id)) return;

        _db.UserRoles.Add(new UserRole { UserId = staff.Id, RoleId = faculty.Id, AssignedAt = DateTime.UtcNow });
        await _db.SaveChangesAsync();
    }

    // Bộ tiêu chí chấm (rubric) cho vòng xét duyệt — để reviewer chấm điểm được.
    private async Task SeedRubricCriteriaAsync()
    {
        // Mỗi bộ tự kiểm tồn tại RIÊNG. Trước đây hàm này `return` ngay khi thấy bộ REVIEW, nên
        // bộ nghiệm thu thêm sau đó KHÔNG BAO GIỜ được seed trên cơ sở dữ liệu đã chạy — chỉ máy
        // nào tạo DB mới mới có. Bỏ chung một cửa kiểm là dính lại đúng bẫy đó.
        await SeedAcceptanceRubricAsync();

        if (await _db.RubricTemplates.AnyAsync(t => t.TemplateType == "REVIEW"))
            return;

        var template = new RubricTemplate
        {
            TemplateType = "REVIEW",
            Name = "Phiếu chấm xét duyệt đề tài",
            MaxTotalScore = 100m,
            IsActive = true
        };
        _db.RubricTemplates.Add(template);
        await _db.SaveChangesAsync();

        // QĐ543 **BM03** — Phiếu đánh giá thẩm định đề cương: 5 mục 10+20+40+20+10, dòng cuối
        // ghi "Cộng 100". Phải cộng ĐÚNG MaxTotalScore, nếu không bộ này không dùng chấm được.
        _db.RubricCriteria.AddRange(
            new RubricCriterion { TemplateId = template.Id, CriterionName = "Mục đích, ý nghĩa khoa học và thực tiễn của đề tài", MaxScore = 10m, Sequence = 1, IsActive = true },
            new RubricCriterion { TemplateId = template.Id, CriterionName = "Phương pháp nghiên cứu",                              MaxScore = 20m, Sequence = 2, IsActive = true },
            new RubricCriterion { TemplateId = template.Id, CriterionName = "Nội dung nghiên cứu và kết quả dự kiến",              MaxScore = 40m, Sequence = 3, IsActive = true },
            new RubricCriterion { TemplateId = template.Id, CriterionName = "Năng lực của chủ nhiệm đề tài và nhóm nghiên cứu",    MaxScore = 20m, Sequence = 4, IsActive = true },
            new RubricCriterion { TemplateId = template.Id, CriterionName = "Tính hợp lý của dự toán kinh phí",                    MaxScore = 10m, Sequence = 5, IsActive = true }
        );
        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Bộ tiêu chí cho vòng NGHIỆM THU — chép nguyên <b>QĐ543 Biểu mẫu 10</b>
    /// ("Nhận xét đề tài NCKH — dùng cho người phản biện").
    ///
    /// <para>
    /// Nghiệm thu chấm KHÁC xét duyệt, và khác cả về ai chấm (Điều 12.3.b):
    /// <list type="bullet">
    ///   <item>Chỉ <b>phản biện</b> phải nhận xét bằng văn bản theo BM10 — thang <b>1–5</b> mỗi mục
    ///         (1 Yếu · 2 Trung bình · 3 Khá · 4 Tốt · 5 Xuất sắc).</item>
    ///   <item><b>Mọi thành viên có mặt</b> bỏ phiếu theo BM11: chỉ <b>Đạt / Không đạt</b>, không
    ///         điểm số. Phần đó đã có ở <c>AcceptanceEvaluationForm</c>, không dùng bộ này.</item>
    /// </list>
    /// </para>
    /// <para>
    /// <b>Vì sao 4 mục chứ không phải 5:</b> BM10 liệt kê 5 đề mục, nhưng mục thứ năm là
    /// "Những nhận xét khác" — lời bình tự do, không phải một chiều đo để cho điểm. Bốn mục đầu
    /// mới là "nội dung đánh giá" mà câu "Thang điểm: Từ 1 đến 5 cho mỗi nội dung đánh giá" nhắm
    /// tới. Giao diện vẫn cho chọn đúng mức 1-5, nhưng lưu theo trọng số 5-10-15-20-25 để tổng chuẩn hóa
    /// thành <b>100</b>, giúp so sánh và hiển thị nhất quán với các phiếu khác. "Những nhận xét khác" và
    /// "Đánh giá chung" nhập ở ô nhận xét chung của phiếu chấm.
    /// </para>
    /// </summary>
    private async Task SeedAcceptanceRubricAsync()
    {
        var existingBm10 = await _db.RubricTemplates
            .Include(t => t.Criteria)
            .FirstOrDefaultAsync(t => t.TemplateType == "ACCEPTANCE" && t.Name.Contains("BM10"));

        if (existingBm10 != null)
        {
            // BM10 gốc cho người phản biện chọn mức 1-5 ở bốn nội dung. Hệ thống lại so sánh và
            // tổng hợp các phiếu theo thang 100, vì vậy lưu trọng số 25 điểm/nội dung (mức 1-5
            // tương ứng 5-10-15-20-25) thay vì để riêng bộ này ở thang 20. Khi DB demo cũ đã có
            // phiếu, nhân cả điểm chi tiết để giữ nguyên tỷ lệ đánh giá, không làm đổi ý nghĩa lịch sử.
            var activeCriteria = existingBm10.Criteria.Where(c => c.IsActive).ToList();
            if (existingBm10.MaxTotalScore == 20m
                && activeCriteria.Count == 4
                && activeCriteria.All(c => c.MaxScore == 5m))
            {
                var criterionIds = activeCriteria.Select(c => c.Id).ToList();
                var scoreDetails = await _db.ReviewScoreDetails
                    .Where(d => criterionIds.Contains(d.CriterionId))
                    .ToListAsync();

                foreach (var detail in scoreDetails)
                    detail.GivenScore *= 5m;
                foreach (var criterion in activeCriteria)
                    criterion.MaxScore = 25m;

                existingBm10.MaxTotalScore = 100m;
                await _db.SaveChangesAsync();
                _log.LogInformation("Đã chuẩn hóa bộ BM10 và {Count} điểm chi tiết từ thang 20 sang thang 100.", scoreDetails.Count);
            }

            return;
        }

        if (await _db.RubricTemplates.AnyAsync(t => t.TemplateType == "ACCEPTANCE"))
            return;

        var template = new RubricTemplate
        {
            TemplateType = "ACCEPTANCE",   // khớp ReviewRound.RoundType; BE chưa có hằng số riêng
            Name = "Phiếu nhận xét nghiệm thu (BM10 — dành cho phản biện)",
            MaxTotalScore = 100m,
            IsActive = true,
            AppliesBasic = true,
            AppliesApplied = true
        };
        _db.RubricTemplates.Add(template);
        await _db.SaveChangesAsync();

        _db.RubricCriteria.AddRange(
            new RubricCriterion { TemplateId = template.Id, CriterionName = "Tính cấp thiết của đề tài nghiên cứu",                        MaxScore = 25m, Sequence = 1, IsActive = true },
            new RubricCriterion { TemplateId = template.Id, CriterionName = "Đóng góp khoa học",                                            MaxScore = 25m, Sequence = 2, IsActive = true },
            new RubricCriterion { TemplateId = template.Id, CriterionName = "Ý nghĩa thực tiễn",                                            MaxScore = 25m, Sequence = 3, IsActive = true },
            new RubricCriterion { TemplateId = template.Id, CriterionName = "Kết quả thực tế đạt được so với sản phẩm dự kiến trong đề cương", MaxScore = 25m, Sequence = 4, IsActive = true }
        );
        await _db.SaveChangesAsync();
    }

    // Kịch bản "đã duyệt + đang thực hiện hợp đồng" (idempotent theo ProjectCode DEMO-2026-002)
    // để demo các màn sau duyệt: hợp đồng (có PHASE), giải ngân, sản phẩm/nghiệm thu.
    /// <summary>
    /// Lý lịch khoa học (BM02) cho toàn bộ tài khoản demo.
    ///
    /// <para>
    /// Trước 17/08 chỉ mỗi <c>pi.demo</c> có hồ sơ, và cũng thiếu đúng những trường mà **hợp đồng
    /// cần**: số căn cước, nơi cấp, số tài khoản, ngân hàng. Vì vậy xuất hợp đồng Word (BM05) ra
    /// toàn dấu "…" phải điền tay, còn màn lý lịch của pi2 và các thành viên hội đồng thì trống.
    /// </para>
    /// <para>
    /// Số liệu công bố để mức hợp lý theo học vị, không phải con số gây chú ý — QĐ543 không quy
    /// định ngưỡng công bố để làm chủ nhiệm, nên đây thuần tuý là dữ liệu demo.
    /// Chạy lại nhiều lần an toàn: chỉ thêm cho người CHƯA có hồ sơ.
    /// </para>
    /// </summary>
    private async Task SeedAcademicProfilesAsync()
    {
        var rows = new (string Email, string Title, string Degree, string Spec, int Year,
                        int Isi, int Intl, int Dom, int Patents, int Phd, int Master)[]
        {
            ("pi.demo@furpms.edu.vn",        "TS",    "Tiến sĩ",  "Khoa học máy tính",         1980, 6,  4, 11, 1, 2, 9),
            ("pi2.demo@furpms.edu.vn",       "ThS",   "Thạc sĩ",  "Hệ thống thông tin",        1987, 1,  2,  7, 0, 0, 4),
            ("reviewer1.demo@furpms.edu.vn", "PGS.TS","Tiến sĩ",  "Trí tuệ nhân tạo",          1972, 21, 14, 30, 3, 7, 26),
            ("reviewer2.demo@furpms.edu.vn", "TS",    "Tiến sĩ",  "Khoa học dữ liệu",          1983, 9,  6, 15, 1, 1, 12),
            ("reviewer3.demo@furpms.edu.vn", "TS",    "Tiến sĩ",  "Xử lý ngôn ngữ tự nhiên",   1981, 12, 7, 18, 2, 3, 15),
            ("reviewer4.demo@furpms.edu.vn", "TS",    "Tiến sĩ",  "Công nghệ phần mềm",        1985, 7,  5, 13, 0, 1, 10),
            ("reviewer5.demo@furpms.edu.vn", "ThS",   "Thạc sĩ",  "Kỹ thuật máy tính",         1989, 2,  3,  8, 0, 0, 3),
            ("staff.demo@furpms.edu.vn",     "ThS",   "Thạc sĩ",  "Quản lý khoa học",          1986, 0,  1,  5, 0, 0, 2),
        };

        var emails = rows.Select(r => r.Email).ToList();
        var users = await _db.Users.IgnoreQueryFilters()
            .Where(u => emails.Contains(u.Email)).ToListAsync();
        var existing = await _db.AcademicProfiles
            .Where(a => users.Select(u => u.Id).Contains(a.UserId)).ToListAsync();

        var added = 0;
        var seq = 0;
        foreach (var r in rows)
        {
            var user = users.FirstOrDefault(u => u.Email == r.Email);
            if (user == null) continue;
            seq++;

            // Số điện thoại nằm ở bảng users — hợp đồng BM05 in "Điện thoại: …" lấy từ đây.
            if (string.IsNullOrWhiteSpace(user.Phone))
                user.Phone = $"09{(12 + seq):D2}{(100000 + seq * 7777) % 1000000:D6}";

            // Hồ sơ CÓ SẴN thì chỉ bù trường còn trống, KHÔNG ghi đè thứ người dùng đã nhập.
            // (pi.demo được tạo từ trước với hồ sơ thiếu đúng phần hợp đồng cần.)
            var current = existing.FirstOrDefault(a => a.UserId == user.Id);
            if (current != null)
            {
                current.NationalId ??= $"0010{DateTime.UtcNow.Year % 100:D2}{seq:D6}";
                current.NationalIdIssuedDate ??= new DateOnly(2021, 6, 1).AddDays(seq * 11);
                current.NationalIdIssuedPlace ??= "Cục Cảnh sát QLHC về TTXH";
                current.BankAccountNumber ??= $"19{seq:D2}8800{seq:D4}";
                current.BankName ??= "Ngân hàng TMCP Kỹ Thương Việt Nam (Techcombank) — CN Hoà Lạc";
                if (current.IsiScopusCount == 0) current.IsiScopusCount = r.Isi;
                if (current.DomesticJournalCount == 0) current.DomesticJournalCount = r.Dom;
                current.UpdatedAt = DateTime.UtcNow;
                continue;
            }

            _db.AcademicProfiles.Add(new AcademicProfile
            {
                UserId = user.Id,
                AcademicTitle = r.Title,
                DegreeLevel = r.Degree,
                Specialization = r.Spec,
                SpecializationAreas = r.Spec,
                DateOfBirth = new DateOnly(r.Year, 3 + (seq % 9), 5 + (seq % 20)),
                Gender = seq % 3 == 0 ? "Nữ" : "Nam",
                Hometown = "Hà Nội",
                Nationality = "Việt Nam",
                Institution = "Trường Đại học FPT",
                InstitutionAddress = "Khu Công nghệ cao Hoà Lạc, Thạch Thất, Hà Nội",
                IsiScopusCount = r.Isi,
                IntlJournalCount = r.Intl,
                DomesticJournalCount = r.Dom,
                IntlConferenceCount = r.Intl + 2,
                DomesticConferenceCount = r.Dom + 3,
                PatentsCount = r.Patents,
                PhdSupervisedCount = r.Phd,
                MasterSupervisedCount = r.Master,
                IsEligiblePi = true,
                // Những trường dưới đây là thứ BM05 (hợp đồng) bốc ra để điền sẵn.
                NationalId = $"0010{DateTime.UtcNow.Year % 100:D2}{seq:D6}",
                NationalIdIssuedDate = new DateOnly(2021, 6, 1).AddDays(seq * 11),
                NationalIdIssuedPlace = "Cục Cảnh sát QLHC về TTXH",
                BankAccountNumber = $"19{seq:D2}8800{seq:D4}",
                BankName = "Ngân hàng TMCP Kỹ Thương Việt Nam (Techcombank) — CN Hoà Lạc",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            added++;
        }

        await _db.SaveChangesAsync();
    }

    private async Task SeedApprovedScenarioAsync()
    {
        const string code = "DEMO-2026-002";
        if (await _db.Projects.IgnoreQueryFilters().AnyAsync(p => p.ProjectCode == code))
            return;

        var admin = await _db.Users.IgnoreQueryFilters().FirstAsync(u => u.Email == "admin@furpms.edu.vn");
        var pi = await _db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Email == "pi.demo@furpms.edu.vn");
        var cycle = await _db.ResearchCycles.FirstOrDefaultAsync(c => c.CycleYear == 2026 && c.SemesterCode == "SU26");
        var track = await _db.ResearchTracks.FirstOrDefaultAsync(t => t.Code == "AI");
        var rt = await _db.ResearchTypes.FirstOrDefaultAsync(t => t.Code == "APPLIED");
        var unit = await _db.OrganizationalUnits.FirstOrDefaultAsync(u => u.Code == "DEMO-UNIT");
        if (pi == null || cycle == null || track == null || rt == null || unit == null)
            return; // SeedDemoProposalAsync chạy trước đã tạo các bản ghi này

        var category = await _db.ProductCategories.FirstOrDefaultAsync(c => c.Code == "ARTICLE");
        if (category == null)
        {
            category = new ProductCategory { Code = "ARTICLE", Name = "Bài báo khoa học", IsActive = true };
            _db.ProductCategories.Add(category);
            await _db.SaveChangesAsync();
        }

        var cycleTrack = await GetOrCreateCycleTrackAsync(cycle.Id, track.Id);
        var defaultOrder = await GetOrCreateDefaultOrderAsync(cycle.Id, unit.Id, admin.Id);

        var now = DateTime.UtcNow;

        // PROJECT (gốc) — đang thực hiện hợp đồng
        var project = new Project
        {
            Id = Guid.NewGuid(),
            ProjectCode = code,
            CycleTrackId = cycleTrack.Id,
            OrderId = defaultOrder.Id,
            PiUserId = pi.Id,
            HostingUnitId = unit.Id,
            ResearchTypeId = rt.Id,
            TitleVi = "Hệ thống khuyến nghị học liệu cá nhân hoá dựa trên học sâu",
            TitleEn = "Personalized Learning Material Recommendation using Deep Learning",
            Status = ProjectStatus.InProgress,
            PlannedStartDate = new DateOnly(2026, 3, 1),
            PlannedEndDate = new DateOnly(2027, 3, 1),
            CreatedAt = now.AddDays(-70),
            UpdatedAt = now
        };
        _db.Projects.Add(project);

        var proposal = new Proposal
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            VersionNo = 1,
            IsCurrent = true,
            TitleVi = project.TitleVi,
            TitleEn = project.TitleEn,
            AbstractVi = "Đề tài xây dựng hệ thống khuyến nghị học liệu cá nhân hoá cho sinh viên dựa trên mô hình học sâu; đã được hội đồng phê duyệt và ký hợp đồng triển khai.",
            ResearchObjectives = "1. Xây dựng mô hình khuyến nghị cá nhân hoá.\n2. Tích hợp vào hệ thống LMS.\n3. Đánh giá hiệu quả trên dữ liệu thực tế.",
            Methodology = "Học sâu (collaborative filtering + content-based), thử nghiệm A/B trên dữ liệu thực tế.",
            ExpectedOutput = "01 bài báo hội nghị quốc tế, 01 hệ thống tích hợp LMS.",
            DurationMonths = 12,
            PlannedStartDate = new DateOnly(2026, 3, 1),
            PlannedEndDate = new DateOnly(2027, 3, 1),
            FundingMethod = FundingMethod.Partial,
            Status = ProposalStatus.Approved,
            SubmittedAt = now.AddDays(-60),
            ReviewedAt = now.AddDays(-30),
            ApprovedAt = now.AddDays(-30),
            ApprovedBy = admin.Id,
            CreatedAt = now.AddDays(-70),
            UpdatedAt = now
        };
        _db.Proposals.Add(proposal);
        await _db.SaveChangesAsync();

        _db.ProposalBudgets.Add(new ProposalBudget
        {
            ProposalId = proposal.Id, TotalAmount = 145_000_000m,
            LaborAmount = 90_000_000m, EquipmentAmount = 26_000_000m,
            ConferenceAmount = 15_000_000m, OfficeSuppliesAmount = 10_000_000m,
            IncidentalIpAmount = 4_000_000m
        });
        _db.ProjectMembers.AddRange(
            new ProjectMember { ProjectId = project.Id, FullName = pi.FullName, Email = pi.Email, UnitName = "Khoa CNTT", WorkContent = "Chủ nhiệm", WorkMonths = 8, IsPi = true, Sequence = 1 },
            new ProjectMember { ProjectId = project.Id, FullName = "Đỗ Thị Em", Email = "em.dt@fpt.edu.vn", UnitName = "SE", WorkContent = "Thành viên chính", WorkMonths = 5, Sequence = 2 }
        );
        await _db.SaveChangesAsync();

        var contract = new Contract
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            ContractNumber = "HD-2026-002",
            ScopeTitle = "Toàn bộ nội dung đề tài (2 giai đoạn)",
            Status = ContractStatus.Active,
            SignedAt = now.AddDays(-25),
            TotalAmount = 145_000_000m,
            StartDate = new DateOnly(2026, 3, 1),
            EndDate = new DateOnly(2027, 3, 1),
            OriginalEndDate = new DateOnly(2027, 3, 1),
            MaxExtensionMonths = 6,
            SideARepresentative = "Nguyễn Kim Ánh",
            CreatedBy = admin.Id,
            CreatedAt = now.AddDays(-26),
            UpdatedAt = now
        };
        _db.Contracts.Add(contract);
        await _db.SaveChangesAsync();

        // Giai đoạn thực hiện trong hợp đồng (Review 2 điểm e)
        var phase1 = new ContractPhase { ContractId = contract.Id, PhaseNo = 1, Name = "Giai đoạn 1 — Mô hình & bài báo", StartDate = new DateOnly(2026, 3, 1), EndDate = new DateOnly(2026, 9, 1), Amount = 87_000_000m, Status = "IN_PROGRESS" };
        var phase2 = new ContractPhase { ContractId = contract.Id, PhaseNo = 2, Name = "Giai đoạn 2 — Tích hợp LMS & nghiệm thu", StartDate = new DateOnly(2026, 9, 1), EndDate = new DateOnly(2027, 3, 1), Amount = 58_000_000m, Status = "PLANNED" };
        _db.ContractPhases.AddRange(phase1, phase2);
        await _db.SaveChangesAsync();

        // Sản phẩm của PROJECT — gắn hợp đồng + phase (gộp expected_product + deliverable)
        var deliv1 = new ProjectDeliverable { ProjectId = project.Id, ContractId = contract.Id, ContractPhaseId = phase1.Id, CategoryId = category.Id, ProductName = "Bài báo hội nghị quốc tế", DueDate = new DateOnly(2026, 9, 1), AcceptanceStatus = AcceptanceStatus.Passed, IsCompleted = true, SubmittedAt = now.AddDays(-10), FileUrl = "https://example.com/paper.pdf", QualityAssessment = "Đạt yêu cầu, đã được chấp nhận đăng", Sequence = 1 };
        var deliv2 = new ProjectDeliverable { ProjectId = project.Id, ContractId = contract.Id, ContractPhaseId = phase2.Id, CategoryId = category.Id, ProductName = "Hệ thống tích hợp LMS", DueDate = new DateOnly(2027, 2, 1), AcceptanceStatus = AcceptanceStatus.Pending, IsCompleted = false, Sequence = 2 };
        _db.ProjectDeliverables.AddRange(deliv1, deliv2);
        await _db.SaveChangesAsync();

        // QĐ543 Điều 16.1 — đề tài ỨNG DỤNG giải ngân 04 đợt 30–30–30–10 (khớp bảng
        // disbursement_templates ở SeedDisbursementTemplatesAsync).
        _db.ContractDisbursements.AddRange(
            new ContractDisbursement { ContractId = contract.Id, RoundNumber = 1, PhaseId = phase1.Id, Percentage = 30m, PlannedAmount = 43_500_000m, ActualAmount = 43_500_000m, ConditionDescription = "Sau khi ký kết Hợp đồng NCKH với Chủ nhiệm đề tài", Status = DisbursementStatus.Disbursed, DisbursedAt = now.AddDays(-24), BankReference = "FT2026030100123" },
            new ContractDisbursement { ContractId = contract.Id, RoundNumber = 2, PhaseId = phase1.Id, Percentage = 30m, PlannedAmount = 43_500_000m, ConditionDescription = "Sau khi đánh giá tiến độ giai đoạn 1 đạt yêu cầu", Status = DisbursementStatus.Pending, DeliverableId = deliv1.Id },
            new ContractDisbursement { ContractId = contract.Id, RoundNumber = 3, PhaseId = phase2.Id, Percentage = 30m, PlannedAmount = 43_500_000m, ConditionDescription = "Sau khi đánh giá tiến độ giai đoạn 2 đạt yêu cầu", Status = DisbursementStatus.Pending, DeliverableId = deliv2.Id },
            new ContractDisbursement { ContractId = contract.Id, RoundNumber = 4, PhaseId = phase2.Id, Percentage = 10m, PlannedAmount = 14_500_000m,  ConditionDescription = "Sau khi Hội đồng nghiệm thu đánh giá \"Đạt\"", Status = DisbursementStatus.Pending, DeliverableId = deliv2.Id }
        );
        await _db.SaveChangesAsync();
    }

    private async Task SeedRolesAsync()
    {
        var roleNames = new[] { "Admin", "Staff", "Faculty", "ReviewCommittee" };
        foreach (var name in roleNames)
        {
            if (!await _db.Roles.AnyAsync(r => r.Name == name))
                _db.Roles.Add(new Role { Name = name, Description = name + " role" });
        }
        await _db.SaveChangesAsync();
    }


    /// <summary>
    /// Mật khẩu ban đầu của tài khoản quản trị.
    /// <para>Development ⇒ mật khẩu demo. Ngoài ra ⇒ cấu hình, hoặc sinh ngẫu nhiên rồi ghi log.</para>
    /// </summary>
    private string ResolveAdminPassword()
    {
        if (_env.IsDevelopment()) return DemoPassword;

        var configured = _config["SeedAdminPassword"];
        if (!string.IsNullOrWhiteSpace(configured)) return configured;

        var generated = Convert.ToBase64String(
            System.Security.Cryptography.RandomNumberGenerator.GetBytes(18)).TrimEnd('=');
        _log.LogWarning(
            "═══ Đã tạo tài khoản quản trị với mật khẩu NGẪU NHIÊN: {Password} ═══", generated);
        _log.LogWarning(
            "Ghi lại ngay và đổi sau lần đăng nhập đầu — mật khẩu này chỉ hiện MỘT LẦN trong log. " +
            "Đặt biến SeedAdminPassword để tự chọn mật khẩu ban đầu.");
        return generated;
    }
    private async Task SeedAdminAsync()
    {
        const string adminEmail = "admin@furpms.edu.vn";
        if (await _db.Users.IgnoreQueryFilters().AnyAsync(u => u.Email == adminEmail))
            return;

        var adminRole = await _db.Roles.FirstAsync(r => r.Name == "Admin");

        var admin = new User
        {
            Id = Guid.NewGuid(),
            Email = adminEmail,
            FullName = "System Administrator admin",
            // Ở Development dùng mật khẩu demo cho tiện. Ngoài Development thì KHÔNG:
            // mật khẩu đó nằm trong mã nguồn, ai đọc repo cũng đăng nhập được bằng quyền quản trị.
            // Lấy từ cấu hình `SeedAdminPassword`; không đặt thì sinh ngẫu nhiên và ghi log MỘT LẦN
            // để người triển khai đổi ngay — thà bắt họ đi tìm còn hơn để cửa mở.
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(ResolveAdminPassword(), workFactor: 12),
            Status = UserStatus.Active,
            IsExternal = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.Users.Add(admin);
        await _db.SaveChangesAsync();

        _db.UserRoles.Add(new UserRole
        {
            UserId = admin.Id,
            RoleId = adminRole.Id,
            AssignedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
    }

    private async Task SeedPersonnelRoleTypesAsync()
    {
        var roles = new[]
        {
            new PersonnelRoleType { Code = "CNNV", Name = "Chủ nhiệm nghiên cứu viên", DefaultCoefficient = 0.79m, IsActive = true },
            new PersonnelRoleType { Code = "TKKH", Name = "Thư ký khoa học",           DefaultCoefficient = 0.49m, IsActive = true },
            new PersonnelRoleType { Code = "TVC",  Name = "Thành viên chính",           DefaultCoefficient = 0.49m, IsActive = true },
            new PersonnelRoleType { Code = "TV",   Name = "Thành viên",                 DefaultCoefficient = 0.25m, IsActive = true },
            new PersonnelRoleType { Code = "KTV",  Name = "Kỹ thuật viên",              DefaultCoefficient = 0.30m, IsActive = true },
        };

        foreach (var r in roles)
        {
            if (!await _db.PersonnelRoleTypes.AnyAsync(x => x.Code == r.Code))
                _db.PersonnelRoleTypes.Add(r);
        }
        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Hạng mục dự toán — đúng <b>06 hạng mục của QĐ543 Điều 15.1</b> kèm tỷ lệ tối đa tính trên
    /// tổng kinh phí đề tài: thù lao 100% · thiết bị/vật tư 60% · thuê ngoài 60% · hội thảo 30% ·
    /// văn phòng phẩm &amp; chi khác 20% · phát sinh/SHTT 10%.
    /// <para>
    /// Trước đây seed <b>12</b> hạng mục lấy từ mẫu thuyết minh cấp Bộ (có "Chi đoàn ra", "Quản lý
    /// phí"…) — những hạng mục đó <b>không nằm trong quy định của trường</b> nên không soi được tỷ
    /// lệ nào, và chủ nhiệm phải điền một mớ ô không dùng tới.
    /// </para>
    /// <para>
    /// Hạng mục cũ <b>không xoá</b> mà chỉ <c>IsActive = false</c>: dự toán đã lưu vẫn trỏ FK vào
    /// chúng, xoá là gãy dữ liệu cũ. Ngừng hiện ở form là đủ.
    /// </para>
    /// </summary>
    private async Task SeedBudgetExpenseCategoriesAsync()
    {
        // (code, tên theo Điều 15, tỷ lệ tối đa %, thứ tự)
        var categories = new[]
        {
            new BudgetExpenseCategory { Code = "LABOR",         Name = "Thù lao nghiên cứu",                  MaxPercentage = 100m, Sequence = 1, IsActive = true },
            new BudgetExpenseCategory { Code = "EQUIPMENT",     Name = "Thiết bị, vật tư, nguyên liệu",       MaxPercentage = 60m,  Sequence = 2, IsActive = true },
            new BudgetExpenseCategory { Code = "OUTSOURCED",    Name = "Thuê ngoài",                          MaxPercentage = 60m,  Sequence = 3, IsActive = true },
            new BudgetExpenseCategory { Code = "CONFERENCE",    Name = "Hội nghị/hội thảo/seminar",           MaxPercentage = 30m,  Sequence = 4, IsActive = true },
            new BudgetExpenseCategory { Code = "OFFICE_OTHER",  Name = "Văn phòng phẩm, chi khác",            MaxPercentage = 20m,  Sequence = 5, IsActive = true },
            new BudgetExpenseCategory { Code = "INCIDENTAL_IP", Name = "Chi phí phát sinh, sở hữu trí tuệ",   MaxPercentage = 10m,  Sequence = 6, IsActive = true },
        };

        foreach (var c in categories)
        {
            var existing = await _db.BudgetExpenseCategories.FirstOrDefaultAsync(x => x.Code == c.Code);
            if (existing == null)
            {
                _db.BudgetExpenseCategories.Add(c);
                continue;
            }
            // LABOR / OUTSOURCED / CONFERENCE đã tồn tại từ bộ 12 cũ với tên khác và không có tỷ lệ
            // → cập nhật tại chỗ, giữ nguyên Id để dự toán cũ không mất liên kết.
            existing.Name = c.Name;
            existing.MaxPercentage = c.MaxPercentage;
            existing.Sequence = c.Sequence;
            existing.IsActive = true;
        }

        var keepCodes = categories.Select(c => c.Code).ToArray();
        var legacy = await _db.BudgetExpenseCategories
            .Where(x => !keepCodes.Contains(x.Code) && x.IsActive)
            .ToListAsync();
        foreach (var l in legacy)
            l.IsActive = false;

        await _db.SaveChangesAsync();
    }

    private async Task SeedAmendmentCategoriesAsync()
    {
        // BM07 chia đúng 4 nhóm nội dung. Thay đổi nhân sự vẫn được Điều 10.2 cho phép nhưng
        // biểu mẫu xếp vào "thay đổi khác", không cần một loại riêng làm form dài thêm.
        var cats = new[]
        {
            new AmendmentCategory { Code = "SCOPE_CHANGE",  Name = "Thay đổi nội dung nghiên cứu hoặc tên đề tài", IsActive = true },
            new AmendmentCategory { Code = "EXTENSION",     Name = "Thay đổi tiến độ, thời gian nghiên cứu (gia hạn)", IsActive = true },
            new AmendmentCategory { Code = "BUDGET_ADJUST", Name = "Thay đổi dự toán kinh phí", IsActive = true },
            new AmendmentCategory { Code = "OTHER",         Name = "Thay đổi khác (gồm cả nhân sự)", IsActive = true },
        };
        foreach (var c in cats)
        {
            var existing = await _db.AmendmentCategories.FirstOrDefaultAsync(x => x.Code == c.Code);
            if (existing == null)
                _db.AmendmentCategories.Add(c);
            else
            {
                existing.Name = c.Name;
                existing.IsActive = true;
            }
        }

        var legacyTeam = await _db.AmendmentCategories.FirstOrDefaultAsync(x => x.Code == "TEAM_CHANGE");
        if (legacyTeam != null) legacyTeam.IsActive = false;
        await _db.SaveChangesAsync();
    }

    /// <summary>
    /// Bù dữ liệu đã có trước khi trạng thái SETTLED/TERMINATED được đưa vào state machine.
    /// Không xoá hay tạo hồ sơ: chỉ suy trạng thái hợp đồng từ chính mốc pháp lý đã lưu.
    /// </summary>
    private async Task NormalizeContractClosureStatusesAsync()
    {
        var settledIds = await _db.ContractSettlements
            .Where(s => s.SettlementSignedAt != null)
            .Select(s => s.ContractId)
            .ToListAsync();

        var contracts = await _db.Contracts
            .Where(c => settledIds.Contains(c.Id) || c.TerminatedAt != null)
            .ToListAsync();
        foreach (var contract in contracts)
        {
            contract.Status = contract.TerminatedAt.HasValue
                ? ContractStatus.Terminated
                : ContractStatus.Settled;
        }

        if (contracts.Count > 0) await _db.SaveChangesAsync();
    }

    private async Task SeedSystemFinancialConfigsAsync()
    {
        if (!await _db.SystemFinancialConfigs.AnyAsync(x => x.Code == "BASE_DAILY_SALARY"))
        {
            _db.SystemFinancialConfigs.Add(new SystemFinancialConfig
            {
                Code = "BASE_DAILY_SALARY",
                Value = 1_490_000m,
                Description = "Mức lương cơ bản ngày (QĐ 543/QĐ-ĐHFPT)",
                EffectiveDate = new DateOnly(2025, 1, 1),
                IsActive = true
            });
            await _db.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Mốc giải ngân theo QĐ543 **Điều 16** — lịch phụ thuộc **LOẠI ĐỀ TÀI**, không phải lựa chọn
    /// của chủ nhiệm:
    /// <list type="bullet">
    /// <item><b>Ứng dụng</b>: 04 đợt <b>30–30–30–10</b>. Đợt 1 sau khi ký hợp đồng; đợt 2 và 3 sau
    /// khi đánh giá tiến độ giai đoạn 1, giai đoạn 2 "Đạt"; đợt 4 sau khi nghiệm thu "Đạt".</item>
    /// <item><b>Cơ bản</b>: toàn bộ kinh phí giải ngân <b>01 lần</b> sau khi nghiệm thu "Đạt".</item>
    /// </list>
    /// <para>
    /// Để trong bảng master data (không cắm số vào code) để Phòng QLKH đổi được khi quy định đổi —
    /// cẩm nang capstone gọi việc cắm tỷ lệ 30/30/30/10 vào mã nguồn là hardcode tham số nghiệp vụ.
    /// </para>
    /// </summary>
    /// <summary>
    /// Dọn cấu hình <c>DISBURSEMENT_WHOLE_TRANCHES</c> khỏi DB đã triển khai.
    /// <para>
    /// Từ 11/08 lịch giải ngân sinh theo <b>loại đề tài</b> (QĐ543 Điều 16, bảng
    /// <c>disbursement_templates</c>) nên cấu hình này <b>không còn ảnh hưởng gì</b>. Để lại thì
    /// Admin vẫn thấy nó trên màn Cài đặt, sửa xong tưởng có tác dụng — mà phần mô tả lại viện dẫn
    /// QĐ543 sai ("yêu cầu tối thiểu 3 đợt"). Nút bấm không làm gì còn tệ hơn không có nút.
    /// </para>
    /// </summary>
    private async Task RemoveObsoleteSettingsAsync()
    {
        var obsolete = await _db.SystemSettings
            .Where(s => s.Key == "DISBURSEMENT_WHOLE_TRANCHES")
            .ToListAsync();
        if (obsolete.Count == 0) return;

        _db.SystemSettings.RemoveRange(obsolete);
        await _db.SaveChangesAsync();
    }

    private async Task SeedDisbursementTemplatesAsync()
    {
        if (await _db.DisbursementTemplates.AnyAsync()) return;

        var applied = await _db.ResearchTypes.FirstOrDefaultAsync(t => t.Code == "APPLIED");
        var basic = await _db.ResearchTypes.FirstOrDefaultAsync(t => t.Code == "BASIC");

        var rows = new List<DisbursementTemplate>();
        if (applied != null)
        {
            rows.AddRange(new[]
            {
                new DisbursementTemplate { ResearchTypeId = applied.Id, RoundNumber = 1, Percentage = 30m, ConditionDescription = "Sau khi ký kết Hợp đồng NCKH với Chủ nhiệm đề tài", IsActive = true },
                new DisbursementTemplate { ResearchTypeId = applied.Id, RoundNumber = 2, Percentage = 30m, ConditionDescription = "Sau khi đánh giá tiến độ giai đoạn 1 đạt yêu cầu", IsActive = true },
                new DisbursementTemplate { ResearchTypeId = applied.Id, RoundNumber = 3, Percentage = 30m, ConditionDescription = "Sau khi đánh giá tiến độ giai đoạn 2 đạt yêu cầu", IsActive = true },
                new DisbursementTemplate { ResearchTypeId = applied.Id, RoundNumber = 4, Percentage = 10m, ConditionDescription = "Sau khi Hội đồng nghiệm thu đánh giá \"Đạt\"", IsActive = true },
            });
        }
        if (basic != null)
        {
            rows.Add(new DisbursementTemplate { ResearchTypeId = basic.Id, RoundNumber = 1, Percentage = 100m, ConditionDescription = "Sau khi Hội đồng nghiệm thu đánh giá \"Đạt\"", IsActive = true });
        }
        if (rows.Count == 0) return;

        _db.DisbursementTemplates.AddRange(rows);
        await _db.SaveChangesAsync();
    }

    private async Task SeedSystemSettingsAsync()
    {
        var defaults = new[]
        {
            new SystemSetting
            {
                Key = SystemSettingKeys.UploadMaxFileSizeMb,
                Value = SystemSettingKeys.RecommendedMaxFileSizeMb.ToString(),
                RecommendedValue = SystemSettingKeys.RecommendedMaxFileSizeMb.ToString(),
                Description = $"Dung lượng tối đa mỗi file tài liệu đính kèm (MB). " +
                              $"Cho phép {SystemSettingKeys.MinAllowedFileSizeMb}–{SystemSettingKeys.MaxAllowedFileSizeMb} MB, " +
                              $"khuyến cáo {SystemSettingKeys.RecommendedMaxFileSizeMb} MB."
            },
            new SystemSetting
            {
                Key = SystemSettingKeys.UploadAllowedExtensions,
                Value = SystemSettingKeys.DefaultAllowedExtensions,
                RecommendedValue = SystemSettingKeys.DefaultAllowedExtensions,
                Description = "Các định dạng file được phép tải lên, phân tách bằng dấu phẩy."
            },
            new SystemSetting
            {
                Key = SystemSettingKeys.CouncilInviteDeadlineDays,
                Value = SystemSettingKeys.DefaultCouncilInviteDeadlineDays.ToString(),
                RecommendedValue = SystemSettingKeys.DefaultCouncilInviteDeadlineDays.ToString(),
                Description = "Số ngày reviewer được phép xác nhận/từ chối lời mời trước khi quá hạn (1–60)."
            },
            // Công tắc này đã có hằng số + đã được CouncilService đọc từ lâu, nhưng chưa bao giờ
            // được seed ⇒ không có dòng nào trong bảng ⇒ màn Cấu hình hệ thống không bày ra được.
            // Tính năng vẫn chạy theo giá trị mặc định, chỉ là Admin không tắt được bằng giao diện.
            new SystemSetting
            {
                Key = SystemSettingKeys.CouncilAllowRespondOnBehalf,
                Value = SystemSettingKeys.DefaultCouncilAllowRespondOnBehalf.ToString().ToLowerInvariant(),
                RecommendedValue = SystemSettingKeys.DefaultCouncilAllowRespondOnBehalf.ToString().ToLowerInvariant(),
                Description = "Cho phép chuyên viên xác nhận/từ chối thư mời THAY thành viên hội đồng " +
                              "(thầy trả lời qua điện thoại, chuyên viên ghi nhận hộ). Tắt = chỉ thành viên tự trả lời. " +
                              "Dù bật, hệ thống luôn ghi lại ai đã bấm hộ."
            },
            new SystemSetting
            {
                Key = SystemSettingKeys.ScoreDecimalPlaces,
                Value = SystemSettingKeys.DefaultScoreDecimalPlaces.ToString(),
                RecommendedValue = SystemSettingKeys.DefaultScoreDecimalPlaces.ToString(),
                Description = "Số chữ số thập phân khi chấm điểm (0 = chỉ số nguyên, 1 = cho 0.5/7.5…). " +
                              "QĐ543 không quy định; BM03 để điểm tối đa toàn số nguyên nên mặc định 0. " +
                              "Đổi KHÔNG hồi tố — chỉ áp cho phiếu chấm mới."
            },
            new SystemSetting
            {
                Key = SystemSettingKeys.DeadlineReminderDays,
                Value = SystemSettingKeys.DefaultDeadlineReminderDays,
                RecommendedValue = SystemSettingKeys.DefaultDeadlineReminderDays,
                Description = "Nhắc PI trước hạn nộp sản phẩm bao nhiêu ngày, cách nhau bằng dấu phẩy (vd: 30,14,7)."
            },
            // ── Rà trùng lặp đề cương (thêm 26/08 — gạch 3 của biên bản) ──
            new SystemSetting
            {
                Key = SystemSettingKeys.AiDuplicateThreshold,
                Value = SystemSettingKeys.DefaultAiDuplicateThreshold,
                RecommendedValue = SystemSettingKeys.DefaultAiDuplicateThreshold,
                Description = "Độ tương đồng từ mức này trở lên thì cảnh báo Phòng QLKH xem lại (0–1). " +
                              "Con số 0.86 nằm GIỮA khoảng trống đo được: cặp trùng thật thấp nhất 0.889, cặp cùng lĩnh vực " +
                              "khác đề tài cao nhất 0.835 (bộ 35 cặp gán nhãn, gemini-embedding-001). Xem docs/AI_Duplicate_Detection.md."
            },
            new SystemSetting
            {
                Key = SystemSettingKeys.AiDuplicateTopK,
                Value = SystemSettingKeys.DefaultAiDuplicateTopK.ToString(),
                RecommendedValue = SystemSettingKeys.DefaultAiDuplicateTopK.ToString(),
                Description = "Lấy bao nhiêu đề tài giống nhất để đối chiếu (1–20)."
            },
            new SystemSetting
            {
                Key = SystemSettingKeys.AiDuplicateBlockThreshold,
                Value = SystemSettingKeys.DefaultAiDuplicateBlockThreshold,
                RecommendedValue = SystemSettingKeys.DefaultAiDuplicateBlockThreshold,
                Description = "Từ mức này trở lên thì đánh dấu 'gần như trùng khít'. " +
                              "Hệ thống VẪN không tự chặn nộp — kết luận là của Phòng QLKH (rule #12)."
            },
            // ── Ngưỡng cảnh báo kết luận lệch điểm (thêm 25/08 — góp ý hội đồng) ──
            new SystemSetting
            {
                Key = SystemSettingKeys.ReviewPassThresholdPct,
                Value = SystemSettingKeys.DefaultReviewPassThresholdPct.ToString(),
                RecommendedValue = SystemSettingKeys.DefaultReviewPassThresholdPct.ToString(),
                Description = "Điểm trung bình dưới bao nhiêu phần trăm thang điểm thì coi là thấp. " +
                              "Kết luận LỆCH với ngưỡng này (điểm thấp mà Đạt, hoặc điểm cao mà Không đạt) " +
                              "buộc Thư ký phải ghi rõ lý do. Hệ thống KHÔNG tự kết luận thay hội đồng."
            },
            // ── Hạn từng giai đoạn đề tài (thêm 25/08 — biên bản bảo vệ lần 2) ──
            new SystemSetting
            {
                Key = SystemSettingKeys.ScoringWindowDays,
                Value = SystemSettingKeys.DefaultScoringWindowDays.ToString(),
                RecommendedValue = SystemSettingKeys.DefaultScoringWindowDays.ToString(),
                Description = "Hội đồng có bao nhiêu ngày để chấm xong một vòng, tính từ lúc vòng được mở. " +
                              "Quá hạn thì vòng bị gắn cờ, KHÔNG tự đóng — kết luận vẫn là của Chủ tịch (rule #12)."
            },
            new SystemSetting
            {
                Key = SystemSettingKeys.RevisionDeadlineDays,
                Value = SystemSettingKeys.DefaultRevisionDeadlineDays.ToString(),
                RecommendedValue = SystemSettingKeys.DefaultRevisionDeadlineDays.ToString(),
                Description = "Chủ nhiệm có bao nhiêu ngày để nộp bản chỉnh sửa sau khi hội đồng yêu cầu sửa."
            },
            new SystemSetting
            {
                Key = SystemSettingKeys.FinalReportLeadDays,
                Value = SystemSettingKeys.DefaultFinalReportLeadDays.ToString(),
                RecommendedValue = SystemSettingKeys.DefaultFinalReportLeadDays.ToString(),
                Description = "Báo cáo nghiệm thu phải nộp trước ngày kết thúc đề tài bao nhiêu ngày. " +
                              "QĐ543 Điều 11.2.a quy định ít nhất 30 ngày — đổi số này là đổi luật, cân nhắc kỹ."
            },
            new SystemSetting
            {
                Key = SystemSettingKeys.ContractSignWindowDays,
                Value = SystemSettingKeys.DefaultContractSignWindowDays.ToString(),
                RecommendedValue = SystemSettingKeys.DefaultContractSignWindowDays.ToString(),
                Description = "Số ngày để ký hợp đồng kể từ khi hội đồng chốt duyệt đề cương."
            },
            new SystemSetting
            {
                Key = SystemSettingKeys.ArchivalLeadDays,
                Value = SystemSettingKeys.DefaultArchivalLeadDays.ToString(),
                RecommendedValue = SystemSettingKeys.DefaultArchivalLeadDays.ToString(),
                Description = "Số ngày để hoàn tất lưu trữ hồ sơ sau khi nộp báo cáo tổng kết."
            },
            new SystemSetting
            {
                Key = SystemSettingKeys.MeetingDeadlineWorkingDays,
                Value = SystemSettingKeys.DefaultMeetingDeadlineWorkingDays.ToString(),
                RecommendedValue = SystemSettingKeys.DefaultMeetingDeadlineWorkingDays.ToString(),
                Description = "Hội đồng phải họp trong bao nhiêu NGÀY LÀM VIỆC kể từ khi được lập (QĐ543 Điều 8.3.a)."
            },
            new SystemSetting
            {
                Key = SystemSettingKeys.EmailEnabled,
                Value = SystemSettingKeys.DefaultEmailEnabled.ToString().ToLowerInvariant(),
                RecommendedValue = SystemSettingKeys.DefaultEmailEnabled.ToString().ToLowerInvariant(),
                Description = "Tắt để chạy demo mà không gửi email thật ra ngoài. Thông báo trong ứng dụng vẫn hoạt động."
            },
            new SystemSetting
            {
                Key = SystemSettingKeys.ContractSideARepresentative,
                Value = SystemSettingKeys.DefaultContractSideARepresentative,
                RecommendedValue = SystemSettingKeys.DefaultContractSideARepresentative,
                Description = "Người đại diện Bên A ký hợp đồng, dùng khi tạo hợp đồng không ghi rõ."
            },
            new SystemSetting
            {
                Key = SystemSettingKeys.DemoDataEnabled,
                // Giá trị ban đầu theo MÔI TRƯỜNG, không phải một hằng số. Trước đây luôn ghi
                // "true", nên dòng cấu hình này tự vô hiệu hoá mọi phép kiểm môi trường khác:
                // lần khởi động đầu tiên trên máy chủ là DB thật có ngay 10 đề tài giả.
                Value = _env.IsDevelopment() ? "true" : "false",
                RecommendedValue = "false",
                Description = "Seed bộ dữ liệu kịch bản demo (8 đề tài ở 8 bước của quy trình). " +
                              "Đặt false trước khi bàn giao bản chạy thật để không dính đề tài giả."
            }
        };

        // Khuyến cáo của công tắc trả lời thay đã đổi từ true → false: cập nhật phần KHUYẾN CÁO cho DB cũ
        // nhưng không tự ý ghi đè Value mà Admin đang vận hành. Admin vẫn chủ động chọn lúc nào được phép bật.
        var respondOnBehalf = await _db.SystemSettings
            .FirstOrDefaultAsync(s => s.Key == SystemSettingKeys.CouncilAllowRespondOnBehalf);
        if (respondOnBehalf != null)
        {
            var recommended = SystemSettingKeys.DefaultCouncilAllowRespondOnBehalf.ToString().ToLowerInvariant();
            if (respondOnBehalf.RecommendedValue != recommended)
                respondOnBehalf.RecommendedValue = recommended;
        }

        var existing = await _db.SystemSettings.Select(s => s.Key).ToListAsync();
        var missing = defaults.Where(d => !existing.Contains(d.Key)).ToList();
        if (missing.Count > 0) _db.SystemSettings.AddRange(missing);
        if (missing.Count > 0 || respondOnBehalf != null) await _db.SaveChangesAsync();
    }

    // ── Helpers (project-centric) ────────────────────────────────────────────

    private async Task<OrganizationalUnit> GetOrCreateDemoUnitAsync()
    {
        var unit = await _db.OrganizationalUnits.FirstOrDefaultAsync(u => u.Code == "DEMO-UNIT");
        if (unit != null) return unit;

        unit = new OrganizationalUnit
        {
            Name = "Khoa Công nghệ Thông tin (Demo)",
            Code = "DEMO-UNIT",
            UnitType = "FACULTY",
            IsActive = true,
            SortOrder = 99
        };
        _db.OrganizationalUnits.Add(unit);
        await _db.SaveChangesAsync();
        return unit;
    }

    // Review 2 điểm (b): quan hệ đợt–track.
    private async Task<CycleTrack> GetOrCreateCycleTrackAsync(int cycleId, int trackId)
    {
        var ct = await _db.CycleTracks.FirstOrDefaultAsync(x => x.CycleId == cycleId && x.TrackId == trackId);
        if (ct != null) return ct;

        ct = new CycleTrack { CycleId = cycleId, TrackId = trackId, IsOpen = true };
        _db.CycleTracks.Add(ct);
        await _db.SaveChangesAsync();
        return ct;
    }

    // Review 2 điểm (d): order "Nghiên cứu tự do/cơ bản" mặc định — 100% project thuộc 1 order.
    private async Task<ResearchOrder> GetOrCreateDefaultOrderAsync(int cycleId, int unitId, Guid createdBy)
    {
        var order = await _db.ResearchOrders.FirstOrDefaultAsync(o => o.CycleId == cycleId && o.IsDefault);
        if (order != null) return order;

        order = new ResearchOrder
        {
            CycleId = cycleId,
            OrderingUnitId = unitId,
            ResearchArea = "Nghiên cứu tự do (đề xuất của PI)",
            ProblemDescription = "Order mặc định của đợt — gom các đề tài PI tự đề xuất (không có đơn vị đặt hàng cụ thể).",
            IsDefault = true,
            Status = "OPEN",
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow
        };
        _db.ResearchOrders.Add(order);
        await _db.SaveChangesAsync();
        return order;
    }

    // Rule #8: APPLIED phải có RequireOrderingUnit=true (đăng ký theo danh mục đặt hàng).
    // DB cũ từng seed nhầm false — luôn rà và sửa (idempotent, chạy mỗi lần khởi động).
    private async Task FixAppliedOrderingUnitFlagAsync()
    {
        var applied = await _db.ResearchTypes.FirstOrDefaultAsync(x => x.Code == "APPLIED" && !x.RequireOrderingUnit);
        if (applied == null) return;
        applied.RequireOrderingUnit = true;
        await _db.SaveChangesAsync();
    }

    // ── Demo project + proposal v1 (idempotent via ProjectCode "DEMO-2026-001") ──

    private async Task SeedDemoProposalAsync()
    {
        const string demoCode = "DEMO-2026-001";
        if (await _db.Projects.IgnoreQueryFilters().AnyAsync(p => p.ProjectCode == demoCode))
            return;

        var admin = await _db.Users.IgnoreQueryFilters()
            .FirstAsync(u => u.Email == "admin@furpms.edu.vn");

        var unit = await GetOrCreateDemoUnitAsync();

        // ResearchType — APPLIED. Rule #8: Applied đi theo danh mục ĐẶT HÀNG (có OrderingUnit,
        // nhiều PI đăng ký cạnh tranh) → RequireOrderingUnit = true. FE dựa cờ này đổi luồng nộp.
        var rt = await _db.ResearchTypes.FirstOrDefaultAsync(x => x.Code == "APPLIED");
        if (rt == null)
        {
            rt = new ResearchType
            {
                Code = "APPLIED",
                Name = "Nghiên cứu ứng dụng",
                // QĐ543 Điều 14.1.b — ứng dụng/triển khai tối đa 150 triệu/đề tài.
                // Điều 14.3 cho phép vượt trần nhưng phải do Hiệu trưởng quyết định ⇒ trường hợp đó
                // Admin nâng trần ở màn Loại đề tài, hệ thống không tự mở.
                MaxBudgetCap = 150_000_000m,
                RequireOrderingUnit = true,
                RequirePublication = true,
                IsActive = true
            };
            _db.ResearchTypes.Add(rt);
            await _db.SaveChangesAsync();
        }

        // ResearchType — BASIC
        if (!await _db.ResearchTypes.AnyAsync(x => x.Code == "BASIC"))
        {
            _db.ResearchTypes.Add(new ResearchType
            {
                Code = "BASIC",
                Name = "Nghiên cứu cơ bản",
                // QĐ543 Điều 14.1.a — nghiên cứu cơ bản tối đa 100 triệu/đề tài.
                MaxBudgetCap = 100_000_000m,
                RequireOrderingUnit = false,
                RequirePublication = false,
                IsActive = true
            });
            await _db.SaveChangesAsync();
        }

        // ResearchTrack
        var track = await _db.ResearchTracks.FirstOrDefaultAsync(x => x.Code == "AI");
        if (track == null)
        {
            track = new ResearchTrack
            {
                Code = "AI",
                Name = "Trí tuệ nhân tạo",
                IsActive = true
            };
            _db.ResearchTracks.Add(track);
            await _db.SaveChangesAsync();
        }

        // ResearchCycle
        var cycle = await _db.ResearchCycles.FirstOrDefaultAsync(x => x.CycleYear == 2026 && x.SemesterCode == "SU26");
        if (cycle == null)
        {
            cycle = new ResearchCycle
            {
                CycleYear = 2026,
                SemesterCode = "SU26",
                ResearchTypeId = rt.Id,
                SubmissionOpenDate = new DateOnly(2026, 1, 1),
                SubmissionDeadline = new DateOnly(2026, 3, 31),
                ReviewDeadline = new DateOnly(2026, 5, 31),
                Status = CycleStatus.Open,
                Description = "Chu kỳ nghiên cứu SU26 (Demo)",
                CreatedBy = admin.Id,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _db.ResearchCycles.Add(cycle);
            await _db.SaveChangesAsync();
        }

        // Cycle ↔ Track (điểm b) + order mặc định (điểm d)
        var cycleTrack = await GetOrCreateCycleTrackAsync(cycle.Id, track.Id);
        var defaultOrder = await GetOrCreateDefaultOrderAsync(cycle.Id, unit.Id, admin.Id);

        // ResearchOrder (đặt hàng Applied) — demo cho luồng cạnh tranh nhiều PI → 1 winner
        if (!await _db.ResearchOrders.AnyAsync(o => o.CycleId == cycle.Id && !o.IsDefault))
        {
            _db.ResearchOrders.AddRange(
                new ResearchOrder
                {
                    CycleId = cycle.Id,
                    OrderingUnitId = unit.Id,
                    ResearchArea = "Ứng dụng AI trong quản lý đào tạo",
                    ProblemDescription = "Xây dựng hệ thống gợi ý lộ trình học tập cá nhân hóa.",
                    ExpectedProducts = "Phần mềm demo + báo cáo.",
                    Status = "OPEN",
                    CreatedBy = admin.Id,
                    CreatedAt = DateTime.UtcNow
                },
                new ResearchOrder
                {
                    CycleId = cycle.Id,
                    OrderingUnitId = unit.Id,
                    ResearchArea = "Chuyển đổi số quy trình NCKH",
                    ProblemDescription = "Số hóa quy trình xét duyệt đề tài cấp trường.",
                    ExpectedProducts = "Quy trình + tài liệu hướng dẫn.",
                    Status = "OPEN",
                    CreatedBy = admin.Id,
                    CreatedAt = DateTime.UtcNow
                });
            await _db.SaveChangesAsync();
        }

        // PI user
        const string piEmail = "pi.demo@furpms.edu.vn";
        var piUser = await _db.Users.IgnoreQueryFilters().FirstOrDefaultAsync(u => u.Email == piEmail);
        if (piUser == null)
        {
            var facultyRole = await _db.Roles.FirstAsync(r => r.Name == "Faculty");
            piUser = new User
            {
                Id = Guid.NewGuid(),
                Email = piEmail,
                FullName = "Nguyễn Văn An pi1",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(DemoPassword, workFactor: 12),
                Status = UserStatus.Active,
                UnitId = unit.Id,
                IsExternal = false,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _db.Users.Add(piUser);
            await _db.SaveChangesAsync();

            _db.UserRoles.Add(new UserRole { UserId = piUser.Id, RoleId = facultyRole.Id, AssignedAt = DateTime.UtcNow });
            _db.AcademicProfiles.Add(new AcademicProfile
            {
                UserId = piUser.Id,
                AcademicTitle = "TS",
                DegreeLevel = "Tiến sĩ",
                Specialization = "Khoa học máy tính",
                DateOfBirth = new DateOnly(1980, 5, 15),
                Gender = "Nam",
                Institution = "Trường Đại học FPT",
                InstitutionAddress = "Hoà Lạc, Hà Nội",
                Nationality = "Việt Nam",
                IsEligiblePi = true,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            await _db.SaveChangesAsync();
        }

        // PROJECT (gốc) + Proposal v1
        var project = new Project
        {
            Id = Guid.NewGuid(),
            ProjectCode = demoCode,
            CycleTrackId = cycleTrack.Id,
            OrderId = defaultOrder.Id,
            PiUserId = piUser.Id,
            HostingUnitId = unit.Id,
            ResearchTypeId = rt.Id,
            TitleVi = "Nghiên cứu ứng dụng học máy trong phân tích cảm xúc văn bản tiếng Việt",
            TitleEn = "Machine Learning for Vietnamese Sentiment Analysis",
            Status = ProjectStatus.UnderReview,
            PlannedStartDate = new DateOnly(2026, 7, 1),
            PlannedEndDate = new DateOnly(2028, 1, 1),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Projects.Add(project);

        var proposal = new Proposal
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            VersionNo = 1,
            IsCurrent = true,
            TitleVi = project.TitleVi,
            TitleEn = project.TitleEn,
            AbstractVi = "Đề tài nghiên cứu phát triển hệ thống phân tích cảm xúc văn bản tiếng Việt sử dụng các mô hình học sâu hiện đại. Hệ thống hướng tới độ chính xác cao trên tập dữ liệu thực tế và có khả năng triển khai trong môi trường sản xuất.",
            ResearchObjectives = "1. Xây dựng bộ dữ liệu huấn luyện chất lượng cao cho bài toán phân tích cảm xúc tiếng Việt.\n2. Phát triển mô hình học sâu đạt F1-score >= 0.90 trên tập kiểm thử.\n3. Xây dựng API phục vụ phân tích cảm xúc theo thời gian thực.",
            DurationMonths = 18,
            PlannedStartDate = new DateOnly(2026, 7, 1),
            PlannedEndDate = new DateOnly(2028, 1, 1),
            FundingMethod = FundingMethod.Partial,
            Status = ProposalStatus.Submitted,
            SubmittedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        _db.Proposals.Add(proposal);
        await _db.SaveChangesAsync();

        // Team members (thuộc PROJECT)
        var member1 = new ProjectMember
        {
            ProjectId = project.Id,
            UserId = piUser.Id,
            FullName = "Nguyễn Văn An pi1",
            AcademicTitle = "TS",
            UnitName = "Khoa CNTT",
            WorkContent = "Chủ nhiệm đề tài, xây dựng mô hình học máy, viết báo cáo",
            WorkMonths = 18,
            IsPi = true,
            IsSecretary = false,
            MemberRoleCode = "CNNV",
            SalaryCoefficient = 0.79m,
            Sequence = 1
        };
        var member2 = new ProjectMember
        {
            ProjectId = project.Id,
            FullName = "Trần Thị Bình",
            AcademicTitle = "ThS",
            UnitName = "Khoa CNTT",
            WorkContent = "Thư ký khoa học, xử lý dữ liệu, kiểm thử",
            WorkMonths = 12,
            IsPi = false,
            IsSecretary = true,
            MemberRoleCode = "TKKH",
            SalaryCoefficient = 0.49m,
            Sequence = 2
        };
        var member3 = new ProjectMember
        {
            ProjectId = project.Id,
            FullName = "Lê Minh Cường",
            AcademicTitle = "KS",
            UnitName = "Trung tâm AI",
            WorkContent = "Phát triển API, triển khai hệ thống",
            WorkMonths = 9,
            IsPi = false,
            IsSecretary = false,
            MemberRoleCode = "TVC",
            SalaryCoefficient = 0.49m,
            Sequence = 3
        };
        _db.ProjectMembers.AddRange(member1, member2, member3);
        await _db.SaveChangesAsync();

        // Research contents + activities (nội dung tài liệu — vẫn theo proposal version)
        var rc1 = new ProposalResearchContent
        {
            ProposalId = proposal.Id,
            ContentNumber = 1,
            Title = "Xây dựng và tiền xử lý bộ dữ liệu",
            Description = "Thu thập, làm sạch và gán nhãn dữ liệu văn bản tiếng Việt từ mạng xã hội và diễn đàn.",
            Sequence = 1
        };
        var rc2 = new ProposalResearchContent
        {
            ProposalId = proposal.Id,
            ContentNumber = 2,
            Title = "Phát triển mô hình phân tích cảm xúc",
            Description = "Huấn luyện và tinh chỉnh các mô hình transformer (PhoBERT, XLM-R) cho bài toán phân tích cảm xúc.",
            Sequence = 2
        };
        var rc3 = new ProposalResearchContent
        {
            ProposalId = proposal.Id,
            ContentNumber = 3,
            Title = "Triển khai và đánh giá hệ thống",
            Description = "Xây dựng REST API, đánh giá hiệu suất và viết báo cáo tổng kết.",
            Sequence = 3
        };
        _db.ProposalResearchContents.AddRange(rc1, rc2, rc3);
        await _db.SaveChangesAsync();

        _db.ProposalActivities.AddRange(
            new ProposalActivity { ContentId = rc1.Id, ProposalId = proposal.Id, ActivityName = "Thu thập và tiền xử lý dữ liệu", ExpectedResult = "Bộ dữ liệu 50.000 mẫu đã gán nhãn", StartMonth = 1, EndMonth = 4, Sequence = 1, EstimatedCost = 50_000_000 },
            new ProposalActivity { ContentId = rc2.Id, ProposalId = proposal.Id, ActivityName = "Huấn luyện mô hình PhoBERT", ExpectedResult = "Mô hình đạt F1 >= 0.88 trên tập val", StartMonth = 5, EndMonth = 10, Sequence = 2, EstimatedCost = 400_000_000 },
            new ProposalActivity { ContentId = rc2.Id, ProposalId = proposal.Id, ActivityName = "Tối ưu hoá và tinh chỉnh mô hình", ExpectedResult = "Mô hình đạt F1 >= 0.90 trên tập test", StartMonth = 11, EndMonth = 14, Sequence = 3, EstimatedCost = 200_000_000 },
            new ProposalActivity { ContentId = rc3.Id, ProposalId = proposal.Id, ActivityName = "Xây dựng REST API và tích hợp", ExpectedResult = "API hoạt động ổn định, latency < 200ms", StartMonth = 15, EndMonth = 17, Sequence = 4, EstimatedCost = 150_000_000 },
            new ProposalActivity { ContentId = rc3.Id, ProposalId = proposal.Id, ActivityName = "Viết báo cáo tổng kết đề tài", ExpectedResult = "Báo cáo đầy đủ, nộp Hội đồng", StartMonth = 18, EndMonth = 18, Sequence = 5, EstimatedCost = 10_000_000 }
        );
        await _db.SaveChangesAsync();

        // Sản phẩm dự kiến của PROJECT (chưa gắn hợp đồng — contract_id null)
        var cat = await _db.ProductCategories.FirstOrDefaultAsync();
        _db.ProjectDeliverables.AddRange(
            new ProjectDeliverable { ProjectId = project.Id, CategoryId = cat?.Id, ProductName = "Báo cáo tổng kết đề tài và các phụ lục", ScientificRequirements = "Đầy đủ, chính xác, có giá trị khoa học", Sequence = 1 },
            new ProjectDeliverable { ProjectId = project.Id, CategoryId = cat?.Id, ProductName = "02 bài báo khoa học trên tạp chí quốc tế", ScientificRequirements = "Đăng trên tạp chí Q1/Q2 theo SJR", Sequence = 2 },
            new ProjectDeliverable { ProjectId = project.Id, CategoryId = cat?.Id, ProductName = "Bộ dữ liệu và source code phần mềm", ScientificRequirements = "Bộ dữ liệu >= 50.000 mẫu; phần mềm ổn định", Sequence = 3 }
        );
        await _db.SaveChangesAsync();

        // Budget
        var budget = new ProposalBudget
        {
            ProposalId = proposal.Id,
            // QĐ543 Điều 14.1.b — đề tài ứng dụng tối đa 150tr. Dự toán mẫu trước đây lấy từ mẫu
            // thuyết minh cấp Bộ (900tr) nên vượt trần gấp 6 lần ngay trên màn demo.
            TotalAmount = 145_000_000m,
            // Khớp đúng 4 dòng dự toán bên dưới — 6 cột tổng hợp này trước đây luôn để 0 nên phần
            // tổng hợp kinh phí hiện trống dù dự toán đã có.
            LaborAmount = 95_000_000m,
            EquipmentAmount = 25_000_000m,
            ExternalServiceAmount = 0,
            ConferenceAmount = 15_000_000m,
            OfficeSuppliesAmount = 10_000_000m,
            IncidentalIpAmount = 0
        };
        _db.ProposalBudgets.Add(budget);
        await _db.SaveChangesAsync();

        // Budget items
        var laborCat = await _db.BudgetExpenseCategories.FirstAsync(c => c.Code == "LABOR");
        var equipmentCat = await _db.BudgetExpenseCategories.FirstAsync(c => c.Code == "EQUIPMENT");
        var conferenceCat = await _db.BudgetExpenseCategories.FirstAsync(c => c.Code == "CONFERENCE");
        var officeCat = await _db.BudgetExpenseCategories.FirstAsync(c => c.Code == "OFFICE_OTHER");
        _db.ProposalBudgetItems.AddRange(
            // Tổng 145tr; mọi hạng mục nằm dưới trần % của Điều 15 (thù lao 66% ≤100 · thiết bị
            // 17% ≤60 · hội thảo 10% ≤30 · VPP 7% ≤20).
            new ProposalBudgetItem { ProposalId = proposal.Id, CategoryId = laborCat.Id,      Amount = 95_000_000m, SourceKhoan = 95_000_000m, Sequence = 1 },
            new ProposalBudgetItem { ProposalId = proposal.Id, CategoryId = equipmentCat.Id,  Amount = 25_000_000m, SourceNsnn = 25_000_000m,  Sequence = 2 },
            new ProposalBudgetItem { ProposalId = proposal.Id, CategoryId = conferenceCat.Id, Amount = 15_000_000m, SourceNsnn = 15_000_000m,  Sequence = 4 },
            new ProposalBudgetItem { ProposalId = proposal.Id, CategoryId = officeCat.Id,     Amount = 10_000_000m, SourceNsnn = 10_000_000m,  Sequence = 5 }
        );
        await _db.SaveChangesAsync();

        // Labor details (FK → project_member)
        _db.ProposalBudgetLaborDetails.AddRange(
            new ProposalBudgetLaborDetail { ProposalId = proposal.Id, ProjectMemberId = member1.Id, TotalResearchHours = 50 * 8m, HourlyRate = 0, WorkDays = 50, Coefficient = 0.79m, DailyRate = 0.79m * 1_490_000m, Sequence = 1 },
            new ProposalBudgetLaborDetail { ProposalId = proposal.Id, ProjectMemberId = member2.Id, TotalResearchHours = 40 * 8m, HourlyRate = 0, WorkDays = 40, Coefficient = 0.49m, DailyRate = 0.49m * 1_490_000m, Sequence = 2 },
            new ProposalBudgetLaborDetail { ProposalId = proposal.Id, ProjectMemberId = member3.Id, TotalResearchHours = 30 * 8m, HourlyRate = 0, WorkDays = 30, Coefficient = 0.49m, DailyRate = 0.49m * 1_490_000m, Sequence = 3 }
        );
        await _db.SaveChangesAsync();
    }
}
