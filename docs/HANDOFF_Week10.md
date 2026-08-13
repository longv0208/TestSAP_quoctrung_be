# HANDOFF — Chỉnh hệ thống theo họp tuần 10 (thầy Đức)

> Mục đích: 1 file **tự-chứa** để bất kỳ AI/dev nào (Codex, Cursor, Claude…) tiếp tục được mà không cần context ngoài repo. Đọc file này + `CLAUDE.md` (Business Rules #15–23) + `docs/QD543_Compliance.md` là đủ nắm.

## 0. Cấu trúc repo & CHỖ DỄ NHẦM ⚠️
- **BE:** `FURPMS_BE` — .NET 8, nhánh **`dev`**. Docs nằm ở `FURPMS_BE/docs/`, conventions ở `FURPMS_BE/CLAUDE.md`.
- **FE:** `core/FURPMS-Web` — React 19 + Vite, nhánh **`dev`**. **ĐÂY là FE hiện hành.**
- 🗑️ **Repo FE cũ đuôi `v0` đã BỎ HẲN** (30/07) — không dùng, không tham chiếu, không copy gì từ đó nữa. Mọi công cụ/doc đã trỏ về `core/FURPMS-Web`.

## 1. Chạy app

### Clone BE về chạy — 2 trường hợp
BE tự `Migrate()` (áp PhaseF/G/H/I…) + seed khi boot, **miễn là chạm được SQL Server** ở connection string trong `appsettings.json` (`Server=localhost,1435;…sa/Furpms@Strong123;…`).

**(a) Có Docker** — cách khuyến nghị, khớp sẵn connection string:
```bash
cd FURPMS_BE
docker compose up -d          # dựng SQL Server 2022 ở host cổng 1435 (khớp appsettings.json)
dotnet run --project FURPMS.API   # BE :5068 tự Migrate + seed
```
> ⚠️ Compose cố ý map **`1435:1433`** (KHÔNG dùng 1433) vì nhiều máy đã cài sẵn SQL Server ở cổng mặc định 1433 → clone về bị đụng cổng hoặc lỗi `Login failed for user 'sa'` (BE nối nhầm SQL local). Cổng 1435 né hẳn. Nếu 1435 cũng bận thì đổi **cả compose + connection string** sang cổng trống khác.

**(b) Không Docker** — dùng LocalDB (Windows, có sẵn khi cài Visual Studio). Đừng sửa `appsettings.json` (đã commit). Tạo **`FURPMS.API/appsettings.Development.json`** (đã `.gitignore`, mỗi máy tự đặt — file Development đè file base khi chạy Debug):
```json
{ "ConnectionStrings": { "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=FURPMS_V2;Trusted_Connection=True;TrustServerCertificate=True" } }
```
Rồi `dotnet run --project FURPMS.API` (hoặc F5 trong Visual Studio). Windows auth → không cần sa/mật khẩu, khỏi lỗi `Login failed`. `sqllocaldb info` để kiểm tra LocalDB; chưa có thì `sqllocaldb create MSSQLLocalDB`.

### (c) Bật EMAIL + AI — secret KHÔNG commit
`appsettings.json` (đã commit) cố ý để trống `SmtpUsername`/`SmtpPassword` và không có `GeminiAI`. Nguồn đổ giá trị vào **cùng tên key**, khác nhau theo môi trường — không phải sửa code:

| Môi trường | Nguồn | Cách đặt |
|---|---|---|
| **Local** | `FURPMS.API/appsettings.Development.json` | đã có trong `.gitignore` (dòng 370–371) → không bao giờ lên git |
| **Render (prod)** | Environment Variables | tên key thay `:` → **`__`**: `EmailSettings__SmtpUsername`, `EmailSettings__SmtpPassword`, `GeminiAI__ApiKey`, `ConnectionStrings__DefaultConnection`, `JwtSettings__SecretKey`. Đặt 1 lần trên dashboard, không dán lại mỗi lần deploy |

```json
// FURPMS.API/appsettings.Development.json — tạo tay, mỗi máy tự đặt
{
  "EmailSettings": {
    "SmtpUsername": "<user>@smtp-brevo.com",
    "SmtpPassword": "<xsmtpsib-...>",
    // DEV: hứng hộ mail của các miền GIẢ về 1 hộp thư thật. PROD phải bỏ cả 2 dòng.
    "CatchFakeMailInbox": "ban@gmail.com",
    "RedirectDomains": [ "furpms.edu.vn" ]
  },
  "GeminiAI": { "ApiKey": "<AIza...>", "Model": "gemini-flash-latest" }
}
```
> **Vì sao cần `CatchFakeMailInbox`:** tài khoản seed dùng email **không có thật** (`pi.demo@furpms.edu.vn`…) → đi luồng sẽ không thấy mail nào, tưởng hỏng. Đổi email seed thì hỏng seeder (nó dùng email **làm khóa định danh**: `FirstAsync(u => u.Email == "admin@furpms.edu.vn")`). Nên chuyển hướng ở **tầng gửi**: mail về hộp thư của bạn, tiêu đề ghi `[→ pi.demo@furpms.edu.vn]` để biết ai đáng lẽ nhận. `email_log` vẫn ghi **người nhận thật** nên vẫn trả lời được "đã báo cho PI chưa?".
>
> ⚠️ **`RedirectDomains` quyết định mail nào bị hứng.** Chỉ mail gửi tới các miền liệt kê ở đây mới chuyển hướng; **địa chỉ thật (gmail, fpt.edu.vn…) đi thẳng tới người nhận**. Trước đây chuyển hướng **tất cả** ⇒ tạo tài khoản bằng mail thật thì người ta **không bao giờ nhận được thư**, mà `email_log` vẫn ghi `SENT` nên rất khó lần ra. Bỏ trống `RedirectDomains` = quay lại hành vi cũ (hứng tất cả).
> 📦 **Chỗ lưu file:** có cấu hình `Cloudinary:*` ⇒ lưu lên **Cloudinary**; không có ⇒ về **đĩa local** (`App_Data/uploads`). Production (Render) **bắt buộc** dùng Cloudinary: env `Cloudinary__CloudName`, `Cloudinary__ApiKey`, `Cloudinary__ApiSecret`, `Cloudinary__Folder`.
> ⚠️ **Chỉ áp dụng khi lưu ĐĨA LOCAL:** file nằm ở `FURPMS.API/App_Data/uploads`, THEO THƯ MỤC CHẠY BE. Đổi sang clone/repo khác mà không chép `App_Data` sang thì **mọi tài liệu cũ đều 404** (DB vẫn trỏ tới các file đó). `App_Data` đã gitignore nên git không mang giúp — phải chép tay.
> ⚠️ **Gemini không nhận `.docx` inline** (400 `Unsupported MIME type`). Mọi chỗ đưa file cho AI phải qua **`GeminiFileInput.AskAboutFileAsync`** — nó tự bóc text .docx bằng OpenXml, chỉ PDF/ảnh mới gửi thẳng bytes.
>
> `EmailSettings:FrontendUrl` (mặc định `http://localhost:5173`) dùng để ghép link "Xem chi tiết" trong email — **deploy nhớ đổi** sang URL FE thật, không thì người nhận bấm vào localhost.
> ⚠️ Mail hiện **rơi vào Spam** vì `FromEmail` là `@gmail.com` gửi qua relay Brevo (SPF/DKIM không khớp domain gmail.com). Đây là giới hạn hạ tầng, không phải lỗi code — demo thì dặn người xem mở thư mục Spam, hoặc đổi sang sender đã verify trong Brevo.
> Thiếu key thì **không crash**: mail ghi `email_log` trạng thái FAILED, AI báo "chưa cấu hình" và vẫn nhập tay được. Admin có công tắc tổng **`EMAIL_ENABLED`** để tắt gửi mail khi demo (ghi log `SKIPPED`, chuông in-app vẫn chạy).

### FE
```bash
cd core/FURPMS-Web && cp .env.example .env && npm install && npm run dev  # :5173, trỏ BE :5068
```
**Tài khoản seed:** admin@furpms.edu.vn/`password` · staff.demo@…/`password` · pi.demo@…/`password` · reviewer{1,2,3}.demo@…/`password` (reviewer1=Chủ tịch, reviewer2=Thư ký của hội đồng đề tài "abc").
**Verify:** BE `dotnet build && dotnet test` (84 test) · FE `npx tsc --noEmit && npm run build` + parity vi/en (932=932).

## 2. Migrations mới (tuần 10) — tự áp khi `dotnet run`
`PhaseF_CouncilQaEntry` · `PhaseG_CouncilMemberOpinion` · `PhaseH_MeetingLocation` · `PhaseI_DeadlineExtension`.

## 3. ĐÃ LÀM tuần 10 (đều xanh + đã commit trên `dev`)
| Mục | Trạng thái | Chỗ chính |
|---|---|---|
| **P7** đa vai — đổi vai ở header | ✅ | FE `store/auth.store.ts`, `components/layout/UserMenu.tsx`, `Sidebar.tsx`, `DashboardPage.tsx` |
| **P5** biên bản: roster auto + Q&A + ý kiến TV (chuyên môn/kinh phí) + Thư ký soạn/Chủ tịch khóa | ✅ | BE `CouncilQaEntry`/`CouncilMemberOpinion` + `ReviewScoringService`; FE `features/reviewer/proposal-review/MinutesPanel.tsx` |
| **P-Timeline** tiến trình hợp đồng (mốc+ngày, click mở minh chứng) | ✅ | FE `features/staff/contracts/ContractMilestoneTimeline.tsx` (tab "Tiến trình") |
| **P1** tài chính=minh chứng (không quản tiền): upload chứng từ giải ngân, ẩn tiền + nav + dashboard budget, "đánh dấu đã giải ngân" | ✅ | BE `DisbursementEvidenceController` + `ProposalDocumentService`; FE `DisbursementsPanel/DisbursementEvidence/ConfirmDisbursementDialog`, `nav.ts`, `AdminDashboardPage` |
| **P4** gia hạn deadline đợt = log, không ghi đè | ✅ | BE `DeadlineExtension` + `CycleService.ExtendCycleDeadlineAsync`; FE `features/admin/cycles/ExtendDeadlineDialog.tsx` |
| **P2** lịch họp offline/online + địa điểm; cảnh báo trùng lịch | ✅ (một phần) | BE `CouncilMeeting.Location`, `CouncilService.GetScheduleConflictsAsync`; FE `ScheduleMeetingSheet`, `MeetingsPanel` |
| **P8** tự sinh Word hợp đồng + upload bản đã ký (lưu song song) | ✅ | BE `DocumentExportService.ExportContractDocAsync` + `ContractExportController` + `ContractDocumentsController`; FE `ContractDetailSheet` (nút Xuất Word) + `ContractSignedDocs.tsx` |
| **P3** upload+AI tùy chọn · **P6** báo cáo tiến độ | ✅ ĐÃ CÓ SẴN | Step2 wizard (AI on-demand); `ProgressReportsController` (submit/schedule/evaluate) + FE PI page/Staff panel |

## 4. CÒN LÀM (backlog)
P2 tuần 10 **đã XONG hết**: 2 hội đồng · offline/online+địa điểm · cảnh báo trùng lịch · **điểm danh** · **gate gửi mời** · **slot theo đề tài** (`PUT /councils/{id}/slots`, tab "Lịch chấm" trong chi tiết hội đồng). Chỉ còn (nhỏ, tùy chọn): thay người/đổi lịch **sau khi đã gửi mời** (BE `CouncilMeeting` đã hỗ trợ reschedule; FE chưa có nút chuyên biệt).
- **P6 nối P1 (bỏ chủ động):** progress-report Đạt → set `ConditionMetAt`. Chưa làm vì mapping "báo cáo tiến độ ↔ đợt giải ngân" không khớp data (disbursement hiện gate theo *deliverable*, không theo progress-report). Cần thầy/leader chốt quy tắc map trước.
- **Phụ thuộc:** field BM04 (khách mời, tổng điểm) đã có ảnh phiếu gốc thầy (23/07) nhưng để backlog — hiện biên bản đã đủ dùng.

## 5. Business rules tuần 10 → `CLAUDE.md` #15–23 (đã ghi)
**#15** tài chính=minh chứng (không quản tiền, **#2/#3/#6 SUPERSEDED**) · **#16** chỉ 2 hội đồng · **#17** lịch họp (gate/slot/địa điểm/trùng lịch/không đóng băng) · **#18** biên bản (2 phong cách + ý kiến TV) · **#19** gia hạn=log · **#20** upload+AI tùy chọn, loại suy từ đợt · **#21** tự sinh Word HĐ · **#22** timeline · **#23** multi-role header.

## 6. Gotchas
- Data seed **mỏng** (3 đợt, 1 HĐ HD-2026-002, 1 hội đồng đề tài "abc", 1 meeting Google Meet). Để demo P2 trùng-lịch/offline cần tạo thêm; P7 switcher cần 1 acc **≥2 vai**.
- Lỗi phân quyền trả **403** (không phải 401) — FE không được logout khi gặp 403.
- Không hard-commit mẫu Word (P8 dùng OpenXml build code-sẵn, không template file).
