# KẾ HOẠCH BẢO VỆ LẦN 2 — bản thi hành

> **File này là danh sách việc hiện hành.** Nhóm ra hội đồng kín và bị cho **bảo vệ lần 2**.
> Ai (người hay AI) tiếp nhận giữa chừng thì đọc file này sau `HANDOFF_HIEN_HANH.md`.
>
> Lập 25/08/2026, dựa trên biên bản chính thức + khảo sát lại toàn bộ hai repo.
> Mọi câu "hiện trạng" bên dưới đều **đã mở code ra đọc để xác minh**, không lấy từ trí nhớ
> hay từ doc cũ — vì doc cũ trong repo này đã có lúc lệch thực tế.

---

## 1. Hội đồng yêu cầu gì

**Biên bản chính thức (email) — 4 điểm, đây là nguồn sự thật:**

1. Thể hiện rõ **ngân sách** tương ứng cho các đề tài
2. Thể hiện rõ các **mốc thời gian deadline** cho các giai đoạn của 1 đề tài, **lưu trữ lại các quyết định** liên quan đến đề tài
3. **Cân nhắc** bổ sung tính năng **AI kiểm tra trùng proposal**
4. **Chỉnh sửa lại toàn bộ document** theo góp ý — **đặc biệt report 3, 4 và 5**

**Note miệng ghi thêm** (không có trong biên bản): warning khi điểm thấp mà vẫn đạt · người chấm
phải có chuyên môn · deadline cho từng vòng chấm · nhóm chuyên môn duyệt giải ngân · use case +
system diagram sai · font/border/định dạng.

**Chủ dự án đã chốt phạm vi:**

