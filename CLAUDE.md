# FURPMS Backend — Conventions & Architecture

Capstone SU26SE053 · QĐ 543/QĐ-ĐHFPT · Schema **v3 Project-centric** (~56 bảng, sau Review 2) — nguồn: `docs/ERD_v3_Project_Centric.dbml`. `Project` là thực thể gốc, `Proposal` = tài liệu có version thuộc Project.

## Architecture

N-tier: **Controller → Service → Repository → DbContext**

- No MediatR. No AutoMapper.
- `FURPMS.Domain` — entities only, no logic
- `FURPMS.Application` — interfaces, DTOs, service contracts
- `FURPMS.Infrastructure` — EF Core DbContext, service implementations, seeders
- `FURPMS.API` — controllers, middleware, Program.cs

## Naming Conventions

| Layer | Convention |
|---|---|
| C# class names | `PascalCase` |
| DB table/column names | `snake_case` via `UseSnakeCaseNamingConvention()` |
| Routes | `kebab-case` plural nouns (`/api/research-types`) |
| DTOs | `<Entity><Action>Request` / `<Entity><Action>Response` |

## Entity Rules

- **Business entities** (`proposals`, `users`, `contracts`, etc.) → `Guid` PK
- **Config/lookup tables** (`roles`, `research_types`, etc.) → `int` IDENTITY PK
- **Audit log / email log** → `long` IDENTITY PK
- **Soft delete** on `users` and `proposals` only (`IsDeleted`, `DeletedAt`, `DeletedBy`)
- Global query filters applied for both: `HasQueryFilter(e => !e.IsDeleted)`

## ApiResponse<T> Pattern

All controllers return `ApiResponse<T>` or `ApiResponse`:

```csharp
// Success with data
return Ok(ApiResponse<UserInfoResponse>.Ok(data));

// Success no data
return Ok(ApiResponse.Ok("Operation completed."));

// Error (thrown exception → GlobalExceptionMiddleware)
throw new KeyNotFoundException("Resource not found.");
throw new UnauthorizedAccessException("Invalid credentials.");   // 401 — CHƯA đăng nhập
throw new ForbiddenException("Only the PI can edit this proposal."); // 403 — có đăng nhập, thiếu quyền
throw new ArgumentException("Validation message.");
throw new InvalidOperationException("Conflict message.");
```

HTTP status codes come from `GlobalExceptionMiddleware`:
- `UnauthorizedAccessException` → 401 (**chỉ dùng cho lỗi xác thực** — FE thấy 401 là đăng xuất người dùng)
- `ForbiddenException` → 403 (thiếu quyền với tài nguyên: không phải PI, không phải Thư ký hội đồng…)
- `KeyNotFoundException` → 404
- `ArgumentException` → 400
- `InvalidOperationException` → 409
- Everything else → 500

## Auth

- JWT Bearer, configured in `appsettings.json → JwtSettings`
- `[Authorize]` on protected endpoints; `[Authorize(Roles = "Admin")]` for role gating
- Extract current user ID in controller: `User.FindFirstValue(ClaimTypes.NameIdentifier)`

## Connection String

```
Host=localhost;Port=5433;Database=furpms;Username=postgres;Password=Furpms@Strong123;SSL Mode=Disable
```

> ⚠️ **14/08 đổi từ SQL Server sang PostgreSQL** — Railway (nơi deploy) không có SQL Server.
> `docker compose up -d` dựng Postgres 16 ở cổng **5433** (né 5432 mặc định).
> Chi tiết + 3 bẫy deploy đã gặp thật: **`docs/HANDOFF_HIEN_HANH.md`**.

## EF Core Notes

- `UseSnakeCaseNamingConvention()` — never add `HasColumnName` manually
- Computed column: `HasComputedColumnSql("[col_a] * [col_b]", stored: true)`
- Check constraints: `b.ToTable(t => t.HasCheckConstraint("name", "sql"))`
- Unique nullable: `HasIndex(x => x.Col).IsUnique().HasFilter("[col] IS NOT NULL")`
- Circular FK → always set `OnDelete(DeleteBehavior.NoAction)` on the non-owning side

## Migration Workflow

```bash
# Add migration (run from solution root)
dotnet ef migrations add <Name> --project FURPMS.Infrastructure --startup-project FURPMS.API

# App auto-runs Migrate() + seeder on startup — qua DatabaseStartup, CÓ THỬ LẠI
# (mạng nội bộ Railway mất vài giây mới sẵn sàng; gọi ngay là DNS chưa phân giải được)
```

