> ⚠️ **File này là kế hoạch sau demo 29/07.** Buổi demo **05/08/2026** đã có góp ý MỚI —
> xem `PLAN_Week13_Demo_0508.md` (43 đầu việc). Nhiều mục "chờ quyết định" ở cuối file này
> đã được thầy chốt hướng trong buổi 05/08.

# Kế hoạch tuần 12 — sau demo & góp ý thầy (29/07/2026)

> Nguồn: note của 2 bạn trong buổi demo chiều 29/07 (có trùng lặp, đã gộp) + lỗi phát hiện khi demo trực tiếp + backlog đang dang dở ở `SYSTEM_REVIEW.md`.
> Đối chiếu: QĐ543 (`DB_ANALYTIC_REPORT.md`, `QD543_Compliance.md`) · `CLAUDE.md` Business Rules · `API_CONTRACT.md`.

## P0 — Lỗi chặn luồng ✅ **ĐÃ XONG (30/07)** — BE 92/92 test xanh, FE tsc xanh

### 0.1 ✅ Chấm nghiệm thu LỖI — gốc là **3 lỗi chồng nhau**, đã fix hết
1. **BE sai quyền:** `GET /api/councils/{id}/acceptance` gắn `[Authorize(Roles="Admin,Staff")]` → **Reviewer bị 403** khi mở tab Nghiệm thu (chính họ mới là người chấm). → **Bỏ khóa role**; Thư ký cũng cần xem tổng hợp để lập biên bản.
2. **FE/BE lệch kiểu:** BE trả **mảng** phiếu, FE dùng như **1 object** (`existing.id`) → seed sai. → Thêm **`GET .../acceptance/my`** (phiếu của chính mình, null nếu chưa chấm); FE trỏ sang endpoint này.
3. **Không sửa được phiếu:** `SubmitAsync` ném 409 *"already submitted"* dù UI ghi "Cập nhật đánh giá". → **Upsert**: cho sửa phiếu của mình; **khóa khi Chủ tịch đã chốt biên bản** (rule #12).

### 0.2 ✅ Mạch nghiệm thu → COMPLETED — tìm ra chỗ ĐỨT, đã nối
`ApproveMinutesAsync` set project `APPROVED`/`CANCELLED` **bất kể loại vòng** → nghiệm thu Đạt xong đề tài **vẫn "Đã duyệt"**, không bao giờ `COMPLETED` (trái `Process_Spec_v2`: `ACCEPTANCE → COMPLETED`).
→ Phân biệt theo `round.RoundType`:
- **ACCEPTANCE**: Đạt → **`COMPLETED`** · Không đạt/cần sửa → **`IN_PROGRESS`** (làm tiếp, KHÔNG hủy đề tài); không đụng `proposal.Status` (đề cương đã duyệt từ trước).
- **REVIEW**: giữ nguyên `APPROVED`/`CANCELLED`.
→ +2 test (`ApproveMinutes_AcceptanceRound_SetsProjectLifecycleStatus`).

**Còn lại của mạch cuối (chưa làm):** sau `COMPLETED` → quyết toán/thanh lý (`ContractSettlement`) đã có endpoint nhưng chưa kiểm end-to-end.

## P1 — Upload file thật thay URL — ✅ **XONG phần chính (30/07)**, còn sản phẩm

### ✅ Xong: file báo cáo tiến độ (BM06) — phần thầy nhấn mạnh nhất
- **BE:** `POST/GET /api/progress-reports/{id}/documents` + `/download` (Document polymorphic `EntityType="ProgressReport"`, tái dùng hạ tầng upload sẵn có). Upload gác quyền: **chỉ PI của đề tài** (hoặc Staff/Admin) → người khác 403.
- **FE PI:** form điền báo cáo có khối **"File báo cáo (BM06)"** — chọn file PDF/Word, hiện danh sách đã nộp.
- **FE Staff:** dialog đánh giá **hiện file PI nộp, bấm mở xem**; **khóa nút "Lưu đánh giá" khi chưa có file** (đúng yêu cầu thầy: *"staff phải xem được file rồi mới cho đạt/failed"*).
- 🔴 **Bug phát hiện thêm & đã fix:** 3 bộ từ vựng lệch nhau cho kết quả đánh giá → FE gửi `APPROVED/…`, BE nhận `SATISFACTORY/…` ⇒ **Staff bấm đánh giá LUÔN 400, chưa từng chấm được**. Nay đồng bộ theo doc: **`PASS` / `FAIL` / `CONDITIONAL`**.

### ✅ Xong: upload file thay URL cho báo cáo tổng kết + hợp đồng (30/07)
- **Báo cáo tổng kết (BM09):** `POST/GET /final-reports/{contractId}/documents` + `/download`; FE thay 2 ô dán URL bằng **nút chọn file** (upload xong tự lấy `downloadUrl` của hệ thống làm `reportFileUrl`/`summaryFileUrl`) → PI không phải tự host file ở đâu khác.
- **Hợp đồng (BM05):** bỏ ô *"E-contract URL"* ở form tạo (nguồn của giá trị rác `12`); file thật upload sau khi tạo ở **"Hồ sơ hợp đồng đã ký"** — luồng upload/xem đã có sẵn từ tuần 10.

### Còn lại của P1
- Sản phẩm: `FileUrl` vẫn là URL + chưa cho nhập `TrialEvidenceUrl` (minh chứng thử nghiệm).

Hiện **toàn bộ dùng URL string** (dán link), thầy yêu cầu **upload file PDF**:
| Chỗ | Hiện tại | Cần |
|---|---|---|
| Tạo hợp đồng | `EcontractUrl` (string) | **Upload PDF** |
| Báo cáo tổng kết | `ReportFileUrl` (string) | **Upload PDF** |
| **Báo cáo tiến độ** | **KHÔNG có field file nào** | **PI upload PDF** (mới hoàn toàn) |
| Sản phẩm | `FileUrl` (string) | Upload PDF (+ `TrialEvidenceUrl` minh chứng thử nghiệm — entity đã có, form chưa cho nhập) |
| Giải ngân minh chứng | ✅ đã có (`disbursement/{id}/evidence`) | Verify + đưa lên UI rõ hơn |

> Đã có sẵn hạ tầng upload (`ProposalDocumentService`, Document polymorphic) → tái dùng.

### 1.1 Staff phải XEM được file báo cáo tiến độ rồi mới Đạt/Không đạt
Nút "Đánh giá" chỉ bật khi có file + nút mở/preview file ngay trong dialog đánh giá.

## P2 — Số đợt báo cáo tiến độ LINH HOẠT — ✅ **XONG (30/07)**
- **Bỏ chặn cứng** số kỳ theo loại (trước: tạo quá 2/1 → 409).
- **Staff tự chọn số kỳ** khi sinh: `generate?roundCount=1..12`; bỏ trống → mặc định 2/1 theo loại (vẫn bám QĐ543 nhưng không ép).
- **Đặt/sửa tên đợt**: cột mới `progress_reports.round_name` (migration **PhaseK**, nullable — không mất data) + ô "Tên đợt báo cáo" trong dialog lịch. Chưa đặt → hiện "Kỳ {số}". Hiện ở cả màn Staff và PI.
- **Gia hạn deadline**: đổi `dueDate` trong dialog lịch = gia hạn hạn nộp cho PI (có ghi chú trên UI).

### Nội dung gốc (tham chiếu)
> ⚠️ Đảo ngược một phần việc tuần 11 (đang cứng Ứng dụng 2 / Cơ bản 1 theo QĐ543 Điều 10.1). Thầy chốt: **cho Staff chỉnh**.
- Staff **sửa được số lần** báo cáo (mặc định gợi ý 2/1 theo loại, nhưng cho thêm/bớt).
- **Đặt tên từng đợt** báo cáo (không chỉ "Kỳ 1/Kỳ 2").
- Sửa được **fields** của kỳ báo cáo.

### 2.1 Gia hạn deadline báo cáo tiến độ
Khi Staff đánh giá "cần chỉnh sửa" đúng ngày cuối deadline → cho **gia hạn** (tái dùng cơ chế `DeadlineExtension` = log, không ghi đè — rule #19).

## P3 — Nhắc hạn & thông báo — ✅ **XONG phần nhắc hạn (30/07)**
- Mốc nhắc mặc định thêm **3 ngày**: `DEADLINE_REMINDER_DAYS = 30,14,7,3` (Admin đổi được).
- **Scanner nay quét cả BÁO CÁO TIẾN ĐỘ** (trước chỉ quét sản phẩm → PI không được nhắc gì về báo cáo):
  `REPORT_REMINDER_T{n}` khi tới mốc, `REPORT_OVERDUE` (URGENT) khi quá hạn; chỉ kỳ **chưa nộp** + đã có hạn; không gửi trùng; dùng **tên đợt** Staff đặt trong tiêu đề. +2 test.
- ✅ **04/08 — thông báo qua EMAIL (`INotifier`):** gom "tạo chuông + gửi mail" về **một chỗ dùng chung**, thay vì mỗi service tự nhớ.
  - **Lỗ hổng đã bịt:** `ReviewRoundService` (**kết quả xét duyệt gửi PI**) và `DeliverableService` (**sản phẩm đạt/không đạt**) trước đây **chỉ bắn chuông, không gửi mail** — PI không mở app thì không biết đề tài mình đậu hay rớt.
  - **Phát hiện thêm:** PI **chưa từng** được báo khi sản phẩm **ĐẠT** (chỉ báo khi trượt) → đã thêm.
  - Mail vẫn chịu công tắc tổng của Admin (`EMAIL_ENABLED`); tắt thì ghi `email_log` = `SKIPPED`, chuông vẫn chạy. Test dùng `TestNotifier` (email đi vào hư vô, không gửi thật).
  - **Cấu hình:** local `appsettings.Development.json` (đã gitignore) · Render đặt env var `EmailSettings__SmtpUsername` / `EmailSettings__SmtpPassword` / `EmailSettings__FrontendUrl` / `GeminiAI__ApiKey` (`:` → `__`).
  - **Đã test gửi thật:** SMTP thô trả `235 Authentication succeeded` → `250 OK: queued`. Mail **tới nơi nhưng vào Spam** — do `FromEmail` là `@gmail.com` mà gửi qua relay Brevo ⇒ SPF/DKIM không khớp domain. **Không sửa được bằng code**; muốn sạch phải dùng domain sở hữu / sender đã verify ở Brevo.
  - **Test được luồng mail:** thêm `EmailSettings:RedirectAllTo` — dev thì dồn **mọi** mail về 1 hộp thư thật (tài khoản seed dùng email không tồn tại; đổi email seed sẽ hỏng seeder vì nó dùng email làm **khóa định danh**). Tiêu đề ghi `[→ người-nhận-thật]`; `email_log` vẫn lưu người nhận thật. **PROD để trống.**
  - **Đã giảm điểm spam bằng code:** trước đây body là **text trần nhưng cờ `IsBodyHtml=true`, không có bản text thay thế** — đặc điểm mail rác. Nay gửi đúng **multipart/alternative** (text + HTML có template FURPMS) + **nút "Xem chi tiết"** ghép từ `EmailSettings:FrontendUrl` + `ActionUrl` của thông báo (bỏ qua các `ActionUrl` kiểu `/api/...` vì mở trên trình duyệt không ra gì).
- **Còn lại (chưa làm):** chuông thông báo auto-poll + mở rộng trigger cho các sự kiện mới (xác nhận thay, đánh giá tiến độ xong…).

### Nội dung gốc (tham chiếu)
- Nhắc **trước 3 ngày** + **khi quá hạn** (báo cáo tiến độ, sản phẩm, nộp đề cương).
- Đã có `DeadlineReminderScanner` (T-30/T-14/T-7) → **mở rộng cho báo cáo tiến độ** + bắn notification in-app.
- (Từ `SYSTEM_REVIEW`) mở rộng trigger thông báo cho các sự kiện mới + auto-poll chuông.

## P4 — Tiêu chí chấm chia theo GROUP — ✅ **XONG (01/08)**
Mô hình **"Bộ tiêu chí"** (user thiết kế, chốt tên gọi): 1 bộ = nhiều tiêu chí, gắn **loại đề tài** (☑ Cơ bản ☑ Ứng dụng) + gắn nhiều **(đợt + lĩnh vực)**; **1 bộ dùng lại cho nhiều đợt**; **sao chép bộ** để tạo nhanh.
- **Ràng buộc:** mỗi **(đợt + lĩnh vực + LOẠI VÒNG)** chỉ 1 bộ → cùng lĩnh vực vẫn có bộ Xét duyệt riêng + bộ Nghiệm thu riêng *(chỉnh so với ý ban đầu: nếu khoá "1 lĩnh vực = 1 bộ" thì không chấm nghiệm thu được)*.
- **Fallback:** lĩnh vực chưa gắn bộ → dùng **bộ mặc định** (bộ chưa gắn phạm vi) → không bao giờ kẹt.
- **Sao chép KHÔNG copy phạm vi** (copy sẽ vi phạm ràng buộc ngay).
- Migration **PhaseL**; đã sửa `defaultValue` false→true để bộ đang có không thành "không áp dụng loại nào".
- **Form chấm điểm đã nối:** gọi `GET /rubric-templates/for-council/{councilId}` — BE tự suy (đợt, lĩnh vực, loại vòng) nên FE không cần truyền gì thêm; bỏ prop `roundType` thừa.

### Nội dung gốc (tham chiếu)
Thầy: tách tiêu chí theo **loại nghiên cứu** (Ứng dụng / Cơ bản) **và** **lĩnh vực** (IT, Ngôn ngữ…).
- `RubricTemplate` hiện có `TemplateType` + **`TrackId`** (lĩnh vực) nhưng **THIẾU `ResearchTypeId`** → thêm cột + migration + UI chọn group khi tạo template.
- Khi chấm: tự lấy template khớp (loại NC + lĩnh vực), fallback template chung.

## P5 — Giải ngân gắn sản phẩm minh chứng — ✅ **XONG (04/08)**
Thầy: *"mỗi khi giải ngân từng đợt nên có sản phẩm minh chứng cho tiến độ đó, không phải chỉ input % và note"*.

**3 lỗ hổng tìm ra khi rà code:**
1. `DeliverableId` **chỉ gán lúc SINH đợt và chỉ với PARTIAL**, ghép máy móc theo thứ tự (đợt thứ i ↔ sản phẩm thứ i). Ghép sai thì không sửa được; WHOLE thì **không bao giờ** có sản phẩm.
2. `ConfirmAsync` cho đánh dấu đã giải ngân **bất kể** sản phẩm đã nghiệm thu hay chưa — đúng chỗ thầy chê.
3. DTO chỉ trả `deliverableId` trần (không tên, không trạng thái) ⇒ FE không hiện được minh chứng dù dữ liệu có.

**Đã làm:**
- **`PUT /api/disbursements/{id}/deliverable`** — Staff gắn/gỡ sản phẩm cho **bất kỳ** đợt nào (`null` = gỡ). Chặn sản phẩm **khác hợp đồng** (400) và đợt **đã giải ngân** (409). Gắn sản phẩm đã `PASSED` ⇒ set luôn `conditionMetAt`.
- **Gate `confirm`:** đợt **có gắn** sản phẩm mà chưa `PASSED` → **409** kèm tên sản phẩm. Đợt **không gắn** (tạm ứng khởi động HĐ) vẫn xác nhận bình thường — không chặn oan.
- **`EvaluateAsync`** mở khoá điều kiện theo **LIÊN KẾT** thay vì `fundingMethod`, và mở cho **mọi** đợt trỏ tới sản phẩm đó (trước chỉ `FirstOrDefault`).
- **DTO** trả kèm `deliverableName` / `deliverableAcceptanceStatus` / `deliverableSubmittedAt` / `isBlockedByDeliverable`.
- **FE** `DisbursementDeliverableLink.tsx`: mỗi đợt hiện sản phẩm + badge nghiệm thu, chưa gắn thì có dropdown chọn; nút "Đánh dấu đã giải ngân" **bị khoá** khi sản phẩm chưa Đạt (khỏi bấm rồi mới ăn 409).
- **+6 test** (`Confirm_DeliverableNotPassed_Throws` ×2, `Confirm_NoDeliverableLinked_Succeeds`, `Confirm_DeliverablePassed_Succeeds`, `LinkDeliverable_FromAnotherContract_Throws`, `LinkDeliverable_AlreadyPassed_SetsConditionMet`) → **100/100**.

**Không làm (có chủ ý):** không đụng cách chia đợt/tỷ lệ %, vì rule #15 đã bỏ phần tính tiền — đợt giờ chỉ là **mốc**.

## P6 — Dashboard PI thiết kế lại — ✅ **XONG (31/07)**
Card **"Đợt đang nhận đề cương"** (`OpenCyclesCard`): tên đợt · **loại NC** (Ứng dụng/Cơ bản, suy từ đợt theo rule #7) · **hạn nộp còn bao lâu**, bấm vào đi thẳng wizard nộp.
- Còn (nhỏ): gom "việc cần làm của tôi" (kỳ báo cáo tới hạn, sản phẩm phải nộp) vào 1 card duy nhất — hiện nằm rải ở từng trang.

## P7 — Chuẩn hoá ngôn ngữ + UI — ✅ **XONG (31/07)**
- Quét hết chuỗi hardcode → đưa vào i18n; thêm namespace `toast` (38 key). **Bật English không còn lộ tiếng Việt.** Parity **vi = en = 1335 key**.
- Nhãn **loại vòng chấm đổi được tại `i18n/locales/{vi,en}.ts → reviewBoard.type.*`** — mã bên trái (`REVIEW`/`ACCEPTANCE`) **CỐ Ý fix cứng**, xem `SYSTEM_REVIEW.md` §0b.
- Thầy chốt: **demo thật để tiếng Anh**; lẫn vi trong màn en mới là lỗi (đã hết).

## P8 — AI — 🟡 **ĐANG LÀM (04/08)**: xong bước 1–2, còn 3–6

**Rà ra trước khi code — nặng hơn dự kiến.** Đối chiếu từng lời gọi của FE với route thật của BE thì **4 nút AI trên UI đang bấm vào là 404**, không phải "chưa hoàn thiện":

| FE gọi | Màn | Trạng thái |
|---|---|---|
| `POST /ai/extract` | Wizard nộp đề cương B2 | ✅ **đã sửa** — BE là `/proposals/extract`, FE gọi sai đường dẫn ⇒ **Đường B (upload+AI, rule #10/#20) chưa từng chạy** |
| `POST /ai/proposals/{id}/feedback` | `AiFeedbackCard` (PI) | ✅ **đã làm BE** |
| *(mới)* score-suggestion | Form chấm của reviewer | ✅ **đã làm** |
| `POST /ai/search` | Trang tìm kiếm ngữ nghĩa | ⬜ 404 |
| `POST /ai/suggest-reviewers` | Dialog thêm TV hội đồng | ⬜ 404 |
| `POST /ai/similarity-check` | *không màn nào dùng* | ⬜ code chết cả 2 đầu → nên xoá |

### ✅ Bước 1 — nối lại Đường B (upload + AI)
Không chỉ lệch đường dẫn: `AiExtractionResult` của FE khai `keywords`/`researchArea`/`abstractEN` — **BE chưa bao giờ trả**; ngược lại **4 field BE có** (`researchObjectives`, `methodology`, `expectedOutput`, `durationMonths`) thì FE **bỏ phí**, trong đó objectives + durationMonths lại **bắt buộc** ở bước 2. Nay type khớp `ExtractedProposalDto`, prefill đủ 7 trường, chỉ ghi đè trường AI đọc được (không xoá thứ PI đã gõ), hiện danh sách trường đã điền + `warning` của BE.

### ✅ Bước 2 — AI trợ lý (`IAiAdvisorService`)
- `GET/POST /api/ai/proposals/{id}/feedback` — góp ý 4–6 ý theo nhóm (Mục tiêu/Phương pháp/Sản phẩm/Khả thi/Kinh phí/Trình bày). **GET đọc cache**, không tốn quota.
- `POST /api/ai/councils/{councilId}/proposals/{proposalId}/score-suggestion` — **AI gợi ý điểm theo từng tiêu chí** (thầy nhắc trực tiếp). Gác quyền: chỉ **thành viên hội đồng** hoặc Admin/Staff. Luôn trả **đủ** tiêu chí theo bộ; AI thiếu cái nào thì 0 + ghi chú, **không** để mất tiêu chí. Điểm bị **kẹp** trong `[0, maxScore]`.
- **Kết quả lưu `llm_outputs`** — bảng có sẵn trong schema nhưng trước giờ chỉ dùng cho SUMMARY.
- Gemini hay bọc JSON trong ```` ```json ```` → cắt đúng đoạn mảng rồi mới parse, không để cả tính năng hỏng vì mấy ký tự thừa.
- **Tách `IRubricResolver`**: thứ tự ưu tiên bộ tiêu chí (vòng → đợt+lĩnh vực → mặc định) trước nằm **trong controller**; giờ AI cũng cần ⇒ tách ra dùng chung, `RubricTemplatesController` chuyển sang gọi nó (tránh 2 bản logic lệch nhau).
- **FE:** nút "AI gợi ý điểm" trong form chấm; gợi ý hiện **dưới từng tiêu chí** kèm nút "Áp dụng" — **không tự ghi đè** điểm người chấm đã nhập (rule #12).

### ✅ Bước 3 — sửa đúng người, đúng việc (04/08, sau khi user hỏi "đặt mình vào từng role")
- 🔴 **Tóm tắt AI đang đưa NHẦM NGƯỜI.** Card chỉ có ở màn PI — mà PI là người **viết ra** đề cương, họ thuộc nội dung. Người cần nhất là **reviewer** (đọc 5–10 đề tài, thời gian ngắn) thì màn chấm **không có**. → Đưa `AiSummaryCard` vào `ProposalReviewWorkspace`, đặt **trên** khung xem file.
- 🔴 **Card tóm tắt vốn đang CRASH.** FE render `data.summary` + `data.highlights.map(...)`, BE trả `summaryText` và **không có** `highlights` ⇒ bấm "Tạo tóm tắt" là `undefined.map` → vỡ trang. (Lỗi DTO lệch **thứ ba** trong ngày, cùng họ với `/ai/extract` và `AiExtractionResult`.) → Type khớp `AiSummaryDto`; ưu tiên bản người sửa tay (`editedText`) hơn bản AI.
- ✅ **Thêm `useProposalSummaryQuery`** — mở màn là đọc bản đã sinh sẵn từ `llm_outputs`, **không tốn quota**; chưa có mới hiện nút tạo.
- ✅ **"Đối chiếu form ↔ file"** (`POST /api/ai/proposals/{id}/consistency-check`) — **đây mới là thứ thầy yêu cầu**, khác hẳn "Góp ý AI" (chỉ đọc field đã điền rồi nhận xét chung, **không mở file**). AI đọc file đính kèm mới nhất qua `GenerateFromInlineDataAsync`, trả `MISSING` / `MISMATCH` / `EXTRA` theo từng trường. Chưa có file → `hasFile=false`, FE hiện lời nhắc thay vì báo lỗi.
- **Staff/Admin: cố ý KHÔNG thêm AI.** Việc của họ là **đối sánh/lọc**, không phải sinh chữ. `suggest-reviewers` nên làm **truy vấn thuần** (chính xác + ổn định + không tốn quota); Admin chỉ cần bật/tắt AI + xem log. Nhét AI cho đủ mâm 4 role sẽ bị hỏi ngược *"giải quyết vấn đề gì?"*.

### ⬜ Còn lại
3. `/ai/suggest-reviewers` — nên làm **không cần AI** (truy vấn theo lĩnh vực + lịch sử) → rẻ và ổn định hơn.
4. `/ai/search` — xem mục **⏸ Chờ quyết định** bên dưới.
5. Cache `llm_output` cho các lời gọi còn lại.
6. Xoá `similarity-check` (chết cả 2 đầu).

---

## 🔍 Rà màn "Tiêu chí chấm" + "Báo cáo tiến độ / Nghiệm thu" (05/08)

### ✅ Đã sửa
| Vấn đề | Sự thật tìm được |
|---|---|
| **Không tạo/xoá được bộ tiêu chí** | P4 xây phần gắn đợt/lĩnh vực lên trên API vốn chỉ cho **1 bộ mỗi loại vòng**: `POST /rubric-criteria` tìm bộ bằng `FirstOrDefault(TemplateType == loại)` ⇒ tiêu chí **luôn rơi vào bộ đầu tiên**, bộ "Sao chép" vĩnh viễn rỗng, không thể có 2 bộ REVIEW khác nội dung. → Thêm `POST /rubric-templates`, `DELETE /rubric-templates/{id}`, và **CRUD tiêu chí theo bộ** (`/rubric-templates/{id}/criteria`). Chặn xoá bộ đã dùng chấm (409, bảo TẮT thay vì xoá); tiêu chí đã có điểm thì chỉ tắt. |
| **Bộ `PROGRESS_CHECK` nằm chình ình** | Loại vòng này **chết theo rule #16** (báo cáo tiến độ do Staff duyệt thẳng). Đã kiểm: `ProgressReportService` có **0** tham chiếu rubric ⇒ xoá **không ảnh hưởng** Staff duyệt hay PI điền. Đã xoá dữ liệu + bỏ khỏi `TypeMap`. |
| **Hai phần rời rạc trên trang** | Bảng phẳng "Tất cả tiêu chí" sửa được nhưng không biết thuộc bộ nào; bộ ở trên thì không sửa được tiêu chí. → **Gộp làm một**: mỗi bộ tự quản tiêu chí, thu gọn/mở rộng được. |
| **DTO thiếu `isActive` + lọc mất tiêu chí đã tắt** | FE gạch ngang toàn bộ tiêu chí; và tiêu chí bị tắt tự động **biến mất vĩnh viễn**, không có đường bật lại. → Màn quản lý trả cả tiêu chí tắt + nút "Bật lại"; màn chấm vẫn chỉ lấy tiêu chí bật. |
| **Cột "Loại vòng" hiện `Final`** | Nhãn tiếng Anh hardcode (`ROUND_TYPE_LABELS`), không qua i18n. → dùng chung `reviewBoard.type.*`. |
| 🔴 **Staff KHÔNG thấy PI viết gì trong báo cáo tiến độ** | `GET /progress-reports` chỉ trả **bản tóm tắt** (%, trạng thái). Chi tiết (nội dung đã/chưa hoàn thành, kế hoạch kỳ sau, kiến nghị, bảng hoạt động BM06) nằm ở `GET /progress-reports/{id}` — **BE có sẵn, FE chưa nơi nào gọi**. Staff mở dialog đánh giá chỉ thấy ô chấm ⇒ "PI điền một đống mà chả thấy gì". → Thêm `useProgressReportQuery`, dialog hiện đủ nội dung + bảng hoạt động. |

### ✅ Hội đồng NGHIỆM THU giờ có HỒ SƠ để chấm (05/08)
**Câu hỏi gốc:** *"người chấm nghiệm thu nên thấy cái gì? Đâu thể chỉ như lần đầu chấm."* — đúng.
Trước đó màn chấm nghiệm thu hiện **y hệt vòng 1**: chỉ đề cương + file. Người chấm không có
căn cứ nào ngoài buổi họp trực tiếp.

Theo `Process_Spec_v2` §Giai đoạn 8, sản phẩm đầu ra của giai đoạn nghiệm thu là
**`ProgressReport[]` · `ProjectDeliverable[]` · `FinalReport`** — đó chính là thứ hội đồng phải đọc.

→ Thêm **`GET /api/councils/{councilId}/proposals/{proposalId}/dossier`** + tab **"Hồ sơ nghiệm thu"**
(chỉ hiện ở vòng ACCEPTANCE), gồm:
- **Báo cáo tiến độ từng kỳ** — %, kỳ, **kết quả Staff đã đánh giá** (PASS/CONDITIONAL/FAIL) + nhận xét.
- **Sản phẩm** — trạng thái nghiệm thu từng cái, ngày nộp, có file hay không, nhận xét chất lượng.
- **Báo cáo tổng kết (BM09)** — trạng thái, ngày nộp, có file hay không.
- Dòng tóm tắt đầu bảng: số hợp đồng · **sản phẩm đạt / tổng** · số kỳ báo cáo.

**Vì sao endpoint riêng thay vì mở endpoint hợp đồng:** các endpoint hợp đồng chỉ mở cho PI/Staff;
nới cho reviewer sẽ cho họ xem hợp đồng của **mọi** đề tài. Endpoint này gác đúng phạm vi —
**chỉ thành viên của hội đồng đó** (hoặc Admin/Staff), khác đi trả **403**.

> Về "chỉ PASS/FAIL đã ổn chưa": **ổn**. Chấm điểm theo tiêu chí (BM11) và kết luận đạt/không đạt
> là 2 việc khác nhau và đều đã có. Thiếu sót nằm ở **thông tin để chấm**, không phải ở cách chấm.

### ✅ Nộp điểm xong không bị đá ra ngoài (05/08)
`RubricScoringForm` gọi `navigate(ASSIGNED_REVIEWS)` ngay sau khi nộp ⇒ muốn xem lại/sửa điểm
phải mò vào lại từ đầu. Đã bỏ điều hướng, ở nguyên trang.

### ✅ Báo cáo tiến độ phía PI (05/08)
- 🔴 **4 khoá i18n bị THIẾU** ⇒ giao diện hiện thô `reports.due`, `reports.joinLink` (khoá `joinLink` có tồn tại nhưng ở **namespace khác**). Đã bổ sung.
- **Ô chọn hợp đồng chỉ hiện trơ số ("04")** — không ai đoán được đó là gì. Nay có nhãn "Hợp đồng" + hiện `HĐ số 04 — <tên đề tài>`.
- **Nộp xong không xem lại được** → thêm nút "Xem bài đã nộp", tái dùng `ProgressReportDetailView` (component **dùng chung** cho cả Staff lẫn PI, tránh 2 bản).
- **Nộp xong khoá cứng** (`UpdateAsync` chỉ cho `DRAFT`) — bất nhất với sản phẩm (nộp lại tới khi ĐẠT) và báo cáo tổng kết (nộp lại + yêu cầu chỉnh sửa); PI lỡ sai một chữ là kẹt. → Nay **sửa được tới khi Staff ĐÃ đánh giá**; đánh giá xong mới khoá. *(Xử luôn mục #10 của `SYSTEM_REVIEW` §2.)*

### ✅ Nộp sản phẩm: upload file thật + minh chứng thử nghiệm (05/08)
Sản phẩm là chỗ **CUỐI CÙNG** còn bắt dán URL (form chỉ có ô *"Đường dẫn file"*), trong khi đề cương ·
báo cáo tiến độ · báo cáo tổng kết · hợp đồng đều đã upload thật. Và entity có sẵn **`TrialEvidenceUrl`**
nhưng form **chưa bao giờ cho nhập** ⇒ hồ sơ nghiệm thu thiếu theo **QĐ543 Điều 13.1**.
- BE: `POST/GET /api/deliverables/{id}/documents` + `/download` (Document polymorphic `EntityType="Deliverable"`,
  `DocumentCategory` phân biệt `DELIVERABLE` vs `TRIAL_EVIDENCE`) · `SubmitDeliverableRequest` + `TrialEvidenceUrl` ·
  DTO trả thêm `trialEvidenceUrl`. Nộp lại mà bỏ trống minh chứng thì **giữ bản cũ**, không xoá trắng.
- FE: 2 ô **chọn file** (sản phẩm bắt buộc, minh chứng tuỳ chọn) — upload trước, lấy URL download của BE rồi mới submit.

### ✅ PI xin điều chỉnh / gia hạn (05/08)
BE **đã cho phép PI tạo từ lâu** (`POST /contracts/{id}/amendments` chỉ cần là chủ hợp đồng; chỉ
duyệt/từ chối mới giới hạn Staff/Admin) — nhưng FE **chỉ có màn bên `staff/contracts`** nên PI không
có đường vào, luồng coi như tắc. → Thêm trang **`/my-amendments`** (nav Faculty): xem yêu cầu đã gửi
+ trạng thái + ý kiến phòng QLKH, và dialog gửi yêu cầu mới (loại điều chỉnh · nội dung · giá trị
hiện tại→đề nghị · lý do). Bao gồm **xin gia hạn** (QĐ543: tối đa 6 tháng).

### 🔴 2 lỗi chặn test, đã sửa (05/08)
1. **Chấm nghiệm thu PASS/FAIL luôn 500.** `AcceptanceEvaluationService` **không bao giờ gán `ProjectId`**
   ⇒ rơi vào `Guid.Empty` ⇒ INSERT vi phạm khoá ngoại `fk_acceptance_evaluations_projects_project_id`.
   Nay lấy project từ `CouncilProjectAssignment`; hội đồng chưa gán đề tài thì báo lỗi rõ thay vì 500.
   *(Đã test: hội đồng chưa chốt biên bản → **200**; hội đồng đã chốt → **409** đúng rule #12.)*
2. **Upload chết khi mạng/DNS tới Cloudinary hỏng** ("không reach được server"). Nay **tự rơi về đĩa local**;
   đọc lại vẫn chạy vì `OpenAsync` đã có nhánh dự phòng đọc đĩa. Người dùng không bị chặn nộp bài.

### ⚠️ Duyệt điều chỉnh: CHỈ "gia hạn" được hệ thống tự áp dụng (rà 05/08)
User hỏi *"staff đồng ý rồi thì điều chỉnh kiểu gì?"* — soi `AmendmentService.ApproveAsync`:

| Loại (seed) | Duyệt xong hệ thống làm gì |
|---|---|
| `EXTENSION` — Gia hạn thời gian | ✅ **Tự cộng tháng** vào `contract.EndDate`, chặn vượt `MaxExtensionMonths` (QĐ543: ≤6 tháng) |
| `BUDGET_ADJUST` · `SCOPE_CHANGE` · `TEAM_CHANGE` · `OTHER` | ⚠️ **Chỉ đổi status thành APPROVED** — không sửa gì trong hệ thống. Staff phải tự vào chỗ tương ứng chỉnh tay, mà **có chỗ còn chưa chỉnh được** |

🔴 **Lỗi im lặng đã sửa:** `ApplyExtensionIfNeededAsync` **`return` lặng lẽ** khi `NewValue` không phải số nguyên dương (PI gõ *"3 tháng"* thay vì *"3"*) ⇒ Staff bấm Duyệt, hệ thống báo thành công, **nhưng hạn hợp đồng không hề đổi và không ai biết**. Nay báo lỗi rõ; FE cũng đổi ô nhập thành **kiểu số** (1–6) khi chọn loại Gia hạn.

**Còn phải quyết:** 4 loại kia có nên tự áp dụng không? Ví dụ `TEAM_CHANGE` → sửa `project_member`; `BUDGET_ADJUST` → rule #15 đã bỏ quản tiền nên có thể **chỉ lưu hồ sơ là đủ**. Cần chốt trước khi code.

### ✅ Sửa lỗi vỡ layout ở màn hẹp (05/08)
Thu nhỏ cửa sổ (~820px) thì **chữ lòi khỏi khung, breadcrumb xuống 3 dòng**. Ba nguyên nhân chồng nhau:
1. Cột nội dung dùng `flex-1` mà **thiếu `min-w-0`** — flex item mặc định `min-width:auto` nên không co dưới bề rộng nội dung.
2. Breadcrumb không `truncate`, icon không `shrink-0`.
3. Ô tìm kiếm `w-56` **cố định** hiện từ `sm` — chính nó đẩy header tràn. Nay dưới `lg` dùng nút icon.
*(Bắt bằng script đo `scrollWidth > clientWidth` ở viewport 820px, không phải nhìn bằng mắt.)*

### 📋 Việc user nêu 05/08 — chưa làm, ghi lại để không rơi
1. **Quản lý hội đồng chưa phải CRUD.** Màn "Hội đồng" hiện chỉ là **danh sách đề cương** y hệt màn "Xét duyệt đề cương"
   (cùng cột, cùng nút) ⇒ **thừa và gây nhầm**. Cần đổi thành nơi quản lý **chính các hội đồng**: xem/thêm/sửa/xoá hội đồng
   đã tạo, thành viên, lịch họp.
2. **Màn "Xét duyệt đề cương" cần gom nhóm + số liệu.** Hiện đổ phẳng 9 dòng. Cần: **nhóm theo đợt**, và vài con số
   tổng quan (bao nhiêu đợt đang mở, mỗi đợt bao nhiêu đề tài, bao nhiêu đã duyệt/chờ).
3. **Đổi tên nút "Xét duyệt" → "Xem chi tiết"** và mở ra **trang chi tiết đề tài** gom: đề cương · tiến độ · sản phẩm ·
   nghiệm thu. Hiện các thông tin này **nằm rải trong sheet Hợp đồng** — user thấy sheet đó "ngột ngạt".
   → Cân nhắc **chuyển bớt từ Hợp đồng sang trang chi tiết đề tài**; Hợp đồng chỉ giữ phần thuộc về hợp đồng
   (ký, giải ngân, điều chỉnh, quyết toán).
4. **Màn Hợp đồng chỉ nên hiện đề tài đã qua vòng xét duyệt** (đã sang giai đoạn tiến độ/nghiệm thu) — cần kiểm lại
   điều kiện lọc hiện tại.
5. **"Đề tài được phân công" sắp xếp lộn xộn** — gom nhóm theo loại vòng/trạng thái, hoặc sắp theo hạn / ngày họp gần nhất.
6. **Nộp sản phẩm: có nên BẮT BUỘC file không?** User đề nghị cho **chọn 1 trong 2**: upload file *hoặc* dán link
   (file quá lớn thì upload không nổi). → Nên: giữ upload làm mặc định, thêm tab/ô "dán link" thay thế;
   validate "phải có ít nhất một trong hai".
7. **🔴 "Báo cáo tổng kết" đang nằm trong màn HỢP ĐỒNG của Staff** — form *"Nộp báo cáo tổng kết"* với ô chọn file
   hiện ra ở tab của **Staff**, nhưng theo QĐ543 người nộp BM09 là **PI**. PI đã có trang riêng `/final-reports`.
   → Bên Staff chỉ nên **XEM + yêu cầu chỉnh sửa**, không phải nộp hộ.
8. **Sau khi nghiệm thu Đạt + Chủ tịch chốt biên bản thì sao?** Hiện: đề tài → `COMPLETED` (đã nối ở P0.2).
   Nhưng **không có màn nào nói cho ai biết** điều đó. → Cần: PI thấy **tiến trình đề tài của chính mình**
   (mốc + trạng thái, giống timeline hợp đồng nhưng theo đề tài), và bước cuối cùng còn lại là **quyết toán/thanh lý**
   (`ContractSettlement` có endpoint nhưng chưa kiểm end-to-end).
9. **Tóm tắt AI bên PI có cần không?** — Đang có ở cả màn PI lẫn màn chấm. PI là người **viết** đề cương nên
   ít giá trị; cân nhắc **bỏ ở màn PI**, giữ ở màn người chấm (nơi thật sự cần đọc nhanh). Giữ lại phần
   **"Đối chiếu với file đề cương"** ở màn PI vì cái đó mới giúp PI phát hiện sai sót.

### ⬜ Tìm ra nhưng CHƯA sửa (cần quyết định/ưu tiên)
1. 🔴 **Nộp sản phẩm quá sơ sài so với QĐ543 Điều 13.1.** Entity `ProjectDeliverable` có `ScientificRequirements`, **`TrialEvidenceUrl`** (minh chứng thử nghiệm), `Notes` — nhưng `SubmitDeliverableRequest` **chỉ nhận `FileUrl` + `Description`**. Tệ hơn: `FileUrl` là **URL dán tay**, trong khi báo cáo tiến độ / tổng kết / hợp đồng đều đã chuyển sang **upload file thật**. → Cần: `POST /deliverables/{id}/documents` (tái dùng `Document` polymorphic) + ô minh chứng thử nghiệm.
2. 🔴 **PI không xin được điều chỉnh / gia hạn.** BE **đã cho phép** (`POST /contracts/{id}/amendments` chỉ `[Authorize]` + kiểm chủ hợp đồng; chỉ approve/reject mới giới hạn Staff/Admin). Nhưng **FE chỉ có màn ở `features/staff/contracts`** — PI không có đường vào. Tab "Điều chỉnh" bên Staff = nơi **duyệt/từ chối** yêu cầu đổi phạm vi / kinh phí / thời gian / nhân sự (gồm cả **xin gia hạn**).
3. 🟡 **CRUD sản phẩm**: Staff tạo/sửa được, nhưng **không có endpoint xoá** sản phẩm đã tạo nhầm.
4. 🟡 **Màn "Đề tài được phân công" sắp xếp lộn xộn** — cần gom nhóm (theo loại vòng / trạng thái) hoặc sắp theo hạn/ngày họp gần nhất, thay vì đổ ra một mạch.


### 🧠 Tóm tắt AI đọc gì? — **chỉ trường nhập tay, KHÔNG đọc file** (chốt 05/08)
`AiSummaryService.BuildPrompt` ghép prompt từ **các trường cấu trúc PI đã điền**: tên đề tài, loại
NC, số tháng, mục tiêu, phương pháp/nội dung, sản phẩm dự kiến, thành viên, tổng kinh phí. DTO trả
`source: "textFields"`, `sourceFileName: null` — **file đính kèm không hề được đưa vào prompt.**

Hệ quả cần biết trước khi demo:
- Đề cương nộp theo **Đường A (nhập tay)** → tóm tắt đủ ý, dùng được.
- Nộp theo **Đường B (upload + AI prefill)** mà PI để form sơ sài, nội dung thật nằm trong file
  Word/PDF → tóm tắt sẽ **nghèo nàn/lệch**, vì AI không nhìn thấy file đó.
- Ống dẫn đọc file **đã có sẵn** (`GeminiFileInput`: PDF gửi inline, .docx bóc text OpenXml — đang
  dùng cho luồng trích xuất đề cương). Muốn tóm tắt bám file thì nối lại là xong, chưa làm vì chưa
  chốt: tóm tắt **file** hay tóm tắt **form**, hay ghép cả hai rồi nêu chỗ vênh.
- ⏸ **Chờ quyết định:** giữ nguyên (tóm tắt form) / đổi sang đọc file / ghép cả hai.

### 🏷 Bỏ nhãn "Khoa học" ở màn Hội đồng & Chấm (xong 05/08)
Rule #16 bỏ phương diện FINANCE ⇒ mọi vòng đều `SCIENCE`. Nhãn chỉ còn đúng một giá trị mà vẫn dán
lên chip từng vòng + dòng tiêu đề. Thay bằng **loại vòng** (Xét duyệt đề cương / Nghiệm thu) — đó
mới là thứ phân biệt các vòng với nhau. `dimension` vẫn giữ trong DB/API, chỉ ngừng hiển thị.


### 🔴 Biên bản Thư ký: điểm hiện "—: 58.0", mất tên người chấm (sửa 05/08)
`ScoreResponse` ở FE khai `reviewerId` — **BE không hề có trường đó**, nó trả `evaluatorMemberId` +
`evaluatorName`. Panel tra `membersById.get(score.reviewerId)` → luôn `undefined` → rơi xuống "—".
`FeedbackResponse` sai y hệt (`reviewerId` vs `reviewerMemberId`/`reviewerName`).
Đối chiếu `QD543_Compliance.md` §2: BM04 đòi mục **"Ý kiến thành viên (chuyên môn/kinh phí)"** lấy từ
`ProposalReviewScore.GeneralComments` + `ReviewerFeedback` ⇒ **tên người chấm là bắt buộc**, không
phải trang trí. Nay đọc thẳng `evaluatorName`/`reviewerName` và dùng `totalScore` của BE thay vì
FE tự cộng lại `scoreDetails` cho lệch.

Kèm theo: **"Số liệu cuộc họp" (BM04 II.2) cả 4 ô đều "—"** vì chỉ đọc từ `decision`, mà `decision`
chỉ có SAU khi Chủ tịch duyệt biên bản — đúng lúc Thư ký cần số để soạn thì không có gì. Nay tính
tại chỗ từ roster + điểm danh + phiếu hợp lệ khi chưa chốt.


### 🔴 "Tạo hợp đồng" chào cả đề tài ĐÃ KÝ hợp đồng (sửa 05/08)
Dropdown "Đề tài đã duyệt" đổ thẳng mọi đề tài `APPROVED`. Kiểm bằng API: **4 đề tài APPROVED thì
3 đã có hợp đồng** mà vẫn nằm trong danh sách ⇒ Staff mở ra không biết cái nào còn phải làm, và
tạo nhầm hợp đồng thứ hai lúc nào không hay.

Ký từng giai đoạn đã có `ContractPhase` lo (phase nằm **trong** một hợp đồng), nên một đề tài chỉ
cần một hợp đồng. Nay lọc bỏ đề tài đã có hợp đồng, kèm dòng "Đã ẩn N đề tài vì đã có hợp đồng"
để không ai tưởng mất dữ liệu.


### 📏 Trần gia hạn: doc nội bộ ghi SAI, đã sửa (05/08)
`Process_Spec_v2.md` ghi *"gia hạn tối đa 6 tháng"*. Tra văn bản gốc `QD_543...docx`,
**Điều 10 khoản 4** nguyên văn: *"Gia hạn tối đa **1/2 tổng thời gian thực hiện** của đề tài được
phê duyệt"*. 6 tháng chỉ là ca riêng khi đề tài dài 12 tháng (Mẫu 1: "Thời gian thực hiện không
quá 12 tháng") — hay gặp nên bị chép thành luật.

Nay: `ProposalSummaryDto` trả thêm `DurationMonths` → form tạo hợp đồng **tự điền** trần khi chọn
đề tài, kèm dòng giải thích; BE chặn ở cả tạo lẫn sửa (`ValidateMaxExtension`). Kiểm bằng API:
đề tài 12 tháng, nhập 9 → 400 kèm câu nêu rõ trần.

### ⬜ BM05 còn thiếu phần "BÊN B" — cần user quyết
Đối chiếu mẫu BM05 trong QĐ543 với form + bản Word đang sinh:

| Mục BM05 | Hiện có? |
|---|---|
| Số HĐ (`…/QLKH-FEHO`) · tên đề tài · thời gian · tổng kinh phí · đại diện Bên A | ✅ |
| Điều 2 sản phẩm · Điều 4 chia 4 đợt 30/30/30/còn lại | ✅ (deliverables + generate lịch giải ngân) |
| **Mã số đề tài** (Điều 1) | ❌ |
| **Bên B: đơn vị công tác, điện thoại, địa chỉ** | ❌ (User có `Phone`, chưa đưa vào HĐ) |
| **Bên B: số tài khoản + ngân hàng** | ❌ không có cột nào trong DB |
| **Bên B: số CMND/CCCD + ngày cấp + nơi cấp** | ❌ không có cột nào trong DB |
| Bản Word: mới có bảng tóm tắt + ô ký, **chưa có Điều 1–7 đầy đủ** | ⚠️ |

⏸ **Chờ quyết định:** thêm nhóm trường định danh/ngân hàng của Bên B (cần migration + màn hồ sơ cá
nhân cho PI khai) hay chấp nhận để trống trong bản Word rồi ký tay điền vào.


### 🔴🔴 FE **chưa từng được typecheck** — phát hiện 05/08
`tsconfig.json` ở gốc là **solution-style** (`"files": []` + `references`), nên `tsc --noEmit`
không nạp file nào; còn `npm run build` = `vite build` dùng esbuild — chỉ **bóc** kiểu chứ **không
kiểm**. Kiểm chứng: cố tình viết `const x: number = "chuoi"` → tsc **im lặng**.

Nghĩa là mọi câu "typecheck xanh" trước đó **không có giá trị**. Chạy đúng lệnh
(`tsc -p tsconfig.app.json --noEmit`) ra **43 lỗi**, trong đó có lỗi làm hỏng chức năng thật:

| Lỗi | Hậu quả |
|---|---|
| `originalFileName` (BE trả `fileName`) ở 3 file | Tên file đính kèm hiện `undefined` |
| `ProgressReport` thiếu `items` | Bảng BM06 không prefill được khi PI sửa |
| `MyAmendmentsPage` dùng `reviewNotes` (đúng: `reviewerComments`) | PI **không thấy lý do Staff từ chối đơn** |
| `useProposalAi` còn `onError` (TanStack Query **v5 đã bỏ**) | Kiểu trả về suy ra `{}` ⇒ cả `AiSummaryCard` hỏng kiểu |
| `ApiResponse` thiếu type argument ở 8 chỗ | — |
| Mock `SummaryResult`/`AiExtractionResult` theo shape CŨ | Chạy mock mode là crash |
| `watch` dùng trước khi khai báo trong `CreateContractSheet` | **Trắng cả trang /contracts** (TDZ) |

Đã sửa hết → **0 lỗi**. Chốt lại: `npm run build` nay **chạy typecheck trước** rồi mới build, và
thêm `npm run typecheck`. Từ giờ "build xanh" mới thật sự có nghĩa.

### 🔴 Link báo cáo tiến độ: danh sách có, chi tiết KHÔNG (sửa 05/08)
`ReportFileUrl` chỉ được map ở `MapSummary`, quên `MapDetail`. Dialog đánh giá của Staff đọc
**chi tiết** ⇒ luôn thấy null ⇒ báo đỏ "PI chưa nộp file báo cáo" và **khoá nút Lưu đánh giá**,
dù PI đã nộp link hẳn hoi. Kiểm bằng API: danh sách trả `'abc.com'` còn chi tiết trả `None`.

## 📌 DANH SÁCH VIỆC — rà CRUD toàn hệ thống (05/08)

### A. 🔴 Sai vai — panel viết cho vai này bị tái dùng cho vai khác
**Mẫu lỗi lặp lại 3 lần**, không phải ca lẻ: panel nhúng trong màn Hợp đồng của Staff nhưng
không gác hành động theo vai ⇒ **Staff làm hộ PI**.

| Panel | Hành động sai vai | Trạng thái |
|---|---|---|
| `FinalReportPanel` | Staff nộp hộ báo cáo tổng kết (BM09 là của PI) | ✅ sửa — thêm `canSubmitReport` |
| `AmendmentsPanel` | Staff tự xin điều chỉnh rồi tự duyệt | ✅ sửa — thêm `canRequest` |
| `DeliverablesPanel` | Staff bấm "Nộp lại" sản phẩm hộ PI | ✅ sửa — thêm `canSubmit` |
| `DisbursementsPanel` · `SettlementPanel` | Rà 06/08: **KHÔNG sai vai** — cả hai đều gác đúng bằng `canManage` (`DisbursementsPanel` gác từng nút; `SettlementPanel` chặn sớm bằng `if (!canManage) return <EmptyState/>` khi chưa có quyết toán, và gác các nút hành động). | ✅ đã rà |

### B. 🔴 CRUD thiếu — đếm từ code, không phỏng đoán
**26/38 controller không có endpoint XOÁ.** Có cái không cần (Auth, Admin-tools, AI), nhưng
những cái sau là **thiếu thật, chặn nghiệp vụ**:

| Thực thể | Thiếu | Hậu quả |
|---|---|---|
| ~~**Hợp đồng**~~ | ✅ **XONG 05/08** — `PUT /contracts/{id}` + `DELETE /contracts/{id}`, FE có menu Sửa/Xoá ở danh sách | Đã kiểm bằng API: sửa 200 (giữ nguyên `originalEndDate` khi HĐ đã gia hạn) · ngày sai 400 · xoá HĐ ACTIVE 409 · xoá HĐ chưa ký 200 rồi GET lại 404 (lịch giải ngân dọn theo, không lỗi khoá ngoại) |
| **Sản phẩm** | ❌ xoá | Thêm nhầm không gỡ được |
| **Thành viên đề tài** | ❌ xoá | Không loại được thành viên |
| **Kỳ báo cáo tiến độ** | ❌ xoá | Staff sinh thừa kỳ là kẹt |
| ~~**Lịch họp hội đồng**~~ | ✅ **XONG 05/08** — `PUT`/`DELETE /meetings/{id}` + nút Sửa/Xoá ở panel hội đồng | Kiểm bằng API: sửa 200 · offline trống địa điểm 400 · thời lượng 0 → 400 |
| **Master data** (đơn vị · loại sản phẩm · vai trò nhân sự · hạng mục chi · cấu hình tài chính) | ❌ xoá | Nhập sai là nằm đó mãi |

### C. ⚠️ Điều chỉnh hợp đồng — chỉ 1/5 loại thật sự có tác dụng
- ✅ **Gia hạn**: BE cộng tháng vào `EndDate`, chặn vượt `MaxExtensionMonths`. **Đã kiểm bằng API: `end` đổi từ `2027-01-08` → `2027-04-08`.**
- ⚠️ **4 loại kia** (kinh phí · nội dung NC · thành viên · khác): duyệt xong **chỉ đổi status**, hệ thống không sửa gì.
- 📄 **QĐ543 CÓ quy định**: `QD543_Compliance.md` ghi **BM07 "Phiếu đề nghị thay đổi"** → ánh xạ `ProposalChangeRequest` — **BE có 4 endpoint, FE ❌ chưa có màn nào**.
  ⚠️ Nghĩa là hệ thống đang có **HAI cơ chế thay đổi song song**: `ProposalChangeRequest` (đổi ĐỀ CƯƠNG, theo BM07) và `AmendmentRequest` (đổi HỢP ĐỒNG). **Cần chốt phân vai** kẻo trùng lặp.
- **Đề xuất:** giữ tự-động cho **gia hạn**; `BUDGET_ADJUST` chỉ lưu hồ sơ (rule #15 đã bỏ quản tiền); `TEAM_CHANGE`/`SCOPE_CHANGE` nối vào BM07 thay vì làm riêng.

### D. ✅ Vừa sửa (05/08)
- 🔴 **Duyệt gia hạn xong màn hình vẫn hiện hạn cũ** — FE **không invalidate** query hợp đồng ⇒ người dùng tưởng duyệt không có tác dụng. Nay invalidate + **hiện nhãn "Đã gia hạn — hạn gốc {ngày}"** (trước đây nhìn vào chỉ thấy một cái ngày, không biết đã gia hạn hay chưa). Kèm bổ sung `originalEndDate`/`maxExtensionMonths` vào DTO danh sách.
- Lỗi im lặng khi gia hạn ghi chữ thay vì số (xem mục ở trên).


### 🔎 Rà 2 panel (06/08) — không sai vai, nhưng lộ ra vi phạm **rule #15**
`SettlementPanel` **bắt nhập 2 ô số tiền** (`totalContractedAmount`, `totalDisbursedAmount`) mới
cho tạo quyết toán — `disabled={!contracted || !disbursed || ...}`.

Trái **rule #15** (thầy tuần 10): *"Tài chính = minh chứng, hệ thống **KHÔNG quản tiền**; kế toán
chi tiền ngoài hệ thống… đánh dấu đã giải ngân (**không nhập số tiền**)"*. Chỗ xác nhận giải ngân
đã sửa từ tuần 12 (`confirm` → amount optional) nhưng **quyết toán thì bỏ sót**.

Nay để tuỳ chọn (bỏ trống = 0). BE vốn đã nhận 0 (chỉ chặn số âm) nên không phải đổi.

**Đã rà nốt (06/08) — sạch:**
- `budget-categories` và `financial-config`: **đã ẩn khỏi nav** từ tuần 12 (2 dòng comment trong `nav.ts`).
- Dự toán đề cương: **không còn ô nhập tiền nào** ở màn PI.
- Chỉ còn **`maxBudgetCap`** ở màn Admin *Loại đề tài* — đây là **trần kinh phí của LOẠI đề tài**,
  đúng rule #7 (*"mỗi cycle có timeline + funding cap riêng"*), **không phải** nhập tiền chi tiêu ⇒ giữ.

⇒ Rule #15 nay đã áp đủ ở FE. Phần "strip" còn lại nằm ở BE (bảng vẫn giữ, chỉ ngừng tính tiền) —
đúng như scope đã chốt.

### E. Thứ tự đề xuất
1. ~~**Hợp đồng: thêm SỬA + XOÁ**~~ ✅ xong 05/08.
2. ~~**Rà nốt 2 panel còn lại**~~ ✅ 06/08 — không sai vai, nhưng lộ ra lỗi khác (xem dưới).
3. **Xoá**: sản phẩm · kỳ báo cáo · ~~lịch họp~~ ✅ · thành viên đề tài.
4. **Xoá master data** (5 màn Admin).
5. Chốt hướng 4 loại điều chỉnh + quan hệ với BM07.
6. Rồi mới tới 4 việc lớn về UI (hội đồng CRUD, gom nhóm màn xét duyệt, trang chi tiết đề tài, lọc màn hợp đồng).

## ⏸ Chờ user quyết định (đừng tự làm — hỏi lại rồi mới làm)

### Q1. Tìm kiếm ngữ nghĩa (semantic) — LÀM, THAY, hay BỎ?
> Đặt ra 04/08. User: *"để tôi quyết định sau"*.

**Semantic là gì:** tìm theo **ý nghĩa** thay vì trùng chữ — gõ "dạy học trực tuyến" ra được cả bài viết "e-learning", "lớp học ảo".

**Chi phí thật KHÔNG phải tiền** (embedding là loại gọi AI rẻ nhất, free tier vẫn có), mà là:
- Phải sinh vector cho **mọi** đề cương + **sinh lại mỗi lần PI sửa** (quên → sai âm thầm) + backfill dữ liệu cũ.
- SQL Server bản đang dùng **không có kiểu vector / index ANN** → phải kéo hết vector về C# tính cosine tay. Cỡ đồ án chạy được nhưng là giải pháp đồ chơi.
- Demo phụ thuộc mạng + key; dính 429 giữa buổi là hỏng.

**Hiệu quả so với tìm thường:** không có con số trung thực nào đưa ra được — phụ thuộc kích thước kho và kiểu câu hỏi. Lập luận theo quy mô: kho đồ án cỡ **vài chục–vài trăm** đề cương, ở mức đó **lọc (đợt/lĩnh vực/loại/trạng thái) + tìm chữ** giải quyết gần hết nhu cầu. Semantic chỉ thắng rõ khi (a) người dùng gõ từ khác hẳn tài liệu **và** (b) kho đủ lớn để không lọc tay nổi — đồ án không thoả cả hai. Tiếng Việt còn bất lợi: SQL Server không có bộ tách từ tiếng Việt.

| Phương án | Công | Ghi chú |
|---|---|---|
| **A. Bỏ semantic → "Tìm kiếm nâng cao"** ⭐ *khuyến nghị* | **nhỏ** | BE **đã có sẵn** `GET /api/proposals?cycleId&trackId&status&type&search`. Chỉ còn: (i) mở rộng `search` quét thêm mục tiêu/phương pháp/sản phẩm (**hiện chỉ quét tiêu đề VI/EN**), (ii) đổi `SemanticSearchPage` (144 dòng) sang gọi endpoint có sẵn |
| **A+. Như A, thêm quét bản tóm tắt AI** | nhỏ | Cho `search` quét luôn nội dung trong `llm_outputs`. Tóm tắt do AI viết nên hay dùng **từ khác bản gốc** ⇒ bắt được kha khá ca "khác chữ cùng nghĩa" — lấy ~nửa lợi ích semantic mà **không cần embedding** |
| B. Làm semantic thật | lớn | Embedding + `semantic_search_vector` + backfill + cosine trong C# |
| C. Ẩn nút, không làm gì | ~0 | Mất một mục trong scope |

**Nếu chọn A/A+:** nhớ xoá luôn `similarity-check` (chết cả 2 đầu).

### Q3. Validate ở FE — có rồi nhưng KHÔNG đồng đều
> User nêu 05/08.

**BE: có sẵn** — service ném `ArgumentException` → 400, cộng check constraint ở DB. Không lo.

**FE: mới phủ một nửa.** Có **12 schema zod** (login, đổi mật khẩu, wizard đề cương, 8 màn CRUD của Admin, hợp đồng) / 17 file dùng `useForm`. Nhưng **phần lớn dialog viết tay bằng `useState`, không validate gì** — đánh giá báo cáo tiến độ, xác nhận giải ngân, nộp sản phẩm, lịch báo cáo… Người dùng bấm xong mới ăn 400 từ BE, lỗi hiện bằng **toast chung** chứ không chỉ vào ô sai.

**Việc cần:** rà toàn bộ form, quy ước: form ≥3 trường **hoặc** có ràng buộc số/ngày ⇒ dùng `zod` + `react-hook-form`, **lỗi hiện ngay dưới ô**. Form 1–2 trường thì giữ nguyên cho nhẹ.
**Ưu tiên 🟡 trung bình** — không chặn luồng, nhưng là thứ thầy nhìn thấy ngay khi bấm thử.

### Q4. Google Meet + Google Calendar — thực ra là **MỘT** việc
> User nêu 05/08. Cậu đoán đúng: hai cái này gắn với nhau.

**Không có "API tạo link Meet" riêng để gọi cho nhanh.** Cách chuẩn là gọi **Google Calendar API** tạo sự kiện kèm `conferenceData.createRequest` → Google trả về link Meet. Nên làm Calendar = được luôn Meet, không phải 2 đầu việc.

**Phải đăng ký (khác Gemini, không chỉ 1 API key):**
1. Google Cloud project → bật **Google Calendar API**.
2. Tạo **OAuth 2.0 Client ID** (loại *Web application*) + cấu hình **OAuth consent screen**.
3. Lưu `ClientId` / `ClientSecret` vào env như các secret khác.

**2 cái bẫy phải biết TRƯỚC khi quyết làm:**
- ⚠️ **Service account KHÔNG tạo được link Meet** với Gmail thường — cần Google Workspace + domain-wide delegation. Dùng gmail cá nhân thì phải đi luồng **OAuth người dùng đồng ý**, rồi **lưu + tự làm mới refresh token**.
- ⚠️ App để ở trạng thái **"Testing"** thì **refresh token hết hạn sau 7 ngày** → demo tuần sau là gãy. Muốn bền phải chuyển "In production"; scope Calendar thuộc nhóm **nhạy cảm** nên có thể phải qua xác minh.

**Khó ở đâu:** lời gọi API thì dễ; **OAuth + lưu/refresh token + xử lý hết hạn** mới là phần việc thật. Ước lượng 1–2 ngày nếu suôn, và **có rủi ro tắc ở khâu consent screen**.

**Khuyến nghị:** để **nice-to-have**, làm sau cùng. Hiện Staff dán link thủ công vẫn chạy đủ luồng (rule #17). Đổi lấy 1–2 ngày + rủi ro gãy demo thì không đáng, trừ khi đã xong hết việc cốt lõi.

### Q5. Deploy thật (Vercel + Render) — **lưu file là vấn đề lớn nhất**
> User nêu 05/08. Đây KHÔNG phải "có thể gặp", mà là **chắc chắn gặp**.

| # | Vấn đề | Mức |
|---|---|---|
| 1 | ✅ **ĐÃ XỬ 05/08 — chuyển sang Cloudinary.** ~~Render dùng filesystem TẠM, redeploy là mất sạch file.~~ Xem mục bên dưới. | xong |
| 2 | 🔴 **Không đính kèm nhiều file một lần.** Rà FE: **7 ô chọn file, 0 ô có `multiple`** ⇒ PI phải chọn & upload từng file một. | cao |
| 3 | 🟠 **Render free ngủ sau ~15 phút** không ai gọi → request đầu chờ cỡ 1 phút. Demo phải "đánh thức" BE trước khi thầy vào. | cao |
| 4 | 🟡 Secret nhập tay trên Render dashboard: `ConnectionStrings__DefaultConnection`, `JwtSettings__SecretKey`, `GeminiAI__ApiKey`, `EmailSettings__SmtpUsername/SmtpPassword`, `EmailSettings__FrontendUrl`. Đặt **1 lần**, không phải mỗi lần deploy. | ok |
| 5 | 🟡 FE Vercel: đổi env var xong **phải redeploy** (Vite nhúng lúc build, không đọc lúc chạy). | ok |
| 6 | 🟡 CORS: BE mặc định `AllowAnyOrigin` nếu không cấu hình `Cors:AllowedOrigins`. Có cấu hình thì nhớ thêm origin của Vercel. | ok |

### ✅ #1 đã xử — Cloudinary (05/08)

Tách **`IFileStorage`**: `CloudinaryFileStorage` (có cấu hình `Cloudinary:*` thì dùng) · `LocalDiskFileStorage` (không có thì về đĩa local như cũ). `ProposalDocumentService` bỏ **toàn bộ** thao tác đĩa trực tiếp (10 chỗ) → gọi qua lớp này. **Không cần migration.**

**🔒 Quyết định bảo mật — đã kiểm chứng, không phải phỏng đoán:** thử upload rồi tải lại thì **URL Cloudinary tải được mà KHÔNG cần đăng nhập**, kể cả kiểu `authenticated` (chữ ký nằm sẵn trong URL, không hết hạn). Đề cương/hợp đồng là tài liệu mật ⇒
- URL Cloudinary **chỉ tồn tại phía server**, dựng lại từ `StorageBlobName` mỗi lần cần — **không lưu vào `Document.StorageUrl`**, vì cột đó đang chứa URL tải của BE và **đi thẳng ra FE** (suýt rò rỉ).
- Người dùng luôn tải qua `/documents/{id}/download` để BE còn `[Authorize]` + kiểm chủ sở hữu.
- Tên file là GUID nên không đoán được.

**2 quirk .NET mất thời gian nhất** (ghi lại để khỏi mò lại): `MultipartFormDataContent` mặc định (a) gắn `Content-Type: text/plain; charset=utf-8` cho từng field và (b) ghi `name=api_key` **không có dấu nháy** ⇒ Cloudinary bỏ qua hết field, báo *"Upload preset must be specified when using unsigned upload"* (nghe như thiếu preset, thực ra là **không đọc được api_key**). Phải gỡ `ContentType` và tự thêm nháy quanh tên field.

**Đã test thật qua BE:** upload 200 → tải có token 200 & **khớp từng byte** (104 903) → tải **không token 401** → file **nằm thật trên Cloudinary** → **AI đọc file từ Cloudinary** để đối chiếu vẫn chạy (6 điểm lệch).

**Cấu hình:** local ở `appsettings.Development.json`; Render đặt env `Cloudinary__CloudName`, `Cloudinary__ApiKey`, `Cloudinary__ApiSecret`, `Cloudinary__Folder`. **Bỏ mục này đi là tự động quay về đĩa local**, không gãy.

**Còn lại:** #2 (cho chọn nhiều file) → #3 (đánh thức BE trước khi demo).

### Q2. AI cho từng role — hiện đang LỆCH so với nhu cầu thật
> Rà 04/08 khi user hỏi *"đặt mình vào từng role xem họ cần gì"*.

**✅ ĐÃ XỬ LÝ 04/08** — xem "Bước 3" ở trên. Hiện trạng sau khi sửa:

| Role | Tính năng AI |
|---|---|
| **PI** | Trích xuất từ file (Đường B) · **Đối chiếu form ↔ file** · Tóm tắt · Góp ý |
| **Reviewer** | **Tóm tắt** (mới đưa sang) · Gợi ý điểm từng tiêu chí |
| **Staff/Admin** | *cố ý không có* — cần đối sánh/lọc, không cần sinh chữ |

⚠️ **CHƯA kiểm chứng bằng dữ liệu thật:** build + 100/100 test chỉ chứng minh code biên dịch & logic đơn vị đúng. Chưa biết Gemini trả JSON đúng khuôn không, chất lượng góp ý/đối chiếu ra sao, prompt tiếng Việt ổn không. **Phải chạy app đi luồng thật mới kết luận được.**

> **Chi phí:** Gemini **free tier là đủ** — AI ở đây gọi theo yêu cầu, không chạy nền. Không cần bật billing. Rủi ro duy nhất là 429 khi demo dồn ⇒ đã có cache `llm_outputs` giảm gọi lại.

---

## ✚ Làm thêm ngoài kế hoạch (phát sinh khi rà / user yêu cầu)
| Việc | Vì sao | Chỗ chính |
|---|---|---|
| **Merge nhánh UI redesign** của bạn (`dev01`) vào `dev` | 2 nhánh song song, 70 file UI vs 8 file logic **không đè nhau** → 0 conflict | FE `4e69f50` |
| **Fix lag UI** sau merge | aurora orb dùng `motion.div` animate **vô hạn** với blur 64px → repaint liên tục. Đổi sang `div` tĩnh + `will-change`/`contain: paint` | FE `49a7a72`, `AppLayout/AuthLayout`, `index.css` |
| **PI xem được lịch họp** hội đồng chấm đề tài mình | Process_Spec: PI **trình bày trước hội đồng** rồi rời phòng khi họp kín → phải biết giờ + địa điểm/link. Trước đó chỉ Staff/Reviewer thấy | BE `GET /api/meetings/my`; FE `MyMeetingsPage` |
| **Bảng tiến độ theo hoạt động (BM06)** | Mẫu BM06 có bảng %/hoạt động, form cũ thiếu hẳn | BE `ProgressReportService.UpdateAsync` (thay `items`); FE `CreateProgressReportSheet` |
| **Khoá cửa sau `SCREENING`** ở BE | FE đã bỏ nhưng BE vẫn cho tạo → dữ liệu rác. Nay chỉ nhận `REVIEW`/`ACCEPTANCE` (rule #16) | BE `ReviewBoardService` + `ReviewRoundService` |
| **Bỏ hẳn repo FE cũ đuôi `v0`** | User chốt: không tham chiếu nữa | `7a52239`, docs + `.claude/skills/fe-e2e` |

## Việc dang dở từ trước (giữ trong backlog — `SYSTEM_REVIEW.md`)
- 🔴 **Rà IDOR endpoint con** proposal (`/documents`, `/budget`, `/team-members`) — cùng họ lỗi đã fix ở `GET /proposals/{id}`.
- 🟠 **Auto sản phẩm từ đề cương** (wizard khai sản phẩm dự kiến + tạo HĐ tự gắn) — hiện Staff nhập tay.
- 🟠 **Cho PI sửa báo cáo tiến độ trước khi Staff đánh giá** (hiện nộp xong khóa cứng — không nhất quán với sản phẩm/tổng kết).
- 🟠 **Đa vai role-aware** toàn cục (mới ép đúng `/proposals/my` + `/contracts?mine=true`).
- 🟠 **Báo cáo tiến độ ↔ giải ngân** (Đạt → mở đợt sau) — nay P5 đụng tới, gộp làm chung.
- 🟡 Rich text (thuyết minh/biên bản) · reschedule sau gửi mời · seed dữ liệu thật (bỏ "12") · vòng đóng thủ công thay vì tự đóng.

## Trạng thái tổng (cập nhật 05/08)
✅ **P0 · P1 · P2 · P3 · P4 · P5 · P6 · P7** — xong.
🟡 **P8 (AI)** — xong bước 1–3 (Đường B, đối chiếu form↔file, góp ý, tóm tắt cho reviewer, gợi ý chấm điểm). Còn `/ai/search` (**chờ user chốt — §Q1**), `suggest-reviewers`, xoá `similarity-check`.
→ **18/18 ý thầy góp ý đã có phần hiện thực** (P8 còn phần đuôi).

**Verify:** BE **100/100 test xanh** · FE `tsc` 0 lỗi · build production OK · i18n **vi=en=1367**.

## ✅ E2E thật (05/08) — đã chạy app + Chrome + gọi API thật

**Login 4 role (Admin/Staff/PI/Reviewer): sạch** — không console error, không API ≥400.

**4 tính năng AI đã gọi thật với dữ liệu thật, chất lượng dùng được:**
| Tính năng | Kết quả | Thời gian |
|---|---|---|
| Đối chiếu form ↔ file | ✅ bắt trúng 5 điểm lệch (form nói phát hiện gian lận, file lại là SRS của FURPMS) | ~19s |
| Góp ý đề cương | ✅ 4 ý đúng trọng tâm ("kinh phí 0 VND chưa hợp lý", "tên đề tài cần đặt lại") | ~10s |
| Tóm tắt | ✅ tiếng Việt trôi chảy; lần sau mở màn đọc **cache**, không tốn quota | ~10s |
| Gợi ý chấm điểm | ✅ đủ 5 tiêu chí, điểm hợp lý + lý do, không vượt thang | ~21s |

> ⏱ **10–21 giây mỗi lần gọi** — chậm. Demo nên **bấm trước** cho vào cache, đừng bấm live trước mặt thầy.

### 🔴 Lỗi tìm được khi test thật (đã sửa)
**Gemini KHÔNG nhận file `.docx`** — trả 400 `Unsupported MIME type`. Mà .docx là định dạng phổ biến nhất của đề cương ⇒ "đối chiếu form↔file" **hỏng với đúng loại file hay dùng nhất**. `ProposalExtractionService` vốn đã xử lý đúng (PDF gửi thẳng, .docx bóc text bằng OpenXml rồi gửi) nhưng service AI mới lại gửi thô. → Gom về **`GeminiFileInput.AskAboutFileAsync`** dùng chung cho cả hai.

### ⚠️ Chặn demo — thiếu DỮ LIỆU, không phải lỗi code
1. **Chưa có bộ tiêu chí loại `ACCEPTANCE`** ⇒ mọi hội đồng **Nghiệm thu** hiện ra *"Chưa cấu hình tiêu chí chấm"*, **không chấm được**, và nút AI gợi ý điểm cũng không hiện (nằm sau nhánh có tiêu chí). → Admin vào **Tiêu chí chấm → Bộ tiêu chí** tạo 1 bộ `ACCEPTANCE` (hoặc sao chép bộ Xét duyệt rồi đổi loại).
2. **Vòng REVIEW đang `PENDING`** ⇒ màn chấm hiện *"Vòng chưa mở"*. Staff phải **mở vòng** thì reviewer mới chấm được.
3. **File upload nằm ở `FURPMS.API/App_Data/uploads` THEO THƯ MỤC CHẠY BE.** Chuyển từ repo cũ sang `FURPMS_BEv2` mà không chép `App_Data` sang thì **mọi tài liệu đã upload đều 404** (DB dùng chung container nên vẫn trỏ tới file cũ). Đã chép 9 file sang BEv2.

## Thứ tự đề xuất
**P0 (lỗi nghiệm thu + nối mạch)** → **P1 (upload PDF + Staff xem file mới chấm)** → **P2 (số đợt linh hoạt + gia hạn)** → **P3 (nhắc hạn)** → **P6 (dashboard PI)** → **P7 (chuẩn hoá ngôn ngữ)** → **P4 (rubric group)** → **P5 (giải ngân ↔ sản phẩm)** → **P8 (AI)**.

> Lý do: P0/P1 chặn demo & thầy nhắc trực tiếp; P2/P3 nhỏ mà thấy ngay; P4/P5 đụng schema (migration) nên làm khi đã ổn định; P8 phụ thuộc hạ tầng AI.
