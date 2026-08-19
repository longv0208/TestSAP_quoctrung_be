# FURPMS — Mục lục tài liệu & Việc còn lại

> 📇 **Không biết mở doc nào? → `00_INDEX.md`** — mục lục xếp theo tác dụng (5 tầng): tầng 1 là nguồn sự thật phải tuân theo (QĐ543), tầng 2 là việc đang làm, tầng 5 là lịch sử chỉ tra khi cần.
>
> 🆕 **MỚI NHẤT — `PLAN_Week13_Demo_0508.md`**: gộp 2 bản note của nhóm sau buổi demo với thầy **chiều 05/08/2026** — **43 đầu việc** chia 6 nhóm (hội đồng/chấm/biên bản · AI · hợp đồng-nghiệm thu-giải ngân · báo cáo · UI-i18n-data demo · business rule) + thứ tự đề xuất + **8 câu phải hỏi lại thầy/nhóm trước khi làm**. Đọc file này TRƯỚC `PLAN_Week12.md`.
>
> ⭐ **Nền trước đó: `PLAN_Week12.md`** — kế hoạch sau demo thầy 29/07 + trạng thái từng mục. Xong **P0–P7** (≈17/18 ý thầy, 94%); còn **P8** (AI) — user chủ động hoãn để test trước. % theo nhóm chức năng ở `PROGRESS.md`.
>
> ⏸ **Đang chờ user chốt:** `PLAN_Week12.md` → mục **"Chờ user quyết định"** — **Q1** tìm kiếm ngữ nghĩa (bỏ / thay bằng tìm kiếm nâng cao / làm thật) · **Q2** AI cho từng role *(đã xử lý)* · **Q3** chuẩn hoá validate ở FE · **Q4** Google Calendar/Meet (cần đăng ký OAuth, có bẫy refresh token 7 ngày) · **Q5** 🔴 **deploy thật: Render xoá sạch file upload mỗi lần redeploy** — phải đổi sang storage ngoài trước khi deploy. Đừng tự làm, hỏi lại trước.
>
> 🆕 **Tiếp tục việc tuần 10 (thầy Đức)?** Đọc **`HANDOFF_Week10.md`** trước — file tự-chứa: cấu trúc repo (**FE = `furpms-web`** — repo FE cũ đuôi `v0` đã bỏ hẳn từ 30/07), cách chạy, đã làm / còn backlog, migrations mới. Rule nghiệp vụ tuần 10 ở `CLAUDE.md` #15–23.
>
> 📋 **Rà soát hệ thống + backlog (thừa / chưa ổn / ý tưởng để sau):** **`SYSTEM_REVIEW.md`** — chốt lại đánh giá để không quên (IDOR endpoint con, đa vai, sản phẩm/kỳ báo cáo tự tạo, rich text, lịch/thông báo…). Cập nhật tuần 11.
>
> 📄 **MỚI 16/08 — `RA_SOAT_BAO_CAO_RP1-7.md`**: soi cả 7 báo cáo Capstone, đối chiếu **mẫu FLM + SEP490 StudentGuide + Cẩm nang tránh lỗi**. Kết quả: **RP6 đã hoàn thiện xong** (8 workflow, 19 ảnh chụp thật giao diện tiếng Anh, caption + cross-ref + danh mục hình/bảng tự động); **RP7 phần V và VI còn RỖNG HOÀN TOÀN** mà lại chiếm 32% OGA + 35% TDA; **RP5 có 3 con số mâu thuẫn nhau** (doc ghi 99 unit test, Excel ghi 39, repo thật 190); **thiếu hẳn `Report3_Project Tracking.xlsx` + báo cáo tuần + lịch dự án**; và **2 sơ đồ MAIN FLOW vẽ chức năng hệ thống không có** (AI tìm reviewer, AI insight). Có bảng việc xếp theo điểm÷công.
>
> 🔎 **MỚI 14/08 — `RA_SOAT_LUONG_HAPPY_CASE.md`**: user tự đi lại toàn bộ luồng happy case từ trải nghiệm và ghi ra mọi chỗ thấy "cấn"; file này **kiểm chứng từng nghi ngờ với code thật + QĐ543**. Kết quả: **8 chỗ hoá ra ĐÃ CÓ** (PI xem được lịch họp, Chủ tịch trả biên bản về Thư ký, lưu biên bản chỉ cần quorum 2/3 nên vắng 1 người không kẹt, giải ngân đã đúng Điều 16…) và **12 lỗ hổng THẬT**, nặng nhất là **AI vòng nghiệm thu vẫn đọc đề cương gốc thay vì sản phẩm** và **thư mời không nói mời chấm đề tài nào**. Có §4 xếp thứ tự nên làm theo đau÷công. **Chưa sửa code.**

> Điểm vào cho thư mục `docs/`. Cập nhật: 2026-08-19.
> Lưu ý: KHÔNG đổi tên / di chuyển các file (CLAUDE.md + docs khác đang tham chiếu path) — file này để tra cứu + theo dõi việc dở.

## Chuẩn bị bảo vệ

| Tài liệu | Mục đích |
|---|---|
| `SOFTWARE_PACKAGE_INSTALLATION_GUIDE.md` | Hướng dẫn cài đặt để đặt ở gốc gói nộp cùng BE/FE: cấu trúc source, prerequisites, Docker/PostgreSQL, migration/seed, SQL dump tùy chọn, tài khoản test và checklist đóng gói. |
| `DEMO_SCRIPT_SUBMISSION.md` | Bản Demo Script nộp trước buổi: 8 workflow, UC demo, phân công 4 người, tài khoản/data test, lời thoại, dự phòng và checklist chạy thử. |
| `USE_CASE_STATUS_TRACEABILITY.md` | Bảng trạng thái và traceability đầy đủ 35 Use Case dùng cho slide hội đồng kín. |