⚠️ **14/08 — toàn bộ migration cũ (T-SQL) đã bị xoá**, thay bằng **một** migration nền cho
PostgreSQL: `InitialPostgres`. Chuỗi `PhaseA…PhaseS` chỉ còn ý nghĩa lịch sử, không còn file.
Migration mới từ nay đặt tên tự do, không cần theo phase.

> Lưu ý: `dotnet ef migrations add` build TRƯỚC khi sinh file → phải `dotnet build` lại trước khi `dotnet run --no-build`, nếu không app báo "database is already up to date" mà bảng mới không có.

## DatabaseSeeder

`DatabaseSeeder.SeedAsync()` is idempotent — always check existence before inserting.
Admin credentials: `admin@furpms.edu.vn` / `password`

## Folder Structure

> Minh hoạ (snapshot cũ, trước Review 2). Hiện trạng đã Project-centric: thêm `Entities/Projects/` (Project, ProjectMember, ProjectDeliverable), `ProjectRound`, `CycleTrack`, `CouncilProjectAssignment`, `ContractPhase`… + ~37 controllers/service. Cấu trúc thực tế xem code; sơ đồ dữ liệu xem `docs/ERD_v3_Project_Centric.dbml`.

```
FURPMS.Domain/
  Entities/
    MasterData/   ResearchType, ResearchTrack, ProductCategory, AmendmentCategory, LlmConfig
    Financial/    BudgetAllocationRule, DisbursementTemplate, CouncilRemunerationRate, RubricTemplate, RubricCriterion
    Users/        Role, User, UserRole, OrganizationalUnit, AcademicProfile
    Cycles/       ResearchCycle, ResearchOrder
    Proposals/    Proposal, ProposalTeamMember, ProposalBudget, ProposalBudgetLaborDetail,
                  ProposalResearchContent, ProposalActivity, ProposalExpectedProduct
    Review/       ReviewCouncil, CouncilMember, CouncilMeeting, MeetingAttendance,
                  ProposalReviewScore, ReviewScoreDetail, CouncilDecision, ReviewerFeedback, AcceptanceEvaluation
    Contracts/    Contract, ContractDisbursement, ContractSettlement
    Progress/     ProgressReport, ProgressReportItem, AmendmentRequest, ProductDeliverable, FinalReport
    AI/           LlmOutput, SemanticSearchVector, Document, Notification
    Logs/         AuditLog, EmailLog

FURPMS.Application/
  Common/         ApiResponse.cs
  DTOs/Auth/      LoginRequest, LoginResponse, UserInfoResponse, ChangePasswordRequest
  Interfaces/     IAuthService, IJwtService
  Settings/       JwtSettings

FURPMS.Infrastructure/
  Data/           FURPMSDbContext.cs, DatabaseSeeder.cs
  Migrations/
  Services/       JwtService, AuthService
  Extensions/     DependencyInjection.cs

FURPMS.API/
  Controllers/    ~37 controllers (Auth, Users, Cycles, Proposals, ReviewBoard, Councils, Contracts, …)
  Middleware/     GlobalExceptionMiddleware.cs
```
## Active Spec Documents

> 📇 **Mục lục đầy đủ, xếp theo tác dụng: `docs/00_INDEX.md`** — mở file đó để biết doc nào dùng khi nào.

- **Nghiệp vụ:** `docs/Process_Spec_v2.md` ← đọc trước khi code bất kỳ flow nào.
- **API (bản giao kèo FE↔BE):** `docs/API_CONTRACT.md` — 8 nhóm chức năng; nguồn chính xác nhất vẫn là Swagger `:5068/swagger`.
- **Tiến độ BE:** `docs/PROGRESS.md` — % từng nhóm + việc nên làm tiếp.
- **Dữ liệu:** `docs/ERD_v3_Project_Centric.dbml` (sơ đồ DB duy nhất) · `docs/DB_Redesign_v3_PostReview2.md` (lý do refactor).
- **Việc đang làm:** `docs/PLAN_Week13_Demo_0508.md` (mới nhất) → `docs/PLAN_Week12.md`.
- Read relevant spec section BEFORE implementing any feature.

