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

## Trạng thái tổng (cập nhật 04/08)
✅ **P0 · P1 · P2 · P3 · P4 · P5 · P6 · P7** — xong, BE **100/100 test xanh**, FE `tsc` 0 lỗi, build production OK, i18n vi=en=1341.
⬜ **P8** (AI: hoàn thiện flow + gợi ý chấm điểm) — user chủ động hoãn.
→ **≈17/18 ý thầy góp ý (94%)**.

## Thứ tự đề xuất
**P0 (lỗi nghiệm thu + nối mạch)** → **P1 (upload PDF + Staff xem file mới chấm)** → **P2 (số đợt linh hoạt + gia hạn)** → **P3 (nhắc hạn)** → **P6 (dashboard PI)** → **P7 (chuẩn hoá ngôn ngữ)** → **P4 (rubric group)** → **P5 (giải ngân ↔ sản phẩm)** → **P8 (AI)**.

> Lý do: P0/P1 chặn demo & thầy nhắc trực tiếp; P2/P3 nhỏ mà thấy ngay; P4/P5 đụng schema (migration) nên làm khi đã ổn định; P8 phụ thuộc hạ tầng AI.