---

## 🔧 Việc còn lại / đang dang dở (backlog)

### A-18/08c. Khép P2-2 — chi tiết đề cương có lại tiến trình

- Màn Staff đổi từ **Xét duyệt đề cương** thành **Đề cương**; hàng dùng nút **Xem chi tiết**.
- Trang chi tiết chia đúng 2 tab: **Nội dung đề cương** (thông tin · sản phẩm · file xem tại chỗ)
  và **Tiến trình đề tài** (vòng xét duyệt + timeline hợp đồng/báo cáo/giải ngân dùng chung với PI).
- Kanban và form tạo vòng cũ không mang trở lại; mọi thao tác vòng/hội đồng vẫn ở một nơi là
  **Hội đồng & Chấm**. Việc `BACKLOG_Uu_tien.md` **P2-2** đã xong.

### A-18/08b. Bẫy môi trường đã gỡ + việc còn treo (kiểm lại 18/08)

**Đã gỡ — skill `fe-e2e` trỏ sai cả ba thứ.** File `.claude/skills/fe-e2e/SKILL.md` (có ở cả repo
này lẫn repo BE cũ, và một bản ở `.agents/`) ghi BE nằm ở `…\FURPMS\FURPMS_BE` (**repo đã chết từ
02/08**), cần **SQL Server cổng 1435** (đã bỏ từ 14/08 khi sang PostgreSQL 5433), và mật khẩu demo
`Admin@123456` / `Staff@123456` / `Faculty@123456` (**thực tế tất cả đều là `password`** từ lần
siết seeder 14/08). Chạy theo bản cũ là dựng nhầm repo lên một DB không tồn tại. Cả ba bản đã sửa
và đồng bộ.

**Đã kiểm lại — KHÔNG còn là lỗi** (đừng mất công sửa lại):
- Form "Sửa lại" báo cáo tiến độ của PI **có** prefill đầy đủ, kể cả `items`
  (`CreateProgressReportSheet.tsx` gọi `useProgressReportQuery` + `useEffect` reset). Từng nghi là
  lỗi mất dữ liệu, hoá ra đã sửa từ trước.
- Xuất phụ lục hợp đồng ra Word **đã có** (`ExportAmendmentDocAsync` + endpoint), không phải chưa làm.

**Còn treo thật:**

| Mức | Việc | Chỗ |
|---|---|---|
| Vừa | `ChangeRequestService.CreateAsync` (loại 1 = gia hạn) **không chặn biên** cho `NewValue`; `ReviewAsync` chỉ đổi trạng thái, **không áp dụng** thay đổi nào | `ChangeRequestService.cs` |
| Nhỏ | Xuất BM07 (yêu cầu thay đổi) ra Word — chưa làm | `DocumentExportService.cs` |
| Nhỏ | Chú thích sai "QĐ543: gia hạn tối đa 6 tháng" — Điều 10.4 nói **≤ 1/2 thời gian đã duyệt**, không phải con số cố định | `MyAmendmentsPage.tsx` |
| Nhỏ | Khoá i18n mồ côi `startMeeting` / `endMeeting` / `undoStartMeeting` (nút đã ẩn 17/08) | `vi.ts` · `en.ts` |

**Trước khi nộp:** quét toàn repo tìm khoá/mật khẩu. Đã kiểm `appsettings.json` (đã commit) —
**sạch**: `JwtSettings.SecretKey` là chuỗi placeholder, `EmailSettings` rỗng. Key thật
(`GeminiAI:ApiKey`) nằm ở `appsettings.Development.json` đã gitignore đúng cách.


### A-18/08. Bốn lỗi bạn trong nhóm báo — ĐÃ SỬA

Chi tiết + kết quả kiểm chứng chạy thật: `PROGRESS.md` §"Cập nhật 18/08".

| # | Lỗi | Sửa ở đâu |
|---|---|---|
| ① | Không xoá được người dùng | BE `UsersController` + `UserService.DeleteUserAsync` (xoá mềm, 5 chốt chặn) · FE `admin/users/columns.tsx` + `UsersPage.tsx` |
| ② | Email sai định dạng vẫn tạo được tài khoản | BE `UserService.IsValidEmail` · FE `admin/users/user.schema.ts` |
| ③ | Thêm lĩnh vực xong PI không thấy | BE `TrackDto.CycleCount` · FE `staff/tracks/columns.tsx` (cột "Đợt đang mở" + cảnh báo) |
| ④ | PI/Admin lọt vào danh sách chọn ủy viên | BE `ReviewBoardDto.PiUserId` · FE `utils/council-eligibility.ts` dùng ở `CreateCouncilSheet` + `AddCouncilMemberDialog` |

> **Kèm theo ④:** `AddCouncilMemberDialog` trước đây gửi chức danh `"Chairman"` (trong khi
> `CreateCouncilSheet` gửi `"Chair"`) và **không có "Phản biện"** — nên không thể thêm phản biện cho
> vòng nghiệm thu qua đường này, dù chỉ phản biện mới viết BM10 (Điều 12.3.b). Đã thống nhất về
> `Chair / Secretary / Member / Opponent` và dịch nhãn thay vì in mã tiếng Anh thô.