### ⚖️ QĐ 543 là NGUỒN SỰ THẬT — bắt buộc đối chiếu (chốt 06/08)

`docs/QD_543_DHFPT_Quy_dinh_quan_ly_de_tai_NCKH_clean.docx` là **văn bản pháp quy của trường**.
Code không khớp với nó là **code sai**, không phải "khác biệt thiết kế".

**Bắt buộc, không cần nhắc lại:**
1. Trước khi code/sửa bất kỳ luồng nghiệp vụ, **quyết định ràng buộc, hoặc trả lời câu hỏi nghiệp vụ** → mở file .docx đó ra tra **nguyên văn**, đừng dựa vào trí nhớ hay doc trung gian.
2. Doc trung gian (`Process_Spec_v2.md`, `QD543_Compliance.md`, `CLAUDE.md`…) **có thể chép sai**. Đã dính 1 lần: `Process_Spec_v2.md` ghi *"gia hạn tối đa 6 tháng"* trong khi **Điều 10.4** nguyên văn là *"gia hạn tối đa **1/2 tổng thời gian thực hiện**"* (6 chỉ đúng khi đề tài 12 tháng). Khi văn bản gốc và doc nội bộ lệch nhau → **văn bản gốc thắng**, và phải sửa doc nội bộ ngay trong cùng lượt.
3. Khi trích dẫn, ghi rõ **Điều/khoản hoặc số biểu mẫu** (vd "QĐ543 Điều 10.4", "BM12 mục 10.1") để người sau kiểm lại được.
4. Bảng map biểu mẫu ↔ code: `docs/QD543_Compliance.md` §2. Sửa code chạm biểu mẫu thì cập nhật bảng này.

Lệnh bóc nội dung .docx có ở cuối `docs/00_INDEX.md`.

### 📕 Cẩm nang tránh lỗi Capstone — soi trước mỗi mốc Review
`docs/Cam-nang-tranh-loi-Capstone-SE.pdf`. Hai điều rút ra phải áp dụng liên tục khi code:
- **Không hardcode tham số nghiệp vụ.** Cẩm nang nói rõ *"sửa file appsettings rồi restart vẫn bị coi là hardcode"* — tham số phải nằm trong DB, có màn Admin sửa, sửa xong hiệu lực ngay. Câu hỏi kinh điển lúc bảo vệ: *"đổi con số này từ 70% lên 80%, demo ngay đi"*.
- **Mọi `if` nghiệp vụ trong code phải có một Business Rule tương ứng, và ngược lại.** Lỗi hay bị bắt nhất: BR ghi một đằng, code không kiểm tra (vd tổng điểm bộ tiêu chí = 100 nhưng tạo được 120).

## Business Rule Decisions (không được tự đoán)

### Chu kỳ & loại đề tài
7. **1 đợt (`research_cycle`) = đúng 1 loại đề tài** (Applied *hoặc* Basic — field `ResearchTypeId` đã có). "Mở cả 2 loại" = tạo **2 cycle độc lập**, mỗi cái có timeline + funding cap riêng. KHÔNG gộp vào 1 cycle. Bỏ khái niệm "hạng quý".

### 2 luồng nộp đề tài — khác nhau hoàn toàn
8. **Ứng dụng (Applied):** Staff upload **danh mục đề tài / đặt hàng** (`ResearchOrder`) → nhiều PI đăng ký cùng 1 đề tài (nhiều-nhiều) → xét duyệt ra **0/1/nhiều** PI được duyệt. Hội đồng có thêm **người đặt hàng** (`OrderingUnit`). (Schema: `ResearchOrder` + `Proposal.OrderId` đã có; multi-winner là epic tương lai.)
9. **Cơ bản (Basic):** PI **tự đề xuất tự do** → 1 đề tài = 1 PI → duyệt đạt/không. Luồng hiện tại đang implement theo mô hình này.

### Nộp đề cương — dual intake (không phụ thuộc AI)
10. Có **2 đường** song song, đều hợp lệ:
    - **Đường A (nhập tay):** PI điền form trực tiếp — luôn hoạt động dù không có AI.
    - **Đường B (upload + AI):** PI upload file Word/PDF → AI trích xuất field cấu trúc → prefill form → PI review/sửa → nộp.
    - DB chỉ lưu **structured fields**; **file gốc giữ làm attachment** (cả 2 đường).
    - Trước nộp: nhắc cập nhật **CV/lý lịch** (cũ → cảnh báo, bắt xác nhận). Có **Lưu nháp** (revision) + **Nộp** (nộp xong khóa; hết hạn tự khóa).

