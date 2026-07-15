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
throw new UnauthorizedAccessException("Invalid credentials.");
throw new ArgumentException("Validation message.");
throw new InvalidOperationException("Conflict message.");
```

HTTP status codes come from `GlobalExceptionMiddleware`:
- `UnauthorizedAccessException` → 401
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
Server=(localdb)\MSSQLLocalDB;Database=FURPMS_V2;Trusted_Connection=True;TrustServerCertificate=True
```

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

# App auto-runs db.Database.EnsureCreated() + seeder on startup (Program.cs)
```

One migration per phase. Hiện hành (Project-centric): `PhaseA_ProjectCentric` → `PhaseB_ReviewMN` → `PhaseC_ChangeRequests`.

## DatabaseSeeder

`DatabaseSeeder.SeedAsync()` is idempotent — always check existence before inserting.
Admin credentials: `admin@furpms.edu.vn` / `Admin@123456`

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
## Active Spec Documents (đọc `docs/README.md` §"Thứ tự đọc" để biết đọc theo trình tự nào)
- **Nghiệp vụ:** `docs/Process_Spec_v2.md` ← đọc trước khi code bất kỳ flow nào.
- **API (bản giao kèo FE↔BE):** `docs/API_CONTRACT.md` — 8 nhóm chức năng; nguồn chính xác nhất vẫn là Swagger `:5068/swagger`.
- **Tiến độ BE:** `docs/PROGRESS.md` — % từng nhóm + việc nên làm tiếp.
- **Dữ liệu:** `docs/ERD_v3_Project_Centric.dbml` (sơ đồ DB duy nhất) · `docs/DB_Redesign_v3_PostReview2.md` (lý do refactor).
- Read relevant spec section BEFORE implementing any feature.

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

---
*(Rule 1–6 — review round, giải ngân, COI, PARTIAL/WHOLE — giữ nguyên bên dưới)*

1. Round close result REJECTED → proposal.status = REJECTED (kết thúc luôn).
   Round close result REVISION_REQUIRED → proposal.status = REVISION_REQUIRED;
   PI sửa và resubmit → round đó reopen (status OPEN), giữ score cũ.
2. FINANCE round chỉ được open khi prerequisite SCIENCE round status = PASSED.
   Nếu chưa PASSED → trả 409.
3. Tiền KHÔNG BAO GIỜ tự giải ngân. Khi deliverable PASSED:
   set disbursement.condition_met_at + notify Staff. Staff xác nhận tay.
4. Reviewer từ chối (DECLINED) → notify Staff, Staff gán người thay.
5. COI: thành viên trong proposal_team_members không được làm council_member
   của chính proposal đó. Validate khi add, trả 400 nếu vi phạm.
6. PARTIAL → 1 đợt giải ngân per mốc nghiệm thu.
   WHOLE → tối thiểu 3 đợt (đầu/giữa/cuối). Tỷ lệ % config được, không hard-code.

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

### Chuẩn "xong" 1 việc
- **Build xanh + `dotnet test` xanh** (nêu rõ số pass) TRƯỚC khi coi là xong. Test fail → báo kèm output, không giấu.
- **Báo tiến độ theo %** sau mỗi việc lớn: đã làm gì · % so với plan/phase · còn gì. Không đợi hỏi.
- **KHÔNG tự commit** (kể cả khi mọi thứ xanh) — để user tự commit, trừ khi user yêu cầu trong đúng lượt đó.