| Quyết định | Nội dung |
|---|---|
| Thời gian | còn ~1 tháng, **làm hết nếu có thể** |
| Tài liệu `.docx` | **nhóm tự sửa**; AI chỉ giao danh sách lỗi kèm bằng chứng + render sơ đồ |
| AI trùng proposal | làm **hai tầng đầy đủ kèm bộ đo metric** |
| Góp ý miệng | **LẤY**: cảnh báo điểm lệch · chuyên môn người chấm · hạn từng vòng chấm |
| | **BỎ**: "nhóm chuyên môn duyệt giải ngân" (mâu thuẫn rule #16 có căn cứ QĐ543 — chuẩn bị câu trả lời thay vì đổi code) |

---

## 2. Hiện trạng đã xác minh (25/08)

Tin tốt: **phần lớn dữ liệu hội đồng đòi đã nằm sẵn trong DB, chỉ chưa ai đọc ra màn hình.**

| Phát hiện | Bằng chứng |
|---|---|
| `Contract.TotalAmount` BE **đã trả về** ở cả list lẫn detail, nhưng `src/types/contract.ts` **không khai báo** ⇒ `grep totalAmount` toàn FE = **0 kết quả** | `ContractDtos.cs` có `TotalAmount`; grep FE trống |
| `ContractDisbursement.percentage/plannedAmount/actualAmount` **có** trong `src/types/disbursement.ts:10-13` nhưng `DisbursementsPanel.tsx` **không render** | grep 3 tên trường trong panel = 0 |
| `AnalyticsController` trả `budgetDistribution` nhưng **không có key `totalBudget`** mà FE đang đọc ⇒ KPI "Tổng kinh phí" **luôn hiện 0** | object `result` ở `AnalyticsController.cs` (~133-147) |
| **`ReviewRound` không có cột hạn nào** — giai đoạn chấm hoàn toàn không có deadline | entity `ReviewRound` |
| 8 trường hạn khác có cột nhưng **chết hoặc chỉ-lưu**: `ReviewDeadline` · `MeetingDeadline` · `FinalReport.Deadline` · `SettlementDeadline` · `RevisionDeadline` · `OrderCollectionDeadline` · `CycleTrack.IsOpen` · cả bảng `ContractPhase` | |
| ⚠️ **Đính chính**: `ArchivalDeadline` **nay ĐÃ được ghi** (`FinalReportService.cs:116`) — nhưng **hardcode `UtcNow + 3 tháng`**, không qua `system_settings`. Cẩm nang chống trượt phạt đúng kiểu hardcode này ⇒ đưa vào `ARCHIVAL_LEAD_DAYS` | `FinalReportService.cs:116` |
| `FinalReport.Deadline` (QĐ543 **Điều 11.2.a** — nộp ≥30 ngày trước khi kết thúc đề tài) **vẫn chưa bao giờ được ghi**, chỉ được đọc ra DTO | `FinalReportService.cs:150` |
| Bảng `AuditLog` tồn tại nhưng **chỉ có đúng 1 chỗ ghi** | `ContractIdentityController.cs:109-120` |
| Bảng `semantic_search_vectors` tồn tại, **rỗng, không dòng code nào đọc/ghi**, không có cột vector | migration `20260814015923`, `FURPMSDbContext.cs:95` |
| **Không tồn tại ngưỡng điểm đạt** ở bất cứ đâu. `AverageScore` và `Result` gán ở hai dòng liền kề, **không một `if` nào so sánh**; comment trong code viết thẳng `// điểm/phiếu chỉ để tham khảo` | `ReviewScoringService.cs:298-299` |
| **Không có bảng nối người ↔ lĩnh vực.** `Entities/Users/` chỉ có `AcademicProfile · AcademicWork · OrganizationalUnit · Role · User · UserRole`. Chuyên môn chỉ là 2 ô text tự do, dùng để in Word | grep `UserResearchTrack` = 0 |
| Tài liệu RP1/RP3/RP7 **đã hứa** `text-embedding-004`, `IndexEmbedding`, similarity threshold — code 0 dòng ⇒ làm tính năng AI là **trả nợ tài liệu**, không phải thêm scope | các file `docs/report/*.docx` |

**Khoá `system_settings` hiện có** (để không đặt trùng): `UPLOAD_MAX_FILE_SIZE_MB` · `UPLOAD_ALLOWED_EXTENSIONS` ·
`COUNCIL_INVITE_DEADLINE_DAYS` · `COUNCIL_ALLOW_RESPOND_ON_BEHALF` · `SCORE_DECIMAL_PLACES` ·
`DEADLINE_REMINDER_DAYS` · `EMAIL_ENABLED` · `CONTRACT_SIDE_A_REPRESENTATIVE` · `DEMO_DATA_ENABLED`.

### 2b. Đã làm gì trong 24 commit từ 18→25/08 — ĐỪNG LÀM LẠI

12 commit BE + 12 commit FE (~3.300 dòng thêm ở BE, ~1.800 ở FE, gồm **~1.000 dòng test mới**):

| Đã có rồi | Ghi chú |
|---|---|
| **Vòng đời hợp đồng đủ trạng thái**: thêm `ContractStatus.SETTLED` + `TERMINATED` | `DomainStatus.cs:99-108`. Comment ghi rõ *"Nghiệm thu đề tài Đạt chỉ làm Project = COMPLETED, chưa tự coi hợp đồng đã thanh lý"* — chỉ ký BM13 mới thành `SETTLED` |
| **`ContractListResponse.ProjectStatus`** + FE `contract.projectStatus` | ⇒ câu hỏi cũ *"đề tài nghiệm thu Đạt thì phân loại ở đâu"* **đã giải quyết được** ở màn Hợp đồng; `ContractMilestoneTimeline.tsx:79-87` đã dùng |
| Luồng phụ lục + chấm dứt hợp đồng, siết hoàn tất đề tài | `ContractService.cs` +100 dòng, `AmendmentService`, `ContractClosureTests.cs` |
| **Mã lỗi ổn định kèm mọi phản hồi** | commit `6440857` |
| Trích xuất đề cương bằng Gemini viết lại | `ProposalExtractionService.cs` +142, `GeminiService.cs`, `GeminiFileInput.cs` |
| Cảnh báo trùng lịch họp, siết luồng chấm | `CouncilMeetingConflictTests.cs`, `ReviewShared.cs` (mới, 89 dòng) |
| Lịch báo cáo tiến độ + báo cáo tổng kết | `ProgressReportScheduleTests.cs`, `FinalReportServiceTests.cs` |
| 3 doc mới | `DEMO_SCRIPT_SUBMISSION.md` · `SOFTWARE_PACKAGE_INSTALLATION_GUIDE.md` · `USE_CASE_STATUS_TRACEABILITY.md` |

> ⚠️ **Nhưng `totalAmount` vẫn CHƯA có trong `src/types/contract.ts`** — Codex thêm `projectStatus`
> mà bỏ sót `totalAmount`, dù BE trả cả hai ở cùng một DTO. Đây đúng là cái bẫy §3.2 của
> `AGENTS.md` repo FE cảnh báo: *"kiểu TypeScript của DTO là chép tay, đổi DTO bên BE thì FE không
> hề báo lỗi — màn hình chỉ lặng lẽ hiện `-` hoặc rỗng."* Việc số 3 của Sprint 0 vẫn còn nguyên giá trị.

---

## 3. Hai tiền đề — làm TRƯỚC khi code

### T1. Sửa rule #15 trong `CLAUDE.md`

Rule #15 hiện ghi *"Tài chính = minh chứng, hệ thống KHÔNG quản tiền… scope thực thi = **strip + ẩn**
(ngừng tính tiền, **ẩn nav/UI tiền**)"*. Chính câu này dẫn tới việc `totalAmount` bị giấu —
**đúng chỗ hội đồng bắt**. Không lật ngược rule, chỉ tách hai khái niệm bị gộp nhầm:

> **#15 (sửa sau biên bản bảo vệ lần 2).** Hệ thống quản **HỒ SƠ KINH PHÍ**, không quản **DÒNG TIỀN**.
> - **CÓ**: dự toán được duyệt (Điều 15), trần theo loại đề tài, giá trị hợp đồng, lịch giải ngân
>   theo %/mốc, trạng thái từng đợt, quyết toán — **hiển thị đầy đủ**, vì đây là hồ sơ hành chính.
> - **KHÔNG**: không thanh toán, không nối ngân hàng, không thay sổ kế toán. `ActualAmount` là
>   **ghi nhận lại** con số Phòng Tài chính báo kèm minh chứng.

Rule #16 (báo cáo tiến độ = Staff duyệt, không hội đồng) **giữ nguyên** — có căn cứ QĐ543
(Điều 18.1 chỉ cấp thù lao 2 hội đồng; BM06 gửi Phòng QLKH).

### T2. Bug phải sửa ngay — nó vi phạm chính rule #19 của nhóm

Rule #19 ghi *"deadline hiệu lực = bản gia hạn mới nhất"*. Nhưng `ProposalService.cs:567` (nộp) và
`:407` (sửa) so với `cycle.SubmissionDeadline` **thô**; `CycleService.GetEffectiveDeadlineAsync` là
`private`, `ProposalService` không hề đọc bảng `DeadlineExtension`.

⇒ Admin gia hạn đợt → hệ thống **gửi thông báo "đã gia hạn"** cho PI → PI bấm nộp → **vẫn bị chặn
theo hạn cũ**. Hội đồng rất dễ bắt khi demo. Sửa ~30 phút.

---

## 4. Sprint 0 — ✅ XONG 25/08 (0 migration)

1. ✅ Sửa rule #15 trong `CLAUDE.md` (T1)
2. ✅ Fix bug gia hạn (T2) — tách `IDeadlineResolver` / `DeadlineResolver` làm **nguồn sự thật duy nhất** cho "hạn hiệu lực"; `ProposalService` (nộp + sửa) và `CycleService` cùng gọi nó. 5 test hồi quy ở `FURPMS.Tests/Deadlines/EffectiveDeadlineTests.cs`, phủ cả ca **gia hạn rồi rút ngắn lại** (lấy bản MỚI NHẤT, không phải bản xa nhất)
3. ✅ `totalAmount` vào `src/types/contract.ts` + cột **"Giá trị hợp đồng"** ở danh sách + khối giá trị ở header chi tiết
4. ✅ `DisbursementsPanel` hiện **tỷ lệ %** và **số tiền** từng đợt (kế hoạch vs thực chi)
5. ✅ `AnalyticsController` trả `totalBudget` + `totalContracted` + `totalDisbursed` — KPI hết 0

**Phát sinh ngoài kế hoạch (đã làm):** dòng ghi chú ở tab Giải ngân vẫn viết *"hệ thống không quản
lý tiền"* trong khi ngay dưới nó giờ là số tiền — **tự mâu thuẫn trên màn hình**. Đã đổi
`contract.disbursement.evidenceNote` theo rule #15 mới.

---

## 4b. Nhóm 1 — Ngân sách (gạch 1) — ✅ XONG 25/08 (0 migration)

**BE:** `IProjectBudgetOverviewService` / `ProjectBudgetOverviewService` +
`GET /api/projects/{projectId:guid}/budget` (xem `API_CONTRACT.md` §13.2b). Quyền: Admin · Staff ·
PI · **ủy viên hội đồng được gán** (Điều 13.1.e). 7 test ở `FURPMS.Tests/Budget/ProjectBudgetOverviewTests.cs`.

**FE:** `BudgetOverviewPanel.tsx` → tab **"Ngân sách"** trong `ContractDetailSheet` — 4 thẻ KPI
(dự toán duyệt · giá trị HĐ · đã chi · **còn lại**) + thanh tiến độ + **cơ cấu 06 hạng mục Điều 15**
+ danh sách đợt + khối quyết toán BM13.

**Hai quyết định thiết kế đáng nhớ:**
- **Không nói dối về số liệu.** Đợt đã đánh dấu chi mà `actualAmount` trống ⇒ tổng là **tạm tính
  theo kế hoạch**, và màn hình nói thẳng ra (`hasUnreportedActuals`). Im lặng thì người đọc tưởng
  đó là số quyết toán.
- **Tiến độ chi tính trên GIÁ TRỊ HỢP ĐỒNG**, không trên dự toán: dự toán là số *xin*, hợp đồng mới
  là số *cam kết chi*.

**Phát sinh ngoài kế hoạch (đã làm):**
- Bộ lọc **giai đoạn** ở danh sách Hợp đồng: *Đang thực hiện / Đã nghiệm thu Đạt / Đã thanh lý* —
  dùng `projectStatus` + `ContractStatus.SETTLED` mà Codex đã thêm. Đây là thứ Dũng (FE) đề nghị.
- Rút `ProgressBar` thành component dùng chung ở `components/shared/ProgressBar.tsx` (khuôn vốn
  đang lặp trong `RubricScoringForm.tsx:189-194`), có kẹp giá trị về 0–100 vì dữ liệu thật đã có ca
  chi vượt kế hoạch.
- `projectId` cũng thiếu trong `src/types/contract.ts` y như `totalAmount` — **cùng một cái bẫy
  §3.2**, đã bổ sung.

> **Kiểm chứng đã chạy thật:** đề tài HĐ-001 → dự toán 145tr = đã ký 145tr, 4 đợt đều đã chi ⇒ còn
> lại 0, cờ tạm tính bật đúng; cơ cấu 6 hạng mục cộng ra 100%. Lọc "Đã nghiệm thu Đạt": 5 hợp đồng
> → còn 1. KPI Thống kê: 1.675.000.000 ₫ (trước là 0).

---

## 5. Nhóm 1 — bản thiết kế gốc *(đã thực hiện — giữ lại làm tham chiếu, kết quả xem §4b)*

> Tên thực tế khi code: `IProjectBudgetOverviewService` (không phải `IProjectBudgetService` — tên đó
> đã có sẵn cho dự toán cấp đề cương, dễ nhầm).

`GetOverviewAsync(projectId, userId, roles)` gom từ:

| Số liệu | Nguồn |
|---|---|
| Trần kinh phí | `ResearchType.MaxBudgetCap` |
| Dự toán duyệt + 6 mục Điều 15 | `ProposalBudget` (bản `IsCurrent`) |
| Chi tiết mục chi + 4 nguồn | `ProposalBudgetItem[]` |
| Giá trị hợp đồng | `Contract.TotalAmount` |
| Kế hoạch từng đợt | `ContractDisbursement.Percentage/PlannedAmount` |
| Đã chi | `ActualAmount ?? PlannedAmount` where `Status = DISBURSED` |
| **Còn lại** | `ContractedTotal − MarkedDisbursedTotal` |
| Đợt kế tiếp + điều kiện mở khoá | đợt `PENDING` nhỏ nhất + `ConditionDescription` |
| Quyết toán | `ContractSettlement` (3 tổng) |

Kiểm quyền chép mẫu `ContractsController.AuthorizeContractAsync` (~191). Tái dùng
`IContractRepository`, `IProposalRepository`, `IBudgetPolicyService`.

**Endpoint**: `GET /api/projects/{projectId:guid}/budget` ·
`GET /api/contracts/{id:guid}/settlement-suggestion` (prefill form quyết toán, Staff vẫn sửa được) ·
sửa `GET /api/analytics/overview`.

**FE** — `BudgetOverviewPanel.tsx`. Tái dùng nguyên `KpiCard.tsx` (đã có `format: "currency"`),
`ProgressBar.tsx`, `charts/PieChartCard.tsx`, `StatusBadge`, `EmptyState`. Nhúng vào
`ContractDetailSheet.tsx` (tab `budget`) và `pi/progress-track/MyProjectTimelinePage.tsx`.
Chân panel ghi câu giữ đúng rule #15 đã sửa: *"Hệ thống hiển thị kế hoạch kinh phí và mốc; việc chi
trả do Phòng Tài chính thực hiện."*

**KHÔNG làm**: kích hoạt `ContractPhase` (bảng chết) — biên bản không đòi.

**Đã có sẵn, chỉ cần dùng**: `ContractListResponse.ProjectStatus` + `ContractStatus.SETTLED/TERMINATED`
(Codex thêm 18→25/08) ⇒ thêm **ô lọc "Đang thực hiện / Đã hoàn thành / Đã thanh lý"** ở màn Hợp đồng
là xong, không cần đụng BE. Đây là thứ Dũng (FE) từng đề nghị *"đề tài nghiệm thu Đạt phải có chỗ
phân loại"*.

**Test** `FURPMS.Tests/Budget/ProjectBudgetOverviewTests.cs` — tổng hợp đúng 4 con số · chưa có HĐ
không chia 0 · vượt trần đánh dấu `CapExceeded` · người ngoài ném `ForbiddenException`.

---

## 6. Nhóm 2 — Deadline (nửa đầu gạch 2 + "vòng 1 vòng 2 phải có hạn") · **1 cột nullable**

**Mô hình**: `ProjectTimeline` là **read-model**, KHÔNG phải bảng mới. Một service lắp "12 giai đoạn
+ hạn từng giai đoạn" từ cột có sẵn; **scanner nhắc hạn dùng lại chính nó** ⇒ màn hình và email
không bao giờ lệch nhau.

Mỗi giai đoạn mang 2 trường **dành riêng để trả lời hội đồng**:
- `DeadlineSource`: `CYCLE | EXTENSION | CONTRACT | RULE_QD543 | DERIVED | NOT_SET`
- `DeadlineBasis`: câu tiếng Việt, ví dụ *"QĐ543 Điều 11.2.a — 30 ngày trước ngày kết thúc đề tài"*

Hỏi *"hạn này ở đâu ra?"* thì màn hình tự trả lời.

| # | Giai đoạn | Hạn lấy từ | Việc |
|---|---|---|---|
| 1 | Nộp đề cương | `SubmissionDeadline` + `DeadlineExtension` | fix bug T2 |
| 2 | Sửa theo yêu cầu | `Proposal.RevisionDeadline` | cột có, chưa ai ghi → `RevisionRequestedAt + REVISION_DEADLINE_DAYS` |
| **3** | **Chấm vòng 1 / vòng 2** | **thiếu hẳn** | **thêm `review_rounds.scoring_deadline date NULL`**, mặc định `OpenedAt + SCORING_WINDOW_DAYS` |
| 4 | Họp HĐ xét duyệt | `ReviewCouncil.MeetingDeadline` | cột có, chỉ lưu → đọc + mặc định `EstablishedAt + 15 ngày làm việc` (Điều 8.3.a) |
| 5 | Ký hợp đồng | suy ra `CouncilDecision.FinalizedAt + CONTRACT_SIGN_WINDOW_DAYS` | không thêm cột |
| 6 | Báo cáo tiến độ | `ProgressReport.DueDate` | đã có + đã nhắc |
| 7 | Nộp sản phẩm | `ProjectDeliverable.DueDate` | đã có + đã nhắc |
| 8 | Giải ngân đợt n | không có hạn (điều kiện-driven) | hiện `NO_DEADLINE` — **trung thực, không bịa hạn** |
| 9 | Nộp báo cáo nghiệm thu | `Contract.EndDate − FINAL_REPORT_LEAD_DAYS` | **QĐ543 Điều 11.2.a** — cột có, **chưa bao giờ ghi** |
| 10 | Họp nghiệm thu | `MeetingDeadline` (round ACCEPTANCE) | đọc + cảnh báo quá hạn |
| 11 | Quyết toán | `ContractSettlement.SettlementDeadline` | cột có → đọc |
| 12 | Lưu trữ hồ sơ | `FinalSubmittedAt + ARCHIVAL_LEAD_DAYS` | **đang hardcode +3 tháng** ở `FinalReportService.cs:116` → chuyển sang setting |

**Hạn vòng chấm — làm cho ra tấm ra món** (chủ dự án nhấn mục này): Staff đặt/dời hạn khi mở vòng ·
hiện `DeadlineBadge` ở `RoundTimeline` và `ReviewBoardPage` · dời hạn đã có thì **ghi
`DeadlineExtension`** (giữ rule #19, không ghi đè) · scanner nhắc **Staff + ủy viên chưa nộp phiếu** ·
quá hạn thì gắn cờ trên vòng, **không tự đóng** (giữ rule #12).

**Không hardcode** — 6 khoá mới vào `SystemSettingKeys` + `DatabaseSeeder.SeedSystemSettingsAsync` +
`SystemSettingService.Validate`: `SCORING_WINDOW_DAYS`(15) · `REVISION_DEADLINE_DAYS`(15) ·
`FINAL_REPORT_LEAD_DAYS`(30) · `CONTRACT_SIGN_WINDOW_DAYS`(30) · `ARCHIVAL_LEAD_DAYS`(90) ·
`MEETING_DEADLINE_WORKING_DAYS`(15). Sửa được ngay ở màn Admin ⇒ trả lời thẳng câu *"đổi 30 thành
45, demo ngay đi"*.

**Tách `IDeadlineResolver`** (chuyển logic từ `CycleService.GetEffectiveDeadlineAsync` đang
`private`) để `ProposalService` dùng mà không phụ thuộc ngược vào `CycleService` (tránh vòng DI).

**Scanner**: chép nguyên khuôn `DeadlineReminderScanner.ScanProgressReportsAsync` (dòng 121-183)
cho 4 loại mới — final report · **review round** · settlement · hạn nộp đề cương (dùng hạn **hiệu
lực**). Mở rộng người nhận khỏi mỗi PI bằng `INotifier.NotifyRoleAsync` đã có.

**FE** — nguyên thuỷ quan trọng nhất là **`components/shared/DeadlineBadge.tsx`**: rút logic đếm
ngược đang kẹt trong `OpenCyclesCard.tsx:30-34,66-71` ra dùng chung ("còn N ngày" / "quá hạn N ngày"
đỏ / "chưa đặt hạn" xám, `title` = `DeadlineBasis`). Thả vào `ProgressReportsPanel` ·
`DeliverablesPanel` · `SettlementPanel` · `FinalReportPanel` · `MyMeetingsPage` · `RoundTimeline`
⇒ hết bệnh *"quá hạn nhìn giống chưa tới hạn"*.

Thêm `ProjectTimelinePanel.tsx` (chép markup trục dọc của `ContractMilestoneTimeline.tsx`, dữ liệu
do BE lắp) · `UpcomingDeadlinesCard.tsx` cho dashboard PI/Staff · `SetRoundDeadlineDialog.tsx`.

**Test**: `ProposalSubmissionRespectsExtensionTests` (hồi quy T2) · `ProjectTimelineServiceTests`
(đủ 12 giai đoạn, đúng `DeadlineSource`) · `FinalReportDeadlineFromQd543Tests` (đổi setting → hạn
đổi theo = chứng minh không hardcode) · `ReviewRoundDeadlineTests` (đặt hạn lùi quá khứ → 400; dời
tới → sinh `DeadlineExtension`) · 2 scanner test chép khuôn.

---

## 7. Nhóm 3 — Lưu trữ quyết định (nửa sau gạch 2) · **1 bảng mới**

**Không dùng `AuditLog`** cho việc này: nó có `OldValues`/`NewValues`/`IpAddress`/`UserAgent` —
nhật ký **kỹ thuật** ai-sửa-gì; thiếu `ProjectId`, thiếu loại quyết định, thiếu số văn bản. Lấy
"mọi quyết định của đề tài X" phải so chuỗi qua 8 loại thực thể.

**Bảng mới `project_decisions` là sổ đăng ký MỎNG trỏ ngược bản gốc** — không chép lại nội dung đã
nằm ở `CouncilDecision`/`ProjectRound`/…:

```
ProjectId          ← NEO VÀO ĐỀ TÀI, đây là điểm khác AuditLog
DecisionType · Result · Summary (1 câu tiếng Việt) · Reason · DocumentNo (BM04/BM12)
SourceEntityType + SourceEntityId   ← để mở đúng bản gốc
DecidedBy · DecidedByRole (chức danh LÚC chốt) · DecidedAt · CreatedAt
```

`Id` kiểu `long` IDENTITY (quy ước `CLAUDE.md` cho bảng log). FK `decided_by` **nullable +
`OnDelete(NoAction)`**.

**File đính kèm**: tái dùng `Document` với `EntityType = "ProjectDecision"` — không thêm bảng file,
`DocumentViewer.tsx` đã có sẵn.

**`IDecisionLogger`** gắn ~13 điểm: nộp/rút/sửa đề cương · chốt `CouncilDecision` · `ProjectRound`
PASSED/FAILED · duyệt change request · tạo/ký/chấm dứt HĐ · duyệt phụ lục · đánh giá tiến độ ·
nghiệm thu sản phẩm · xác nhận giải ngân · duyệt báo cáo tổng kết · ký BM13 · gia hạn đợt (một dòng
cho **mỗi** project trong đợt) · **kết luận rà trùng lặp** (nối nhóm 6) · **lý do kết luận lệch
điểm** (nối nhóm 4).

Rule #12 giữ nguyên: `Result` luôn **chép lại** kết luận Chủ tịch, hệ thống không tự chốt.

> ⚠️ **Điểm sắc nhất cả kế hoạch: BACKFILL.** DB Railway đang có dữ liệu thật; không backfill thì
> hôm bảo vệ mở "hồ sơ quyết định" ra **trống trơn**. Viết `POST /api/admin/backfill-decisions`
> (Admin, **idempotent**, dedupe theo `SourceEntityType + SourceEntityId + DecisionType`) đọc
> `CouncilDecision`, `ProjectRound`, `Contract`, `AmendmentRequest`, `ProgressReport`,
> `ProjectDeliverable`, `FinalReport`, `ContractSettlement`, `ProposalChangeRequest`.
> **Chạy thử trên bản sao dữ liệu prod trước.**

**FE**: `DecisionDossierPanel.tsx` — danh sách dọc nhóm theo giai đoạn, mỗi dòng =
`StatusBadge(Result)` + summary + ai quyết + ngày + số văn bản + link "Xem bản gốc" + file đính kèm.
Nhúng vào `ContractDetailSheet` (tab `decisions`), `MyProjectTimelinePage`,
`staff/proposal-reviews/ProposalReviewWorkspace`. **Không tạo page riêng.**

---

## 8. Nhóm 4 — Cảnh báo kết luận lệch điểm · **0 migration** · rẻ, demo rất "ăn"

Hiện `AverageScore` và `Result` độc lập tuyệt đối — `ReviewScoringService.cs:298-299` gán chúng ở
hai dòng liền kề mà không một `if` nào so sánh. Đề tài trung bình 35/100, Thư ký chọn "Đạt", Chủ
tịch duyệt → hệ thống ghi PASSED, đề tài chuyển COMPLETED, **mở khoá giải ngân đợt cuối, không một
tiếng cảnh báo**.

**Giữ nguyên rule #12** (hệ thống không tự chốt) — chỉ **cảnh báo + bắt ghi lý do**:

- Thêm `REVIEW_PASS_THRESHOLD` (mặc định 50/100) vào `system_settings` — **không hardcode**, Admin
  sửa được, và có câu trả lời cho *"con số này ở đâu ra"*.
- `SaveMinutesAsync`: nếu `Result = Approved` mà `AverageScore < ngưỡng` **hoặc** `Result = Rejected`
  mà `AverageScore ≥ ngưỡng` ⇒ đòi `ResultJustification` không rỗng, thiếu thì 400 kèm câu giải
  thích. Đối xứng hai chiều.
- Lý do lưu vào biên bản **và** sinh một dòng `ProjectDecision`
  (`DecisionType = SCORE_DIVERGENCE_JUSTIFIED`) ⇒ nối thẳng vào nhóm 3.
- FE `MinutesPanel.tsx`: dropdown kết luận (~401-412) đổi màu + hiện khối cảnh báo khi lệch, mở ô lý
  do bắt buộc; nút Lưu (~558) `disabled` cho tới khi điền.

**Test**: đạt-nhưng-điểm-thấp không kèm lý do → 400 · kèm lý do → 200 + sinh `ProjectDecision` · đổi
`REVIEW_PASS_THRESHOLD` → ngưỡng cảnh báo đổi theo · vòng NGHIỆM THU (`AverageScore = null`, chỉ
PASS/FAIL) **không bị đụng**.

---

## 9. Nhóm 5 — Chuyên môn người chấm · **1 bảng nối**

QĐ543 **Điều 8.2** đòi hội đồng gồm *"nhà khoa học, giảng viên **có chuyên môn trong lĩnh vực**"*.
Hiện: không có bảng nối; BE gán ủy viên chỉ kiểm COI + trùng tên + số lượng; lọc duy nhất ở FE là
theo **vai trò hệ thống** (`council-eligibility.ts:14-16`, chính comment trong file thừa nhận
*"đây KHÔNG phải hàng rào an ninh"*).

- **Bảng mới `user_research_tracks`** (`UserId` + `TrackId` + `Note`), Admin/Staff quản lý ở màn
  Người dùng; seeder gán sẵn cho `reviewer1..5.demo` để demo chạy được ngay.
- **Endpoint `GET /api/councils/candidates?trackId=&projectId=`** — trả ứng viên **đã xếp hạng**:
  khớp lĩnh vực lên đầu · gắn cờ COI · gắn cờ "chưa khai chuyên môn". Đây chính là
  `/ai/suggest-reviewers` mà `API_CONTRACT.md:1046` ghi *"nên làm bằng truy vấn thuần"* — **làm bằng
  SQL, KHÔNG gọi nó là AI** (cẩm nang phạt *"gọi là AI nhưng thực chất if-else"*).
- **BE chặn có kiểm soát**: gán người ngoài lĩnh vực → 400, trừ khi gửi kèm
  `acceptWithoutExpertise: true` + lý do ⇒ lý do ghi vào `ProjectDecision`.
- **FE**: `review-board/CreateCouncilSheet.tsx` + `proposal-reviews/AddCouncilMemberDialog.tsx` gọi
  endpoint mới; badge "Đúng lĩnh vực" / "Khác lĩnh vực"; xoá nút "Gợi ý AI" đang gọi vào 404
  (`AddCouncilMemberDialog.tsx:69-81`). Tiện thể `AddCouncilMemberDialog.tsx:44` hiện **không truyền
  `piUserIds`** nên FE không lọc COI ở đường này (BE vẫn chặn) — sửa cho hai đường giống nhau.

---

## 10. Nhóm 6 — AI kiểm tra trùng proposal (gạch 3) · **3 cột nullable trên bảng đang rỗng**

**Hai tầng — "lọc bằng embedding, giải thích bằng Gemini"**:

- **Tầng 1** (rẻ, luôn chạy): `text-embedding-004` → cosine toàn kho → top-K (K=5). **Tất định**
  ⇒ đo metric được.
- **Tầng 2** (đắt, chỉ khi vượt ngưỡng hoặc Staff bấm): một lần `GenerateTextAsync` so với top-1..3,
  trả kết luận đọc được + các điểm trùng. Cache `llm_outputs` với `OutputType = "DUPLICATE_CHECK"` +
  `IsActive` versioning ⇒ **xem lại lần hai không tốn quota**.

**Đã loại pgvector**: ảnh `postgres:16` chuẩn không kèm extension; `CREATE EXTENSION vector` fail thì
`Migrate()` lúc khởi động **làm app không lên nổi trên production đang có dữ liệu thật**. Vài trăm
bản ghi ⇒ lợi ích bằng 0.

**Đã loại "chỉ lọc SQL rồi cho Gemini so"**: lọc từ khoá đứng một mình **chính là "gọi là AI nhưng
thực chất if-else"** mà cẩm nang phạt; điểm không tất định nên không bảo vệ được metric.

**DB**: `semantic_search_vectors` (**đang rỗng**) thêm 3 cột nullable `embedding text` (JSON
float[]) · `dimensions int` · `model_used text`. `ContentHash` **đã có sẵn** → nội dung không đổi
thì không embed lại — **đây chính là cơ chế khống chế quota**. 1000 đề cương ≈ 12 MB.

**Tái dùng lớn nhất**: chép gần như nguyên văn `AiSummaryQueue` + `AiSummaryPregenerationService`
cho hàng đợi embedding — bounded channel 200 `DropOldest`, `Task.Yield()` (chống treo host lúc khởi
động), try/catch chống `StopHost`, nghỉ 5s giữa 2 lần gọi, sweep 50 lúc khởi động.
**Mọi cái bẫy khó đã được giải và ghi lại trong comment ở đó — đọc trước khi viết.**

`GeminiService`: tách vòng HTTP + retry + fallback ra `SendAsync` dùng chung cho cả `generateContent`
lẫn `embedContent` — **không viết lại vòng retry**, nó đang giữ lời giải cho lỗi 429/503 thật đã gặp.

### Metric — phần ăn điểm nhất

Cẩm nang chống trượt (§5) phạt: *"không có metric đánh giá"*, *"không giải thích được ngưỡng tin cậy
từ đâu ra"*, *"không có actor duyệt lại kết quả AI"*, *"không quản lý token/chi phí"*.

- Bộ gán nhãn ~35 cặp, 2 thành viên gán độc lập, lưu trong repo
  `FURPMS.Tests/Ai/duplicate-eval-set.json`: ~10 cặp trùng thật (paraphrase) · **~10 cặp cùng lĩnh
  vực nhưng khác đề tài (hard negative — chỗ dễ sai nhất)** · ~15 cặp khác hẳn.
- Quét ngưỡng τ ∈ {0.60 … 0.90}, báo cáo Precision/Recall/F1 cả bảng, chọn τ tại F1 cao nhất,
  **thiên về recall** (bỏ sót một đề tài trùng đắt hơn một cảnh báo thừa).
- Ngưỡng vào `system_settings`: `AI_DUPLICATE_THRESHOLD`(0.78) · `AI_DUPLICATE_TOP_K`(5) ·
  `AI_DUPLICATE_BLOCK_THRESHOLD`(0.92).
- `DuplicateThresholdEvaluationTests` đọc JSON có **vector tính sẵn nhúng trong file** ⇒ test không
  gọi Gemini, luôn xanh offline/CI, assert F1 ≥ mục tiêu. **Metric nằm trong bộ test, không phải
  slide suông.**
- **Ai kiểm chứng đầu ra**: Staff bấm "Đã xem xét" (`NOT_DUPLICATE / NEEDS_REVISION / DUPLICATE` +
  lý do) → ghi vào `ProjectDecision`. **Nhóm 3 chính là bằng chứng có người trong vòng lặp.**
- Ghi `LlmOutput.TokensInput/TokensOutput/LatencyMs` từ `usageMetadata` — **3 cột đã có sẵn mà chưa
  ai ghi**.

**Dọn code chết**: xoá `ai.service.ts checkSimilarity` + `useSimilarityCheckMutation` (gọi vào 404);
viết lại `SimilarityWarningDialog.tsx` → `DuplicateWarningDialog.tsx` **và thực sự import nó** (hiện
không ai import); `PiDashboardPage.tsx:29` quick-action đang link tới **trang chết** — hồi sinh
`/ai/search` (dùng chung vector, gần như miễn phí).

> ⚠️ **Phân biệt hai chiều ngược nhau** — không nói rõ thì hội đồng tưởng nhóm mô tả sai chính sản
> phẩm mình:
>
> | | FE-08 (tài liệu đang mô tả) | Tính năng mới (hội đồng đòi) |
> |---|---|---|
> | So gì | đề cương ↔ **đơn đặt hàng** | đề cương ↔ **kho đề tài đã có** |
> | Cảnh báo khi | điểm **THẤP** | điểm **CAO** |
>
> RP1 còn ghi *"will not support plagiarism detection"* — phải sửa câu đó.

---

## 11. Nhóm 7 — Tài liệu (gạch 4) · **nhóm tự làm `.docx`, AI giao đạn**

Cẩm nang ghi thẳng: *"Tài liệu là phần dễ ăn điểm nhất và bị mất điểm nhiều nhất… công việc không
cần sáng tạo, chỉ cần cẩn thận."* Và *"không tiếp thu góp ý các lần Review"* là lý do trực tiếp dẫn
tới không đạt.

Số chữ hiện tại: RP1 2.165 · RP2 2.306 · **RP3 5.901** · **RP4 2.267** · **RP5 1.196** · RP6 3.647 ·
RP7 17.066. **Ba file hội đồng gọi tên đúng là ba file mỏng nhất/sai nhất.**

**AI giao** — `docs/RA_SOAT_BAO_VE_LAN2.md` (chưa viết), mỗi dòng ghi *file · mục · chỗ sai · sửa
thành gì · bằng chứng trong code*:

- **RP4 (SDD) — nguy hiểm nhất.** System Architecture sai 6 chỗ (nhóm đã tự soát ở
  `RA_SOAT_BAO_CAO_RP1-7.md:152-175` từ 16/08 **nhưng chưa sửa**): vẽ **SQL Server** (thật:
  PostgreSQL) · **Google Meet** + **Google Calendar** (0 file, không tích hợp) · **Google SMTP**
  (thật: Brevo) · **thiếu Cloudinary** (7 file dùng) · **React Native + Expo mobile** (không có mã
  nguồn nào). Và nó đang là *bộ sưu tập logo* nối vào một hộp giữa, không thể hiện ai gọi ai.
  Thêm: Table 1 liệt kê **7 package mobile**, Figure 2 caption ghi *"frontend, backend and mobile
  client"* — câu *"cho xem app mobile"* gần như chắc chắn sẽ được hỏi.
- **RP3 (SRS).** Mô tả `text-embedding-004` / `IndexEmbedding` / similarity threshold — **sẽ khớp**
  vì nhóm 6 làm thật, nhưng phải viết lại cho đúng chiều so sánh. Use Case Diagram hiện là mermaid
  `flowchart` (mermaid **không có ký hiệu use case chuẩn**; `«include»/«extend»` chỉ là nhãn text) —
  **nhóm đã có sẵn bản PlantUML** ở `Review2_Diagrams.md:634-714`, dùng bản đó. **Gộp 4 bản ERD về
  1** — `erd-conceptual.mmd` đặt tên bảng khác hẳn DB thật (`RESEARCH_FIELD` vs `cycle_track`,
  `EVALUATION` vs `review_score`).
- **RP5 (Test).** **Ba con số cho cùng một thứ**: bài ghi 99 test, Excel ghi 39, repo thật
  `dotnet test` = **204+** (cập nhật lại con số cuối khi xong các nhóm trên). Bỏ 4 đoạn nội dung
  thật đang mang **màu placeholder** `#0000ff` (có cả tiêu đề "2. Test Strategy").
  `Report3_Project Tracking.xlsx` và `Project Weekly Report_GroupName.xlsx` vẫn là **file mẫu trắng
  ngày 2021**.
- **4 sơ đồ còn thiếu trong bài nộp**: Context · State Machine · Activity (UML chuẩn) · Use Case
  PlantUML. **Nguồn đã có sẵn** trong `Review2_Diagrams.md` §1, §3–5b, §8b, §9 — AI render ra ảnh,
  nhóm chèn vào. Cẩm nang: *"thiếu hẳn một sơ đồ bắt buộc → mất trắng điểm mục đó"*.
- **Checklist định dạng** đúng thứ hội đồng chê: RP2 trộn **3 họ font** (Tahoma ×65 · Times New
  Roman ×18 · Courier New ×10), RP7 trộn **4 họ**, RP4+RP7 **không khai báo `docDefaults` font** ·
  mỗi báo cáo 6–8 cỡ chữ · caption **hình DƯỚI, bảng TRÊN**, dùng Insert Caption + Cross-reference
  rồi **Ctrl+A → F9** · thêm LoF/LoT tách riêng, số trang, bảng từ viết tắt · xoá placeholder còn
  sót (RP2: `[Title]` `[DDMMYYYY]` `[feature-name]` `[issue]`; RP7: 3 chỗ `<…>` gồm
  `<Proposal Submission & AI Similarity Validation>`) · xoá file rác `~$_543_...docx`.

**AI tự sửa** (docs nội bộ trong repo): `API_CONTRACT.md:1045` · `USE_CASE_STATUS_TRACEABILITY.md`
(UC-24) · `BACKLOG_Uu_tien.md:79` + `PLAN_Week12.md` + `00_INDEX.md:109` (**đang khuyến nghị BỎ
semantic search — phải đảo ngược kèm ghi chú có ngày**) · `HANDOFF_HIEN_HANH.md` · `BUSINESS_RULES.md`
(thêm luật ngưỡng điểm + chuyên môn hội đồng) · **`docs/AI_Duplicate_Detection.md`** (vấn đề · dữ
liệu · model · bảng thí nghiệm ngưỡng · metric · người trong vòng lặp · quota) — **đây mới là thứ
hội đồng thực sự chấm** cho phần AI.

---

## 12. Thứ tự & rủi ro migration

| Sprint | Nội dung | Migration | Rủi ro | Trạng thái |
|---|---|---|---|---|
| 0 | 5 việc nửa ngày | không | **0** | ✅ **XONG 25/08** |
| 1 | Ngân sách | không | **0** | ✅ **XONG 25/08** |
| 2 | Deadline (gồm hạn vòng chấm) | +1 cột nullable | thấp — bản ghi cũ NULL → "chưa đặt hạn" | ⬜ **đang tới** |
| 3 | Quyết định | +1 bảng | schema thấp; **rủi ro thật ở backfill** | ⬜ |
| 4 | Cảnh báo điểm lệch | không | **0** — làm xen kẽ, rất rẻ | ⬜ |
| 5 | Chuyên môn người chấm | +1 bảng nối | thấp | ⬜ |
| 6 | AI trùng + bộ đo metric | +3 cột nullable trên bảng **rỗng** | thấp nhất trong hạng mục mới | ⬜ |
| 7 | Tài liệu | — | chạy **song song** từ đầu, không phụ thuộc code | ⬜ |

Không hạng mục nào đụng cột đang có dữ liệu, không đổi kiểu, không cần `CREATE EXTENSION`
⇒ `Migrate()` tự chạy lúc khởi động trên Railway vẫn an toàn.

**Nếu hụt giờ**, cắt theo thứ tự: kích hoạt `ContractPhase` → export DOCX hồ sơ quyết định → hồi
sinh `/ai/search` → tầng 2 của AI (giữ tầng 1 + metric). **Đừng cắt nhóm 6 đầu tiên** — biên bản ghi
"cân nhắc" nên nó tuỳ chọn, nhưng đó là phần ghi điểm cao nhất cho một đồ án capstone.

---

## 13. Cổng "xong" và cách kiểm

```bash
cd FURPMS_BEv2 && dotnet build          # 0 error
cd FURPMS_BEv2 && dotnet test           # xanh hết (255 sau nhóm 1)
cd furpms-web  && npm run typecheck     # sạch — PHẢI dùng lệnh này, KHÔNG phải npx tsc
cd furpms-web  && npm run build         # xanh
```
Cộng: **đếm khoá `vi.ts` == `en.ts`**.

Chạy thật:
```bash
docker compose up -d                    # PostgreSQL 16 cổng 5433
dotnet run --project FURPMS.API         # :5068
cd ../furpms-web && npm run dev         # :5173
```

- **Ngân sách**: `staff.demo@furpms.edu.vn` / `password` → Hợp đồng → tab Ngân sách → đối chiếu 4
  con số với `SELECT` thẳng trong DB.
- **Deadline**: dùng **công cụ tua thời gian** đã có (`AdminController.cs:51-70`, `IClock` offset,
  chặn ở Production) để nhảy qua mốc T-30/T-7/quá hạn mà không phải sửa dữ liệu; kiểm badge đổi màu
  và `notifications`/`email_logs` có dòng mới.
- **Hạn vòng chấm**: mở vòng 1 → hạn tự đặt theo `SCORING_WINDOW_DAYS` → tua quá hạn → vòng gắn cờ
  quá hạn **nhưng không tự đóng**; dời hạn → sinh dòng `DeadlineExtension`.
- **Bug gia hạn**: tạo đợt hạn hôm qua → gia hạn tới tháng sau → PI nộp **phải thành công**.
- **Quyết định**: chạy backfill trên **bản sao** dữ liệu prod → mở hồ sơ một đề tài đã nghiệm thu
  xong → phải thấy đủ chuỗi từ nộp đề cương tới thanh lý.
- **Cảnh báo điểm lệch**: chấm cho trung bình ~35/100 → Thư ký chọn "Đạt" → phải bị chặn cho tới khi
  ghi lý do → lý do xuất hiện trong hồ sơ quyết định.
- **Chuyên môn**: lập hội đồng cho đề tài lĩnh vực AI → ứng viên khai AI xếp đầu, người ngoài lĩnh
  vực bị 400 nếu không có lý do.
- **AI trùng**: nộp một đề cương paraphrase từ đề tài có sẵn → phải nằm top-1 với điểm > ngưỡng;
  sửa `AI_DUPLICATE_THRESHOLD` ở màn Cài đặt → kết quả đổi theo **mà không cần restart**.

---

## 14. Quy ước bắt buộc khi làm (đọc kỹ nếu bạn là AI mới)

1. **Comment tiếng Việt, giải thích VÌ SAO**, kèm ngày và lỗi cụ thể đã gặp — không mô tả lại code.
   Đây là phong cách xuyên suốt codebase, giữ đúng.
2. **i18n `vi.ts` và `en.ts` phải cân bằng khoá.** Thêm một bên mà quên bên kia thì màn hình hiện
   thẳng chuỗi khoá thô cho người dùng thấy.
3. **Không hardcode tham số nghiệp vụ** — mọi ngưỡng/số ngày vào `system_settings`. Cẩm nang chống
   trượt xếp hardcode là nguyên nhân trượt phổ biến thứ 2.
4. **Không tự commit** trừ khi chủ dự án yêu cầu trong đúng lượt đó. Commit **không** kèm dòng
   `Co-Authored-By`.
5. **Nghiệp vụ bám QĐ543** — 24 quy tắc đánh số ở `CLAUDE.md`. **Đừng tự đoán luật**; đã có lần một
   quy tắc bị bịa ra rồi viện dẫn sai điều khoản.
6. **Deploy**: Railway deploy BE từ nhánh **`master`**, FE Vercel từ **`dev`**. Nhánh làm việc cả
   hai repo là `dev`.
7. Hai repo nằm ở `D:\capstone\newroot\FURPMS_BEv2` và `D:\capstone\newroot\furpms-web`.