### Actor & vai trò
11. **PI là actor DUY NHẤT tương tác** với hệ thống thay cho đề tài; thành viên đề tài chỉ là **data junction** (không đăng nhập, không tự nộp). **Reviewer** = mọi thành viên hội đồng; chức danh (Chủ tịch/Thư ký/Phản biện/Thành viên) chỉ là **field** khi gán — không tách actor.

### Hội đồng & chấm điểm
12. **Kết quả = QUYẾT ĐỊNH của Chủ tịch** sau khi hội đồng họp kín & thống nhất (cập nhật theo ghi âm thầy tuần 7 — KHÁC bản cũ "số phiếu đa số"). Hệ thống **hiển thị điểm/phiếu chỉ để tham khảo, KHÔNG tự đếm phiếu chốt**. Biên bản: **Thư ký soạn (nháp) → Chủ tịch duyệt = khóa → mới cập nhật status đề tài** (thành viên khác chỉ xem). Đã code: `ReviewScoringService.SaveMinutesAsync` (Thư ký) + `ApproveMinutesAsync` (Chủ tịch), cờ khóa = `CouncilDecision.FinalizedAt`.

### Mời reviewer & biểu mẫu
13. Gán reviewer hết trước → 1 nút **"Gửi thư mời"** đồng loạt (tránh spam khi còn sửa); thư có deadline xác nhận/từ chối; quá hạn → tìm người khác. **Biểu mẫu pin version active tại thời điểm tạo đề tài** — đổi active chỉ áp đề tài mới.

### Nguyên tắc chung (từ advisor)
14. Bám sát **QĐ 543/QĐ-ĐHFPT**. Phân tích theo **Quy trình → từng giai đoạn** (ai tham gia / sản phẩm đầu ra / văn bản liên quan).