### A0. Đã TẠM ẨN khỏi giao diện (code còn nguyên, bật lại dễ)

> Ẩn chứ không xoá — mỗi mục ghi rõ **ẩn ở đâu** và **vì sao**, để lôi ra lại không phải dò.

| Ẩn ngày | Cái gì | Ẩn ở đâu | Vì sao |
|---|---|---|---|
| 17/08 | **3 thẻ AI bên PI** — Đối chiếu form↔file, Tóm tắt, Góp ý | `features/pi/proposals/ProposalDetailPage.tsx` (chú thích khối render + khối import) | Qua các đợt thử chưa lần nào giúp PI ra quyết định gì: PI đã biết rõ đề tài của mình, tóm tắt do máy viết không thêm thông tin. **AI vẫn chạy và vẫn hữu ích ở màn NGƯỜI CHẤM** (`ProposalReviewWorkspace` → `AiSummaryCard` với `councilId`+`autoGenerate`) — ở đó người đọc chưa từng thấy đề tài. Endpoint/hook/component giữ nguyên. |
| 17/08 | **Nút Bắt đầu / Kết thúc họp** | `features/staff/meetings/columns.tsx` · `features/staff/proposal-reviews/MeetingsPanel.tsx` | Không luồng nào chờ `IN_PROGRESS`; Chủ tịch chốt biên bản thì `ReviewScoringService` đã tự đóng buổi họp. Endpoint BE giữ lại. |
| 17/08 | **Ô "Phản biện ngoài"** khi thêm thành viên hội đồng | `features/staff/proposal-reviews/AddCouncilMemberDialog.tsx` | Cờ `IsExternal` chỉ được lưu rồi trả về, không luồng nào rẽ nhánh theo nó — mức thù lao riêng cho người ngoài trường đã bỏ cùng toàn bộ phần tính tiền (rule #15). Cột DB giữ nguyên, vẫn gửi `false`. |
| 17/08 | **"Tìm kiếm bằng AI"** ở menu PI | `constants/nav.ts` (chú thích dòng `nav.aiSearch`) | Route `ROUTES.AI_SEARCH` và `SemanticSearchPage` vẫn còn, chỉ gỡ khỏi menu. |
| 17/08 | **Tab "Chấm điểm" ở vòng NGHIỆM THU** | `features/reviewer/proposal-review/ProposalReviewWorkspace.tsx` | **QĐ543 Biểu mẫu 11 không có tiêu chí lẫn thang điểm** — chỉ ĐẠT / KHÔNG ĐẠT + lý do; BM12 chỉ đếm phiếu Đạt/Không đạt/Xuất sắc. Tab "Nghiệm thu" (`AcceptanceEvaluationForm`, `PASS`/`FAIL`) đã làm đúng BM11. Bày tab chấm điểm chỉ ra ô trống "Chưa cấu hình tiêu chí chấm". Vòng XÉT DUYỆT vẫn chấm theo thang bình thường. |
| 17/08 | **Menu người chấm: "Thành viên hội đồng" + "Chấm điểm"** | `constants/nav.ts` | Cả hai chỉ là **cách bày khác** của "Đề tài được phân công": cùng `useMyMembershipsQuery`, và **cả ba điều hướng tới đúng một đích** `assigned-reviews/{councilId}`. "Chấm điểm" = tập con (lọc thêm `roundStatus === OPEN`); "Thành viên hội đồng" = cùng dữ liệu đổi thẻ thành bảng. Ba lối vào một màn khiến người chấm tưởng còn việc ở tab kia. |
| 17/08 | **File đề cương + thẻ AI ở vòng NGHIỆM THU** | `reviewer/proposal-review/ProposalReviewWorkspace.tsx` | Cả hai xoay quanh **đề cương** (bản kế hoạch đầu kỳ): `ProposalDocumentViewer` mở file thuyết minh, `AiSummaryCard` → `SuggestScoresAsync` đọc `Proposals` + file đề cương, **không đọc sản phẩm/báo cáo tổng kết**. Vòng 2 phải kết luận đề tài *làm ra được gì*, nên chìa bản kế hoạch ra là sai hướng. Hồ sơ đúng nằm ở tab "Hồ sơ nghiệm thu". Vòng XÉT DUYỆT giữ nguyên cả hai. |
| 17/08 | **Đoạn văn tóm tắt** trong thẻ AI hỗ trợ chấm | `features/pi/proposals/AiSummaryCard.tsx` | Chỉ diễn đạt lại tab "Thông tin đề tài" ngay bên cạnh. Giữ Ưu điểm / Nhược điểm — đó mới là nhận định, và là chỗ AI đối chiếu file đính kèm với biểu mẫu. `summaryText` vẫn sinh & lưu ở máy chủ. |

### A1. Xem chi tiết sản phẩm / báo cáo tiến độ (17/08)

Trước đây cả Staff lẫn thành viên hội đồng chỉ thấy **tên + ngày nộp + trạng thái**. Mọi thứ chủ nhiệm thực sự gõ — yêu cầu khoa học, mô tả sản phẩm, nội dung đã làm / còn tồn, kế hoạch kỳ sau, kiến nghị, bảng tiến độ theo hoạt động (BM06), link Drive — **đã có sẵn trong phản hồi máy chủ** nhưng không màn nào bày ra.

- Component dùng chung: `components/shared/DossierDetailSheet.tsx` (`DeliverableDetailSheet` + `ProgressReportDetailSheet`) — panel trượt phải, **chỉ xem**, nút duyệt vẫn ở chỗ cũ.
- Gắn ở: hội đồng `reviewer/proposal-review/AcceptanceDossierPanel.tsx` · Staff `staff/contracts/DeliverablesPanel.tsx` + `ProgressReportsPanel.tsx`.
- BE: `DeliverableResponse` thêm `scientificRequirements` + `notes`.
- Link demo: mọi minh chứng trong `DemoScenarioSeeder` trỏ về **một thư mục Drive có thật** (`DemoEvidenceFolderUrl`) — trước đó là đường dẫn bịa `.../1demo-.../view`, bấm vào ra trang lỗi Google nên lúc demo trông như hỏng.

### A. Code chưa làm / làm dở
- **Phase 5 — Versioning + pin biểu mẫu chấm (RubricTemplate)**: chưa làm. Cần migration + rewire chỗ resolve template khi chấm (FE `RubricForm` đang lấy template active đầu tiên). Rủi ro vừa → làm phiên riêng.
- **i18n incremental**: mới key qua `t()` ở 2 màn (CycleManagement, TrackWorkspace). Các màn khác đã Việt hoá "track"→"lĩnh vực" **inline** → chuyển dần sang key i18n.
- **Dropdown loại đề tài ở form nộp** (`ProposalSubmission`) còn cứng 2 lựa chọn (Ứng dụng/Cơ bản). Loại mới hiện ở dropdown *tạo đợt* nhưng chưa ở *form nộp* (đề tài lấy loại theo đợt nên không chặn — chỉ là chỉnh hiển thị).
- **Thùng rác toàn hệ thống**: mới làm *mẫu* trên ResearchType. Nhân rộng = thống nhất `IsDeleted` + EF global query filter + 1 màn recycle-bin tổng. Để sau.

### B. Kết quả AUDIT FE↔BE toàn bộ route (07/07/2026 — 81 route FE so với 117 route BE)
- ✅ **Đã fix trong đợt audit:** (1) `my-memberships`/`meetings` trả **id bản đề cương hiện hành** thay vì projectId (trước đó reviewer bấm vào đề tài sẽ 404); (2) thêm 2 endpoint FE đang gọi mà BE thiếu: `PATCH /api/users/{id}/toggle-active` + `POST /api/users/{id}/reset-password` (reset về `Furpms@123456`) — đã smoke test sống.
- ✅ **`changeRequestService` (4 route) — ĐÃ IMPLEMENT BE (09/07/2026):** bảng `proposal_change_requests` (migration `PhaseC_ChangeRequests`) + `ChangeRequestService` + `ChangeRequestsController` + DTO khớp FE (type/status PascalCase). 40/40 test. **E2E UI đầy đủ:** PI gửi "Gia hạn thời gian" → admin thấy trong queue → duyệt → queue trống; 3 endpoint đều 200.
- ❌ **404 còn lại (pre-existing):** `POST /api/assignments/{id}/ai-feedback` + `ai-rubric-assessment` (aiService, gọi từ ReviewerInterface/AiSummaryPanel) — tính năng AI gợi ý nhận xét khi chấm. → implement qua GeminiService khi có key, hoặc ẩn nút.
- `AcceptanceEvaluationsController` (`/api/councils/{id}/acceptance`) → FE không gọi → orphan (luồng nghiệm thu FE dùng review-scoring).

### B2. DB Redesign v3 — PROJECT-CENTRIC (biên bản Review 2 — xem `DB_Redesign_v3_PostReview2.md`)
- ✅ **Phase A ĐÃ CODE XONG (07/07/2026):** bảng `project` (gốc) + proposal versioning (`project_id`+`version_no`+`is_current`, REVISION tạo bản mới) + `project_member` + gộp `project_deliverable` + `cycle_track` + order mặc định "Nghiên cứu tự do" (100% project có order) + `contract`→project **1-n** + `contract_phase` + `final_report`→project + review round/council tạm neo project. **DB đã reset** (migrations xóa sạch → 1 bản `PhaseA_ProjectCentric`, 54 bảng; FURPMS_V2 drop + tạo lại + seed). Build 0 lỗi · **36/36 test xanh** · smoke test API OK (login/proposals/detail/contracts) · FE tsc+build xanh (2 type patch).
- ✅ **Phase B ĐÃ CODE XONG (07/07/2026):** `review_round`→**cycle_track** (unique cycle_track+số vòng+dimension; nhiều project cùng track DÙNG CHUNG round — tự reuse khi tạo) + bảng nối **`project_rounds`** (kết quả TỪNG đề tài; rule #2 check per-project) + **`council_project_assignments`** (council chấm nhóm đề tài; COI check với TẤT CẢ đề tài được gán) + score/decision/feedback/acceptance thêm chiều `project_id` (unique 3 chiều; biên bản Chủ tịch khóa TỪNG đề tài; council DECIDED khi mọi đề tài có biên bản khóa). Migration `PhaseB_ReviewMN` (56 bảng). Build 0 lỗi · 36/36 test · smoke test round create→open→close PASSED. **API compat giữ nguyên**: request thêm field optional `projectId`/`proposalProjectId` — council/round 1 đề tài thì tự suy, FE chưa cần sửa.
- ✅ **Phase C — Yêu cầu thay đổi đề tài (09/07/2026):** bảng `proposal_change_requests` + service/controller/DTO + 4 endpoint (§6 API_CONTRACT). 40/40 test + E2E UI. → tổng **~56 bảng**.
- ✅ **Docs kéo theo ĐÃ CẬP NHẬT (09/07/2026):** `Review2_Diagrams.md` (§0 status có Project, §3 versioning, §4 Project SM cột thật, §5b project_round, §8 use case +change-request, §9 activity 9.2/9.4/9.6, banner §7), `RP4_Diagrams.md` (DB 56 bảng + class/sequence Project-centric), `API_CONTRACT.md` (banner + DTO mới + endpoint change-request/users), `DB_ANALYTIC_REPORT.md` (banner V2.0 + bảng thay đổi). `ERD_v3_Project_Centric.dbml` đã có banner từ trước.

### B3. Redesign màn "Hội đồng & Chấm" cấp Track (15/07/2026 — theo góp ý user test tay)
- **Bối cảnh:** user test màn Staff "Đề xuất & Phân công" thấy rối + lỗi tùm lum. Soi code xác nhận 4 bug thật: (1) thiếu nút "Mở vòng" dù FE có sẵn `roundService.openRound()` nhưng không nơi nào gọi → bấm Đạt/Từ chối luôn 409; (2) không có endpoint xóa vòng → vòng test rác tồn tại mãi; (3) nút "Đạt/Từ chối" gọi thẳng `close` — bypass biên bản Thư ký→Chủ tịch (vi phạm rule #12), đồng thời `ApproveMinutesAsync` (đường đúng) không cập nhật `project_rounds` → 2 nguồn dữ liệu lệch nhau; (4) UX nhét phân công vào modal TỪNG đề xuất trong khi DB đã đúng mô hình round-theo-track/hội-đồng-chấm-nhóm.
- ✅ **BE:** `IReviewBoardService`/`ReviewBoardService` (mới) + `ReviewBoardController` — endpoint `GET/POST .../tracks/{trackId}/rounds`, `DELETE /api/rounds/{id}`, `POST/DELETE .../projects`, `POST .../councils` (tạo hội đồng trọn gói, COI check trước khi tạo — vi phạm thì không tạo nửa vời). `ApproveMinutesAsync` fix nối mạch `project_round` + tự đóng round khi mọi đề tài xong (xem API_CONTRACT §8). 11 test mới, 51/51 xanh.
- ✅ **FE:** component mới `ReviewBoard.tsx` (+ `reviewBoardService.ts`) — chọn Đợt → Lĩnh vực → board: đề tài + rounds + hội đồng, có Mở vòng/Xóa vòng/Thêm-gỡ đề tài/Tạo hội đồng trọn gói (không còn nút chốt trực tiếp). Tab "Hội đồng & Chấm" thêm vào cả Staff + Admin dashboard. `ReviewRoundsPanel.tsx` (cũ) đã **xóa hẳn** — modal chi tiết đề xuất giờ chỉ hiện badge trạng thái vòng (read-only) + link sang tab mới.
- ✅ **E2E qua Playwright MCP (dữ liệu thật, không mock):** board load đúng 4 đề tài + 4 round có sẵn từ lần test trước của user → xóa thành công 2 vòng rác (Vòng 3 Sàng lọc, Vòng 4 Nghiệm thu — PENDING, không hội đồng) → "Mở vòng" trên Vòng 1 Tài chính chuyển PENDING→OPEN thành công (đúng bug user report) → link "Quản lý ở Hội đồng & Chấm →" từ modal đề xuất điều hướng kèm cycleId/trackId, board tự chọn đúng. Đồng thời fix luôn UX nhỏ tồn đọng ở dòng dưới (C2, "Gửi thư mời bị disabled tới khi reload") vì board mới refetch toàn bộ sau mỗi thao tác.
- ✅ **Code-review (8 góc) + fix bug BE (15/07):** rà lại chính diff redesign, sửa 5 vấn đề thật ở **BE thuần** (không đụng FE):
  - **#1+#5** gom logic đóng round vào `ReviewRoundFinalizer` (dùng chung `ApproveMinutesAsync` + `CloseRoundAsync`): `REVISION_REQUIRED` **không** còn khóa `project_round` thành FAILED (giữ OPEN cho PI nộp lại — rule #1); `round.status/result` suy nhất quán từ "tất cả PASSED?" nên hết mâu thuẫn/phụ thuộc thứ tự duyệt.
  - **#2** `CreateCouncilPackageAsync` bắt buộc ≥1 Chair + ≥1 Secretary (thiếu → 400) — tránh hội đồng "chết" không chốt được.
  - **#4** `FinalizeDecisionAsync` (`POST .../decision`) **khóa lại → 409**: đường tắt bỏ qua biên bản + không đồng bộ project_round. Chốt chỉ qua minutes (Thư ký→Chủ tịch).
  - **#3** thay reviewer bị DECLINED (rule #4): BE **đã có sẵn** `POST /councils/{id}/members` + `DELETE /council-members/{id}` → không cần sửa BE, FE mới gọi (contract §8.1/§8.3).
  - 53/53 test (thêm test REVISION-reopenable + Missing-Secretary). Cập nhật `API_CONTRACT.md §8`.
- ✅ **Dọn nợ kỹ thuật (15/07):** gộp N+1 (tạo vòng theo track + COI tạo hội đồng), projection `GET review-board` (4 field thay full entity), **siết quyền board = Admin/Staff** (đóng lỗ lộ danh tính hội đồng). Build + 53/53 test.
- ✅ **P0 reopen REVISION (15/07):** `ReviewRoundService.ReopenAfterResubmitAsync` — PI nộp lại bản REVISION (submit v2+) → council `DECIDED`→`FORMING`, biên bản về nháp, project_round→OPEN, **giữ điểm cũ** (rule #1). Hook vào `SubmitProposalAsync`. 54/54 test.
- ✅ **P2 gom code trùng (15/07):** `ReviewShared` — COI (rule #5) + MapMember + tạo/tái-dùng round gom về 1 chỗ, 3 service (ReviewBoard/ReviewRound/Council) dùng chung. 54/54 test.
- ⚠️ **Follow-up còn lại** (xem `PROGRESS.md`): (a) **P1** — config Gemini/SMTP key để demo trọn (hạ tầng, không phải code); (b) track mồ côi "a.i" (id=2, chưa gắn cycle).
- 🧊 **FE đóng băng:** FE hiện tại (`ReviewBoard.tsx`…) **giữ nguyên, không sửa tiếp** — sẽ có FE mới do thành viên khác làm. `API_CONTRACT.md` là bản giao kèo để FE mới code khớp BE.

### B4. Tích hợp FE MỚI (FURPMS-Web của Dũng) ↔ BE local (15/07/2026)
- **FE mới** ở `d:\...\core\FURPMS-Web` (React 19 + Vite + React Query + radix, mock-first bằng MSW). Đã đối chiếu **toàn bộ endpoint FE gọi vs BE**: khớp route + envelope `ApiResponse` + enum (họ tự "khám nghiệm sống" BE: INVITED/CONFIRMED, SCIENCE/FINANCE...).
- ✅ **Sửa trong repo FE (tối thiểu):** (1) `main.tsx` — cờ `VITE_USE_MOCK_API` trước đây bị bỏ qua (dev luôn mock) → giờ mock chỉ bật khi cờ =true; (2) tạo `.env` trỏ `http://localhost:5068/api` + mock=false; (3) `.gitignore` thêm `.env`.
- ✅ **BE thêm 3 endpoint dashboard** `GET /api/analytics/dashboard/{staff|faculty|reviewer}` (FE đang phải mock vì BE thiếu — lệch sống duy nhất phát hiện được). Kèm fix authz: nhiều `[Authorize]` là AND → hạ class-level xuống `[Authorize]`, role đặt per-endpoint (trước đó Faculty/Reviewer bị 403).
- ✅ **Smoke test headless (Playwright script) 4 role × ~25 trang qua BE local thật (mock OFF): 0 API ≥400, 0 console error.** Login admin/staff/pi/reviewer đều OK, dashboard 3 role render dữ liệu thật.
- ⚠️ **FE mới còn thiếu (việc của FE, BE đã sẵn):** luồng biên bản Thư ký→Chủ tịch (minutes) — hiện chỉ có scores + `rounds/close` legacy; màn Review Board cấp track (§8.1); các mảng contracts/deliverables/disbursements/progress/final-reports/change-requests/documents chưa có service. Endpoint FE gọi mà BE **cố tình chưa làm**: `/ai/search`, `/ai/similarity-check`, `/ai/suggest-reviewers`, `/integrations/google-meet/generate` (mock-only, cần Gemini key hoặc để mock).
- Lưu ý vận hành: DB dev = **PostgreSQL 16 Docker** (`furpms-db-1`, cổng **5433**) — phải `docker compose up -d` TRƯỚC khi chạy BE, nếu không BE thử lại 10 lần × 3 giây rồi thoát. FE dev chạy `localhost:5173` (CORS BE = AllowAnyOrigin nên OK).
- ✅ **Fix seed (16/07):** loại đề tài APPLIED trước seed nhầm `RequireOrderingUnit=false` → sai rule #8 (Applied phải đi theo danh mục đặt hàng). Đã sửa giá trị mới + thêm `FixAppliedOrderingUnitFlagAsync` chạy mỗi lần khởi động (idempotent) để DB cũ tự đúng. FE dựa cờ này phân biệt 2 luồng nộp (đặt hàng vs tự đề xuất). 54/54 test.
- 🎨 **FE mới — cải thiện wizard nộp proposal (repo FURPMS-Web của Dũng, 16/07):** validation đảo đúng chiều BE (bắt buộc titleVI + objectives, không phải titleEN); Funding Method thành Select WHOLE/PARTIAL; copy 2 loại đề tài đúng nghiệp vụ; Step 2 ghi rõ "optional"; Step 3 chia section; **Research Field scope theo đợt** (`GET /cycles/{id}/tracks`) nên hết lòi track mồ côi; thêm nút "Fill with sample data" + trang **Settings** (toggle bật/tắt, zustand persist). Cài skill `frontend-design` (Anthropic) để làm. Đã verify sống bằng Playwright + build xanh.

### C2. E2E browser test (07/07/2026)
- ✅ **Đã test UI (Playwright + Chrome headless):** login 4 role · PI **tạo đề tài mới qua UI** (điền mẫu → wizard 5 bước → lưu nháp → tạo project thật) · PI nộp (DRAFT→SUBMITTED) · thấy order mặc định "Nghiên cứu tự do" (điểm d) · reviewer nhận phân công + mở đề tài (chỗ từng bug projectId — nay HTTP 200) + chấm **86/100** · staff mở chi tiết → panel **Vòng phản biện (2)** hiển thị đúng: SCIENCE "Đã duyệt/Đạt", FINANCE có reviewer1 badge **Accepted** (gán+mời+nhận round-trip OK) · thống kê 3 đề tài đúng.
- ✅ **Bug tìm được + đã fix:** `RoundMeetings.tsx` fallback `councilId ?? roundId` → vòng chưa lập hội đồng gọi `/api/councils/{roundId}/meetings` → **404**. Fix: chỉ fetch khi có `councilId` (vòng chưa có hội đồng thì không có lịch họp). tsc xanh.
- ✅ **LUỒNG CORE FULL-UI HOÀN TẤT (08/07/2026, qua Playwright MCP):** staff tạo vòng → gán **Chủ tịch/Thư ký/Phản biện** → gửi 3 thư mời (alert "Đã gửi 3 thư mời") → Thư ký (reviewer2) + Chủ tịch (reviewer1) nhận → **Thư ký soạn biên bản** (nháp, proposal chưa đổi — đúng rule #12) → **Chủ tịch duyệt & khóa** ("Đã khóa", kết quả Đạt) → **proposal APPROVED** → staff **tạo hợp đồng HD-2026-003** → **ký kết → ACTIVE** → **project IN_PROGRESS**. Verify API xác nhận từng bước. 0 lỗi API.
- ✅ **GIẢI NGÂN FULL-UI (08/07):** staff "Tạo lịch giải ngân" HD-2026-003 → tự sinh **3 đợt 33/33/34%** (WHOLE ≥3 — rule #6, đều PENDING) · HD-2026-002 (PARTIAL): **PI nộp sản phẩm** (URL file) → **Staff đánh giá PASSED** → điều kiện đợt 3 tự đạt → nút "Xác nhận giải ngân" CHỈ hiện ở đợt đó (rule #3 — tiền không tự chuyển) → confirm với mã NH → **DISBURSED 90M**. Chuỗi nghiệm thu→giải ngân trọn vẹn qua UI.
- ✅ **3 bug fix trong đợt MCP:** (1) BE `AddRoundMemberAsync` bổ sung `Secretary` vào whitelist (trước gán Thư ký 400); (2) FE `ROLE_LABEL` thêm `Secretary: 'Thư ký'`; (3) FE nút "Đánh giá" sản phẩm đòi `!acceptanceStatus` nhưng BE set `PENDING` khi PI nộp → **staff không bao giờ thấy nút** — sửa thành `PENDING` cũng hiện (`ContractManagement.tsx`).
- ✅ **UX nhỏ đã fix gián tiếp (xem B3):** sau khi gán người đầu, "Gửi thư mời" từng bị disabled tới khi reload (state FE cũ không cập nhật `councilId`) — màn `ReviewBoard.tsx` mới refetch toàn bộ board sau mỗi thao tác nên không còn tái hiện.
- Skill tái sử dụng: `.claude/skills/fe-e2e/SKILL.md` + Playwright MCP (scope user). Script/screenshot đặt ở `furpms-web/.e2e/` (gitignore).

### C. Vận hành / môi trường
- **Docker BE**: restart để nạp fix mới nhất (ResearchType create Code-optional, track chống trùng mã, appsettings.json có lại connection dev) — `docker compose restart backend`.
- **`appsettings.Development.json` bị mất** → Gemini AI key cũng mất theo → tính năng AI trích xuất báo "chưa cấu hình" (không crash, vẫn nhập tay được). Muốn AI chạy lại: tạo lại file với `GeminiAI:ApiKey`, hoặc set qua env.
- **Deploy BE (Railway + PostgreSQL cùng project)**: push code mới → redeploy → test login trong Swagger; set env var (`ConnectionStrings__DefaultConnection`, `JwtSettings__SecretKey`, `GeminiAI__ApiKey`). **Đổi secret đã lộ** (JWT/SMTP/SQL password — từng commit trong git history).
- **Nhiều thay đổi BE+FE+docs trong phiên này chưa commit** (xem `git status` cả 2 repo): BE (ResearchType CRUD + fix create + appsettings), FE (i18n + track→Lĩnh vực + màn quản lý loại + dropdown reload fix), docs (§2b System Overview, §6b Logical ERD, fix erDiagram labels, README này, cập nhật handoff).

### D. Nhóm tự làm (tài liệu Review 2)
- **Document 1–4** (prose Report 4 Design / Report 5 Testing).
- ~~Bảng Team Contribution Week 1–8~~ → **đã có bản nháp** `Team_Contribution_Week1-8.md` (roster thật + % cân ~20/người + chi tiết tuần). ⚠️ Còn: rà lại số giờ cho hợp lý + **XÓA phụ lục nội bộ trước khi nộp**.

### E. Ngoài phạm vi (đã chốt KHÔNG làm)
- Multi-winner Applied (ghi âm thầy chốt **1 winner**).
- Dual-intake AI nâng cao ngoài phần đã có.

---

## 📚 Danh mục tài liệu

### 🧭 Thứ tự đọc (người mới vào)
1. `../CLAUDE.md` — conventions + **business rules #1–14** (nền nghiệp vụ, đọc trước tiên).
2. `Process_Spec_v2.md` — quy trình nghiệp vụ theo giai đoạn (bức tranh lớn: ai làm gì, đầu ra gì).
3. `DB_Redesign_v3_PostReview2.md` → `ERD_v3_Project_Centric.dbml` — mô hình dữ liệu Project-centric (cấu trúc).
4. `API_CONTRACT.md` — endpoint + payload (để code/tích hợp FE↔BE). **Bản giao kèo chính.**
5. Khi cần diagram: `Review2_Diagrams.md` (context/architecture/use-case/activity) · `RP4_Diagrams.md` (SDD: package/class/sequence).
6. Tham khảo sâu: `DB_ANALYTIC_REPORT.md` (mô tả cột) · `Review2_Tech_Stack.md` · guides `DEMO_GUIDE.md`/`EXPORT_TEST_GUIDE.md`.

### Spec hiện hành — đọc trước khi code
| File | Nội dung |
|---|---|
| `../CLAUDE.md` | Conventions + kiến trúc + **business rules** (rule #1–14). Nguồn gốc nghiệp vụ. |
| `Process_Spec_v2.md` | Quy trình nghiệp vụ chi tiết theo giai đoạn. |
| `API_CONTRACT.md` | Hợp đồng API (endpoint + payload) — **8 nhóm chức năng**. |
| `PROGRESS.md` | **Tiến độ BE theo nhóm chức năng (% + còn thiếu + việc nên làm tiếp).** |
| `FE_PROGRESS.md` | **Đánh giá FE MỚI (FURPMS-Web) — % theo nhóm + việc cần làm theo luồng chính (P0: biên bản; P1: hợp đồng/báo cáo) + bug đã biết.** Đưa Dũng đọc. |
| `QD543_Compliance.md` | **Đối chiếu QĐ 543 ↔ tính năng**: 13 biểu mẫu map vào thực thể/API nào, map field BM04 → `CouncilDecision` (dùng khi làm UI biên bản), điều khoản nào chưa đáp ứng. |
| `QD_543_..._clean.docx` | **Văn bản gốc** (22 Điều + PL01 13 biểu mẫu + PL02 thù lao). Nguồn sự thật nghiệp vụ. |
| `FURPMS_DB_Change_Spec_v1.4.md` | Spec thay đổi DB. |

### Chuẩn bị Review 2 (mới nhất)
| File | Nội dung |
|---|---|
| `USE_CASE_STATUS_TRACEABILITY.md` | **Bảng đối chiếu 35 UC Report 3 ↔ code hiện tại**, số Completed/Partial/Not completed/Superseded, nội dung sẵn cho Slide 5–7, luồng transaction chính và traceability BE–FE. |
| `DB_Redesign_v3_PostReview2.md` | **★ Thiết kế DB v3 Project-centric theo biên bản Review 2** (a)–(e) → schema đích · bảng thay đổi từng thực thể · roadmap code Phase A/B · câu hỏi mở Q1–Q5. |
| `00_INDEX.md` | **📇 MỤC LỤC — mở đầu tiên.** Xếp 26 file trong `docs/` theo 5 tầng tác dụng + đối chiếu cẩm nang tránh lỗi Capstone với hiện trạng dự án. |
| `Cam-nang-tranh-loi-Capstone-SE.pdf` | Cẩm nang tránh lỗi bảo vệ Capstone (19 tr.): 6 nguyên nhân không đạt, lỗi hardcode/AI/BR, checklist D-14/D-7. |
| `PLAN_Week13_Demo_0508.md` | **★★ Góp ý demo 05/08/2026** — bản gộp 2 note của nhóm, 43 đầu việc, đánh dấu ✅/🔶/⬜/❓ từng mục + mục "phải hỏi lại". |
| `PLAN_Week12.md` | **★ Kế hoạch sau demo thầy 29/07** — P0–P8 + trạng thái từng mục + việc làm thêm ngoài kế hoạch. |
| `ERD_v3_Project_Centric.dbml` | **★ ERD hiện hành (đã áp Phase A→L)** — dán vào dbdiagram.io (**62 bảng**: +project, cycle_track, project_round, council_project_assignment, contract_phase). **Sơ đồ DB DUY NHẤT** (các bản .dbml/.sql cũ đã xóa 15/07 — xem git nếu cần lịch sử). |
| `Review2_Diagrams.md` | Bộ diagram: Context · Architecture · **System Overview (§2b)** · State Machine · ERD rút gọn (§6) + **Logical ERD đầy đủ (§6b — có sản phẩm/giải ngân, Proposal→n Contract)** · Use Case · **Activity (§9)** + đánh giá DB/code (§7). |
| `Review2_Tech_Stack.md` | 4 danh sách Product/Tech (3rd-party · stack · DevOps · deploy). |
| `RP4_Diagrams.md` | **Diagram theo khung Report 4 (SDD)**: Package Diagram + Class Diagram + Sequence Diagram (3 feature lõi). Architecture/ERD trỏ về Review2_Diagrams. |
| `Review2_Context_Handoff.md` | Context để dán vào phiên AI khác lo SDD/report. |
| `Team_Contribution_Week1-8.md` | Bảng đóng góp thành viên W1–8 (roster thật + % + chi tiết tuần). ⚠️ có phụ lục nội bộ — xóa trước khi nộp. |

### Báo cáo & phân tích DB (tham khảo)
| File | Nội dung |
|---|---|
| `DB_ANALYTIC_REPORT.md` | Phân tích kiến trúc DB (10 domain · ~56 bảng · mô tả cột). Cho mục "Database". |
| `ERD_v3_Project_Centric.dbml` | Sơ đồ DB hiện hành (nguồn chuẩn duy nhất). *(Bản `DATABASE_DESIGN.md` ở repo FE cũ đuôi `v0` đã bỏ — mô tả schema v0 lỗi thời.)* |

### Hướng dẫn
| File | Nội dung |
|---|---|
| `DEMO_GUIDE.md` | Hướng dẫn demo. |
| `EXPORT_TEST_GUIDE.md` | Hướng dẫn test export tài liệu. |