### Chốt buổi họp tuần 10 — thầy Đức (nguồn: ghi âm tuần 10). Plan: `.claude/plans/audit-task-do-pure-treehouse.md`
15. **Tài chính = "minh chứng", hệ thống KHÔNG quản tiền.** Kế toán chi tiền ngoài hệ thống. Hệ thống chỉ theo dõi **mốc giải ngân** (gate theo tiến độ/nghiệm thu) + cho Staff **upload file HĐ/chứng từ làm minh chứng** + "đánh dấu đã giải ngân" (không nhập số tiền). **→ Rule #2, #3, #6 (giải ngân/FINANCE cũ) SUPERSEDED.** Scope thực thi = **strip+ẩn** (giữ bảng, ngừng tính tiền, ẩn nav/UI tiền), không migration bỏ entity. (Đã làm: `disbursement/{id}/evidence`, ẩn nav financial-config/budget-categories, confirm→optional amount.)
16. **Chỉ 2 hội đồng:** Xét duyệt đề cương + Nghiệm thu. Báo cáo tiến độ giữa kỳ = **Staff duyệt trực tiếp, KHÔNG hội đồng**. Bỏ dimension FINANCE. (Round type dùng REVIEW + ACCEPTANCE.)
17. **Lịch họp:** Staff phải setup **đủ (thành viên + ngày/giờ + địa điểm/link)** trước khi hiện nút "Gửi thư mời". Offline có **địa điểm** (`council_meeting.location`), online có link. Khung giờ tổng + **slot con** theo từng đề tài. **Cảnh báo trùng lịch** giảng viên (2 hội đồng giao giờ — `GET /councils/{id}/schedule-conflicts`). Thay người/đổi lịch **bất kỳ lúc nào** (không đóng băng HĐ). (Đã làm: location + conflict + **điểm danh** + **gate gửi mời** + **slot theo đề tài** `PUT /councils/{id}/slots`.)
18. **Biên bản (BM04/BM12):** auto-prefill DS hội đồng; điểm danh tick+lý do; **2 phong cách** ghi: Hỏi–Đáp (`council_qa_entry`) *hoặc* viết tự do; **ý kiến từng TV** 2 cột chuyên môn/kinh phí (`council_member_opinion`); Thư ký soạn → Chủ tịch chốt = khóa (giữ rule #12). ("Về kinh phí" ở đây = ý kiến định tính, KHÔNG mâu thuẫn #15.)
19. **Gia hạn deadline = LOG, không ghi đè** (chủ yếu cấp ĐỢT `research_cycle.submission_deadline`). Bảng `deadline_extension`; deadline hiệu lực = bản mới nhất, gốc giữ nguyên. (Đã làm.)
20. **Nộp đề cương: upload+AI là TÙY CHỌN** (không ép; nhập tay ngang hàng — giữ rule #10). **Loại đề tài suy từ ĐỢT** (rule #7), PI không chọn.
21. **Tự sinh Word hợp đồng (BM05):** bốc dữ liệu (CN/đề tài/kinh phí) → xuất .docx (`GET /contracts/{id}/export-word`) đem ký ngoài → upload bản ký làm minh chứng (Document polymorphic EntityType="Contract"). (Word gen đã làm.)
22. **Trực quan hóa vòng đời đề tài:** timeline mốc + ngày (ký HĐ, giải ngân từng đợt) + click mở minh chứng. (Đã làm: tab "Tiến trình" ở chi tiết hợp đồng.)
23. **Đa vai (multi-role):** login email+mật khẩu; đổi vai ở **dropdown header** (chỉ hiện vai user thực có). (Đã làm.)

### Chốt từ đối chiếu QĐ543 (nguồn: đọc lại toàn văn quy định, 11/08)
24. **Lịch giải ngân bám QĐ543 Điều 16 — do LOẠI ĐỀ TÀI quyết định, KHÔNG phải PI chọn.**
    - Ứng dụng: **4 đợt 30–30–30–10** (ký HĐ → tiến độ GĐ1 → tiến độ GĐ2 → nghiệm thu Đạt).
    - Cơ bản: **1 đợt 100%** sau khi nghiệm thu "Đạt".
    - Tỷ lệ nằm ở master data `disbursement_templates` (Phòng QLKH sửa được), **không hardcode**.
    - ⚠️ **"Phương thức khoán chi" WHOLE/PARTIAL không có trong QĐ543** (chữ "khoán" chỉ có ở "thuê khoán chuyên môn"/"giao khoán") — nó đến từ mẫu thuyết minh cấp Bộ. Cột `Proposal.FundingMethod` giữ lại để đọc dữ liệu cũ nhưng **không còn quyết định số đợt**.
25. **Đánh giá tiến độ giữa kỳ: Staff làm, KHÔNG lập hội đồng** (giữ rule #16). Căn cứ: QĐ543 nhắc "Hội đồng đánh giá tiến độ" đúng **1 lần ở Điều 10.1**, không có điều nào định nghĩa thành phần/số lượng/thể thức/biên bản; **BM06** đề *"Kính gửi: Phòng Quản lý khoa học"* và chỉ chủ nhiệm ký; **Điều 18.1** chỉ cấp thù lao cho **2 hội đồng** (xét duyệt + nghiệm thu). Staff = đầu mối tiếp nhận và xử lý.
26. **Trần kinh phí (QĐ543 Điều 14): cơ bản ≤ 100tr · ứng dụng/triển khai ≤ 150tr/đề tài.**
    - Chặn ở **cả 4 đường ghi kinh phí** + kiểm lại khi nộp (`BudgetPolicyService`).
    - **Điều 14.3 cho phép vượt trần nếu Hiệu trưởng duyệt** ⇒ trần để ở master data `research_types.max_budget_cap`, Phòng QLKH nâng trần cho trường hợp đó. **Không** hardcode và **không** có cờ "bỏ qua trần" cho PI tự bấm.
    - Đơn đặt hàng có trần riêng thì lấy trần **nghiêm ngặt hơn**; đơn nới rộng không phá được trần Điều 14.
    - Wizard nộp đề cương: **bỏ ô "Phương thức cấp kinh phí"** (WHOLE/PARTIAL — xem #24), thay bằng **Tổng dự toán** có hiện trần ngay tại chỗ.
27. **Dự toán = đúng 06 hạng mục của QĐ543 Điều 15.1**, kèm trần % trên tổng (user chốt 12/08, chọn phương án "chuẩn quy định nhất"):
    | Hạng mục | Mã | Trần |
    |---|---|---|
    | Thù lao nghiên cứu | `LABOR` | 100% |
    | Thiết bị, vật tư, nguyên liệu | `EQUIPMENT` | 60% |
    | Thuê ngoài | `OUTSOURCED` | 60% |
    | Hội nghị/hội thảo/seminar | `CONFERENCE` | 30% |
    | Văn phòng phẩm, chi khác | `OFFICE_OTHER` | 20% |
    | Chi phí phát sinh, sở hữu trí tuệ | `INCIDENTAL_IP` | 10% |
    - **Bỏ bộ 12 hạng mục cũ** (lấy từ mẫu thuyết minh cấp Bộ: "Chi đoàn ra", "Quản lý phí"… — không có trong quy định của trường). Hạng mục cũ chuyển `IsActive = false`, **KHÔNG xoá**: dự toán đã lưu vẫn trỏ FK vào chúng.
    - Trần % **để ở master data** `budget_expense_categories.max_percentage`, cùng lý do với rule #26.
    - **Tổng dự toán = tổng các hạng mục**, không nhập tay riêng (hai con số ở hai chỗ là hai lần sai).
    - 6 cột tổng hợp của `proposal_budgets` (`labor_amount`, `equipment_amount`, …) map 1-1 với 6 hạng mục và **nay đã được ghi** — trước đây tồn tại nhưng luôn bằng 0.

### Chốt buổi demo 14/08 — thầy góp ý. Chi tiết: `docs/GOPY_Thay_Demo_1408.md`
28. **Lý lịch khoa học phải LIỆT KÊ CHI TIẾT, không chỉ đếm số** (thầy: *"đừng có đếm số mà phải xem được nguồn, các thứ báo nào"*).
    - Căn cứ: **QĐ543 BM02 đòi CẢ HAI**. Mục **14.1–14.5** là số lượng, nhưng mục **14.6** ghi rõ: *"Liệt kê đầy đủ các công bố nêu trên từ trước đến nay theo thứ tự thời gian, ưu tiên các dòng đầu cho 5 công trình tiêu biểu… (tên tác giả, năm xuất bản, tên công trình, **tên tạp chí, volume, trang số**)"*. Tương tự **16.3** (sản phẩm ứng dụng), **17** (đề tài đã chủ trì/tham gia), **19.4** (hướng dẫn SĐH). Hệ thống cũ **chỉ làm phần số** ⇒ hồ sơ nộp lên thiếu so với biểu mẫu.
    - Bảng mới `academic_work`, mỗi `work_type` ứng với **đúng một mục BM02**: `BOOK`(13) · `PUBLICATION`(14.6) · `PATENT`(15) · `APPLICATION`(16.3) · `PROJECT`(17) · `AWARD`(18) · `SUPERVISION`(19.4).
    - **Mục 17 tách theo VAI TRÒ chứ không theo cấp quản lý**: 17.1 *"đã và đang chủ trì"* / 17.2 *"tham gia với tư cách thành viên"*. Có ô **tình trạng** với đúng 3 giá trị biểu mẫu liệt kê: *đã nghiệm thu / chưa nghiệm thu / không hoàn thành*.
    - **8 ô đếm cũ nay là SỐ SUY RA** — máy chủ cộng lại từ `academic_work` sau mỗi thay đổi, không nhập tay. Lý do: khai tay hai chỗ (số ở 14.1–14.5, danh sách ở 14.6) thì sớm muộn cũng lệch, mà lệch ở hồ sơ năng lực chủ nhiệm là chuyện hội đồng soi trúng ngay.
    - **Ghi thì chỉ chính chủ, kể cả Admin cũng 403.** Lý lịch khoa học là lời khai có trách nhiệm của người đứng tên; khai hộ là làm hỏng giá trị pháp lý. Admin/Staff **xem** được (thẩm định hồ sơ theo Điều 7).
29. **Trang Hồ sơ chia tab, không cuộn một mạch** (thầy: *"chia phần ra, chứ đừng có lướt lướt xuống cuối thế"*) — 4 tab: Tài khoản · Lý lịch khoa học · Công trình & đề tài · Thông tin lập hợp đồng.

---
*(Rule 1–6 — review round, giải ngân, COI, PARTIAL/WHOLE. ⚠️ #2/#3/#6 SUPERSEDED bởi #15/#16 — giữ lại để tra cứu lịch sử.)*

1. Round close result REJECTED → proposal.status = REJECTED (kết thúc luôn).
   Round close result REVISION_REQUIRED → proposal.status = REVISION_REQUIRED;
   PI sửa và resubmit → round đó reopen (status OPEN), giữ score cũ.
2. ⚠️SUPERSEDED (#16 — bỏ FINANCE round). ~~FINANCE round chỉ được open khi prerequisite SCIENCE round status = PASSED.~~
3. ⚠️SUPERSEDED (#15 — không quản tiền). ~~Tiền KHÔNG BAO GIỜ tự giải ngân...~~ (giữ cơ chế condition_met_at + minh chứng, bỏ phần số tiền).
4. Reviewer từ chối (DECLINED) → notify Staff, Staff gán người thay.
5. COI: thành viên trong proposal_team_members không được làm council_member
   của chính proposal đó. Validate khi add, trả 400 nếu vi phạm.
6. ⚠️SUPERSEDED phần %/tiền (#15). ~~PARTIAL → 1 đợt/mốc; WHOLE ≥3 đợt; tỷ lệ %...~~ (giữ khái niệm mốc/đợt giải ngân, bỏ tính % tiền).

---

## Cách làm việc với Claude Code (standing instructions — luôn áp dụng, không cần nhắc lại)

### Quy trình cập nhật docs (BẮT BUỘC — làm CÙNG LƯỢT với thay đổi code, không tách riêng chờ nhắc)
Sau khi sửa code, tự hỏi *"thay đổi này chạm doc nào?"* và cập nhật NGAY trong cùng lượt:

| Khi thay đổi… | Cập nhật doc |
|---|---|
| Thêm/sửa/xóa endpoint · đổi request/response · đổi quyền `[Authorize]` · đổi enum/status | `docs/API_CONTRACT.md` (đúng §) |
| Xong/bỏ 1 feature lớn · đổi mức hoàn thiện 1 nhóm chức năng | `docs/PROGRESS.md` (%, "còn thiếu", việc nên làm tiếp) |
| Xong 1 việc backlog · phát sinh việc dở mới · thêm/xóa file trong `docs/` | `docs/README.md` (backlog + mục lục sống) |
| Đổi schema/bảng/cột/quan hệ | `docs/ERD_v3_Project_Centric.dbml` (+ `DB_ANALYTIC_REPORT.md` nếu chạm mô tả) |
| Đổi luồng nghiệp vụ / state machine / diagram | `docs/Process_Spec_v2.md` · `docs/Review2_Diagrams.md` · `docs/RP4_Diagrams.md` (§ liên quan) |
| Advisor/thầy chốt rule nghiệp vụ mới | **file này** (Business Rules) — đánh số + ghi nguồn (buổi nào) |

Nguyên tắc: **không để user phát hiện doc lệch rồi mới sửa**. Nếu 1 doc đang mô tả SAI hiện trạng sau thay đổi của mình → tự sửa/đánh dấu ngay cùng lượt. Xóa file docs → sửa luôn cross-ref + mục lục README (đừng để link chết).

### Đầu mỗi phiên — TỰ TRA DOC, đừng hỏi "làm gì tiếp"
Mỗi phiên Claude mất sạch ngữ cảnh phiên trước; chỉ file này được nạp tự động. Nên khi user hỏi *"giờ làm gì?"* / *"còn gì làm không?"* → **đọc theo thứ tự này TRƯỚC khi trả lời**, không đoán, không hỏi lại user:

1. **`docs/PLAN_Week12.md`** → mục **"Trạng thái tổng"** (P nào ✅, P nào ⬜) + mục **"⏸ Chờ user quyết định"**.
2. **`docs/SYSTEM_REVIEW.md`** → §2 "Chưa ổn / rủi ro" + §3 backlog.
3. **`docs/PROGRESS.md`** → % từng nhóm + cột "còn thiếu".

Việc **đang chờ user chốt** thì nhắc lại lựa chọn + khuyến nghị, KHÔNG tự làm.

### Chuẩn "xong" 1 việc
- **Build xanh + `dotnet test` xanh** (nêu rõ số pass) TRƯỚC khi coi là xong. Test fail → báo kèm output, không giấu.
- **Báo tiến độ theo %** sau mỗi việc lớn: đã làm gì · % so với plan/phase · còn gì. Không đợi hỏi.
- **KHÔNG tự commit** (kể cả khi mọi thứ xanh) — để user tự commit, trừ khi user yêu cầu trong đúng lượt đó.